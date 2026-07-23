using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Budgets;
using Amazon.Budgets.Model;
using System.Linq;
using Amazon.Runtime;
using Amazon;
using Microsoft.Extensions.Configuration;

namespace BLL.Service
{
    public class BudgetService(IUnitOfWork _uow, IMapper _mapper, IConfiguration _config) : IBudgetService
    {
        public async Task<IEnumerable<BudgetDto>> GetAllAsync()
        {
            var budgets = await _uow.BudgetRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<BudgetDto>>(budgets);
        }

        public async Task<IEnumerable<BudgetDto>> GetByUserIdAsync(string userId)
        {
            var budgets = await _uow.BudgetRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<BudgetDto>>(budgets);
        }

        public async Task<BudgetDto> GetByIdAsync(int id)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(id);
            return _mapper.Map<BudgetDto>(budget);
        }

        public async Task<BudgetDto> CreateAsync(string userId, BudgetDto budgetDto)
        {
            ArgumentNullException.ThrowIfNull(budgetDto);
            if (budgetDto.Amount < 0) throw new ArgumentException("Budget amount cannot be negative");
            if (budgetDto.EndDate.HasValue && budgetDto.EndDate.Value <= budgetDto.StartDate)
                throw new ArgumentException("EndDate must be after StartDate");

            var userBudgets = await _uow.BudgetRepository.GetByUserIdAsync(userId);
            if (userBudgets.Any(b => string.Equals(b.Name, budgetDto.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("A budget with this name already exists.");
            }

            if (budgetDto.IsDefault)
            {
                var oldDefaults = userBudgets.Where(b => b.IsDefault).ToList();
                foreach (var old in oldDefaults)
                {
                    old.IsDefault = false;
                    _uow.BudgetRepository.Update(old);
                }
            }
            var entity = _mapper.Map<DAL.Entities.Budget>(budgetDto);

            entity.UserId = userId;

            // Tính tổng tiền từ các IncomeSource
            var total = budgetDto.BudgetIncomeSources.Sum(x => x.Amount);

            entity.Amount = total;
            entity.CurrentAmount = total;

            // Lưu Budget trước để có Id
            await _uow.BudgetRepository.AddAsync(entity);
            await _uow.Complete();

            // Lưu BudgetIncomeSource
            foreach (var item in budgetDto.BudgetIncomeSources)
            {
                await _uow.BudgetIncomeSourceRepository.AddAsync(new DAL.Entities.BudgetIncomeSource
                {
                    BudgetId = entity.Id,
                    IncomeSourceId = item.IncomeSourceId,
                    Amount = item.Amount
                });
            }

            await _uow.Complete();

            return _mapper.Map<BudgetDto>(entity);
        }

        public async Task<BudgetDto> UpdateAsync(int id, BudgetDto budgetDto)
        {
            ArgumentNullException.ThrowIfNull(budgetDto);
            if (budgetDto.Amount < 0) throw new ArgumentException("Budget amount cannot be negative");
            if (budgetDto.EndDate.HasValue && budgetDto.EndDate.Value <= budgetDto.StartDate)
                throw new ArgumentException("EndDate must be after StartDate");

            var existingEntity = await _uow.BudgetRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Budget not found");
            }

            if (budgetDto.IsDefault)
            {
                var userBudgets = await _uow.BudgetRepository.GetByUserIdAsync(existingEntity.UserId);
                var oldDefaults = userBudgets.Where(b => b.IsDefault && b.Id != id).ToList();
                foreach (var old in oldDefaults)
                {
                    old.IsDefault = false;
                    _uow.BudgetRepository.Update(old);
                }
            }

            _mapper.Map(budgetDto, existingEntity);
            _uow.BudgetRepository.Update(existingEntity);
            await _uow.Complete();
            return _mapper.Map<BudgetDto>(existingEntity);
        }

        public async Task<BudgetDto> DeleteAsync(int id)
        {
            var existingEntity = await _uow.BudgetRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Budget not found");
            }
            existingEntity.IsActive = false;
            _uow.BudgetRepository.Update(existingEntity);
            await _uow.Complete();
            return _mapper.Map<BudgetDto>(existingEntity);
        }

        public async Task<int> DeductMoneyAsync(string userId, decimal amount, int? budgetId = null)
        {
            DAL.Entities.Budget targetBudget = null;

            if (budgetId.HasValue)
            {
                // Chọn thủ công: Lấy đúng ví truyền vào
                targetBudget = await _uow.BudgetRepository.GetByIdAsync(budgetId.Value);
                if (targetBudget == null || targetBudget.UserId != userId || !targetBudget.IsActive)
                    throw new Exception("Ví ngân sách không tồn tại hoặc đã bị khóa.");
            }
            else
            {
                // Tự động (Scan hóa đơn): Tìm ví đang được set IsDefault = true
                var userBudgets = await _uow.BudgetRepository.GetByUserIdAsync(userId);
                targetBudget = userBudgets.FirstOrDefault(b => b.IsDefault && b.IsActive);

                if (targetBudget == null)
                    throw new Exception("Không tìm thấy ví mặc định để tự động trừ tiền. Vui lòng thiết lập ví mặc định.");
            }

            // 1. Trừ tiền số dư khả dụng
            targetBudget.CurrentAmount -= amount;
            _uow.BudgetRepository.Update(targetBudget);

            return targetBudget.Id;

        }

        public async Task<IEnumerable<TransactionDto>> GetBudgetHistoryAsync(string userId, int budgetId)
        {
            var history = await _uow.TransactionRepository.FindAsync(
            t => t.UserId == userId && t.BudgetId == budgetId && !t.IsDeleted
            );

            var sortedHistory = history.OrderByDescending(t => t.TransactionDate).ToList();

            return _mapper.Map<IEnumerable<TransactionDto>>(sortedHistory);
        }

        public async Task ProcessPeriodicRolloverAsync()
        {
            var now = DateTime.UtcNow.AddHours(7);

            // Lấy tất cả ví lên để lọc
            var allBudgets = await _uow.BudgetRepository.GetAllAsync();

            // Tìm các ví Periodic hết hạn
            var expiredBudgets = allBudgets.Where(b =>
                b.IsActive &&
                b.Type == DAL.Enums.BudgetType.Periodic &&
                b.EndDate.HasValue &&
                b.EndDate.Value.Date <= now.Date).ToList();

            if (!expiredBudgets.Any()) return;

            foreach (var oldBudget in expiredBudgets)
            {
                // Khóa ví cũ
                oldBudget.IsActive = false;
                _uow.BudgetRepository.Update(oldBudget);

                if (!oldBudget.IsAutoRenew)
                {
                    continue;
                }

                // Tạo ví mới lưu setting ví cũ
                var newBudget = new DAL.Entities.Budget
                {
                    UserId = oldBudget.UserId,
                    Name = oldBudget.Name,
                    Type = DAL.Enums.BudgetType.Periodic,
                    Amount = oldBudget.Amount,
                    CurrentAmount = oldBudget.Amount, // Reset lại bằng hạn mức gốc
                    IsActive = true,
                    StartDate = now,
                    EndDate = now.AddMonths(1),
                    PreviousBudgetId = oldBudget.Id
                };

                await _uow.BudgetRepository.AddAsync(newBudget);

                await _uow.Complete();

                // Tìm các liên kết BudgetIncomeSource của ví cũ
                var allBudgetIncomeSources = await _uow.BudgetIncomeSourceRepository.GetAllAsync();
                var oldBudgetIncomeSources = allBudgetIncomeSources.Where(bis => bis.BudgetId == oldBudget.Id).ToList();

                var allIncomeSources = await _uow.IncomeSourceRepository.GetAllAsync();
                
                // Lọc ra các nguồn thu thuộc về user này, đang active và LÀ ĐỊNH KỲ
                var recurringSources = oldBudgetIncomeSources
                    .Select(bis => new { 
                        IncomeSource = allIncomeSources.FirstOrDefault(s => s.Id == bis.IncomeSourceId),
                        BisAmount = bis.Amount 
                    })
                    .Where(x => x.IncomeSource != null && 
                                x.IncomeSource.UserId == oldBudget.UserId && 
                                x.IncomeSource.IsActive == true && 
                                x.IncomeSource.IsRecurring == true)
                    .ToList();

                if (recurringSources.Any())
                {
                    foreach (var item in recurringSources)
                    {
                        var source = item.IncomeSource;
                        
                        // 1. Tạo liên kết BudgetIncomeSource cho ví mới
                        var newBudgetIncomeSource = new DAL.Entities.BudgetIncomeSource
                        {
                            BudgetId = newBudget.Id,
                            IncomeSourceId = source.Id,
                            Amount = item.BisAmount // Giữ nguyên số tiền setup cho ví
                        };
                        await _uow.BudgetIncomeSourceRepository.AddAsync(newBudgetIncomeSource);

                        // 2. Tạo lịch sử nhận tiền (IncomeHistory) cho ví mới
                        var newHistory = new DAL.Entities.IncomeHistory
                        {
                            BudgetId = newBudget.Id, // Gắn vào ID của ví mới vừa tạo
                            IncomeSourceId = source.Id,
                            Amount = item.BisAmount, // Lấy đúng số tiền từ liên kết
                            ReceivedDate = now,
                            Note = $"Thu nhập định kỳ tự động cộng từ: {source.Name}"
                        };
                        await _uow.IncomeHistoryRepository.AddAsync(newHistory);

                        // 3. Bơm tiền thực tế vào CurrentAmount của ví mới
                        newBudget.CurrentAmount += item.BisAmount;
                    }
                    
                    // 4. Update lại cái ví mới vì CurrentAmount đã thay đổi
                    _uow.BudgetRepository.Update(newBudget);
                    await _uow.Complete(); // Lưu toàn bộ (BudgetIncomeSource + IncomeHistory + cập nhật Budget) xuống DB
                }
            }
        }
    }
}

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

        public async Task<IEnumerable<BudgetDto>> GetAllAccessibleBudgetsAsync(string userId)
        {
            var ownedBudgets = (await _uow.BudgetRepository.GetByUserIdAsync(userId)).Where(b => b.IsActive);

            var budgetMembers = await _uow.BudgetMemberRepository.FindAsync(m => m.MemberId == userId && m.Status == DAL.Enums.InvitationStatus.Accepted);
            var sharedBudgetIds = budgetMembers.Select(m => m.BudgetId).Distinct().ToList();

            var sharedBudgets = new List<DAL.Entities.Budget>();
            foreach (var id in sharedBudgetIds)
            {
                var budget = await _uow.BudgetRepository.GetByIdAsync(id);
                if (budget != null && budget.IsActive)
                {
                    sharedBudgets.Add(budget);
                }
            }

            var allBudgets = ownedBudgets.Union(sharedBudgets).DistinctBy(b => b.Id).ToList();
            return _mapper.Map<IEnumerable<BudgetDto>>(allBudgets);
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

            // QUAN TRỌNG: AutoMapper tự động map mảng BudgetIncomeSources từ DTO sang Entity.
            // Khi gọi _uow.BudgetRepository.AddAsync(entity), EF Core sẽ tự động insert các phần tử trong mảng này.
            // Do bên dưới chúng ta tự lặp mảng (để lọc data rác và lưu thêm IncomeHistory), 
            // nên cần Clear mảng này đi để tránh bị EF Core insert trùng lặp (lỗi nhân đôi).
            entity.BudgetIncomeSources?.Clear();

            entity.UserId = userId;

            // Tính tổng tiền từ các nguồn thu được chọn
            decimal totalFromSources = budgetDto.BudgetIncomeSources
                .Where(x => x.IncomeSourceId > 0)
                .Sum(x => x.Amount);

            // Ưu tiên lấy số tiền do người dùng nhập (nếu lớn hơn tổng nguồn thu).
            // Đề phòng FE truyền nhầm vào CurrentAmount thay vì Amount, ta lấy số lớn nhất trong cả 2.
            decimal userProvidedAmount = Math.Max(budgetDto.Amount, budgetDto.CurrentAmount);
            decimal finalAmount = userProvidedAmount > totalFromSources ? userProvidedAmount : totalFromSources;
            
            entity.Amount = finalAmount;
            entity.CurrentAmount = finalAmount;

            // Lưu Budget trước để có Id
            await _uow.BudgetRepository.AddAsync(entity);
            await _uow.Complete();

            // Lưu BudgetIncomeSource và IncomeHistory
            foreach (var item in budgetDto.BudgetIncomeSources)
            {
                // Bỏ qua các object rỗng (incomeSourceId = 0) do FE gửi thừa
                if (item.IncomeSourceId <= 0) continue; 

                await _uow.BudgetIncomeSourceRepository.AddAsync(new DAL.Entities.BudgetIncomeSource
                {
                    BudgetId = entity.Id,
                    IncomeSourceId = item.IncomeSourceId,
                    Amount = item.Amount
                });

                await _uow.IncomeHistoryRepository.AddAsync(new DAL.Entities.IncomeHistory
                {
                    BudgetId = entity.Id,
                    IncomeSourceId = item.IncomeSourceId,
                    Amount = item.Amount,
                    ReceivedDate = DateTime.UtcNow.AddHours(7),
                    Note = "Nạp từ nguồn thu khi tạo ví"
                });

            }

            // Phần tiền còn lại (nếu có) sẽ được ghi nhận là nạp thủ công
            decimal manualAmount = finalAmount - totalFromSources;
            if (manualAmount > 0)
            {
                await _uow.IncomeHistoryRepository.AddAsync(new DAL.Entities.IncomeHistory
                {
                    BudgetId = entity.Id,
                    IncomeSourceId = null, // null = Tự nhập thủ công
                    Amount = manualAmount,
                    ReceivedDate = DateTime.UtcNow.AddHours(7),
                    Note = "Nạp tiền thủ công"
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
            if (existingEntity.IsDefault)
            {
                throw new InvalidOperationException("Không thể xóa ví đang được đặt làm mặc định.");
            }
            existingEntity.IsActive = false;
            _uow.BudgetRepository.Update(existingEntity);
            await _uow.Complete();
            return _mapper.Map<BudgetDto>(existingEntity);
        }

        public async Task<BudgetDto> DepositAsync(string userId, int budgetId, DepositBudgetDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            if (budget == null || !budget.IsActive)
                throw new KeyNotFoundException("Ví ngân sách không tồn tại hoặc đã bị khóa.");

            // Kiểm tra quyền
            if (budget.UserId != userId)
            {
                var members = await _uow.BudgetMemberRepository.FindAsync(m => m.BudgetId == budgetId && m.MemberId == userId);
                var member = members.FirstOrDefault();
                if (member == null || member.Status != DAL.Enums.InvitationStatus.Accepted || member.Role != DAL.Enums.BudgetRole.Editor)
                {
                    throw new Exception("Bạn không có quyền nạp tiền vào ví ngân sách này.");
                }
            }

            using var dbTransaction = await _uow.BeginTransactionAsync();
            try
            {
                // Cập nhật số tiền
                budget.Amount += dto.Amount;
                budget.CurrentAmount += dto.Amount;
                _uow.BudgetRepository.Update(budget);

                // Lưu lịch sử
                var history = new DAL.Entities.IncomeHistory
                {
                    BudgetId = budgetId,
                    IncomeSourceId = dto.IncomeSourceId > 0 ? dto.IncomeSourceId : null,
                    Amount = dto.Amount,
                    ReceivedDate = DateTime.UtcNow.AddHours(7),
                    Note = !string.IsNullOrWhiteSpace(dto.Note) ? dto.Note : (dto.IncomeSourceId > 0 ? "Nạp từ nguồn thu" : "Nạp tiền thủ công")
                };
                
                await _uow.IncomeHistoryRepository.AddAsync(history);
                
                await _uow.Complete();
                await dbTransaction.CommitAsync();

                return _mapper.Map<BudgetDto>(budget);
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<int> DeductMoneyAsync(string userId, decimal amount, int? budgetId = null, bool isExpense = true)
        {
            DAL.Entities.Budget targetBudget = null;

            if (budgetId.HasValue)
            {
                // Chọn thủ công: Lấy đúng ví truyền vào
                targetBudget = await _uow.BudgetRepository.GetByIdAsync(budgetId.Value);
                if (targetBudget == null || !targetBudget.IsActive)
                    throw new Exception("Ví ngân sách không tồn tại hoặc đã bị khóa.");

                bool isOwner = targetBudget.UserId == userId;
                bool isAuthorizedMember = false;

                if (!isOwner)
                {
                    var members = await _uow.BudgetMemberRepository.FindAsync(m => m.BudgetId == budgetId.Value && m.MemberId == userId);
                    var member = members.FirstOrDefault();
                        
                    if (member != null && member.Role == DAL.Enums.BudgetRole.Editor && member.Status == DAL.Enums.InvitationStatus.Accepted)
                    {
                        isAuthorizedMember = true;
                    }
                }

                if (!isOwner && !isAuthorizedMember)
                {
                    throw new Exception("Bạn không có quyền sử dụng ví ngân sách này.");
                }
            }
            else
            {
                // Tự động (Scan hóa đơn): Tìm ví đang được set IsDefault = true
                var userBudgets = await _uow.BudgetRepository.GetByUserIdAsync(userId);
                targetBudget = userBudgets.FirstOrDefault(b => b.IsDefault && b.IsActive);

                if (targetBudget == null)
                    throw new Exception("Không tìm thấy ví mặc định để tự động trừ tiền. Vui lòng thiết lập ví mặc định.");
            }

            // 1. Cập nhật số dư khả dụng (Trừ tiền nếu IsExpense = true, Cộng tiền nếu IsExpense = false)
            if (isExpense)
            {
                targetBudget.CurrentAmount -= amount;
            }
            else
            {
                targetBudget.CurrentAmount += amount;
            }
            _uow.BudgetRepository.Update(targetBudget);

            return targetBudget.Id;

        }

        public async Task<IEnumerable<TransactionDto>> GetBudgetHistoryAsync(string userId, int budgetId)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            if (budget == null)
            {
                throw new Exception("Ví ngân sách không tồn tại.");
            }

            bool isOwner = budget.UserId == userId;
            bool isMember = false;

            if (!isOwner)
            {
                var members = await _uow.BudgetMemberRepository.FindAsync(m => m.BudgetId == budgetId && m.MemberId == userId);
                var member = members.FirstOrDefault();
                if (member != null && member.Status == DAL.Enums.InvitationStatus.Accepted)
                {
                    isMember = true;
                }
            }

            if (!isOwner && !isMember)
            {
                throw new Exception("Bạn không có quyền xem lịch sử giao dịch của ví ngân sách này.");
            }

            var history = await _uow.TransactionRepository.FindAsync(
                t => t.BudgetId == budgetId && !t.IsDeleted
            );

            var sortedHistory = history.OrderByDescending(t => t.TransactionDate).ToList();

            return _mapper.Map<IEnumerable<TransactionDto>>(sortedHistory);
        }

        public async Task<IEnumerable<IncomeHistoryDto>> GetIncomeHistoryAsync(string userId, int budgetId)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            if (budget == null)
            {
                throw new Exception("Ví ngân sách không tồn tại.");
            }

            bool isOwner = budget.UserId == userId;
            bool isMember = false;

            if (!isOwner)
            {
                var members = await _uow.BudgetMemberRepository.FindAsync(m => m.BudgetId == budgetId && m.MemberId == userId);
                var member = members.FirstOrDefault();
                if (member != null && member.Status == DAL.Enums.InvitationStatus.Accepted)
                {
                    isMember = true;
                }
            }

            if (!isOwner && !isMember)
            {
                throw new Exception("Bạn không có quyền xem lịch sử nạp tiền của ví ngân sách này.");
            }

            var history = await _uow.IncomeHistoryRepository.FindAsync(h => h.BudgetId == budgetId);
            
            var incomeSourceIds = history.Where(h => h.IncomeSourceId.HasValue).Select(h => h.IncomeSourceId.Value).Distinct().ToList();
            var incomeSources = await _uow.IncomeSourceRepository.FindAsync(s => incomeSourceIds.Contains(s.Id));
            var sourceDict = incomeSources.ToDictionary(s => s.Id, s => s.Name);
            
            var result = history.OrderByDescending(h => h.ReceivedDate).Select(h => new IncomeHistoryDto
            {
                Id = h.Id,
                BudgetId = h.BudgetId,
                IncomeSourceId = h.IncomeSourceId,
                IncomeSourceName = h.IncomeSourceId.HasValue && sourceDict.ContainsKey(h.IncomeSourceId.Value) 
                    ? sourceDict[h.IncomeSourceId.Value] 
                    : null,
                Amount = h.Amount,
                ReceivedDate = h.ReceivedDate,
                Note = h.Note
            }).ToList();

            return result;
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
                        IncomeSource = allIncomeSources.FirstOrDefault(s => s.Id == bis.IncomeSourceId)
                    })
                    .Where(x => x.IncomeSource != null && 
                                x.IncomeSource.UserId == oldBudget.UserId && 
                                x.IncomeSource.IsActive == true && 
                                x.IncomeSource.IsRecurring == true)
                    .ToList();

                if (recurringSources.Any())
                {
                    // CẢI TIẾN: Tránh lỗi nhân đôi số dư (Double Funding)
                    // Nếu ví có nguồn thu tự động bơm tiền vào, ta cần reset CurrentAmount về 0 trước, 
                    // nếu không nó sẽ bị cộng dồn với oldBudget.Amount đã gán lúc khởi tạo newBudget.
                    newBudget.CurrentAmount = 0;
                    newBudget.Amount = 0; // Reset luôn hạn mức để cộng lại từ đầu

                    foreach (var item in recurringSources)
                    {
                        var source = item.IncomeSource;
                        // SỬA LỖI theo đúng ý bạn: Lấy Amount mới nhất trực tiếp từ IncomeSource
                        var currentIncomeAmount = source.Amount; 
                        
                        // 1. Tạo liên kết BudgetIncomeSource cho ví mới
                        var newBudgetIncomeSource = new DAL.Entities.BudgetIncomeSource
                        {
                            BudgetId = newBudget.Id,
                            IncomeSourceId = source.Id,
                            Amount = currentIncomeAmount // Lưu snapshot giá trị mới nhất
                        };
                        await _uow.BudgetIncomeSourceRepository.AddAsync(newBudgetIncomeSource);

                        // 2. Tạo lịch sử nhận tiền (IncomeHistory) cho ví mới
                        var newHistory = new DAL.Entities.IncomeHistory
                        {
                            BudgetId = newBudget.Id, // Gắn vào ID của ví mới vừa tạo
                            IncomeSourceId = source.Id,
                            Amount = currentIncomeAmount, // Nạp bằng đúng số tiền MỚI NHẤT
                            ReceivedDate = now,
                            Note = $"Thu nhập định kỳ tự động cộng từ: {source.Name}"
                        };
                        await _uow.IncomeHistoryRepository.AddAsync(newHistory);

                        // 3. Bơm tiền thực tế vào CurrentAmount và Hạn mức Amount của ví mới
                        newBudget.CurrentAmount += currentIncomeAmount;
                        newBudget.Amount += currentIncomeAmount;
                    }
                    
                    // 4. Update lại cái ví mới vì CurrentAmount và Amount đã thay đổi
                    _uow.BudgetRepository.Update(newBudget);
                    await _uow.Complete(); // Lưu toàn bộ xuống DB
                }
            }
        }
    }
}

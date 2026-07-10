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
            entity.CurrentAmount = budgetDto.Amount;
            entity.UserId = userId;
            await _uow.BudgetRepository.AddAsync(entity);
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
            _uow.BudgetRepository.Delete(existingEntity);
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
    }
}

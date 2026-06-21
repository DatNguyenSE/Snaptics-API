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
        private readonly string _awsAccountId = "193619625738";
        public async Task<IEnumerable<BudgetDto>> GetAllAsync()
        {
            var budgets = await _uow.BudgetRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<BudgetDto>>(budgets);
        }

        public async Task<BudgetDto> GetByIdAsync(int id)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(id);
            return _mapper.Map<BudgetDto>(budget);
        }

        public async Task<BudgetDto> CreateAsync(BudgetDto budgetDto)
        {
            if (budgetDto.Amount < 0) throw new ArgumentException("Budget amount cannot be negative");
            if (budgetDto.EndDate.HasValue && budgetDto.EndDate.Value <= budgetDto.StartDate)
                throw new ArgumentException("EndDate must be after StartDate");

            var entity = _mapper.Map<DAL.Entities.Budget>(budgetDto);
            await _uow.BudgetRepository.AddAsync(entity);
            await _uow.Complete();
            return _mapper.Map<BudgetDto>(entity);
        }

        public async Task<BudgetDto> UpdateAsync(int id, BudgetDto budgetDto)
        {
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

        public async Task<IEnumerable<BudgetDto>> GetAwsBudgetsAsync()
        {
            // 1. Tự tay móc chìa khóa từ appsettings.json
            var accessKey = _config["AWS:AccessKey"];
            var secretKey = _config["AWS:SecretKey"];

            // Check nhẹ coi nó đọc được json chưa
            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("Lỗi: Không tìm thấy AccessKey/SecretKey trong appsettings.json!");
            }

            // 2. Ép chìa khóa vào credentials
            var credentials = new BasicAWSCredentials(accessKey, secretKey);

            // 3. Khởi tạo Client bằng tay (nhớ đổi RegionEndpoint.USEast1 nếu xài vùng khác)
            using var client = new AmazonBudgetsClient(credentials, RegionEndpoint.USEast1);

            var request = new DescribeBudgetsRequest
            {
                AccountId = _awsAccountId
            };

            // 4. Gọi API của Amazon thông qua cái client tự chế
            var response = await client.DescribeBudgetsAsync(request);

            // 5. Map data trả về DTO
            return response.Budgets.Select(b => new BudgetDto
            {
                Id = 0, 
                UserId = "user-12345-mock-id", 
                Amount = b.BudgetLimit?.Amount ?? 0m, 
                StartDate = b.TimePeriod?.Start ?? DateTime.UtcNow, 
                EndDate = b.TimePeriod?.End, 
                IsActive = true, 
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }
    }
}

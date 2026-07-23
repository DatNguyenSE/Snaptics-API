using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using DAL.Entities;

namespace BLL.Service
{
    public class BudgetIncomeSourceService(
        IUnitOfWork _uow,
        IMapper _mapper)
        : IBudgetIncomeSourceService
    {
        public async Task<IEnumerable<BudgetIncomeSourceDto>> GetByBudgetIdAsync(int budgetId)
        {
            var list = await _uow.BudgetIncomeSourceRepository.GetByBudgetIdAsync(budgetId);

            return _mapper.Map<IEnumerable<BudgetIncomeSourceDto>>(list);
        }

        public async Task<BudgetIncomeSourceDto> AddAsync(BudgetIncomeSourceDto dto)
        {
            var entity = _mapper.Map<BudgetIncomeSource>(dto);

            await _uow.BudgetIncomeSourceRepository.AddAsync(entity);

            await _uow.Complete();

            return _mapper.Map<BudgetIncomeSourceDto>(entity);
        }

        public async Task<BudgetIncomeSourceDto> UpdateAsync(int id, BudgetIncomeSourceDto dto)
        {
            var entity = await _uow.BudgetIncomeSourceRepository.GetByIdAsync(id);

            if (entity == null)
                throw new KeyNotFoundException("BudgetIncomeSource not found.");

            entity.Amount = dto.Amount;

            entity.BudgetId = dto.BudgetId;

            entity.IncomeSourceId = dto.IncomeSourceId;

            _uow.BudgetIncomeSourceRepository.Update(entity);

            await _uow.Complete();

            return _mapper.Map<BudgetIncomeSourceDto>(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _uow.BudgetIncomeSourceRepository.GetByIdAsync(id);

            if (entity == null)
                throw new KeyNotFoundException("BudgetIncomeSource not found.");

            _uow.BudgetIncomeSourceRepository.Delete(entity);

            await _uow.Complete();
        }
    }
}
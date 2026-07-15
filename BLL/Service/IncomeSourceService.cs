using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.IRepositories;

namespace BLL.Service
{
    public class IncomeSourceService(IUnitOfWork _uow, IMapper _mapper) : IIncomeSourceService
    {
        public async Task<IEnumerable<IncomeSourceDto>> GetAllAsync()
        {
            var incomes = await _uow.IncomeSourceRepository.GetAllAsync();

            return _mapper.Map<IEnumerable<IncomeSourceDto>>(incomes);
        }

        public async Task<IncomeSourceDto> GetByIdAsync(int id)
        {
            var income = await _uow.IncomeSourceRepository.GetByIdAsync(id);

            return _mapper.Map<IncomeSourceDto>(income);
        }

        public async Task<IEnumerable<IncomeSourceDto>> GetByUserIdAsync(string userId)
        {
            var incomes = await _uow.IncomeSourceRepository.GetByUserIdAsync(userId);

            return _mapper.Map<IEnumerable<IncomeSourceDto>>(incomes);
        }

        public async Task<IncomeSourceDto> CreateAsync(IncomeSourceDto dto)
        {
            var entity = _mapper.Map<DAL.Entities.IncomeSource>(dto);

            await _uow.IncomeSourceRepository.AddAsync(entity);

            await _uow.Complete();

            return _mapper.Map<IncomeSourceDto>(entity);
        }

        public async Task<IncomeSourceDto> UpdateAsync(int id, IncomeSourceDto dto)
        {
            var existingEntity =
                await _uow.IncomeSourceRepository.GetByIdAsync(id);

            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Income Source not found");
            }

            _mapper.Map(dto, existingEntity);

            _uow.IncomeSourceRepository.Update(existingEntity);

            await _uow.Complete();

            return _mapper.Map<IncomeSourceDto>(existingEntity);
        }

        public async Task<IncomeSourceDto> DeleteAsync(int id)
        {
            var existingEntity =
                await _uow.IncomeSourceRepository.GetByIdAsync(id);

            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Income Source not found");
            }

            _uow.IncomeSourceRepository.Delete(existingEntity);

            await _uow.Complete();

            return _mapper.Map<IncomeSourceDto>(existingEntity);
        }
    }
}
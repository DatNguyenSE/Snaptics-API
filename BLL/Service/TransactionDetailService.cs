using BLL.Interfaces.IServices;
using System;
using System.Collections.Generic;
using System.Text;
using BLL.Interfaces.IServices;
using BLL.Dtos;
using DAL.IRepositories;
using AutoMapper;

namespace BLL.Service
{
    public class TransactionDetailService(IUnitOfWork _uow, IMapper mapper) : ITransactionDetailService
    {
        public async Task<IEnumerable<TransactionDetailDto>> GetAllAsync()
        {
            var details = await _uow.TransactionDetailRepository.GetAllAsync();
            return mapper.Map<IEnumerable<TransactionDetailDto>>(details);
        }

        public async Task<TransactionDetailDto> GetByIdAsync(int id)
        {
            var detail = await _uow.TransactionDetailRepository.GetByIdAsync(id);
            return mapper.Map<TransactionDetailDto>(detail);
        }

        public async Task<TransactionDetailDto> CreateAsync(TransactionDetailDto transactionDetailDto)
        {
            var entity = mapper.Map<DAL.Entities.TransactionDetail>(transactionDetailDto);
            await _uow.TransactionDetailRepository.AddAsync(entity);
            await _uow.Complete();
            return mapper.Map<TransactionDetailDto>(entity);
        }

        public async Task<TransactionDetailDto> UpdateAsync(TransactionDetailDto transactionDetailDto)
        {
            var existingEntity = await _uow.TransactionDetailRepository.GetByIdAsync(transactionDetailDto.Id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Transaction detail not found");
            }
            mapper.Map(transactionDetailDto, existingEntity);
            _uow.TransactionDetailRepository.Update(existingEntity);
            await _uow.Complete();
            return mapper.Map<TransactionDetailDto>(existingEntity);
        }

        public async Task<TransactionDetailDto> DeleteAsync(int id)
        {
            var existingEntity = await _uow.TransactionDetailRepository.GetByIdAsync(id);
            if (existingEntity == null)
            {
                throw new KeyNotFoundException("Transaction detail not found");
            }
            _uow.TransactionDetailRepository.Delete(existingEntity);
            await _uow.Complete();
            return mapper.Map<TransactionDetailDto>(existingEntity);
        }
    }
}

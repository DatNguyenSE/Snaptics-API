using AutoMapper;
using BLL.Dtos;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.IRepositories;

namespace BLL.Service
{
    public class TransactionService(
        IUnitOfWork _uow,
        IMapper mapper
    ) : ITransactionService
    {
        public async Task<IEnumerable<TransactionDto>
        > GetAllAsync()
        {
            var transactions = await _uow.TransactionRepository.GetAllAsync();
            return mapper.Map<IEnumerable<TransactionDto>>(transactions);
        }

        public async Task<TransactionDto> GetByIdAsync(int transactionId)
        {
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            return mapper.Map<TransactionDto>(transaction);
        }

        public async Task<TransactionDto> CreateAsync(TransactionDto transactionDto)
        {
            var entity = mapper.Map<DAL.Entities.Transaction>(transactionDto);
            if (entity == null)
            {
                throw new Exception("Failed to create transaction");
            }
            await _uow.TransactionRepository.AddAsync(entity);
            await _uow.Complete();
            return mapper.Map<TransactionDto>(entity);
        }

        public async Task<TransactionDto> UpdateAsync(int transactionId, TransactionDto transactionDto)
        {
            var existingTransaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            if (existingTransaction == null)
            {
                throw new Exception("Transaction not found");
            }

            //update data
            mapper.Map(transactionDto, existingTransaction);
            _uow.TransactionRepository.Update(existingTransaction);
            await _uow.Complete();
            return mapper.Map<TransactionDto>(existingTransaction);
        }

        public async Task<TransactionDto> DeleteAsync(int transactionId)
        {
            var transaction = await _uow.TransactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new Exception("Transaction not found");
            }
            _uow.TransactionRepository.Delete(transaction);
            await _uow.Complete();
            return mapper.Map<TransactionDto>(transaction);
        }
    }
}
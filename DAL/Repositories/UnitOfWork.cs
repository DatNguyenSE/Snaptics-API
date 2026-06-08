using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Data;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class UnitOfWork(AppDbContext _context) : IUnitOfWork
    {
        private ICategoryRepository? _categoryRepository;
        private ITransactionDetailRepository? _transactionDetailRepository;
        private ITransactionRepository? _transactionRepository;
        private IItemInventoryRepository? _itemInventoryRepository;
        private IItemDictionaryRepository? _itemDictionaryRepository;

        //when other function call (uow.ProductRepository) -> check and avoid create multiple instance
        public ICategoryRepository CategoryRepository => _categoryRepository 
            ??= new CategoryRepository(_context);  
        public ITransactionDetailRepository TransactionDetailRepository => _transactionDetailRepository
            ??= new TransactionDetailRepository(_context);

        public ITransactionRepository TransactionRepository => _transactionRepository 
            ??= new TransactionRepository(_context);

        public IItemInventoryRepository ItemInventoryRepository => _itemInventoryRepository
            ??= new ItemInventoryRepository(_context);

        public IItemDictionaryRepository ItemDictionaryRepository => _itemDictionaryRepository
            ??= new ItemDictionaryRepository(_context);

        public async Task<bool> Complete()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public bool HasChange()
        {
            return _context.ChangeTracker.HasChanges();
        }
    }
}
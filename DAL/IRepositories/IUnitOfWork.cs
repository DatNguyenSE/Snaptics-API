using DAL.IRepositories;

namespace DAL.IRepositories
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        Task<bool> Complete();
        bool HasChange();
    }
}
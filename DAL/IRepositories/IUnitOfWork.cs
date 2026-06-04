namespace DAL.IRepositories
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }
        ITransactionDetailRepository TransactionDetailRepository { get; }
        Task<bool> Complete();
        bool HasChange();
    }
}
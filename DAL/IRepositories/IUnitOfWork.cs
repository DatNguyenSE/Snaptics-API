using DAL.IRepositories;

namespace DAL.IRepositories
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }
        ITransactionDetailRepository TransactionDetailRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        IItemInventoryRepository ItemInventoryRepository { get; }
        IItemDictionaryRepository ItemDictionaryRepository { get; }
        IBudgetRepository BudgetRepository { get; }
        INotificationRepository NotificationRepository { get; }
        Task<bool> Complete();
        bool HasChange();
    }
}
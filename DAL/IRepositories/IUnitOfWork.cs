using DAL.IRepositories;

namespace DAL.IRepositories
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }
        IUserCategorySettingRepository UserCategorySettingRepository { get; }
        ITransactionDetailRepository TransactionDetailRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        IItemInventoryRepository ItemInventoryRepository { get; }
        IItemDictionaryRepository ItemDictionaryRepository { get; }
        IBudgetRepository BudgetRepository { get; }
        INotificationRepository NotificationRepository { get; }
        IIncomeSourceRepository IncomeSourceRepository { get; }

        IIncomeHistoryRepository IncomeHistoryRepository { get; }
        IBudgetMemberRepository BudgetMemberRepository { get; }
        IBudgetIncomeSourceRepository BudgetIncomeSourceRepository { get; }
        ISupportTicketRepository SupportTicketRepository { get; }
        ISupportMessageRepository SupportMessageRepository { get; }
        ISupportAttachmentRepository SupportAttachmentRepository { get; }
        Task<bool> Complete();
        bool HasChange();
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
    }
}

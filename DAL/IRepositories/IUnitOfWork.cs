namespace DAL.IRepositories
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }
        Task<bool> Complete();
        bool HasChange();
    }
}
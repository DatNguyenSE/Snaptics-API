using DAL.Entities;

namespace DAL.IRepositories
{
    public interface IBudgetRepository : IGenericRepository<Budget>
    {
        Task<IEnumerable<Budget>> GetByUserIdAsync(string userId);
    }
}

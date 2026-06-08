using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class BudgetRepository(AppDbContext context) : GenericRepository<Budget>(context), IBudgetRepository
    {
    }
}

using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class UserCategorySettingRepository(AppDbContext context)
        : GenericRepository<UserCategorySetting>(context), IUserCategorySettingRepository
    {
    }
}

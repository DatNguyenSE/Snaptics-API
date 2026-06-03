using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class CategoryRepository(AppDbContext _context) : GenericRepository<Category>(_context), ICategoryRepository
    {
        
    }
}
using DAL.Data;
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class TransactionRepository(AppDbContext _context) : GenericRepository<Transaction>(_context), ITransactionRepository
    {

    }
}

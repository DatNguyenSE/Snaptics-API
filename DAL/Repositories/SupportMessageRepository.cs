using System;
using System.Collections.Generic;
using System.Text;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class SupportMessageRepository(AppDbContext _context)
        : GenericRepository<SupportMessage>(_context), ISupportMessageRepository
    {
        public async Task<IEnumerable<SupportMessage>> GetByTicketIdAsync(int ticketId)
        {
            return await _dbSet
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .Where(m => m.TicketId == ticketId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}

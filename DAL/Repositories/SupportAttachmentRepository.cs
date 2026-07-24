using System;
using System.Collections.Generic;
using System.Text;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class SupportAttachmentRepository(AppDbContext _context)
        : GenericRepository<SupportAttachment>(_context), ISupportAttachmentRepository
    {
        public async Task<IEnumerable<SupportAttachment>> GetByTicketIdAsync(int ticketId)
        {
            return await _dbSet
                .Where(a => a.TicketId == ticketId)
                .ToListAsync();
        }

        public async Task<IEnumerable<SupportAttachment>> GetByMessageIdAsync(int messageId)
        {
            return await _dbSet
                .Where(a => a.MessageId == messageId)
                .ToListAsync();
        }
    }
}

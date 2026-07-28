using System;
using System.Collections.Generic;
using System.Text;
using DAL.Data;
using DAL.Entities;
using DAL.Enums;
using DAL.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class SupportTicketRepository(AppDbContext _context)
        : GenericRepository<SupportTicket>(_context), ISupportTicketRepository
    {
        public async Task<IEnumerable<SupportTicket>> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(st => st.User)
                .Include(st => st.AssignedTo)
                .Include(st => st.Messages)
                .Where(st => st.UserId == userId)
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();
        }

        public async Task<SupportTicket?> GetWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(st => st.User)
                .Include(st => st.AssignedTo)
                .Include(st => st.Messages)
                    .ThenInclude(m => m.Sender)
                .Include(st => st.Messages)
                    .ThenInclude(m => m.Attachments)
                .Include(st => st.Attachments)
                .FirstOrDefaultAsync(st => st.Id == id);
        }

        public async Task<IEnumerable<SupportTicket>> SearchAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketCategory? category,
            int page,
            int size,
            string userId)
        {
            var query = _dbSet
                .Include(st => st.User)
                .Include(st => st.AssignedTo)
                .Include(st => st.Messages)
                .Where(st => st.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(st =>
                    st.Subject!.Contains(search) ||
                    st.Description!.Contains(search));

            if (status.HasValue)
                query = query.Where(st => st.Status == status.Value);

            if (category.HasValue)
                query = query.Where(st => st.Category == category.Value);

            return await query
                .OrderByDescending(st => st.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<int> CountAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketCategory? category,
            string userId)
        {
            var query = _dbSet.Where(st => st.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(st =>
                    st.Subject!.Contains(search) ||
                    st.Description!.Contains(search));

            if (status.HasValue)
                query = query.Where(st => st.Status == status.Value);

            if (category.HasValue)
                query = query.Where(st => st.Category == category.Value);

            return await query.CountAsync();
        }

        public async Task<IEnumerable<SupportTicket>> AdminSearchAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketPriority? priority,
            SupportTicketCategory? category,
            string? assignedToId,
            int page,
            int size)
        {
            var query = _dbSet
                .Include(st => st.User)
                .Include(st => st.AssignedTo)
                .Include(st => st.Messages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(st =>
                    st.Subject!.Contains(search) ||
                    st.Description!.Contains(search));

            if (status.HasValue)
                query = query.Where(st => st.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(st => st.Priority == priority.Value);

            if (category.HasValue)
                query = query.Where(st => st.Category == category.Value);

            if (!string.IsNullOrEmpty(assignedToId))
                query = query.Where(st => st.AssignedToId == assignedToId);

            return await query
                .OrderByDescending(st => st.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<int> AdminCountAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketPriority? priority,
            SupportTicketCategory? category,
            string? assignedToId)
        {
            var query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(st =>
                    st.Subject!.Contains(search) ||
                    st.Description!.Contains(search));

            if (status.HasValue)
                query = query.Where(st => st.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(st => st.Priority == priority.Value);

            if (category.HasValue)
                query = query.Where(st => st.Category == category.Value);

            if (!string.IsNullOrEmpty(assignedToId))
                query = query.Where(st => st.AssignedToId == assignedToId);

            return await query.CountAsync();
        }
    }
}

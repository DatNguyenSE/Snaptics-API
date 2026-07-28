using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;
using DAL.Enums;

namespace DAL.IRepositories
{
    public interface ISupportTicketRepository : IGenericRepository<SupportTicket>
    {
        Task<IEnumerable<SupportTicket>> GetByUserIdAsync(string userId);
        Task<SupportTicket?> GetWithDetailsAsync(int id);
        Task<IEnumerable<SupportTicket>> SearchAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketCategory? category,
            int page,
            int size,
            string userId);
        Task<int> CountAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketCategory? category,
            string userId);
            
        Task<IEnumerable<SupportTicket>> AdminSearchAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketPriority? priority,
            SupportTicketCategory? category,
            string? assignedToId,
            int page,
            int size);
            
        Task<int> AdminCountAsync(
            string? search,
            SupportTicketStatus? status,
            SupportTicketPriority? priority,
            SupportTicketCategory? category,
            string? assignedToId);
    }
}
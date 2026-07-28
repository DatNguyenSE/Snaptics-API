using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface ISupportAttachmentRepository : IGenericRepository<SupportAttachment>
    {
        Task<IEnumerable<SupportAttachment>> GetByTicketIdAsync(int ticketId);
        Task<IEnumerable<SupportAttachment>> GetByMessageIdAsync(int messageId);
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface ISupportMessageRepository : IGenericRepository<SupportMessage>
    {
        Task<IEnumerable<SupportMessage>> GetByTicketIdAsync(int ticketId);
    }
}

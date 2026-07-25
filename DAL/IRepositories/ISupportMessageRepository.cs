using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DAL.Entities;

namespace DAL.IRepositories
{
    public interface ISupportMessageRepository : IGenericRepository<SupportMessage>
    {
        Task<SupportMessage?> GetWithDetailsAsync(int id);
        Task<IEnumerable<SupportMessage>> GetByTicketIdAsync(int ticketId);
    }
}

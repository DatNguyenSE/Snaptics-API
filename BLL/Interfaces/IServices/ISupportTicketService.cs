using System;
using System.Collections.Generic;
using System.Text;
using BLL.Dtos;
using BLL.Dtos.Support;
using Microsoft.AspNetCore.Http;

namespace BLL.Interfaces.IServices
{
    public interface ISupportTicketService
    {
        Task<SupportTicketDto> CreateTicketAsync(string userId, CreateSupportTicketDto dto);
        Task<PaginatedResultDto<SupportTicketDto>> GetUserTicketsAsync(string userId, UserTicketQueryDto query);
        Task<SupportTicketDetailDto?> GetUserTicketDetailAsync(string userId, int ticketId);
        Task<SupportMessageDto> SendMessageAsync(string userId, int ticketId, SendMessageDto dto);
        Task<SupportTicketDto> CloseTicketAsync(string userId, int ticketId);
        Task<SupportTicketDto> ReopenTicketAsync(string userId, int ticketId);
        Task<SupportTicketStatisticsDto> GetUserStatisticsAsync(string userId);
        Task<SupportAttachmentDto> UploadAttachmentAsync(string userId, int? ticketId, int? messageId, IFormFile file);
        
        // Admin Methods
        Task<PaginatedResultDto<SupportTicketDto>> AdminGetTicketsAsync(AdminTicketQueryDto query);
        Task<SupportTicketDetailDto?> AdminGetTicketDetailAsync(int ticketId);
        Task<SupportTicketDto> AdminAssignTicketAsync(int ticketId, string assignedToId);
        Task<SupportTicketDto> AdminUpdateTicketStatusAsync(int ticketId, DAL.Enums.SupportTicketStatus status);
        Task<SupportTicketDto> AdminUpdateTicketPriorityAsync(int ticketId, DAL.Enums.SupportTicketPriority priority);
        Task<SupportMessageDto> AdminSendMessageAsync(string adminId, int ticketId, AdminSendMessageDto dto);
    }
}

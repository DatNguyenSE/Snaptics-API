using System;
using System.Collections.Generic;
using System.Text;
using BLL.Dtos;
using BLL.Dtos.Support;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.Enums;
using DAL.IRepositories;
using Microsoft.AspNetCore.Http;

namespace BLL.Service
{
    public class SupportTicketService(
        IUnitOfWork _uow,
        IS3Service _s3Service
    ) : ISupportTicketService
    {
        public async Task<SupportTicketDto> CreateTicketAsync(string userId, CreateSupportTicketDto dto)
        {
            var ticket = new SupportTicket
            {
                UserId = userId,
                Subject = dto.Subject,
                Description = dto.Description,
                Category = dto.Category,
                Status = SupportTicketStatus.Pending,
                Priority = SupportTicketPriority.Normal,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.SupportTicketRepository.AddAsync(ticket);
            await _uow.Complete();

            return MapToDto(ticket);
        }

        public async Task<PaginatedResultDto<SupportTicketDto>> GetUserTicketsAsync(string userId, UserTicketQueryDto query)
        {
            var tickets = await _uow.SupportTicketRepository.SearchAsync(
                query.Search, query.Status, query.Category,
                query.Page, query.Size, userId);

            var totalCount = await _uow.SupportTicketRepository.CountAsync(
                query.Search, query.Status, query.Category, userId);

            return new PaginatedResultDto<SupportTicketDto>
            {
                Items = tickets.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = query.Page,
                Size = query.Size
            };
        }

        public async Task<SupportTicketDetailDto?> GetUserTicketDetailAsync(string userId, int ticketId)
        {
            var ticket = await _uow.SupportTicketRepository.GetWithDetailsAsync(ticketId);
            if (ticket == null || ticket.UserId != userId) return null;

            return MapToDetailDto(ticket);
        }

        public async Task<SupportMessageDto> SendMessageAsync(string userId, int ticketId, SendMessageDto dto)
        {
            var ticket = await _uow.SupportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null || ticket.UserId != userId)
                throw new KeyNotFoundException("Ticket not found");

            var message = new SupportMessage
            {
                TicketId = ticketId,
                SenderId = userId,
                Content = dto.Content,
                IsFromAdmin = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.SupportMessageRepository.AddAsync(message);

            if (ticket.Status == SupportTicketStatus.WaitingForUser ||
                ticket.Status == SupportTicketStatus.Resolved)
            {
                ticket.Status = SupportTicketStatus.InProgress;
                ticket.UpdatedAt = DateTime.UtcNow;
                _uow.SupportTicketRepository.Update(ticket);
            }

            await _uow.Complete();

            return MapMessageToDto(message);
        }

        public async Task<SupportTicketDto> CloseTicketAsync(string userId, int ticketId)
        {
            var ticket = await _uow.SupportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null || ticket.UserId != userId)
                throw new KeyNotFoundException("Ticket not found");

            ticket.Status = SupportTicketStatus.Closed;
            ticket.UpdatedAt = DateTime.UtcNow;
            _uow.SupportTicketRepository.Update(ticket);
            await _uow.Complete();

            return MapToDto(ticket);
        }

        public async Task<SupportTicketDto> ReopenTicketAsync(string userId, int ticketId)
        {
            var ticket = await _uow.SupportTicketRepository.GetByIdAsync(ticketId);
            if (ticket == null || ticket.UserId != userId)
                throw new KeyNotFoundException("Ticket not found");

            if (ticket.Status != SupportTicketStatus.Closed &&
                ticket.Status != SupportTicketStatus.Resolved)
                throw new InvalidOperationException("Only closed or resolved tickets can be reopened");

            ticket.Status = SupportTicketStatus.InProgress;
            ticket.UpdatedAt = DateTime.UtcNow;
            _uow.SupportTicketRepository.Update(ticket);
            await _uow.Complete();

            return MapToDto(ticket);
        }

        public async Task<SupportTicketStatisticsDto> GetUserStatisticsAsync(string userId)
        {
            var tickets = await _uow.SupportTicketRepository.GetByUserIdAsync(userId);
            var list = tickets.ToList();

            return new SupportTicketStatisticsDto
            {
                Total = list.Count,
                Pending = list.Count(t => t.Status == SupportTicketStatus.Pending),
                InProgress = list.Count(t => t.Status == SupportTicketStatus.InProgress),
                WaitingForUser = list.Count(t => t.Status == SupportTicketStatus.WaitingForUser),
                Resolved = list.Count(t => t.Status == SupportTicketStatus.Resolved),
                Closed = list.Count(t => t.Status == SupportTicketStatus.Closed)
            };
        }

        public async Task<SupportAttachmentDto> UploadAttachmentAsync(string userId, int? ticketId, int? messageId, IFormFile file)
        {
            var fileKey = await _s3Service.UploadFileAsync(file, $"support-{userId}", "support-attachments");

            var attachment = new SupportAttachment
            {
                TicketId = ticketId,
                MessageId = messageId,
                FileUrl = fileKey,
                FileName = file.FileName,
                FileType = file.ContentType,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.SupportAttachmentRepository.AddAsync(attachment);
            await _uow.Complete();

            return MapAttachmentToDto(attachment);
        }

        // ==================== Mapping Helpers ====================

        private static SupportTicketDto MapToDto(SupportTicket ticket)
        {
            return new SupportTicketDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                UserName = ticket.User?.DisplayName ?? ticket.User?.Email,
                UserEmail = ticket.User?.Email,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                Category = ticket.Category,
                AssignedToId = ticket.AssignedToId,
                AssignedToName = ticket.AssignedTo?.DisplayName ?? ticket.AssignedTo?.Email,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                MessageCount = ticket.Messages?.Count ?? 0
            };
        }

        private static SupportTicketDetailDto MapToDetailDto(SupportTicket ticket)
        {
            return new SupportTicketDetailDto
            {
                Id = ticket.Id,
                UserId = ticket.UserId,
                UserName = ticket.User?.DisplayName ?? ticket.User?.Email,
                UserEmail = ticket.User?.Email,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                Category = ticket.Category,
                AssignedToId = ticket.AssignedToId,
                AssignedToName = ticket.AssignedTo?.DisplayName ?? ticket.AssignedTo?.Email,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                MessageCount = ticket.Messages?.Count ?? 0,
                Messages = ticket.Messages?.Select(MapMessageToDto).ToList() ?? new(),
                Attachments = ticket.Attachments?.Select(MapAttachmentToDto).ToList() ?? new()
            };
        }

        private static SupportMessageDto MapMessageToDto(SupportMessage message)
        {
            return new SupportMessageDto
            {
                Id = message.Id,
                TicketId = message.TicketId,
                SenderId = message.SenderId,
                SenderName = message.Sender?.DisplayName ?? message.Sender?.Email,
                Content = message.Content,
                IsFromAdmin = message.IsFromAdmin,
                CreatedAt = message.CreatedAt,
                Attachments = message.Attachments?.Select(MapAttachmentToDto).ToList() ?? new()
            };
        }

        private static SupportAttachmentDto MapAttachmentToDto(SupportAttachment attachment)
        {
            return new SupportAttachmentDto
            {
                Id = attachment.Id,
                FileUrl = attachment.FileUrl,
                FileName = attachment.FileName,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                CreatedAt = attachment.CreatedAt
            };
        }
    }
}

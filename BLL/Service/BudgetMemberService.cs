using BLL.Dtos;
using BLL.Interfaces;
using BLL.Interfaces.IServices;
using DAL.Entities;
using DAL.Enums;
using DAL.IRepositories;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class BudgetMemberService : IBudgetMemberService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<AppUser> _userManager;
        private readonly INotificationService _notificationService;

        public BudgetMemberService(IUnitOfWork uow, UserManager<AppUser> userManager, INotificationService notificationService)
        {
            _uow = uow;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<BudgetMemberResponseDto> InviteMemberAsync(int budgetId, string ownerId, InviteMemberRequestDto request)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            if (budget == null || budget.UserId != ownerId)
                throw new Exception("Budget not found or you are not the owner.");

            var userToInvite = await _userManager.FindByEmailAsync(request.EmailOrUsername) 
                               ?? await _userManager.FindByNameAsync(request.EmailOrUsername);
            
            if (userToInvite == null)
                throw new Exception("User not found.");

            if (userToInvite.Id == ownerId)
                throw new Exception("You cannot invite yourself.");

            var existingMember = await _uow.BudgetMemberRepository.GetByBudgetAndMemberAsync(budgetId, userToInvite.Id);
            if (existingMember != null)
                throw new Exception("User is already a member or has a pending invitation.");

            var budgetMember = new BudgetMember
            {
                BudgetId = budgetId,
                MemberId = userToInvite.Id,
                Role = request.Role,
                Status = InvitationStatus.Pending
            };

            await _uow.BudgetMemberRepository.AddAsync(budgetMember);
            await _uow.Complete();

            var owner = await _userManager.FindByIdAsync(ownerId);
            var ownerName = owner?.DisplayName ?? owner?.UserName ?? "Ai đó";
            
            await _notificationService.CreateAsync(new NotificationDto
            {
                UserId = userToInvite.Id,
                Message = $"{ownerName} vừa mời bạn tham gia ví {budget.Name}.",
                Type = NotificationType.BudgetInvitation,
                CreatedAt = DateTime.UtcNow.AddHours(7),
                IsRead = false,
                RelatedId = budgetMember.Id
            });

            return new BudgetMemberResponseDto
            {
                Id = budgetMember.Id,
                BudgetId = budgetMember.BudgetId,
                MemberId = budgetMember.MemberId,
                MemberName = userToInvite.DisplayName ?? userToInvite.UserName,
                MemberEmail = userToInvite.Email,
                Role = budgetMember.Role,
                Status = budgetMember.Status,
                CreatedAt = budgetMember.CreatedAt
            };
        }

        public async Task<bool> RespondToInvitationAsync(int invitationId, string userId, RespondInviteRequestDto request)
        {
            var invitation = await _uow.BudgetMemberRepository.GetByIdAsync(invitationId);
            if (invitation == null || invitation.MemberId != userId)
                throw new Exception("Invitation not found or unauthorized.");

            if (request.Status == InvitationStatus.Rejected)
            {
                // Cách 1: Xóa luôn record nếu từ chối
                _uow.BudgetMemberRepository.Delete(invitation);
            }
            else if (request.Status == InvitationStatus.Accepted)
            {
                invitation.Status = InvitationStatus.Accepted;
                invitation.JoinedAt = DateTime.UtcNow.AddHours(7);
                _uow.BudgetMemberRepository.Update(invitation);
            }
            
            return await _uow.Complete();
        }

        public async Task<IEnumerable<BudgetMemberResponseDto>> GetBudgetMembersAsync(int budgetId, string userId)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            if (budget == null)
                throw new Exception("Budget not found.");

            bool isOwner = budget.UserId == userId;
            bool isMember = false;

            if (!isOwner)
            {
                var member = await _uow.BudgetMemberRepository.GetByBudgetAndMemberAsync(budgetId, userId);
                isMember = member != null && member.Status == InvitationStatus.Accepted;
            }

            if (!isOwner && !isMember)
                throw new Exception("Unauthorized to view members of this budget.");

            var members = await _uow.BudgetMemberRepository.GetMembersByBudgetIdAsync(budgetId);

            var resultList = new List<BudgetMemberResponseDto>();

            // Thêm chủ ví vào đầu danh sách
            if (!string.IsNullOrEmpty(budget.UserId))
            {
                var owner = await _userManager.FindByIdAsync(budget.UserId);
                if (owner != null)
                {
                    resultList.Add(new BudgetMemberResponseDto
                    {
                        Id = 0,
                        BudgetId = budget.Id,
                        BudgetName = budget.Name,
                        MemberId = owner.Id,
                        MemberName = owner.DisplayName ?? owner.UserName,
                        MemberEmail = owner.Email,
                        Role = BudgetRole.Editor,
                        Status = InvitationStatus.Accepted,
                        CreatedAt = budget.CreatedAt,
                        JoinedAt = budget.CreatedAt,
                        IsOwner = true
                    });
                }
            }

            // Thêm danh sách thành viên
            resultList.AddRange(members.Select(bm => new BudgetMemberResponseDto
            {
                Id = bm.Id,
                BudgetId = bm.BudgetId,
                BudgetName = budget.Name,
                MemberId = bm.MemberId,
                MemberName = bm.Member?.DisplayName ?? bm.Member?.UserName,
                MemberEmail = bm.Member?.Email,
                Role = bm.Role,
                Status = bm.Status,
                CreatedAt = bm.CreatedAt,
                JoinedAt = bm.JoinedAt,
                IsOwner = bm.MemberId == budget.UserId
            }));

            return resultList;
        }

        public async Task<IEnumerable<BudgetMemberResponseDto>> GetMySharedBudgetsAsync(string userId)
        {
            var shared = await _uow.BudgetMemberRepository.GetSharedBudgetsByUserIdAsync(userId);
            
            return shared.Select(bm => new BudgetMemberResponseDto
            {
                Id = bm.Id,
                BudgetId = bm.BudgetId,
                BudgetName = bm.Budget?.Name,
                MemberId = bm.MemberId,
                Role = bm.Role,
                Status = bm.Status,
                CreatedAt = bm.CreatedAt,
                JoinedAt = bm.JoinedAt
            });
        }

        public async Task<bool> RemoveMemberAsync(int budgetId, string memberId, string ownerId)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            // Allow owner to remove, or member to leave
            if (budget == null || (budget.UserId != ownerId && memberId != ownerId))
                throw new Exception("Unauthorized to remove this member.");

            var member = await _uow.BudgetMemberRepository.GetByBudgetAndMemberAsync(budgetId, memberId);
            if (member == null)
                throw new Exception("Member not found in this budget.");

            _uow.BudgetMemberRepository.Delete(member);
            return await _uow.Complete();
        }

        public async Task<bool> UpdateMemberRoleAsync(int budgetId, string memberId, string ownerId, UpdateMemberRoleRequestDto request)
        {
            var budget = await _uow.BudgetRepository.GetByIdAsync(budgetId);
            if (budget == null || budget.UserId != ownerId)
                throw new Exception("Unauthorized.");

            var member = await _uow.BudgetMemberRepository.GetByBudgetAndMemberAsync(budgetId, memberId);
            if (member == null)
                throw new Exception("Member not found in this budget.");

            member.Role = request.Role;
            _uow.BudgetMemberRepository.Update(member);
            return await _uow.Complete();
        }
    }
}

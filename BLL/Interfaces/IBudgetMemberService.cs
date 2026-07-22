using System.Collections.Generic;
using System.Threading.Tasks;
using BLL.Dtos;
using DAL.Enums;

namespace BLL.Interfaces
{
    public interface IBudgetMemberService
    {
        Task<BudgetMemberResponseDto> InviteMemberAsync(int budgetId, string ownerId, InviteMemberRequestDto request);
        Task<bool> RespondToInvitationAsync(int invitationId, string userId, RespondInviteRequestDto request);
        Task<IEnumerable<BudgetMemberResponseDto>> GetBudgetMembersAsync(int budgetId, string userId);
        Task<IEnumerable<BudgetMemberResponseDto>> GetMySharedBudgetsAsync(string userId);
        Task<bool> RemoveMemberAsync(int budgetId, string memberId, string ownerId);
        Task<bool> UpdateMemberRoleAsync(int budgetId, string memberId, string ownerId, UpdateMemberRoleRequestDto request);
    }
}

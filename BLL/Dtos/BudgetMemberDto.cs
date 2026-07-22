using System;
using System.ComponentModel.DataAnnotations;
using DAL.Enums;

namespace BLL.Dtos
{
    public class InviteMemberRequestDto
    {
        [Required]
        public string EmailOrUsername { get; set; }
        public BudgetRole Role { get; set; }
    }

    public class RespondInviteRequestDto
    {
        public InvitationStatus Status { get; set; }
    }

    public class UpdateMemberRoleRequestDto
    {
        public BudgetRole Role { get; set; }
    }

    public class BudgetMemberResponseDto
    {
        public int Id { get; set; }
        public int BudgetId { get; set; }
        public string? BudgetName { get; set; }
        public string MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberEmail { get; set; }
        public BudgetRole Role { get; set; }
        public InvitationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? JoinedAt { get; set; }
    }
}

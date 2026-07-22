using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DAL.Enums;

namespace DAL.Entities
{
    public class BudgetMember
    {
        [Key]
        public int Id { get; set; }

        public int BudgetId { get; set; }
        [ForeignKey("BudgetId")]
        public virtual Budget Budget { get; set; }

        public string MemberId { get; set; }
        [ForeignKey("MemberId")]
        public virtual AppUser Member { get; set; }

        public BudgetRole Role { get; set; } = BudgetRole.Viewer;
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? JoinedAt { get; set; }
    }
}

using API.Extensions;
using BLL.Dtos;
using BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/budgets")]
    [Authorize]
    public class BudgetMemberController : Controller
    {
        private readonly IBudgetMemberService _budgetMemberService;

        public BudgetMemberController(IBudgetMemberService budgetMemberService)
        {
            _budgetMemberService = budgetMemberService;
        }

        [HttpPost("{budgetId}/members/invite")]
        public async Task<IActionResult> InviteMember(int budgetId, [FromBody] InviteMemberRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _budgetMemberService.InviteMemberAsync(budgetId, userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("members/invitations/{invitationId}/respond")]
        public async Task<IActionResult> RespondToInvitation(int invitationId, [FromBody] RespondInviteRequestDto request)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _budgetMemberService.RespondToInvitationAsync(invitationId, userId, request);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{budgetId}/members")]
        public async Task<IActionResult> GetMembers(int budgetId)
        {
            try
            {
                var userId = User.GetUserId();
                var members = await _budgetMemberService.GetBudgetMembersAsync(budgetId, userId);
                return Ok(members);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("shared-with-me")]
        public async Task<IActionResult> GetSharedBudgets()
        {
            try
            {
                var userId = User.GetUserId();
                var budgets = await _budgetMemberService.GetMySharedBudgetsAsync(userId);
                return Ok(budgets);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{budgetId}/members/{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(int budgetId, string memberId, [FromBody] UpdateMemberRoleRequestDto request)
        {
            try
            {
                var ownerId = User.GetUserId();
                var result = await _budgetMemberService.UpdateMemberRoleAsync(budgetId, memberId, ownerId, request);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{budgetId}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int budgetId, string memberId)
        {
            try
            {
                var ownerId = User.GetUserId();
                var result = await _budgetMemberService.RemoveMemberAsync(budgetId, memberId, ownerId);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

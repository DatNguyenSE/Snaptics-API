using BLL.Dtos;
using BLL.Service;
using BLL.Interfaces.IServices;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/hangfire-config")]
public class HangfireConfigController : ControllerBase
{
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireConfigController(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }


    // ==========================================
    // 1. TỰ ĐỘNG GIA HẠN VÍ (PERIODIC ROLLOVER)
    // ==========================================

    [HttpPost("periodic-rollover")]
    public IActionResult UpdatePeriodicRolloverSchedule([FromBody] UpdateHangfireScheduleRequest request)
    {
        if (request.Hour < 0 || request.Hour > 23)
            return BadRequest("Hour must be between 0 and 23.");

        if (request.Minute < 0 || request.Minute > 59)
            return BadRequest("Minute must be between 0 and 59.");

        var cron = $"{request.Minute} {request.Hour} * * *";

        _recurringJobManager.AddOrUpdate<IBudgetService>(
            "process-periodic-rollover-daily",
            job => job.ProcessPeriodicRolloverAsync(),
            cron);

        return Ok(new
        {
            message = "Periodic rollover schedule updated successfully.",
            cron
        });
    }

    [HttpPost("trigger/periodic-rollover")]
    public async Task<IActionResult> TriggerPeriodicRolloverNow([FromServices] IBudgetService budgetService)
    {
        await budgetService.ProcessPeriodicRolloverAsync();
        return Ok(new { message = "Periodic rollover executed successfully." });
    }

    // ==========================================
    // 2. NHẮC NHỞ ĐÁNH GIÁ MÓN ĐỒ (ITEM REVIEW)
    // ==========================================

    [HttpPost("item-review")]
    public IActionResult UpdateItemReviewSchedule([FromBody] UpdateHangfireScheduleRequest request)
    {
        if (request.Hour < 0 || request.Hour > 23)
            return BadRequest("Hour must be between 0 and 23.");

        if (request.Minute < 0 || request.Minute > 59)
            return BadRequest("Minute must be between 0 and 59.");

        var cron = $"{request.Minute} {request.Hour} * * *";

        _recurringJobManager.AddOrUpdate<IItemReviewJobService>(
            "remind-item-review-daily",
            job => job.ScanAndSendNotificationAsync(30),
            cron);

        return Ok(new
        {
            message = "Item review schedule updated successfully.",
            cron
        });
    }

    [HttpPost("trigger/item-review")]
    public async Task<IActionResult> TriggerItemReviewNow([FromServices] IItemReviewJobService itemReviewService)
    {
        // Truyền 30 ngày giống như config của Hangfire đang dùng
        await itemReviewService.ScanAndSendNotificationAsync(30);
        return Ok(new { message = "Item review check executed successfully." });
    }

    // ==========================================
    // 3. DỌN DẸP THÔNG BÁO CŨ (CLEANUP NOTIFICATIONS)
    // ==========================================

    [HttpPost("cleanup")]
    public IActionResult UpdateCleanupSchedule([FromBody] UpdateHangfireScheduleRequest request)
    {
        if (request.Hour < 0 || request.Hour > 23)
            return BadRequest("Hour must be between 0 and 23.");

        if (request.Minute < 0 || request.Minute > 59)
            return BadRequest("Minute must be between 0 and 59.");

        var cron = $"{request.Minute} {request.Hour} * * *";

        _recurringJobManager.AddOrUpdate<INotificationService>(
            "cleanup-old-notifications-daily",
            job => job.CleanUpOldNotificationsAsync(),
            cron);

        return Ok(new
        {
            message = "Cleanup schedule updated successfully.",
            cron
        });
    }

    [HttpPost("trigger/cleanup")]
    public async Task<IActionResult> TriggerCleanupNow([FromServices] INotificationService notificationService)
    {
        await notificationService.CleanUpOldNotificationsAsync();
        return Ok(new { message = "Old notifications cleanup executed successfully." });
    }
}
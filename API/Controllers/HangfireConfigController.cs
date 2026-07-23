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

    [HttpPost("missing-price")]
    public IActionResult UpdateMissingPriceSchedule([FromBody]UpdateHangfireScheduleRequest request)
    {
        if (request.Hour < 0 || request.Hour > 23)
            return BadRequest("Hour must be between 0 and 23.");

        if (request.Minute < 0 || request.Minute > 59)
            return BadRequest("Minute must be between 0 and 59.");

        var cron = $"{request.Minute} {request.Hour} * * *";

        _recurringJobManager.AddOrUpdate<IMissingPriceJob>(
            "remind-missing-price-daily",
            job => job.ScanAndSendNotificationAsync(),
            cron);

        return Ok(new
        {
            message = "Missing price schedule updated successfully.",
            cron
        });
    }

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
}
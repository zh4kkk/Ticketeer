using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Models;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/statistics")]
public sealed class StatisticsController(StatisticsService statisticsService, AttemptService attemptService) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = "Cashier")]
    public async Task<ActionResult<StatisticsResponse>> Me(CancellationToken cancellationToken)
    {
        try
        {
            return await WithUserId<StatisticsResponse>(async userId => Ok(await statisticsService.GetMyStatisticsAsync(userId, cancellationToken)));
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("me/activity")]
    [Authorize(Roles = "Cashier")]
    public async Task<ActionResult<IReadOnlyList<ActivityResponse>>> MyActivity(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await WithUserId<IReadOnlyList<ActivityResponse>>(async userId => Ok(await statisticsService.GetActivityAsync(userId, days, cancellationToken)));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<UserStatisticsResponse>>> Users(
        int page = 1,
        int pageSize = 20,
        int? groupId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await statisticsService.GetUserStatisticsListAsync(page, pageSize, groupId, cancellationToken));
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("users/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserStatisticsResponse>> UserStatistics(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await statisticsService.GetUserStatisticsAsync(userId, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("users/{userId:int}/activity")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ActivityResponse>>> UserActivity(
        int userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await statisticsService.GetUserActivityAsync(userId, days, cancellationToken));
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("users/{userId:int}/attempts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResponse<AttemptHistoryItem>>> UserAttempts(
        int userId,
        int page = 1,
        int pageSize = 20,
        AttemptKind? kind = null,
        AttemptSource? source = null,
        AttemptStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await statisticsService.GetUserAttemptHistoryAsync(
                userId, page, pageSize, kind, source, status, cancellationToken));
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("users/{userId:int}/attempts/{attemptId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AttemptResponse>> UserAttempt(
        int userId,
        int attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await attemptService.GetForUserAsync(attemptId, userId, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("groups")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<GroupStatisticsResponse>>> Groups(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await statisticsService.GetGroupStatisticsAsync(cancellationToken));
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet("groups/{groupId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<GroupStatisticsResponse>> Group(int groupId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await statisticsService.GetGroupStatisticsAsync(groupId, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    private async Task<ActionResult<T>> WithUserId<T>(Func<int, Task<ActionResult<T>>> action)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !int.TryParse(claim, out var userId) ? Unauthorized() : await action(userId);
    }
}

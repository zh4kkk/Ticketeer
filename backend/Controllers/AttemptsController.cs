using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Models;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/attempts")]
[Authorize(Roles = "Cashier")]
public sealed class AttemptsController(AttemptService attemptService, StatisticsService statisticsService) : ControllerBase
{
    [HttpPost("training/tickets/{ticketId:int}")]
    public async Task<ActionResult<AttemptResponse>> StartTrainingTicket(
        int ticketId,
        CancellationToken cancellationToken) =>
        await StartAttempt(
            userId => attemptService.StartTrainingTicketAsync(ticketId, userId, cancellationToken));

    [HttpPost("exams/{examId:int}")]
    public async Task<ActionResult<AttemptResponse>> StartExam(
        int examId,
        CancellationToken cancellationToken) =>
        await StartAttempt(
            userId => attemptService.StartExamAsync(examId, userId, cancellationToken));

    [HttpPost("training/random")]
    public async Task<ActionResult<AttemptResponse>> StartRandomTraining(
        StartRandomTrainingRequest request,
        CancellationToken cancellationToken) =>
        await StartAttempt(userId => attemptService.StartRandomTrainingAsync(
            request.TopicId, request.QuestionCount, userId, cancellationToken));

    [HttpPost("exams/random")]
    public async Task<ActionResult<AttemptResponse>> StartRandomExam(
        CancellationToken cancellationToken) =>
        await StartAttempt(userId => attemptService.StartRandomExamAsync(userId, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttemptResponse>> Get(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await attemptService.GetAsync(id, userId, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AttemptHistoryItem>>> GetHistory(
        int page = 1,
        int pageSize = 20,
        AttemptKind? kind = null,
        AttemptSource? source = null,
        AttemptStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await statisticsService.GetAttemptHistoryAsync(
                userId, page, pageSize, kind, source, status, cancellationToken));
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

    [HttpPost("{id:int}/questions/{questionId:int}/answer")]
    public async Task<ActionResult<AttemptResponse>> SubmitAnswer(
        int id,
        int questionId,
        SubmitAttemptAnswerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await attemptService.SubmitAnswerAsync(
                id, questionId, userId, request, cancellationToken));
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private async Task<ActionResult<AttemptResponse>> StartAttempt(
        Func<int, Task<AttemptResponse>> start)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await start(userId);
            return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (ForbiddenException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/attempts")]
[Authorize(Roles = "Cashier")]
public sealed class AttemptsController(AttemptService attemptService) : ControllerBase
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttemptResponse>> Get(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await attemptService.GetAsync(id, userId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
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

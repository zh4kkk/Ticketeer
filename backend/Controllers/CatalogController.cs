using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize(Roles = "Cashier")]
public sealed class CatalogController(CatalogService catalogService) : ControllerBase
{
    [HttpGet("topics")]
    public async Task<ActionResult<IReadOnlyList<CatalogTopicResponse>>> Topics(
        string? search,
        CancellationToken cancellationToken)
    {
        try { return Ok(await catalogService.GetTopicsAsync(search, cancellationToken)); }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("tickets")]
    public async Task<ActionResult<PagedResponse<CatalogTicketListResponse>>> Tickets(
        string? search,
        int? topicId,
        bool? isCompleted,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await catalogService.GetTicketsAsync(userId, search, topicId, isCompleted, page, pageSize, cancellationToken));
        }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult<CatalogTicketResponse>> Ticket(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var response = await catalogService.GetTicketAsync(id, userId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("exams")]
    public async Task<ActionResult<PagedResponse<CatalogExamListResponse>>> Exams(
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await catalogService.GetExamsAsync(search, page, pageSize, cancellationToken));
        }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("exams/{id:int}")]
    public async Task<ActionResult<CatalogExamResponse>> Exam(int id, CancellationToken cancellationToken)
    {
        var response = await catalogService.GetExamAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("questions")]
    public async Task<ActionResult<PagedResponse<CatalogQuestionListResponse>>> Questions(
        string? search,
        int? topicId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await catalogService.GetQuestionsAsync(search, topicId, page, pageSize, cancellationToken));
        }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("questions/{id:int}")]
    public async Task<ActionResult<CatalogQuestionResponse>> Question(int id, CancellationToken cancellationToken)
    {
        var response = await catalogService.GetQuestionAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}

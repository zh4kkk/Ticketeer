using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize(Roles = "Admin")]
public sealed class QuestionsController(QuestionService questionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuestionListResponse>>> GetAll(int? topicId, string? search, CancellationToken cancellationToken)
    {
        try { return Ok(await questionService.GetAllAsync(topicId, cancellationToken, search)); }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var response = await questionService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<QuestionResponse>> Create(QuestionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await questionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuestionResponse>> Update(int id, QuestionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await questionService.UpdateAsync(id, request, cancellationToken);
            return response is null ? NotFound() : Ok(response);
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await questionService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}

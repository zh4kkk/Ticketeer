using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize(Roles = "Admin")]
public sealed class ExamsController(ExamService examService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExamListResponse>>> GetAll(string? search, CancellationToken cancellationToken)
    {
        try { return Ok(await examService.GetAllAsync(cancellationToken, search)); }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExamResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var response = await examService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ExamResponse>> Create(ExamRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await examService.CreateAsync(request, cancellationToken);
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
    public async Task<ActionResult<ExamResponse>> Update(int id, ExamRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await examService.UpdateAsync(id, request, cancellationToken);
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
            return await examService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}

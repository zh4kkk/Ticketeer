using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/topics")]
[Authorize(Roles = "Admin")]
public sealed class TopicsController(TopicService topicService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TopicResponse>>> GetAll(string? search, CancellationToken cancellationToken)
    {
        try { return Ok(await topicService.GetAllAsync(cancellationToken, search)); }
        catch (ValidationException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TopicResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var response = await topicService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TopicResponse>> Create(TopicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await topicService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = response.Id }, response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (ValidationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TopicResponse>> Update(int id, TopicRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await topicService.UpdateAsync(id, request, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
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
            return await topicService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}

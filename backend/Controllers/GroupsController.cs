using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize(Roles = "Admin")]
public sealed class GroupsController(GroupService groupService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await groupService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GroupResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var response = await groupService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<GroupResponse>> Create(GroupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await groupService.CreateAsync(request, cancellationToken);
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
    public async Task<ActionResult<GroupResponse>> Update(int id, GroupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await groupService.UpdateAsync(id, request, cancellationToken);
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await groupService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketeer.DTOs;
using Ticketeer.Services;

namespace Ticketeer.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize(Roles = "Admin")]
public sealed class TicketsController(TicketService ticketService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketListResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await ticketService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var response = await ticketService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(TicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await ticketService.CreateAsync(request, cancellationToken);
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
    public async Task<ActionResult<TicketResponse>> Update(int id, TicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await ticketService.UpdateAsync(id, request, cancellationToken);
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
            return await ticketService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (ConflictException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}

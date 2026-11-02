using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using race_day_backend_api.Data;
using race_day_backend_api.Models;

namespace race_day_backend_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ResultsController(ApplicationDbContext context) => _context = context;

    [HttpGet("event/{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEventResults(int eventId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var eventItem = await _context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
        if (eventItem == null) return NotFound(new { message = "Event not found." });
        if (eventItem.UserId != userId.Value) return Forbid();

        var rows = await _context.Enrolments
            .Where(e => e.EventId == eventId)
            .AsNoTracking()
            .OrderBy(e => e.Result == null)
            .ThenBy(e => e.Result == null ? TimeSpan.MaxValue : e.Result.Time)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new {
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event.Title,
                e.UserId,
                ParticipantName = e.Participant.UserAccount.FirstName + " " +
                                  e.Participant.UserAccount.LastName,
                Email = e.Participant.UserAccount.EmailAddress,
                EnrolledAt = e.CreatedAt,
                ResultId = e.Result == null ? (int?)null : e.Result.ResultsId,
                TimeText = e.Result == null ? "" : e.Result.Time.ToString(@"hh\:mm\:ss")
            }).ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> SaveResult([FromBody] SaveResultRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var enrolment = await _context.Enrolments
            .Include(e => e.Event).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId, ct);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event.UserId != userId.Value) return Forbid();
        if (request.Time <= TimeSpan.Zero)
            return BadRequest(new { message = "Finish time must be greater than zero." });

        if (enrolment.Result == null)
        {
            _context.Results.Add(new Result
            {
                EnrolmentId = enrolment.EnrolmentId,
                Time = request.Time
            });
        }
        else
        {
            enrolment.Result.Time = request.Time;
            _context.Results.Update(enrolment.Result);
        }

        await _context.SaveChangesAsync(ct);
        return Ok(new { message = "Race result saved successfully." });
    }

    [HttpGet("my-results")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyResults(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var results = await _context.Enrolments
            .Where(e => e.UserId == userId.Value && e.Result != null)
            .AsNoTracking()
            .OrderBy(e => e.Event.Date)
            .Select(e => new {
                ResultsId = e.Result!.ResultsId,
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event.Title,
                EventDate = e.Event.Date,
                e.Event.Distance,
                TimeText = e.Result!.Time.ToString(@"hh\:mm\:ss")
            }).ToListAsync(ct);

        return Ok(results);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? User.FindFirstValue("UserId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    public sealed class SaveResultRequest
    {
        [Range(1, int.MaxValue)]
        public int EnrolmentId { get; set; }
        [Required]
        public TimeSpan Time { get; set; }
    }
}

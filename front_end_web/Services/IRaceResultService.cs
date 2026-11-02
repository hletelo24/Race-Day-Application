using front_end_web.Models.Results;

namespace front_end_web.Services;

public interface IRaceResultService
{
    Task<List<RaceResultViewModel>> GetEventResultsAsync(int eventId, string token, CancellationToken ct = default);
    Task<(bool Success, string Message)> SaveResultAsync(int enrolmentId, string timeText, string token, CancellationToken ct = default);
    Task<List<ParticipantRaceResultViewModel>> GetMyResultsAsync(string token, CancellationToken ct = default);
}

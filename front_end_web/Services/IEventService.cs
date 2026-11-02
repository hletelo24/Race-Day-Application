using front_end_web.Models.Events;

namespace front_end_web.Services
{
    public interface IEventService
    {
        Task<List<EventViewModel>> GetEventsAsync(string token, CancellationToken cancellationToken = default);
        Task<EventViewModel?> GetEventAsync(int id, string token, CancellationToken cancellationToken = default);
        Task<List<CategoryOptionViewModel>> GetCategoriesAsync(string token, CancellationToken cancellationToken = default);
        Task<List<RouteOptionViewModel>> GetRoutesAsync(string token, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> CreateEventAsync(EventViewModel model, string token, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> UpdateEventAsync(int id, EventViewModel model, string token, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> DeleteEventAsync(int id, string token, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> EnrolAsync(EnrolEventViewModel model, string token, CancellationToken cancellationToken = default);
        Task<List<ParticipantEnrolmentViewModel>> GetMyEnrolmentsAsync(string token, CancellationToken cancellationToken = default);
    }
}

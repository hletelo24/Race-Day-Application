using front_end_web.Models.Organiser;

namespace front_end_web.Services;

public interface IOrganiserService
{
    Task<List<OrganiserEventViewModel>> GetEventsAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<List<OrganiserEnrolmentViewModel>> GetEnrolmentsAsync(
        string token,
        CancellationToken cancellationToken = default);
}
using front_end_web.Models.Organiser;

namespace front_end_web.Services;

public interface ICategoryService
{
    Task<List<CategoryViewModel>> GetAllAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<CategoryViewModel?> GetByIdAsync(
        int id,
        string token,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> CreateAsync(
        CategoryViewModel model,
        string token,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> UpdateAsync(
        int id,
        CategoryViewModel model,
        string token,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteAsync(
        int id,
        string token,
        CancellationToken cancellationToken = default);
}
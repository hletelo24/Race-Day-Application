using front_end_web.Models.Profile;

namespace front_end_web.Services
{
    public interface IProfileService
    {
        Task<ProfileViewModel?> GetMyProfileAsync(
            string token,
            CancellationToken ct = default);

        Task<(bool Success, string Message, string? ImageUrl)>
            UploadProfileImageAsync(
                IFormFile image,
                string token,
                CancellationToken ct = default);
    }
}
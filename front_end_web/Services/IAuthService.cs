using front_end_web.Models.Auth;

namespace front_end_web.Services;

public interface IAuthService
{
    Task<AuthServiceResult<ApiAuthResponse>> LoginAsync(
        LoginViewModel model,
        CancellationToken cancellationToken = default);

    Task<AuthServiceResult<ApiAuthResponse>> RegisterAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default);
}
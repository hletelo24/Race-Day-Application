using front_end_web.Models.Auth;
using front_end_web.Services;
using Microsoft.AspNetCore.Mvc;

namespace front_end_web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (IsLoggedIn())
            return RedirectByRole();

        ViewData["ReturnUrl"] = returnUrl;

        return View(new LoginViewModel());
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (IsLoggedIn())
            return RedirectByRole();

        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.LoginAsync(
            model, cancellationToken);

        if (!result.Success || result.Data?.User == null)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Message);

            return View(model);
        }

        var auth = result.Data;
        var user = auth.User!;

        // Start a fresh session after successful authentication.
        HttpContext.Session.Clear();

        HttpContext.Session.SetString(
            "JwtToken", auth.Token!);

        HttpContext.Session.SetString(
            "UserId", user.UserId.ToString());

        HttpContext.Session.SetString(
            "UserRole", user.Role);

        HttpContext.Session.SetString(
            "UserName",
            $"{user.FirstName} {user.LastName}".Trim());

        HttpContext.Session.SetString(
            "UserEmail", user.EmailAddress);

        if (!string.IsNullOrWhiteSpace(user.ImageUrl))
        {
            HttpContext.Session.SetString(
                "UserImageUrl", user.ImageUrl);
        }

        _logger.LogInformation(
            "User {UserId} authenticated with role {Role}.",
            user.UserId, user.Role);

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectByRole();
    }

    // GET: /Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn())
            return RedirectByRole();

        return View(new RegisterViewModel());
    }

    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (IsLoggedIn())
            return RedirectByRole();

        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.RegisterAsync(
            model, cancellationToken);

        if (!result.Success)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Message);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Your account was created successfully. Please sign in.";

        return RedirectToAction(nameof(Login));
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Home");
    }

    // GET: /Auth/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrWhiteSpace(
            HttpContext.Session.GetString("UserId"));
    }

    private IActionResult RedirectByRole()
    {
        var role = HttpContext.Session.GetString("UserRole");

        return role switch
        {
            "Organiser" => RedirectToAction(
                "Dashboard", "Organiser"),

            "Participant" => RedirectToAction(
                "Index", "Home"),

            _ => RedirectToAction("Index", "Home")
        };
    }
}
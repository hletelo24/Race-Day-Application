using front_end_web.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("RaceDayApi", client =>
{
    var apiBaseUrl =
        builder.Configuration["ApiSettings:BaseUrl"];

    if (!Uri.TryCreate(
            apiBaseUrl,
            UriKind.Absolute,
            out var apiUri) ||
        (apiUri.Scheme != Uri.UriSchemeHttps &&
         apiUri.Scheme != Uri.UriSchemeHttp))
    {
        throw new InvalidOperationException(
            "Configure a valid ApiSettings:BaseUrl in appsettings.json.");
    }

    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddScoped<IProfileService, ProfileService>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IOrganiserService, OrganiserService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
using CardGameEngine.Api.Data;
using CardGameEngine.Api.Hubs;
using CardGameEngine.Api.Services;
using CardGameEngine.Engine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Game services (singletons for in-memory state)
builder.Services.AddSingleton<GameDefinitionService>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddSingleton<RuleEngine>();
builder.Services.AddSingleton<StateProjector>();
builder.Services.AddSingleton<MatchConnectionRegistry>();
builder.Services.AddSingleton<BotService>();
builder.Services.AddSingleton<CampaignService>();
builder.Services.AddSingleton<DeckService>();
builder.Services.AddScoped<IAppEmailSender, SmtpEmailSender>();

// ---- Accounts: EF Core + SQLite + ASP.NET Core Identity ----
// The DB file lives beside the "definitions"/"profiles"/"decks" data folders (one level above
// wherever definitions/ resolves to), not inside the publish/output dir, so it survives
// redeploys the same way CampaignService's profiles/ and DeckService's decks/ already do
// (deploy.ps1 only overwrites the app binaries + definitions/, never sibling runtime data).
var dbPath = ResolveDbPath(builder.Environment.ContentRootPath);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    // This is an API consumed by an SPA — never redirect to a Razor login page that doesn't
    // exist. Return plain status codes the frontend can branch on instead.
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

var authBuilder = builder.Services.AddAuthentication();
// Only register a provider once real credentials exist. RemoteAuthenticationHandler checks
// on EVERY request (not just a challenge) whether that request is its own OAuth callback, so
// an empty ClientId/Secret doesn't fail lazily on first challenge — it throws on every single
// request in the app. Martien hasn't created the Google/Facebook app registrations yet, so
// both stay unregistered until vault credentials land; GET /api/account/providers already
// reports which are live so the frontend hides missing buttons instead of offering a dead link.
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

builder.Services.AddAuthorization();

// CORS - allow frontend dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Behind the IIS/ARR reverse proxy: trust X-Forwarded-Proto/Host so request-derived
// URLs (e.g. emailed confirmation links) carry the public https origin, not localhost.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                     | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
});

// Greenfield DB — Migrate() only ever creates tables here, never alters/drops existing data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Account-level Admin role, seeded from config (Admin:Emails). Admin accounts may use
    // the lobby's admin mode (full card pool, no deck limits); everyone else is refused
    // server-side. Config-driven so admin status survives a fresh database.
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var adminEmail in app.Configuration.GetSection("Admin:Emails").Get<string[]>() ?? [])
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            app.Logger.LogInformation("Granted Admin role to {Email}", adminEmail);
        }
    }
}

// Support running at a subpath (e.g. /townwars) behind a reverse proxy.
// Set ASPNETCORE_PATHBASE env var on the server; has no effect when empty.
var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE") ?? "";
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Serve static frontend files in production
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

// Fallback for SPA routing
app.MapFallbackToFile("index.html");

app.Run();

static string ResolveDbPath(string contentRoot)
{
    // Mirrors GameDefinitionService's own search so the DB ends up next to profiles/decks
    // (one level above "definitions") in both dev and the deployed layout. Duplicated rather
    // than reused because this needs to run before the DI container that owns
    // GameDefinitionService is built.
    var searchPaths = new[]
    {
        Path.Combine(contentRoot, "definitions"),
        Path.Combine(contentRoot, "..", "..", "..", "definitions"),
        Path.Combine(contentRoot, "..", "..", "definitions"),
        Path.Combine(AppContext.BaseDirectory, "definitions"),
    };

    var definitionsPath = searchPaths
        .Select(Path.GetFullPath)
        .FirstOrDefault(Directory.Exists) ?? contentRoot;

    return Path.GetFullPath(Path.Combine(definitionsPath, "..", "townwars.db"));
}

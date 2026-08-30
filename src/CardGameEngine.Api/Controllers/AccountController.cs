using System.Security.Claims;
using System.Text;
using CardGameEngine.Api.Data;
using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace CardGameEngine.Api.Controllers;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? DisplayName { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAppEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAppEmailSender emailSender,
        IConfiguration config,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
    }

    private string FrontendBaseUrl =>
        (_config["Frontend:BaseUrl"] ?? "http://localhost:5173/").TrimEnd('/') + "/";

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { errors = new[] { "Email and password are required" } });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? request.Email.Split('@')[0]
                : request.DisplayName.Trim(),
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await SendConfirmationEmailAsync(user);

        return Ok(new { message = "Registered. Check your email to confirm your account before logging in." });
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = $"{FrontendBaseUrl}confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(encodedToken)}";

        var html = $"""
            <p>Welcome to Town Wars!</p>
            <p>Confirm your account to start playing:</p>
            <p><a href="{link}">{link}</a></p>
            <p>This link expires and can only be used once.</p>
            """;

        await _emailSender.SendAsync(user.Email!, "Confirm your Town Wars account", html);
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "Missing confirmation parameters" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return BadRequest(new { error = "Invalid confirmation link" });

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "Invalid confirmation link" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
            return BadRequest(new { error = "Confirmation link is invalid or has expired" });

        return Ok(new { message = "Email confirmed. You can now log in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { error = "Invalid email or password" });

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true);

        if (result.IsNotAllowed)
            return StatusCode(403, new { error = "Confirm your email before logging in" });
        if (result.IsLockedOut)
            return StatusCode(423, new { error = "Account locked — too many failed attempts. Try again later." });
        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid email or password" });

        return Ok(await MeResponse(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Ok(new { authenticated = false });

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Ok(new { authenticated = false });

        return Ok(await MeResponse(user));
    }

    private async Task<object> MeResponse(ApplicationUser user) => new
    {
        authenticated = true,
        userId = user.Id,
        email = user.Email,
        displayName = user.DisplayName,
    };

    /// <summary>Which OAuth providers are actually configured, so the frontend can hide buttons
    /// for providers Martien hasn't registered app credentials for yet.</summary>
    [HttpGet("providers")]
    public IActionResult Providers()
    {
        bool configured(string key) => !string.IsNullOrWhiteSpace(_config[key]);
        return Ok(new
        {
            google = configured("Authentication:Google:ClientId") && configured("Authentication:Google:ClientSecret"),
            facebook = configured("Authentication:Facebook:AppId") && configured("Authentication:Facebook:AppSecret"),
        });
    }

    [HttpGet("login/google")]
    public IActionResult LoginGoogle([FromQuery] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(_config["Authentication:Google:ClientId"]))
            return NotFound(new { error = "Google sign-in is not configured yet" });
        var redirectUrl = Url.Action(nameof(ExternalCallback), "Account", new { returnUrl })!;
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("login/facebook")]
    public IActionResult LoginFacebook([FromQuery] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(_config["Authentication:Facebook:AppId"]))
            return NotFound(new { error = "Facebook sign-in is not configured yet" });
        var redirectUrl = Url.Action(nameof(ExternalCallback), "Account", new { returnUrl })!;
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(FacebookDefaults.AuthenticationScheme, redirectUrl);
        return Challenge(properties, FacebookDefaults.AuthenticationScheme);
    }

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback([FromQuery] string? returnUrl)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl) ? FrontendBaseUrl : returnUrl;

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return Redirect(AppendQuery(target, "authError", "external_login_failed"));

        // Already-linked account: sign in directly.
        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (signInResult.Succeeded)
            return Redirect(target);

        // First time seeing this provider identity: create (or attach to) an account.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return Redirect(AppendQuery(target, "authError", "no_email_from_provider"));

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                // The provider already verified this address — no confirmation email needed.
                EmailConfirmed = true,
                DisplayName = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name,
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("External sign-in could not create a local account for {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return Redirect(AppendQuery(target, "authError", "account_creation_failed"));
            }
        }
        else if (!user.EmailConfirmed)
        {
            // Provider-verified email is at least as trustworthy as our own confirmation link.
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
        {
            _logger.LogWarning("Could not link {Provider} login for {Email}: {Errors}",
                info.LoginProvider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            return Redirect(AppendQuery(target, "authError", "link_failed"));
        }

        await _signInManager.SignInAsync(user, isPersistent: true);
        return Redirect(target);
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}{key}={Uri.EscapeDataString(value)}";
    }
}

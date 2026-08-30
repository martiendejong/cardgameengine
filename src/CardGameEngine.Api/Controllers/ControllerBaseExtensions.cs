using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

public static class ControllerBaseExtensions
{
    /// <summary>The authenticated account id — never null on an [Authorize]-gated action.</summary>
    public static string UserId(this ControllerBase controller) =>
        controller.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}

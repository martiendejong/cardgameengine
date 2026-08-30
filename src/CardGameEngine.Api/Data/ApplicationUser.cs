using Microsoft.AspNetCore.Identity;

namespace CardGameEngine.Api.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}

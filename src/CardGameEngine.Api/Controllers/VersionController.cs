using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

/// <summary>
/// Reports the deploy version of the running instance. Backed by the repo-root VERSION file via
/// Directory.Build.props (which sets &lt;Version&gt; for every project), so this reflects whatever was
/// actually published — not the source checkout, which is what makes it useful for catching a
/// partial/stale deploy that a repo-only check (like JengoAGI's static scan) can't see.
/// </summary>
[ApiController]
[Route("api/version")]
public class VersionController : ControllerBase
{
    private static readonly Assembly EntryAssembly = Assembly.GetExecutingAssembly();

    private static readonly string Version =
        EntryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? EntryAssembly.GetName().Version?.ToString()
        ?? "unknown";

    private static readonly DateTime? BuildTimeUtc = TryGetBuildTimeUtc();

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        Version,
        BuildTimeUtc,
    });

    private static DateTime? TryGetBuildTimeUtc()
    {
        try
        {
            var location = EntryAssembly.Location;
            return string.IsNullOrEmpty(location) ? null : System.IO.File.GetLastWriteTimeUtc(location);
        }
        catch
        {
            return null;
        }
    }
}

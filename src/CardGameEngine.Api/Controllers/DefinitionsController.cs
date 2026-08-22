using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

[ApiController]
[Route("api/definitions")]
public class DefinitionsController : ControllerBase
{
    private readonly GameDefinitionService _definitionService;

    public DefinitionsController(GameDefinitionService definitionService)
    {
        _definitionService = definitionService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var defs = _definitionService.GetAll().Select(d => new
        {
            d.Id,
            d.Name,
            d.Version
        });
        return Ok(defs);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var def = _definitionService.GetById(id);
        if (def == null)
            return NotFound($"Game definition '{id}' not found");
        Response.Headers["Cache-Control"] = "no-store";
        return Ok(def);
    }
}

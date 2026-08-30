using CardGameEngine.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardGameEngine.Api.Controllers;

public class SaveDeckRequest
{
    public string GameId { get; set; } = "town-tcg";
    public string Name { get; set; } = "";
    public string? HqId { get; set; }
    public string? HeroId { get; set; }
    public Dictionary<string, int> Cards { get; set; } = new();
}

[ApiController]
[Route("api/decks")]
[Authorize]
public class DecksController : ControllerBase
{
    private readonly DeckService _decks;

    public DecksController(DeckService decks)
    {
        _decks = decks;
    }

    /// <summary>Every deck saved under the logged-in account, for the given game.</summary>
    [HttpGet]
    public IActionResult List([FromQuery] string gameId = "town-tcg")
    {
        return Ok(new { decks = _decks.ListDecks(this.UserId(), gameId) });
    }

    [HttpPost]
    public IActionResult Create([FromBody] SaveDeckRequest request)
    {
        var (deck, error) = _decks.SaveDeck(this.UserId(), request.GameId, null, request.Name, request.HqId, request.HeroId, request.Cards);
        if (deck == null) return BadRequest(error);
        return Ok(deck);
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id, [FromBody] SaveDeckRequest request)
    {
        var (deck, error) = _decks.SaveDeck(this.UserId(), request.GameId, id, request.Name, request.HqId, request.HeroId, request.Cards);
        if (deck == null) return BadRequest(error);
        return Ok(deck);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        if (!_decks.DeleteDeck(this.UserId(), id))
            return NotFound($"Deck '{id}' not found");
        return NoContent();
    }
}

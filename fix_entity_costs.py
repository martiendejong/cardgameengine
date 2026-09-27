"""Remove entity-scoped resources (dp, death_power, poison) from playCosts.
These resources are stored on units/entities, not in the bank, so they
cannot be paid from the standard bank pool that CreateMatch stocks.
"""
import json, pathlib

GAME_JSON = pathlib.Path(__file__).parent / "definitions/town-tcg/game.json"
ENTITY_SCOPED = {"dp", "death_power", "poison"}

with open(GAME_JSON, encoding="utf-8") as f:
    game = json.load(f)

changed = []
for card in game["cards"]:
    pc = card.get("playCosts", {})
    removed = {k: v for k, v in pc.items() if k in ENTITY_SCOPED}
    if removed:
        for k in removed:
            del pc[k]
        changed.append((card["id"], removed))

for cid, rem in changed:
    print(f"  removed from {cid}: {rem}")

with open(GAME_JSON, "w", encoding="utf-8") as f:
    json.dump(game, f, indent=2, ensure_ascii=False)
print(f"Written ({len(changed)} cards updated)")

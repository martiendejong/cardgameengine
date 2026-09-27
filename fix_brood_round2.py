"""
Round 2 fixes for Brood cards:
- Move 9 apex spell abilities to onPlay (preserving existing choices from round 1 fix)
- Fix flesh-weaver ability cost (remove scope:player biomass cost, keep only tap)
"""
import json, pathlib

GAME_JSON = pathlib.Path(__file__).parent / "definitions/town-tcg/game.json"

APEX_SPELL_IDS = [
    "apex-mass-sacrifice", "apex-evolve-surge", "apex-biomass-feast",
    "apex-spore-burst", "apex-enzyme-flood", "apex-apex-signal",
    "apex-hive-pulse", "apex-scent-trail", "apex-molt",
]

with open(GAME_JSON, encoding="utf-8") as f:
    game = json.load(f)

cards_by_id = {c['id']: c for c in game['cards']}
changed = []

for cid in APEX_SPELL_IDS:
    card = cards_by_id.get(cid)
    if not card or not card.get('abilities'):
        continue
    first = card['abilities'][0]
    on_play = {"id": first.get("id", f"{cid}-cast"), "name": first.get("name", "Cast")}
    if "effects" in first:
        on_play["effects"] = first["effects"]
    if "conditions" in first:
        on_play["conditions"] = first["conditions"]
    if "choice" in first:
        on_play["choice"] = first["choice"]
    if "costs" in first:
        on_play["costs"] = first["costs"]
    card["onPlay"] = on_play
    remaining = card['abilities'][1:]
    if remaining:
        card['abilities'] = remaining
    else:
        card.pop('abilities', None)
    changed.append(f"  [apex spell->onPlay] {cid}")

# Fix flesh-weaver: remove scope:player biomass cost from weave-flesh ability
fw = cards_by_id.get('brood-flesh-weaver')
if fw:
    for ab in fw.get('abilities', []):
        if ab.get('id') == 'weave-flesh':
            old_costs = ab.get('costs', [])
            new_costs = [c for c in old_costs
                         if not (c.get('type') == 'resource' and c.get('scope') == 'player' and c.get('resourceId') == 'biomass')]
            if len(new_costs) != len(old_costs):
                ab['costs'] = new_costs
                changed.append("  [flesh-weaver cost fix] removed scope:player biomass cost")

for msg in changed:
    print(msg)

with open(GAME_JSON, "w", encoding="utf-8") as f:
    json.dump(game, f, indent=2, ensure_ascii=False)
print(f"Written ({len(changed)} changes)")

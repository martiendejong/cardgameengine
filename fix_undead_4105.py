"""
Fix Undead card defects for task 4105.

Defects:
1. 5 spells: effects in abilities[] → move to onPlay
2. 7 abilities: target-scoped effects with no choice → add choice
3. 23 effects: gain_resource scope:player on entity resources → gain_bank_resource scope:self
4. 1 effect: targetZone unknown key on plague-rot-summoner → remove
5. 1 effect: cost_discount resourceId:gold on lich-herald → change to corpses
6. 42 cards: gold pricing → corpses pricing (1:1)
7. 36 cards: housingCost with no housing provider → remove

Run: python fix_undead_4105.py [--dry-run]
"""
import json, sys, pathlib

GAME_JSON = pathlib.Path(__file__).parent / "definitions/town-tcg/game.json"
DRY_RUN = "--dry-run" in sys.argv

# ── Spells: move ability → onPlay ─────────────────────────────────────────
# For each, optionally add a choice block if the onPlay has target-scoped effects
SPELL_MOVE_TO_ONPLAY = {
    "undead-unholy-blight": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
    "undead-bone-shatter": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
    "undead-death-coil": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
    "undead-soul-harvest": None,  # no target-scoped effects, no choice needed
    "undead-plague-wind": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
}

# ── Ability choice repairs ────────────────────────────────────────────────
# { card_id: { ability_id: choice_definition } }
ABILITY_CHOICE_REPAIRS = {
    "undead-wraith": {
        "wraith-life-drain": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "undead-banshee": {
        "banshee-wail": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "undead-death-knight": {
        "death-knight-necrotic-strike": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "undead-bone-archer": {
        "bone-archer-poisoned-arrow": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "undead-wight": {
        "wight-chill-touch": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "undead-shadow-revenant": {
        "shadow-revenant-consume-soul": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "undead-plague-pit": {
        "plague-pit-spread": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
}


def move_spell_ability_to_onplay(card, choice):
    abilities = card.get("abilities", [])
    if not abilities:
        return False
    first = abilities[0]
    on_play = {
        "id": first.get("id", f"{card['id']}-cast"),
        "name": first.get("name", "Cast"),
    }
    if "effects" in first:
        on_play["effects"] = first["effects"]
    if "conditions" in first:
        on_play["conditions"] = first["conditions"]
    if choice is not None and "choice" not in first:
        on_play["choice"] = choice
    card["onPlay"] = on_play
    remaining = abilities[1:]
    if remaining:
        card["abilities"] = remaining
    else:
        card.pop("abilities", None)
    return True


def add_ability_choice(card, ability_id, choice):
    for ab in card.get("abilities", []):
        if ab.get("id") == ability_id and "choice" not in ab:
            ab["choice"] = choice
            return True
    return False


def fix_gain_resource_scope(card):
    """Convert gain_resource scope:player on entity resources to gain_bank_resource scope:self."""
    changed = False
    entity_resources = {"corpses", "dp", "death_power", "poison"}

    def fix_effects(effects):
        nonlocal changed
        for eff in effects:
            if (eff.get("type") == "gain_resource"
                    and eff.get("scope") == "player"
                    and eff.get("resourceId") in entity_resources):
                eff["type"] = "gain_bank_resource"
                eff["scope"] = "self"
                changed = True

    for ab in card.get("abilities", []):
        fix_effects(ab.get("effects", []))
    for tr in card.get("triggers", []):
        fix_effects(tr.get("effects", []))
    if card.get("onPlay"):
        fix_effects(card["onPlay"].get("effects", []))

    # Also fix cost_discount that still references gold (lich-herald)
    for ab in card.get("abilities", []):
        for eff in ab.get("effects", []):
            if eff.get("type") == "cost_discount" and eff.get("resourceId") == "gold":
                eff["resourceId"] = "corpses"
                changed = True

    return changed


def fix_unknown_keys(card):
    """Remove targetZone (and other unknown keys) from effects."""
    changed = False
    for ab in card.get("abilities", []):
        for eff in ab.get("effects", []):
            if "targetZone" in eff:
                del eff["targetZone"]
                changed = True
    return changed


def reprice_to_corpses(card):
    """Convert gold pricing to corpses (1:1)."""
    changed = False
    if "playCosts" in card:
        costs = card["playCosts"]
        if "gold" in costs and "corpses" not in costs:
            old_gold = costs.pop("gold")
            costs["corpses"] = old_gold
            changed = True
        elif "gold" in costs:
            costs.pop("gold")
            changed = True
    if "playCost" in card:
        resource = card.get("playCostResource", "gold")
        if resource == "gold" or resource is None:
            old_val = card.pop("playCost")
            card.pop("playCostResource", None)
            card["playCosts"] = {"corpses": old_val}
            changed = True
    return changed


# Cards whose deck provides housing — keep their housingCost as-is
# (plague-cathedral provides 8, undead-bone-citadel provides 8)
KEEP_HOUSING = {
    # Batch-1 Legion deck units
    "undead-carrion-ghoul", "undead-cairn-sentinel", "undead-pyre-zealot", "undead-barrow-sentinel",
    "undead-grave-vulture", "undead-marrow-leech", "undead-ravenous-revenant", "undead-withering-shade",
    "undead-grave-broker", "undead-bone-drover", "undead-hexbolt-acolyte", "undead-dirge-bearer",
    # Plague deck units (plague-cathedral provides 8 housing)
    "plague-bone-archer", "plague-festering-brute", "plague-miasma-wraith", "plague-siege-catapult",
    "plague-rot-summoner", "plague-venom-stalker", "plague-hollow-sovereign",
}


def fix_gravekeeper_add_progress(card):
    """gravekeeper-bury: add_progress has no scope/choice — add both."""
    changed = False
    for ab in card.get("abilities", []):
        if ab.get("id") == "gravekeeper-bury":
            for eff in ab.get("effects", []):
                if eff.get("type") == "add_progress" and "scope" not in eff:
                    eff["scope"] = "target"
                    changed = True
            if "choice" not in ab:
                ab["choice"] = {
                    "type": "entity", "controller": "self", "objectType": "building",
                    "requireUnderConstruction": True, "min": 1, "max": 1
                }
                changed = True
    return changed


def fix_dual_resource_costs(card):
    """Remove dp from death-coil playCosts (entity-scoped, not available in standard bank)."""
    changed = False
    if "playCosts" in card and "dp" in card["playCosts"] and "corpses" in card["playCosts"]:
        del card["playCosts"]["dp"]
        changed = True
    return changed


def remove_housing_cost(card):
    if "housingCost" in card and card["id"] not in KEEP_HOUSING:
        del card["housingCost"]
        return True
    return False


def main():
    with open(GAME_JSON, encoding="utf-8") as f:
        game = json.load(f)

    cards = game.get("cards", [])
    undead_cards = [c for c in cards if c.get("faction") == "undead"]
    print(f"Found {len(undead_cards)} undead faction cards")

    stats = {
        "spells_moved": 0,
        "choices_added": 0,
        "resource_scope_fixed": 0,
        "unknown_keys_removed": 0,
        "repriced": 0,
        "housing_removed": 0,
        "special_fixes": 0,
    }

    for card in undead_cards:
        cid = card["id"]

        # 1. Move spell abilities to onPlay
        if cid in SPELL_MOVE_TO_ONPLAY:
            choice = SPELL_MOVE_TO_ONPLAY[cid]
            if move_spell_ability_to_onplay(card, choice):
                stats["spells_moved"] += 1
                print(f"  [spell->onPlay] {cid}")
                if choice:
                    stats["choices_added"] += 1

        # 2. Add choice to abilities
        if cid in ABILITY_CHOICE_REPAIRS:
            for ability_id, choice in ABILITY_CHOICE_REPAIRS[cid].items():
                if add_ability_choice(card, ability_id, choice):
                    stats["choices_added"] += 1
                    print(f"  [ability choice] {cid}/{ability_id}")

        # 3. Fix gain_resource scope + cost_discount gold
        if fix_gain_resource_scope(card):
            stats["resource_scope_fixed"] += 1
            print(f"  [resource scope] {cid}")

        # 4. Remove unknown keys
        if fix_unknown_keys(card):
            stats["unknown_keys_removed"] += 1
            print(f"  [unknown keys] {cid}")

        # 5a. Special card fixes (gravekeeper add_progress, death-coil dp cost)
        if cid == "undead-gravekeeper":
            if fix_gravekeeper_add_progress(card):
                stats["special_fixes"] += 1
                print(f"  [gravekeeper add_progress] {cid}")
        if cid == "undead-death-coil":
            if fix_dual_resource_costs(card):
                stats["special_fixes"] += 1
                print(f"  [death-coil remove dp] {cid}")

        # 5. Reprice gold → corpses
        if reprice_to_corpses(card):
            stats["repriced"] += 1
            new_cost = card.get("playCosts", {}).get("corpses", "?")
            print(f"  [reprice] {cid}: -> corpses {new_cost}")

        # 6. Remove housing cost
        if remove_housing_cost(card):
            stats["housing_removed"] += 1
            print(f"  [housing] {cid}")

    print(f"\nStats: {stats}")

    if not DRY_RUN:
        with open(GAME_JSON, "w", encoding="utf-8") as f:
            json.dump(game, f, indent=2, ensure_ascii=False)
        print(f"Written: {GAME_JSON}")
    else:
        print("(dry-run, not written)")


if __name__ == "__main__":
    main()

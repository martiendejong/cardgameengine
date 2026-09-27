"""
Fix Brood card defects for task 4121.

Defects:
1. 5 spells: effects in abilities[] -> move to onPlay
2. 3 unit abilities: target-scoped effects with no choice -> add choice
3. 21 effects: gain_resource scope:player on biomass -> gain_bank_resource scope:self
4. Apex cards: invalid 'target' key in effects -> fix scopes/choices/AoE types
5. 33 cards: gold pricing -> biomass pricing (1:1)

Run: python fix_brood_4121.py [--dry-run]
"""
import json, sys, pathlib

GAME_JSON = pathlib.Path(__file__).parent / "definitions/town-tcg/game.json"
DRY_RUN = "--dry-run" in sys.argv

# Spells: move ability -> onPlay
# { card_id: choice_definition or None }
SPELL_MOVE_TO_ONPLAY = {
    "brood-biomass-surge": None,        # gain_resource only (no target scope)
    "brood-consume": {                  # destroy scope:target -> choice opponent unit
        "type": "entity", "controller": "opponent", "objectType": "unit", "min": 1, "max": 1
    },
    "brood-evolve-sentinel": None,      # buff_tag_until_end_of_turn (applies to all soldiers, no target)
    "brood-mass-sacrifice": None,       # gain_resource + draw_cards (no target scope)
    "brood-chitin-fortification": {     # modify_property scope:target -> choice friendly unit
        "type": "entity", "controller": "self", "objectType": "unit", "min": 1, "max": 1
    },
}

# Unit abilities missing choice
ABILITY_CHOICE_REPAIRS = {
    "brood-acid-spewer": {
        "acid-spit": {
            "type": "entity", "controller": "opponent", "objectType": "unit", "min": 1, "max": 1
        }
    },
    "brood-scythe-hunter": {
        "scythe-lunge": {
            "type": "entity", "controller": "opponent", "objectType": "unit", "min": 1, "max": 1
        }
    },
    "brood-flesh-weaver": {
        "weave-flesh": {
            "type": "entity", "controller": "self", "objectType": "unit", "min": 1, "max": 1
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


def fix_biomass_scope(card):
    """Convert gain_resource scope:player on biomass to gain_bank_resource scope:self."""
    changed = False

    def fix_effects(effects):
        nonlocal changed
        for eff in effects:
            if (eff.get("type") == "gain_resource"
                    and eff.get("scope") == "player"
                    and eff.get("resourceId") == "biomass"):
                eff["type"] = "gain_bank_resource"
                eff["scope"] = "self"
                changed = True

    for ab in card.get("abilities", []):
        fix_effects(ab.get("effects", []))
    for tr in card.get("triggers", []):
        fix_effects(tr.get("effects", []))
    if card.get("onPlay"):
        fix_effects(card["onPlay"].get("effects", []))
    return changed


def fix_apex_target_key(card):
    """Fix apex-* cards that use invalid 'target' key in effects."""
    changed = False
    cid = card.get("id", "")
    if not cid.startswith("apex-"):
        return False

    def fix_effects(effects, ability_obj):
        nonlocal changed
        needs_choice = False
        choice_def = None

        for eff in effects:
            target_val = eff.pop("target", None)
            if target_val is None:
                continue
            changed = True

            if target_val == "self":
                eff["scope"] = "self"
            elif target_val in ("all_enemy_units",):
                # AoE: convert to AoE effect types
                if eff["type"] in ("direct_damage", "damage"):
                    eff["type"] = "damage_enemy_units"
                elif eff["type"] == "freeze":
                    eff["type"] = "tap_enemy_units"
                elif eff["type"] in ("modify_property",):
                    eff["type"] = "modify_property_enemy_units"
                elif eff["type"] in ("reveal", "reveal_attachments"):
                    # No AoE reveal: target single enemy unit
                    eff["scope"] = "target"
                    needs_choice = True
                    choice_def = {
                        "type": "entity", "controller": "opponent",
                        "objectType": "unit", "min": 1, "max": 1
                    }
            elif target_val in ("all_friendly_units",):
                if eff["type"] == "heal":
                    eff["type"] = "heal_own_units"
                elif eff["type"] in ("direct_damage", "damage"):
                    eff["type"] = "damage_all_units"
                else:
                    eff["scope"] = "self"
            elif target_val in ("target_enemy_unit", "target_enemy_building"):
                eff["scope"] = "target"
                needs_choice = True
                if target_val == "target_enemy_building":
                    choice_def = {
                        "type": "entity", "controller": "opponent",
                        "objectType": "building", "min": 1, "max": 1
                    }
                else:
                    choice_def = {
                        "type": "entity", "controller": "opponent",
                        "objectType": "unit", "min": 1, "max": 1
                    }
            elif target_val in ("target_friendly_unit",):
                eff["scope"] = "target"
                needs_choice = True
                choice_def = {
                    "type": "entity", "controller": "self",
                    "objectType": "unit", "min": 1, "max": 1
                }
            else:
                # Unknown target: set scope=target + choice opponent unit
                eff["scope"] = "target"
                needs_choice = True
                choice_def = {
                    "type": "entity", "controller": "opponent",
                    "objectType": "unit", "min": 1, "max": 1
                }

        if needs_choice and choice_def and "choice" not in ability_obj:
            ability_obj["choice"] = choice_def
            changed = True

    for ab in card.get("abilities", []):
        fix_effects(ab.get("effects", []), ab)
    if card.get("onPlay"):
        fix_effects(card["onPlay"].get("effects", []), card["onPlay"])

    return changed


def reprice_to_biomass(card):
    """Convert gold pricing to biomass (1:1)."""
    changed = False
    if "playCosts" in card:
        costs = card["playCosts"]
        if "gold" in costs and "biomass" not in costs:
            old_gold = costs.pop("gold")
            costs["biomass"] = old_gold
            changed = True
        elif "gold" in costs:
            costs.pop("gold")
            changed = True
    if "playCost" in card:
        resource = card.get("playCostResource", "gold")
        if resource == "gold" or resource is None:
            old_val = card.pop("playCost")
            card.pop("playCostResource", None)
            card["playCosts"] = {"biomass": old_val}
            changed = True
    return changed


def is_brood_or_apex(card):
    return card.get("faction") == "brood" or card.get("id", "").startswith("apex-")


def main():
    with open(GAME_JSON, encoding="utf-8") as f:
        game = json.load(f)

    cards = game.get("cards", [])
    target_cards = [c for c in cards if is_brood_or_apex(c)]
    print(f"Found {sum(1 for c in cards if c.get('faction') == 'brood')} brood faction cards")
    print(f"Found {sum(1 for c in cards if c.get('id','').startswith('apex-'))} apex-* cards")

    stats = {
        "spells_moved": 0,
        "choices_added": 0,
        "biomass_scope_fixed": 0,
        "apex_target_fixed": 0,
        "repriced": 0,
    }

    for card in target_cards:
        cid = card["id"]

        # 1. Move spell abilities to onPlay
        if cid in SPELL_MOVE_TO_ONPLAY:
            choice = SPELL_MOVE_TO_ONPLAY[cid]
            if move_spell_ability_to_onplay(card, choice):
                stats["spells_moved"] += 1
                print(f"  [spell->onPlay] {cid}")
                if choice:
                    stats["choices_added"] += 1

        # 2. Add choice to unit abilities
        if cid in ABILITY_CHOICE_REPAIRS:
            for ability_id, choice in ABILITY_CHOICE_REPAIRS[cid].items():
                if add_ability_choice(card, ability_id, choice):
                    stats["choices_added"] += 1
                    print(f"  [ability choice] {cid}/{ability_id}")

        # 3. Fix biomass resource scope
        if fix_biomass_scope(card):
            stats["biomass_scope_fixed"] += 1
            print(f"  [biomass scope] {cid}")

        # 4. Fix apex target key
        if fix_apex_target_key(card):
            stats["apex_target_fixed"] += 1
            print(f"  [apex target] {cid}")

        # 5. Reprice gold -> biomass
        if reprice_to_biomass(card):
            stats["repriced"] += 1
            new_cost = card.get("playCosts", {}).get("biomass", "?")
            print(f"  [reprice] {cid}: -> biomass {new_cost}")

    print(f"\nStats: {stats}")

    if not DRY_RUN:
        with open(GAME_JSON, "w", encoding="utf-8") as f:
            json.dump(game, f, indent=2, ensure_ascii=False)
        print(f"Written: {GAME_JSON}")
    else:
        print("(dry-run, not written)")


if __name__ == "__main__":
    main()

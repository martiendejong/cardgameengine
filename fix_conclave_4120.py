"""
Fix Conclave card defects for task 4120.

Defects:
1. 9 spells: effects in abilities[] → move to onPlay
2. 2 equipment: missing slots → add slots
3. 15 target-scoped effects: no choice → add choice
4. 27 mana gains: gain_resource(scope:player) → gain_bank_resource(scope:self)
5. 55 cards: gold pricing → mana pricing

Run: python fix_conclave_4120.py [--dry-run]
"""
import json, copy, sys, pathlib

GAME_JSON = pathlib.Path(__file__).parent / "definitions/town-tcg/game.json"
DRY_RUN = "--dry-run" in sys.argv

# Spells that have effects in abilities[] instead of onPlay
SPELL_ABILITIES_TO_ONPLAY = {
    "conclave-arcane-surge",
    "conclave-mass-freeze",
    "conclave-spell-echo",
    "conclave-void-rift",
    "conclave-mana-drain",
    "conclave-greater-counterspell",
    "conclave-arcane-collapse",
    "conclave-ley-tap",
    "conclave-reveal-mind",
}

# Equipment cards missing slots
EQUIPMENT_MISSING_SLOTS = {
    "conclave-spellshard-staff": ["mainHand"],
    "conclave-focus-crystal": ["offHand"],
}

# Cards where target-scoped effects on specific abilities need a choice block.
# Format: { card_id: { ability_id: choice_definition } }
# The choice is "entity" targeting opponent units/buildings as appropriate.
TARGET_CHOICE_REPAIRS = {
    "conclave-storm-caller": {
        "storm-caller-bolt": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "conclave-aether-blade": {
        "aether-blade-surge": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "conclave-arcane-tower": {
        "arcane-tower-blast": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "conclave-spellshard-staff": {
        "spellshard-staff-empower": {
            "type": "entity", "controller": "self",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "conclave-senior-enchanter": {
        "senior-enchanter-reinforce": {
            "type": "entity", "controller": "self",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
}

# For spells whose onPlay has target-scoped effects, add choice to onPlay
ONPLAY_TARGET_CHOICES = {
    "conclave-arcane-surge": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
    "conclave-mass-freeze": None,  # all_enemy_units via scope:opponent+broadcast — no choice needed
    "conclave-void-rift": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
    "conclave-mana-drain": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
    "conclave-arcane-collapse": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 3
    },
    "conclave-reveal-mind": {
        "type": "entity", "controller": "opponent",
        "objectType": "unit", "min": 1, "max": 1
    },
}

# Mana cost guide: rough equivalence to gold costs
# Conclave mana economy: HQ base = 2/turn, buildings can add
GOLD_TO_MANA = {
    1: 1, 2: 2, 3: 3, 4: 4, 5: 5, 6: 6, 7: 7, 8: 8
}

def fix_mana_resource_effects(card):
    """Replace gain_resource(scope:player, mana) with gain_bank_resource(scope:self, mana).
    Also fix set_resource(scope:opponent, mana, -2) to drain_bank_resource."""
    changed = False
    for bucket in ["abilities", "triggers"]:
        for ab in card.get(bucket, []):
            for eff in ab.get("effects", []):
                if eff.get("type") == "gain_resource" and eff.get("scope") == "player" and eff.get("resourceId") == "mana":
                    eff["type"] = "gain_bank_resource"
                    eff["scope"] = "self"
                    changed = True
                elif eff.get("type") == "set_resource" and eff.get("scope") in ("player", "opponent") and eff.get("resourceId") == "mana":
                    # arcane-mana-siphon case: drain from opponent HQ bank
                    eff["type"] = "drain_bank_resource"
                    eff.pop("amount", None)
                    eff["drainAmount"] = 2
                    eff["scope"] = "opponent"
                    changed = True
    if "onPlay" in card:
        for eff in card["onPlay"].get("effects", []):
            if eff.get("type") == "gain_resource" and eff.get("scope") == "player" and eff.get("resourceId") == "mana":
                eff["type"] = "gain_bank_resource"
                eff["scope"] = "self"
                changed = True
    return changed

def reprice_to_mana(card):
    """Convert gold pricing to mana pricing."""
    changed = False
    old_cost = None
    if "playCost" in card and card.get("playCost", 0) > 0:
        old_cost = card.pop("playCost")
        card["playCosts"] = {"mana": GOLD_TO_MANA.get(old_cost, old_cost)}
        changed = True
    elif "playCosts" in card and "gold" in card["playCosts"] and "mana" not in card["playCosts"]:
        old_cost = card["playCosts"].pop("gold")
        if not card["playCosts"]:
            card["playCosts"] = {"mana": GOLD_TO_MANA.get(old_cost, old_cost)}
        else:
            card["playCosts"]["mana"] = GOLD_TO_MANA.get(old_cost, old_cost)
        changed = True
    return changed, old_cost

def move_spell_abilities_to_onplay(card):
    """For spells: move abilities[] content to onPlay."""
    abilities = card.get("abilities", [])
    if not abilities:
        return False
    # Take the first ability as onPlay (spells typically have one)
    first = abilities[0]
    # Build an onPlay definition
    on_play = {
        "id": first.get("id", f"{card['id']}-cast"),
        "name": first.get("name", "Cast"),
    }
    if "effects" in first:
        on_play["effects"] = first["effects"]
    if "choice" in first:
        on_play["choice"] = first["choice"]
    if "conditions" in first:
        on_play["conditions"] = first["conditions"]
    card["onPlay"] = on_play
    # Remove the abilities that are now on onPlay
    remaining = abilities[1:]
    if remaining:
        card["abilities"] = remaining
    else:
        card.pop("abilities", None)
    return True

def add_equipment_slots(card, slots):
    card["slots"] = slots
    if "attachTo" not in card:
        card["attachTo"] = "chooseCharacter"
    return True

def add_ability_choice(card, ability_id, choice):
    for ab in card.get("abilities", []):
        if ab.get("id") == ability_id and "choice" not in ab:
            ab["choice"] = choice
            return True
    return False

def main():
    with open(GAME_JSON, encoding="utf-8") as f:
        game = json.load(f)

    cards = game.get("cards", [])
    conclave_cards = [c for c in cards if c.get("faction") == "conclave"]
    print(f"Found {len(conclave_cards)} conclave cards")

    stats = {
        "spells_moved": 0,
        "equipment_slotted": 0,
        "choices_added": 0,
        "mana_resource_fixed": 0,
        "repriced": 0,
    }

    for card in conclave_cards:
        cid = card["id"]
        obj_type = card.get("objectType", "")

        # 1. Move spell abilities to onPlay
        if cid in SPELL_ABILITIES_TO_ONPLAY:
            if move_spell_abilities_to_onplay(card):
                stats["spells_moved"] += 1
                print(f"  [spell->onPlay] {cid}")
            # Also add choice to onPlay if needed
            choice = ONPLAY_TARGET_CHOICES.get(cid)
            if choice and "onPlay" in card and "choice" not in card["onPlay"]:
                # Check if onPlay has target-scoped effects
                has_target = any(
                    e.get("scope") in ("target", "host")
                    for e in card.get("onPlay", {}).get("effects", [])
                )
                if has_target:
                    card["onPlay"]["choice"] = choice
                    stats["choices_added"] += 1
                    print(f"  [onPlay choice] {cid}")

        # 2. Add slots to equipment
        if cid in EQUIPMENT_MISSING_SLOTS and "slots" not in card:
            add_equipment_slots(card, EQUIPMENT_MISSING_SLOTS[cid])
            stats["equipment_slotted"] += 1
            print(f"  [slots] {cid}")

        # 3. Add choice to abilities
        if cid in TARGET_CHOICE_REPAIRS:
            for ability_id, choice in TARGET_CHOICE_REPAIRS[cid].items():
                if add_ability_choice(card, ability_id, choice):
                    stats["choices_added"] += 1
                    print(f"  [ability choice] {cid}/{ability_id}")

        # 4. Fix mana resource scope
        if fix_mana_resource_effects(card):
            stats["mana_resource_fixed"] += 1
            print(f"  [mana scope] {cid}")

        # 5. Reprice to mana
        changed, old = reprice_to_mana(card)
        if changed:
            stats["repriced"] += 1
            new_cost = card.get("playCosts", {}).get("mana")
            print(f"  [reprice] {cid}: gold {old} -> mana {new_cost}")

    print(f"\nStats: {stats}")

    if not DRY_RUN:
        with open(GAME_JSON, "w", encoding="utf-8") as f:
            json.dump(game, f, indent=2, ensure_ascii=False)
        print(f"Written: {GAME_JSON}")
    else:
        print("(dry-run, not written)")

if __name__ == "__main__":
    main()

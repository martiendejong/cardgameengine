"""
Fix Machine card defects for task 4100.

Defects:
1. 12 tagged cards: target-scoped effects on abilities with no choice
2. 15 sentry-* cards: invalid keys (target/property) + all_enemy_units AoE
3. 64 of 66 cards: gold pricing -> energy pricing (1:1)

Run: python fix_machine_4100.py [--dry-run]
"""
import json, sys, pathlib

GAME_JSON = pathlib.Path(__file__).parent / "definitions/town-tcg/game.json"
DRY_RUN = "--dry-run" in sys.argv

# Tagged machine cards missing choice on their ability
# { card_id: { ability_id: choice_definition } }
TAGGED_CHOICE_REPAIRS = {
    "machine-arc-emitter": {
        "arc-emitter-discharge": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-proximity-mine-launcher": {
        "proximity-mine-launcher-deploy": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-chief-engineer": {
        "chief-engineer-supervise": {
            "type": "entity", "controller": "self",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-field-technician": {
        "field-technician-patch": {
            "type": "entity", "controller": "self",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-siege-operator": {
        "siege-operator-precision": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-repair-bay": {
        "repair-bay-restore": {
            "type": "entity", "controller": "self",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-overload-pulse": {
        "overload-pulse-cast": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-system-reboot": {
        "system-reboot-cast": {
            "type": "entity", "controller": "self",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-targeting-override": {
        "targeting-override-cast": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-sensor-array": {
        "sensor-array-scan": {
            "type": "entity", "controller": "opponent",
            "objectType": "unit", "min": 1, "max": 1
        }
    },
    "machine-demolition-expert": {
        "demolition-expert-detonate": {
            "type": "entity", "controller": "opponent",
            "objectType": "building", "min": 1, "max": 1
        }
    },
    # machine-overdrive-governor uses scope:host (attached hero), no choice needed
}


def fix_sentry_card(card):
    """Fix invalid keys in sentry-* cards."""
    cid = card["id"]
    changed = False

    def fix_effects(effects, ability_obj, ability_id):
        nonlocal changed
        needs_choice = False  # does this ability need a choice added?
        choice_def = None

        for eff in effects:
            target_val = eff.pop("target", None)
            # rename property -> propertyId
            if "property" in eff:
                eff["propertyId"] = eff.pop("property")
                changed = True

            if target_val is None:
                continue

            changed = True
            if target_val in ("all_enemy_units",):
                # AoE: convert to registered AoE effect types, no target needed
                if eff["type"] == "freeze":
                    eff["type"] = "tap_enemy_units"
                elif eff["type"] in ("direct_damage", "damage"):
                    eff["type"] = "damage_enemy_units"
                elif eff["type"] == "reveal_attachments":
                    # reveal_attachments on "all" -> convert to single building choice
                    eff["scope"] = "target"
                    needs_choice = True
                    choice_def = {
                        "type": "entity", "controller": "opponent",
                        "objectType": "building", "min": 1, "max": 1
                    }
                # other AoE types: just remove the invalid target key
            else:
                # single target: target_enemy_unit / target / random_enemy_unit / target_friendly_building
                eff["scope"] = "target"
                needs_choice = True
                if target_val in ("target_enemy_unit", "target", "random_enemy_unit"):
                    if eff["type"] in ("reveal", "reveal_attachments"):
                        # Reveal for sentry recon -> scan a building for spies
                        eff["type"] = "reveal_attachments"
                        choice_def = {
                            "type": "entity", "controller": "opponent",
                            "objectType": "building", "min": 1, "max": 1
                        }
                    else:
                        choice_def = {
                            "type": "entity", "controller": "opponent",
                            "objectType": "unit", "min": 1, "max": 1
                        }
                elif target_val == "target_friendly_building":
                    choice_def = {
                        "type": "entity", "controller": "self",
                        "objectType": "building", "min": 1, "max": 1
                    }
                else:
                    # modify_property with scope:target and property key
                    # controller depends on card context
                    if eff.get("scope") == "target":
                        # already handled above (scope already set)
                        pass
                    choice_def = {
                        "type": "entity", "controller": "self",
                        "objectType": "unit", "min": 1, "max": 1
                    }

        if needs_choice and choice_def and "choice" not in ability_obj:
            ability_obj["choice"] = choice_def
            changed = True

    # special: sentry-caster and sentry-overwatch-drone have scope:target + property key
    # fix those inline in abilities
    for ab in card.get("abilities", []):
        for eff in list(ab.get("effects", [])):
            if "property" in eff:
                eff["propertyId"] = eff.pop("property")
                changed = True
            # after rename, if scope is target and no choice
        has_target_scope = any(e.get("scope") == "target" or
                               e.get("target") is not None
                               for e in ab.get("effects", []))
        if has_target_scope:
            fix_effects(ab.get("effects", []), ab, ab.get("id", ""))

    if "onPlay" in card:
        on = card["onPlay"]
        for eff in list(on.get("effects", [])):
            if "property" in eff:
                eff["propertyId"] = eff.pop("property")
                changed = True
        has_target_scope = any(e.get("scope") == "target" or
                               e.get("target") is not None
                               for e in on.get("effects", []))
        if has_target_scope:
            fix_effects(on.get("effects", []), on, "onPlay")

    return changed


def reprice_to_energy(card):
    """Convert gold pricing to energy using playCostResource pattern (matching batch-1 style)."""
    changed = False
    # playCosts dict: remove gold, convert gold-only to energy-only
    if "playCosts" in card:
        costs = card["playCosts"]
        if "gold" in costs and "energy" not in costs:
            old_gold = costs.pop("gold")
            costs["energy"] = old_gold
            changed = True
        elif "gold" in costs:
            # Has both gold and energy: drop gold
            costs.pop("gold")
            changed = True
    # playCost (scalar) with no resource or gold resource: add playCostResource: energy
    if "playCost" in card and card.get("playCost") is not None:
        resource = card.get("playCostResource", "gold")
        if resource == "gold":
            card["playCostResource"] = "energy"
            changed = True
    return changed


def add_ability_choice(card, ability_id, choice):
    """Add choice to a named ability if not already present."""
    for ab in card.get("abilities", []):
        if ab.get("id") == ability_id and "choice" not in ab:
            ab["choice"] = choice
            return True
    return False


def is_sentry(card):
    return card.get("id", "").startswith("sentry-")


def is_machine(card):
    return card.get("faction") == "machine"


def main():
    with open(GAME_JSON, encoding="utf-8") as f:
        game = json.load(f)

    cards = game.get("cards", [])
    target_cards = [c for c in cards if is_machine(c) or is_sentry(c)]
    print(f"Found {sum(1 for c in cards if is_machine(c))} machine faction cards")
    print(f"Found {sum(1 for c in cards if is_sentry(c))} sentry-* cards")

    stats = {
        "choices_added": 0,
        "sentry_keys_fixed": 0,
        "repriced": 0,
    }

    for card in target_cards:
        cid = card["id"]

        # 1. Add choice to tagged machine abilities
        if cid in TAGGED_CHOICE_REPAIRS:
            for ability_id, choice in TAGGED_CHOICE_REPAIRS[cid].items():
                if add_ability_choice(card, ability_id, choice):
                    stats["choices_added"] += 1
                    print(f"  [choice] {cid}/{ability_id}")

        # 2. Fix sentry invalid keys
        if is_sentry(card):
            if fix_sentry_card(card):
                stats["sentry_keys_fixed"] += 1
                print(f"  [sentry keys] {cid}")

        # 3. Reprice gold -> energy
        if reprice_to_energy(card):
            stats["repriced"] += 1
            new_cost = card.get("playCosts", {}).get("energy", "?")
            print(f"  [reprice] {cid}: -> energy {new_cost}")

    print(f"\nStats: {stats}")

    if not DRY_RUN:
        with open(GAME_JSON, "w", encoding="utf-8") as f:
            json.dump(game, f, indent=2, ensure_ascii=False)
        print(f"Written: {GAME_JSON}")
    else:
        print("(dry-run, not written)")


if __name__ == "__main__":
    main()

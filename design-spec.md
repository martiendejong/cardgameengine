All 124 messages (62 user + 62 assistant) read in full. Producing the specification.

# TRADING CARD GAME + GENERIC ENGINE — EXHAUSTIVE DESIGN SPECIFICATION

Source: `E:\projects\cardgameengine\design-conversation.md` (124 messages). Where a mechanic evolved during the conversation, the **FINAL** decision is given and superseded versions are noted.

---

## 1. CORE RULES

### 1.1 Win/Loss condition
- Each player starts with a **Headquarters (HQ) building** and a **Hero**, both already in play (outside the deck).
- **A player loses only when BOTH their Hero AND their HQ have been destroyed.** Killing one does not end the game; a hero-less player can recover a Hero (per-faction mechanics, §4), and a HQ-less player can fight on with the Hero.
- Robot deck variant of the same rule: AX-01 alive + Landing Pad destroyed → still alive; AX-01 destroyed + Landing Pad alive → can rebuild; both destroyed → game over.
- Win/loss is a configurable rule in the engine, not hardcoded (e.g. `WHEN EntityDestroyed IF player.hero destroyed AND player.hq destroyed DO Player loses`).

### 1.2 Combat math
- **Damage = max(0, Attacker.Attack − Target.Armor)** — armor subtracts from every incoming attack; minimum damage 0. (Explicitly decided; supersedes armor-as-extra-HP.)
- Examples confirmed: 3 ATK vs 2 Armor = 1 dmg; 2 ATK vs 2 Armor = 0 dmg.
- Consequence acknowledged as design principle: armor and attack concentration have superlinear value (see §6 balance).
- Poison/self-inflicted/direct damage (e.g. Forbidden Knowledge, Poison counters) is **not reduced by Armor** (not an attack).
- Damage pipeline (event lifecycle): `AttackDeclared → DamageProposed → modification/replacement rules → DamageCalculated → DamageApplied → DamageDealt → HealthChanged → possible EntityDestroyed`. Replacement rules can cancel, replace, modify, or redirect events (e.g. "Last Stand: if your Hero would be destroyed, set HP to 1 instead, destroy this card").
- Healing needs **max health**: model `baseHealth/maxHealth` + `currentHealth`; Heal N = `SET hp = MIN(hp + N, maxHealth)`.

### 1.3 Attacking
- Attack action: available when source controlled by active player, on battlefield, attack > 0, untapped; **attacking taps the attacker**; player chooses one legal enemy target (any enemy battlefield entity, subject to Guard/Fortification restrictions).
- **Retaliation exists implicitly**: the `Ranged` keyword is defined as "When attacking a non-Ranged character, that character doesn't deal combat damage back to the attacker" — i.e., normal (non-Ranged) attacks incur defender retaliation damage.
- **Guard** (Town Guard): "While this unit is untapped, opponents must attack a Guard before they can attack your Hero or Headquarters." (Earlier phrasing: enemy characters cannot attack your Hero/HQ while they could attack this unit.)
- **Fortification** (Wooden Palisade): while untapped, enemies cannot attack your non-Defense Buildings unless they first destroy a Defense Building.
- Early-game protection options raised for testing (NOT final decisions, both to be playtested):
  - HQ protection zone: "Units cannot be attacked directly while their HQ stands unless they have attacked or another effect exposes them."
  - `Civilian` keyword: "This unit cannot be attacked while it has not attacked, unless the opponent controls a card that can target Civilians."
- Robot balance proposal: **1 attack per turn** for the Robot; Humans counter it via action economy, not per-unit strength.

### 1.4 Turn structure
- Flow hierarchy (all levels optional/configurable in engine): `Game → Round → Turn → Phase → Step`.
- TCG turn: **Start → Main → Combat → End**.
  - Start: emit `turnStarted`; **untap all** entities controlled by the active player.
  - Main: allowed actions `playCard`, `activateAbility` (equipping happens in Main phase unless stated otherwise).
  - Combat: allowed actions `attack`, `activateAbility`.
  - End: remove modifiers with duration `endOfTurn`; emit `turnEnded`.
- **Draw: 1 card per turn** (stated as the given draw rate; late-game card draw buildings compensate).
- Setup (Town TCG): each player places chosen HQ and chosen Hero onto the battlefield, shuffles deck, **draws 5**.

### 1.5 Tapping and summoning sickness
- Tap is a boolean state; tapping is a common ability cost and the cost of attacking; untap occurs in the owner's Start phase.
- **Summoning sickness (general rule): "A unit cannot attack or activate an ability with a Tap cost during the turn it entered the battlefield, unless an ability explicitly allows it."** Covers attacking, building, harvesting, all tap abilities. A future keyword (`Ready` / `Haste` / `Active`) may bypass it.
- **Buildings enter play untapped** unless the card says otherwise.

### 1.6 Action Points (AP)
- AP is an **entity-scoped resource** (belongs to a specific card instance, not the player). Heroes with AP gain **1 AP at the start of their owner's turn**; AP **persists between turns** until spent. Two entities with AP have completely separate pools. Not all heroes use AP (Wizard heroes use Mana; Robot uses Energy).

### 1.7 Battlefield layout / movement
- No lines, rows, positioning, or movement rules were defined. (Movement/positioning was only mentioned as a possible future "Nomads" faction core mechanic, and Jump Jets references "whatever movement/combat mechanic we eventually settle on".) Zones per player: Deck, Hand, Battlefield, Discard (plus proposed Equipment/Inventory zone, §3.2).

### 1.8 Game states
Runtime match states: `WaitingForAction, WaitingForReaction, WaitingForPriority, WaitingForChoice, ResolvingStack, ResolvingRule, GameEnded`.

---

## 2. RESOURCES & ECONOMIES

The engine distinguishes three economic value kinds:

| Kind | Behavior | Examples |
|---|---|---|
| **Resource (player-scoped)** | gained, stored, spent | Gold, Training Points |
| **Entity resource** | same, but owned by one entity instance; lost if the entity dies | AP, Mana, Energy, Glory, Death Power, Intel, Biomass |
| **Capacity** | provided and occupied/released, not spent | Housing, Power (Machines), Mana Capacity, Corpse Capacity |

### 2.1 Gold
- Player-scoped, shared name across Town and Raider decks but generated completely differently.
- Town generation: Town Hall tap (+2), Gold Mines via Workers, Bag of Gold, Marketplace-era economy. Raider generation: pillaging/stealing (Spoils of War, Scavenge, Pillage/Loot triggers, Loot card, Stolen Treasure).
- **Steal is a distinct primitive from "lose + gain"**: Gold physically transfers (`X = min(amount, opponent's Gold)`); an effect "your Gold cannot be stolen" blocks both sides.

### 2.2 Training Points
- **Player-scoped** military resource. Generated by Barracks (`Tap: gain 1 Training Point`). **FINAL: Training accumulates until spent** (final deck text: "Training accumulates until spent"; supersedes an earlier lean toward end-of-turn reset).
- Spent as part of unit costs (Soldier 1, Archer 2, Town Guard 1, Knight 3, Paladin 2). Creates implicit infrastructure dependency without "Requires: Barracks" text.
- The "buildings produce units" / "Requires Barracks" / "Produce (selector-based production permission)" designs were all explored and **superseded**: FINAL model is (a) Barracks generates Training Points; (b) Archery Range creates cost discounts; units are played normally from hand.

### 2.3 Building Points / Construction (FINAL model — supersedes two earlier versions)
- Superseded v1: BP as player resource paid at build time. Superseded v2: BP as per-turn expiring labor pool.
- **FINAL: construction progress is stored on the individual building instance.** Play a building by paying its **Gold cost**; it enters the battlefield **Under Construction** at `0/N`. A **Builder** taps: "Add 1 Construction Progress to a Building you control that is Under Construction." When progress reaches the Construction Requirement → `ConstructionCompleted`.
- Terminology: **Building Points** = work an effect provides (Peasant Building Power 1; Master Builder ability adds 2; future Builder's Tools +1); **Construction Requirement** = amount the building needs; **Construction Progress** = accumulated on the instance.
- Under-construction buildings: physically on battlefield, provide **none** of their benefits (no Housing, no abilities), **can be attacked**, use normal HP/Armor (start simple).
- Multiple Builders may work on the same building in one turn. Progress spans multiple turns.

### 2.4 Housing (capacity)
- Town-faction capacity, **not universal** — other decks may use no capacity or a different one (Machines: Power capacity — Generator provides 5, Automaton consumes 1, War Machine 3; a unit could consume multiple capacities, e.g. Steam Knight Housing 1 + Power 2; Skeleton consumes 0 of everything).
- Providers: **Town Hall +5** (FINAL; earlier 3 superseded), **Barracks +2** (FINAL; earlier +3 superseded), Tavern +1, Cottages +4 (4 HP/0 Armor/2 Gold — design floated), Master Builder's earlier "Expansion" (+2 Housing per building — superseded ability).
- Consumers (**Housing Cost**): Town Chief **3** (FINAL; earlier 1 superseded), Paladin 3, Master Builder 2, Peasant/Soldier/Archer/Town Guard/Mercenary/Merchant 1, Knight 2, examples: Heavy Infantry 2, Siege Crew 2, Giant 3, War Elephant 3, Dragon 5, Rat Swarm 0.
- Deploy rule: `CurrentHousingUsed + Unit.HousingCost <= CurrentHousingCapacity`.
- **Overpopulation:** if capacity drops below usage, existing units remain, but you cannot deploy additional housing-consuming units while over capacity. (No instant deaths.)

### 2.5 Gold Mine & Worker mechanic
- **Gold Mine — Building — Mine — Unlimited deck copies.** Cost **0 Gold**, Construction **0** (enters completed). **Gold reserves: 5.**
- **Harvest: Tap a Worker you control → remove 1 Gold from this Mine and gain 1 Gold.** The **mine does not tap**; the Worker does — multiple Workers can harvest the same mine in one turn. At 0/5 it can't be harvested (exhausted-mine disposal left open).
- **Worker** keyword: may be tapped to perform Worker actions (harvesting etc.).
- **Hunk's own "Work" ability (Tap → gain 1 Gold) is gated on this rule too** (task 1411,
  balance fix): it now requires controlling a mine (tag `resource-node`, e.g. Gold Mine)
  via a `controls_tagged` condition — previously it was condition-free and produced Gold
  every turn with no mine at all, bypassing the mine-gated economy entirely. The Hunks
  faction's default deck gained **3× Gold Mine** so it keeps a working economy.

### 2.6 Bag of Gold
- **Treasure/Resource card. Cost 0. Deck limit 4.** "Gain **3 Gold**, then discard this card." Generic — any deck may include it.

### 2.7 Mana (Wizard deck)
- **Entity-scoped**, stored on Casters and on the Arcane Nexus, each with a **Mana Capacity**. **Mana persists between turns** (no reset). **If a caster dies holding Mana, that Mana is lost.**
- Transfer: Nexus "Mana Exchange" — tap a Caster → move any amount between that Caster and the Nexus (respect capacities). Transfer taps the Caster (tempo cost); bypass cards exist (Mana Surge, Mana Communion).

### 2.8 Energy (Robot deck)
- Entity-scoped on Landing Pad and AX-01. The Robot deck **uses no Gold at all** — module Installation Costs are paid in Energy (Plasma Cannon 3, Fusion Reactor 4, Reinforced Plating 2).

### 2.9 Biomass (Brood)
- Stored as counters on Hive-type entities. Units have a **Biomass Value** (Larva 1, Ravager 2, Carapace Drone 2, Brood Behemoth 5). Gained by sacrificing organics (Hive Consume), deaths (Corpse Harvester). Spent on Spawn, evolutions, metamorphosis, Royal Egg maturation.

### 2.10 Corpses / Death Power / Bones (Undead)
- **FINAL Graveyard model** (supersedes automatic "every death becomes your corpse"): Graveyard has **Corpse Capacity 8**; **Exhume — Tap: create 1 Corpse token** (Corpse Value 1); **Inter — Reaction, once per round:** when an enemy non-token Unit dies, you may put that card into your Graveyard as a Corpse instead of its discard pile. When full, must exile/discard one to add another. Crypt: capacity +4 (and/or protects up to 3 corpses from exile/steal).
- **Corpse Value** per unit: Peasant 1, Soldier 2, Archer 2, Knight 4, Hero special, Skeleton 0, summoned tokens 0.
- **Death Power**: Necromancer entity resource — +1 whenever another non-token Unit dies, max ~5.
- **Bone counters** on Ossuary: Skeleton you control dies → +1 Bone; remove 3 Bones → summon Skeleton.

### 2.11 Glory (Raiders)
- Entity resource on Raider units: **+1 Glory whenever the unit participates in killing an enemy** (kill or assist). Spent as the promotion cost printed on Hero cards (§4.2).

### 2.12 Intel (Shadow Guild) & Reagents (Alchemists)
- Intel accumulates on infiltrated Spies (+1 at start of your turn); spent on infiltration effects. Reagents are the Alchemist economy, **typed** (Fire, Acid, Frost, Poison) with recipes — concept stage.

---

## 3. KEYWORDS & MECHANICS

### 3.1 Keyword list
- **Worker** — tap to perform worker actions (harvest Gold Mine).
- **Builder** — Tap: add 1 Construction Progress to an under-construction Building you control.
- **Ranged** — when attacking a non-Ranged character, that character deals no combat damage back.
- **Guard** — untapped Guards must be attacked before your Hero/HQ.
- **Fortification** (Defense buildings) — untapped Defense Building must be destroyed before non-Defense Buildings can be attacked.
- **Defender** (early Castle Guard concept): +1 Armor while defending your HQ.
- **Two-Handed** — equipment occupying Main Hand + Off Hand. (Explicitly renamed from user's "dual-wield": dual wield ≠ two-handed.)
- **Dual Wield** (optional keyword) — may equip a one-handed Weapon in both hands; alternatively everyone may, and no keyword is needed (left open).
- **Civilian** (proposal, to test) — see §1.3.
- **Pillage / Scavenge / Loot** (Raider triggers, §4.2). **Unstable** (upkeep-or-die, early Elemental). **Unlimited** (deck-limit flag, §4.9).

### 3.2 Equipment system (characters)
- **Slot-based**, not hardcoded categories. Character slots: **Main Hand ×1, Off Hand ×1, Body ×1, Head ×1** (later possible: accessory, boots, mount, feet — Boots of Haste use "Feet"). Not every character needs all slots (a Peasant might have only hands+body).
- Equipment declares `RequiresSlots` (e.g. Broadsword: MainHand 1 + OffHand 1) and provides Modifiers (`EquippedEntity.Attack +2`). Slot occupancy automatically prevents two Body armors, Bow+Shield, etc.
- **Equip Cost** may be separate from the card's play/buy cost (e.g. Broadsword: Cost 3 Gold, Equip 1 Gold) — proposed; flow `Hand → Equipment/Inventory zone → Character`.
- **Equipment survives its bearer's death** (returns to the Equipment/Inventory zone; a resurrected/replacement Hero can re-equip) unless stated otherwise — proposed default; resurrection explicitly returns the Hero without equipment "unless equipment explicitly survives death".
- Swapping: equipping into occupied slot first unequips old item to inventory; equip during Main phase unless stated.
- **Cross-faction**: equipment is not faction-locked by default (Raider Warchief can use Town swords; Town could take Battle Axe) unless deck/faction restrictions are added.
- Generic attachment+slot+modifier mechanism should also cover: spaceship weapons, vehicle upgrades, held items, enchantments, robot modules, building upgrades.

### 3.3 Robot module slots
`Head ×1, Core ×1, Left Arm ×1, Right Arm ×1, Torso ×1, Legs ×1, Utility ×2`. Modules can require multiple slots (Heavy Fusion Cannon: both Arms → must uninstall both arm modules). Installation costs paid in Energy.

### 3.4 Durations / lifetimes
- Generic **Lifetime/Expiration** ontology concept. Duration counters tick at the start of the controller's turn; at 0 the object is destroyed/transforms.
- Supported expirations: `Permanent, UntilEndOfTurn, UntilStartOfNextTurn, N ControllerTurns, N Rounds, UntilEvent X, UntilCasterDies, UntilSourceLeavesPlay, UntilUsed, While condition true, While source exists`.
- Used by: Wizard magic equipment (Mage Armor 3 turns, Arcane Blade 3, Staff of Power 4, Boots of Haste 2), summons (Fire Elemental 3, Arcane Guardian 2, Lightning Spirit 1), Cocoon (2, then transforms), temporary buffs (Call to Arms until EOT; Divine Shield until start of your next turn).
- Temporary units concept: stronger stats per cost because temporary (Fire Elemental 5/4/1, Arcane Guardian 3/6/3 "emergency defender" variant listed as 3 ATK|6 HP|3 Armor Duration 2 in one place and 4/7/2 in the final deck — final deck version 4/7/2 governs).

### 3.5 Cost modifiers
- Generic `CostModifier` object: `{Resource, Amount −1, Filter (tag), AppliesTo: NextMatchingAction, Uses: 1, Expires: EndOfTurn}`.
- Archery Range: Tap → next Archer this turn costs 1 less Training (stacks by tapping multiple ranges; min 0). Tavern: next Mercenary −1 Gold. War Tent: next Raider −1 Gold. Future: Blacksmith (next Equipment −1 Gold), Stable (next Cavalry −1 Training), Temple (next Cleric −1 Gold), Experienced Commander (next Soldier −1 Training).

### 3.6 Progress tracks / counters
Generic `ProgressTrack/Counter` primitive used by: construction progress; Shrine Resurrection Ritual (3); Robot Reconstruction (6); Royal Egg (5 Biomass); Arcane Essence (6 Mana); Combat Learning XP (3 → +1 base ATK); examples cited: Castle 2 turns, Research Gunpowder 4, Dragon Egg 3, siege assembly 2, channeled spells 3.

### 3.7 Reactions / Instants / stack / priority
- **Reaction is a game-wide card type/timing, not control-deck exclusive.** "Instant" may be a template-specific name.
- **Timing is a card property**, not a class: `Timing: Main | Reaction | both` (e.g. Arcane Shield: Main + Reaction). Cards can also declare `playTiming.allowedDuring: [ownPriority, opponentPriority]` and `reactionTo: [attackDeclared, damageProposed]`.
- Flow: `Action proposed → validated → reaction window → players get priority → responses → resolve`. Reactions can respond to reactions.
- **ResolutionStack** of StackItems `{Source, Controller, Action/Ability, Targets, Choices, Parameters, CostsPaid, ResolutionState}`; resolves **LIFO** (latest reaction first; each lower item resolves only if still valid).
- **Configurable ResolutionPolicy**: Immediate | Stack (LIFO) | Queue (FIFO) | Simultaneous. Configurable **ReactionWindowDefinition** per event (e.g. AttackDeclared true, DamageProposed true, DamageApplied false). Configurable **PrioritySystemDefinition**: action proposed → active player priority → each player may react/activate/pass → next player; **when everyone consecutively passes, resolve top item**; priority passes clockwise in multiplayer.
- **Interruptibility restriction:** you can react to *actions* (attack declared, spell cast, ability activated, card played, building activated, equip, resource transfer initiated, unit summoned) but **not** to internal state changes (HP change, counter removal, duration tick, state-based death). `Action.interruptible: true/false`, card-overridable.
- Rule relationship types with events: **Mandatory trigger, Optional trigger, Replacement, Reaction, Continuous.**
- Balancing note: a Reaction-timed effect is worth more than the identical Main-timed effect.

### 3.8 Secrets / face-down cards / private state (Shadow Guild driver)
- Secrets: played face down; owner knows, opponent sees only "something is there"; on trigger → reveal → effect. Examples: **Ambush** (trigger: enemy attacks your Hero → 3 damage to attacker), **Inside Man** (opponent activates a Building → cancel it, gain 1 Intel), **False Orders** (enemy Unit attacks → change its target to another legal target).
- Infiltration: Spy placed face-down **under an enemy Building** (leaves your battlefield); accumulates Intel; spends against host: 1 Intel look at target player's hand, 2 steal 2 Gold, 3 disable infiltrated Building a turn, 4 destroy it.

### 3.9 Transformations / replacement operations
- **Promotion** (Raiders): generic entity replacement with configurable state transfer — `Transfer: Equipment yes; Damage no; Status effects no; Glory no; Temporary modifiers no`.
- **Evolution** (Brood): card replaces existing creature (Larva + Biomass → Ravager etc.).
- **Metamorphosis**: Cocoon → Brood Behemoth after Duration.
- **Zombie raising** (Undead): corpse card → derived Zombie (ATK −1, same HP, Armor −1, Undead tag).
- **Transmutation** (Control): non-Hero Unit becomes a Sheep (1/2/0, abilities disabled) until beginning of your next turn.

### 3.10 Poison / Plague
- **Poison counter:** at the end of the poisoned unit's controller's turn, it takes 1 damage per Poison counter, then removes one counter. Armor does not reduce it.
- **Plague counter** (Plague Zombie): placed on the killer; at end of that player's next turn, take 2 damage.

### 3.11 Control/debuff toolkit (Alchemist/anti-concentration; §4.8)
Freeze (tap + skip next untap), Corrosive Acid (−2 Armor until EOT), Armor Dissolver (your next attack vs target ignores Armor), Disarm (disable target Weapon 2 turns), Rust (−1 Armor; vs Robot maybe −2 — noted as less clean), Weaken (−2 ATK until your next turn), **Crippling Curse** (halve ATK rounded down until EOT), **Armor Fracture** (lose half Armor rounded up until EOT), Silence (lose activated abilities until your next turn), Suppress (passive ability off this turn), Seal (can't gain counters/resources until your next turn), Ground (no movement/evasion abilities), Dispel (destroy temporary magic equipment/ongoing magical effect), Disable (Reaction: cancel activated ability; entity stays tapped, costs stay paid).

---

## 4. FACTIONS / DECKS

Deck construction (§4.9) applies to all: 40-card deck + HQ + Hero outside the deck.

### 4.1 TOWN (Humans) — reference deck

**Town Hall — Starting HQ.** 8 HP | 2 Armor | **Housing 5**.
- **Collect Taxes — Tap:** Gain 2 Gold.
- **Recruit Peasant — 2 Gold, Tap:** Summon/create a Peasant.
- Later balance addition: **Repopulate — Passive:** if you control no Peasants at the beginning of your turn, create one Peasant. (Anti-death-spiral.)
- Floated (not adopted): Town Walls passive (+1 Armor to Hall+Chief while no completed non-HQ buildings).

**Town Chief — Starting Hero.** 4 ATK | 5 HP | 2 Armor | **Housing 3** | gains 1 AP/turn.
- **Call to Arms — 3 AP:** All Peasants you control gain **+1 Attack / +1 Armor** until end of turn. (FINAL: +1 Armor, explicitly superseding the original +1 HP.)
- **Regenerate — 2 AP, Tap:** Heal this Hero 3 HP.

**Alternative HQs designed (Town faction options):**
- **Castle** — **10 HP | 3 Armor** (FINAL; superseded 12 HP/4 Armor as too durable under subtractive armor). *Royal Treasury — Tap:* gain 1 Gold. *Raise the Guard — 3 Gold, Tap:* summon a **Guard** (3 ATK | 4 HP | 2 Armor, **Defender:** +1 Armor while defending your HQ).
- **Trading Post** — 7 HP | 1 Armor. *Commerce — Tap:* gain 2 Gold. *Trade — 1 Gold, Tap:* draw a card, then discard a card.
- HQ and Hero chosen independently (3 HQs × 3 Heroes = 9 starting combos).

**FINAL 40-card Town deck** (v2, supersedes the first 40-card list):

| Qty | Card | Cost | Stats / text |
|---:|---|---|---|
| 7 | **Gold Mine** (Unlimited) | 0 Gold, 0 Construction | Building — Mine. 5 Gold reserves. Harvest: tap a Worker → remove 1, gain 1 Gold. |
| 3 | **Bag of Gold** (limit 4) | 0 | Treasure. Gain 3 Gold, discard. |
| 6 | **Peasant** (Unlimited) | **1 Gold** (FINAL; superseded 2 Gold) | Unit — Peasant/Worker/Builder. 2 ATK / 3 HP / 1 Armor, Housing 1. Worker; Builder — Tap: +1 construction progress. |
| 3 | **Barracks** | 3 Gold, 3 Construction | 6 HP / 2 Armor / Housing +2. Military Training — Tap: gain 1 Training Point (accumulates). |
| 2 | **Archery Range** | 3 Gold, 3 Construction | 5 HP / 1 Armor. Archery Training — Tap: next Archer this turn costs 1 less Training (stacks). |
| 2 | **Tavern** | 2 Gold, 2 Construction | 5 HP / 1 Armor / Housing +1. Recruitment — Tap: next Mercenary this turn costs 1 less Gold. |
| 1 | **Shrine** | 4 Gold, 4 Construction | 5 HP / 1 Armor. Resurrection Ritual — 3 Gold, Tap: begin resurrecting your dead Hero; 3 turns of progress (start of your turn +1); at 3 return the Hero at full HP, 0 AP, untapped, no temp modifiers, no equipment. Ritual is attached to the Shrine: **Shrine destroyed → ritual lost.** |
| 3 | **Soldier** | 2 Gold + 1 Training | 3 ATK / 4 HP / 2 Armor, Housing 1. |
| 2 | **Archer** | 2 Gold + 2 Training | 4 ATK / 3 HP / 1 Armor, Housing 1. **Ranged.** |
| 2 | **Town Guard** | 3 Gold + 1 Training | 2 ATK / 5 HP / 2 Armor, Housing 1. **Guard.** |
| 2 | **Mercenary** | 4 Gold (no Training) | 4 ATK / 4 HP / 1 Armor, Housing 1. |
| 1 | **Knight** | 5 Gold + 3 Training | 5 ATK / 5 HP / 3 Armor, Housing 2. No ability text. |
| 1 | **Paladin** (reserve Hero) | 6 Gold + 2 Training | 3 ATK / 7 HP / 3 Armor, Housing 3, 1 AP/turn. **Divine Shield — 2 AP:** friendly character +2 Armor until beginning of your next turn. **Holy Light — 3 AP, Tap:** heal **3 HP** (FINAL; superseded 4) to another friendly character or Building. |
| 1 | **Master Builder** (reserve Hero) | 4 Gold | 2 ATK / 5 HP / 1 Armor, Housing 2, 1 AP/turn. **Rapid Construction — 2 AP:** add 2 construction progress to a Building. **Organize Workers — 3 AP, Tap:** untap up to two Peasants. (FINAL; supersedes earlier ability sets: "Oversee Construction: next building −2 Gold"/"Mobilize Workers"; and "Rapid Construction: next building −2 Gold"/"Expansion: building +2 Housing while MB in play".) |
| 1 | **Short Sword** | 2 Gold | Weapon, one hand (Main OR Off). +1 ATK. |
| 1 | **Broadsword** | 3 Gold | Weapon, Two-Handed. +2 ATK. |
| 1 | **Wooden Shield** | 2 Gold | Shield, Off Hand. +1 Armor. |
| 1 | **Leather Armor** | 3 Gold | Armor, Body. +1 Armor. |
| 1 | **Longbow** | 3 Gold | Weapon, Two-Handed. +1 ATK, grants **Ranged**. |

Total 40. Only one Hero may be active at a time.
- Superseded v1 deck differences: 4× Barracks, 3× Archery Range, 2× Tavern, 2× Shrine, 4× Peasant(2 Gold), 3× Archer, 3× Short Sword, 2× Broadsword/Shield/Leather/Longbow, and **1× Commander's Helmet** (3 Gold, Head, +1 HP; on a Hero: at beginning of your turn, if the Hero has 0 AP, gain 1 additional AP) — the Helmet was dropped from the final list but remains a designed card. Iron Sword/Shield/Heavy Armor/Warhammer(+2 ATK vs buildings)/Blacksmith were early sketches.
- Plate Armor sketch: +2 Armor with drawback/requirement (kept scarce deliberately; +Armor equipment intentionally rare).

**Later Town additions (post-balancing discussion; adopted design direction):**
- **Wooden Palisade** — Building — Defense. 1 Gold, 1 Construction. 5 HP / 2 Armor. **Fortification** (see §1.3).
- **Rebuild the Town** — Action, max 4. Choose a destroyed Building from your discard; play it under construction; **its Gold cost is reduced by 2**; still needs normal construction progress.
- **Man the Walls** — an untapped Peasant defending a Defense Building gives it +1 Armor for that attack and becomes tapped.
- **Marketplace** — Building, 4 Gold, 3 Construction, 5 HP / 1 Armor. **Trade — 1 Gold, Tap: draw a card.** **Sell Wares — Tap: gain 1 Gold** (a second, free-standing ability so the building is useful even with no spare Gold to spend). (An earlier variant — 3 Gold; Tap: gain 1 Gold per two Workers, max 3 — was floated first; the draw version is the one settled on for late-game card supply.)
- **Library** — Building, 4 Gold, 4 Construction, 4 HP / 1 Armor. **Research — 2 Gold, Tap:** look at top 3 cards of your deck; one to hand, rest to bottom in any order.
- **Merchant** — Unit, 3 Gold, 1 ATK / 3 HP / 0 Armor, Housing 1. **Trade — Tap:** pay 1 Gold → draw a card, then discard a card.
- Reactions: **Protect the Town** (2 Gold — when your Building is attacked: target Peasant may tap to intercept the attack), **Emergency Repairs** (2 Gold — prevent the next 3 damage to target Building this turn).
- Human identity (final framing): strongest conventional economy; resilience (layered defense: expendable peasants → walls/guards → resilient Town Hall + rebuilding); vulnerable early if greedy; late-game bottleneck is card draw, solved by Marketplace/Library/Merchant. Four stages: Foundation → Protection → Development → Prosperity.

### 4.2 RAIDERS

**Raider Camp — HQ.** 9 HP | 1 Armor. No Housing.
- **Spoils of War — Passive:** the first time each turn one of your units damages an enemy Hero or HQ, gain 1 Gold.
- **Raid Party — 2 Gold, Tap:** Draw a Raider unit, then discard a card.

**Warchief — Hero.** 5 ATK | 6 HP | 2 Armor (balance revision proposed: **4 ATK / 6 HP / 2 Armor**), 1 AP/turn.
- **Bloodlust — 3 AP:** your attacking units gain +1 Attack until end of turn.
- **Pillage — 2 AP:** the next enemy Building destroyed this turn gives you 3 Gold.

**Cards:**
- **Loot ×4** — **FINAL:** Action — Raid. Cost 0, limit 4, **playable only during your own turn** (not a Reaction). **Steal up to 2 Gold from target opponent** (actual transfer; X = min(2, their Gold)). Supersedes the original "gain 2 Gold" (dominated by Bag of Gold).
- **Scavenger ×4** — 2 Gold, 2 ATK / 3 HP / 1 Armor. **Scavenge:** whenever this participates in destroying an enemy unit, gain 1 Gold.
- **Pillager** — 3 Gold, 3 ATK / 4 HP / 1 Armor. **Pillage:** +2 ATK when attacking Buildings. **Loot:** when this destroys a Building, gain 2 Gold. (Later restated 3/3/1 in balance section.)
- **Stolen Treasure — FINAL:** Building — Treasure, **cost 1 Gold** (revised down from 2), no construction, limit 4. **Plunder — on enter:** remove up to 4 Gold from target opponent; put 1 Gold Token on this per Gold removed. **Claim Spoils — Tap:** remove 1 Gold Token → gain 1 Gold. Destroyable before extraction (counterplay). Supersedes the first version (1 Gold, enters with 4 counters, automatic 1 Gold/turn — rejected as free money).
- Buildings (bought, not constructed — Raiders have **no Building Points**): **War Tent** (3 Gold, 5 HP/1 Armor, Tap: next Raider this turn −1 Gold), **Fighting Pit** (4 Gold, 6 HP/1 Armor, Tap: target Raider +1 ATK this turn), **Trophy Rack** (2 Gold, 4 HP/0 Armor, whenever you destroy an enemy unit with 4+ ATK → gain 1 Gold).
- Equipment: **Battle Axe** (3 Gold, Two-Handed, +2 ATK, +1 additional ATK vs Buildings), **Looted Shield** (2 Gold, Off Hand, +1 Armor). May also use Town equipment; may include Bag of Gold.
- Reactions: **Dirty Trick** (1 Gold — when your Raider attacks or is attacked: target enemy −1 Armor for this combat), **Grab the Loot!** (when an opponent would gain Gold: steal 1 of that Gold instead).

**Hero succession — FINAL model:** Glory lives on units (+1 per kill participation); the **Hero card defines its Promotion Cost**. (Supersedes the "Leaderless status + generic 3-Glory Ascension rule".)
- **Bloodfang, Raider Chief — Hero.** Promotion Cost: **3 Glory**. **Promote:** during your turn, choose a Raider you control with ≥3 Glory; play Bloodfang by promoting it — only if you control no Hero. **All compatible equipment transfers**; damage/status/glory/temp modifiers do not. Stats 6 ATK / 6 HP / 2 Armor (earlier sketch 6/6/1), 1 AP/turn. **Frenzy — 2 AP:** +2 ATK this turn. **Blood Price — 3 AP:** sacrifice another friendly unit → heal Bloodfang 3 and gain 1 Gold.
- Other promotion-cost examples: **Gorak the Conqueror** — 5 Glory (much stronger); **Skull Shaman** — 2 Glory *from a Shaman-type unit*.
- Warband size cap (max 5 Raiders; War Banner +2) was considered and **deliberately not adopted initially** — try no capacity mechanic first.

**Balance direction (from playtests: Raiders overpowered):** reduce base combat stats (e.g. generic 2-Gold Raider: 2 ATK / 4 HP / 1 Armor with "Pillage: combat damage to enemy HQ → gain 1 Gold" instead of 3/4/2); reduce Warchief early threat; remove/reduce passive Gold; tie economy tightly to pillaging economic targets. Rule: **a 3-Gold Raider should lose straight combat to a Human unit costing 3 Gold + Training/infrastructure** — Raider compensation is tempo and self-financing raids. Intended matchup curve: early Raiders control tempo (not "kill everything"), mid even, late Humans dominant.

### 4.3 ARCANE CONCLAVE (Wizards)

**Arcane Nexus — HQ.** 8 HP | 2 Armor | **Mana 3/8** (starts with 3, capacity 8).
- **Channel — Tap:** gain 2 Mana.
- **Mana Exchange:** tap a Caster you control → transfer any amount of Mana between that Caster and the Nexus (respect capacities).
- **Arcane Reconstitution (Hero recovery):** when your Hero dies, its **Arcane Essence** attaches to the Nexus. During your turns, move Mana from the Nexus onto the Essence (any amounts, over multiple turns). At **6 Mana**: remove them, return the Hero at full HP and **0 Mana**.

**Archmage — Starting Hero.** Wizard/Caster. 3 ATK | 5 HP | 1 Armor | **Mana 3/6**. No AP — abilities use personal Mana.
- **Meditate — Tap:** gain 2 Mana.
- **Summon Familiar — 2 Mana, Tap:** summon an Arcane Familiar.

**FINAL 40-card deck:**

| Qty | Card | Cost | Details |
|---:|---|---|---|
| 4 | Apprentice Mage | 2 Gold | 2/3/0, Mana 0/3. **Study — Tap:** +1 Mana. **Arcane Spark — 2 Mana, Tap:** 1 damage to target character. |
| 3 | Mana Seer | 3 Gold | 1/3/1, Mana 0/4. **Attunement:** start of your turn +1 Mana (no tap). **Foresight — 2 Mana, Tap:** look at top 2 of deck; one on top, one on bottom. |
| 2 | Pyromancer | 3 Gold | 3/3/0, Mana 1/4. **Feed the Flame:** first time each turn one of your spells deals damage → +1 Mana. **Flame Bolt — 2 Mana, Tap:** 2 damage to a unit. |
| 2 | Conjurer | 4 Gold | 2/4/1, Mana 0/5. **Summon Fire Elemental — 4 Mana, Tap.** |
| 2 | Mana Leech | 3 Gold | 2/4/1, Mana 0/3 (earlier sketch 2/3/0). Combat damage to a Caster → transfer 1 Mana from it to Mana Leech. |
| 1 | Storm Mage | 4 Gold | 3/4/1, Mana 1/5 (earlier sketch 3/3/1, 0/4). **Stormcharge:** first Instant you cast during another player's turn each [turn] → +1 Mana. **Lightning — 3 Mana, Tap:** 3 damage to a character. |
| 1 | Grand Summoner (Hero) | **5 Gold + 4 Mana from the Nexus** | 3/6/1, Mana 2/8. Only playable with no active Hero. **Greater Summoning — 5 Mana, Tap:** summon Arcane Guardian. **Mana Bond — Passive:** your summoned units get +1 Duration when summoned. |
| 4 | Mana Burst | 0 | Spell. Generate 3 Mana; distribute freely among friendly Casters (respect capacities). |
| 3 | Arcane Rain | 1 Gold | Spell. Every friendly Caster +1 Mana. |
| 3 | Overcharge | 0 | Spell. Target friendly Caster gains **4 Mana** (FINAL; superseded 3); at end of your turn it takes 2 damage (not reduced by Armor — direct damage). |
| 2 | Mana Communion | 2 Mana | **Instant.** Redistribute any amount of Mana among your Casters and Nexus **without tapping them**. |
| 2 | Fireball | 3 Mana | Spell. 4 damage to target character or Building. |
| 2 | Counterspell | 3 Mana | **Instant.** Cancel target Spell being cast. |
| 2 | Arcane Shield | 2 Mana | **Instant.** Target friendly character +2 Armor until end of turn. |
| 1 | Mage Armor | 3 Mana | Magic Equipment — Body. +2 Armor. **Duration 3 turns.** |
| 1 | Arcane Blade | 2 Mana | Magic Equipment — One Hand. +2 ATK. **Duration 3.** |
| 1 | Staff of Power | 3 Mana | Magic Equipment — Two Hands. Mana Capacity +2; when the equipped Caster generates Mana via its own abilities, +1 additional. **Duration 4.** |
| 1 | Mana Potion | 1 Gold | Consumable. Target Caster +2 Mana; discard. |

**Tokens (outside deck):**
- **Arcane Familiar** — 2/2/0, Mana 0/2. **Arcane Bond:** on summon, summoner may transfer 1 Mana to it. **Return Mana — Tap, sacrifice:** transfer all its Mana to its summoner.
- **Fire Elemental** — 5/4/1, **Duration 3**. (Supersedes early "Elemental 5/4/1, Unstable: pay 1 Mana at start of turn or destroy".)
- **Arcane Guardian** — 4/7/2, **Duration 2** (3 under Grand Summoner).

Also designed but not in the final 40: **Nexus Surge** (1 Gold: Nexus +4 Mana), **Mana Surge** (Spell, 1 Mana: transfer up to 3 Mana Nexus→Caster without tapping), **Forbidden Knowledge** (Spell: target Caster +4 Mana, takes 2 damage, armor-ignoring), **Boots of Haste** (2 Mana, Feet, Duration 2: once/turn, after equipped character taps to cast a Spell, untap it), **Death Mage** (2/4/0, cap 5: other unit dies → +1 Mana), Wizard reaction **Blink** (2 Mana: when a friendly Caster is targeted, remove it from play; return it at end of the current action — the action loses its target). Early Instant sketch: **Divine Intervention** (target friendly unit +3 Armor until EOT, reaction to attackDeclared/damageProposed).

### 4.4 THE BROOD (Swarm)

**The Hive — HQ.** 10 HP | 1 Armor. No Gold.
- **Consume — Tap:** sacrifice a friendly Organic unit → put Biomass counters on the Hive equal to its Biomass Value.
- **Spawn — Remove 1 Biomass:** summon a Larva.

**Broodmother — Hero.** 3 ATK | 8 HP | 1 Armor. No AP/Gold.
- **Lay Eggs — Tap:** create 2 Egg tokens.
- **Devour — Sacrifice a friendly unit:** heal Broodmother equal to its Biomass Value.

**Tokens/units:**
- **Egg** — 0/2/0, cannot attack; at the beginning of your next turn, hatches → Larva. Attackable before hatching.
- **Larva** — 1/1/0, Biomass Value 1. Simultaneously combat unit, food, biomass, and evolution material.
- **Ravager** — Evolution: **Larva + 1 Biomass**. 4/3/1, BV 2.
- **Carapace Drone** — Evolution: **Larva + 2 Biomass**. 2/5/**3**, BV 2. **Protector:** your Eggs cannot be attacked while you control an untapped Carapace Drone.
- **Spitter** — Evolution: **Larva + 1 Biomass**. 3/3/0, **Ranged**; whenever it damages a unit, put a Poison counter on it.
- **Cocoon** — **Metamorphosis — 3 Biomass:** replace a Ravager. 0/6/2, Duration 2; at 0 → transform into **Brood Behemoth** (7 ATK / 9 HP / 3 Armor, BV 5).
- **Corpse Harvester** — 3/4/1. Another friendly Organic dies → +1 Biomass on the Hive.
- **Cannibal Drone** — 4/4/1. **Devour — Tap:** sacrifice another friendly Organic → +X ATK this turn (X = its BV).
- **Hive Mind** — Ongoing Effect: 3+ Brood units → +1 ATK when attacking; 6+ → additionally +1 Armor; 10+ → Larvae +1 ATK permanently while Hive Mind remains.
- **Bloated Spawn** — 3/3/0. Dies → summon 2 Larvae.
- **Brood Carrier** — 2/6/1. Takes damage and survives → once per turn, create an Egg.

**Mutations** (permanent attachments; not equipment; not transferable; die with the creature): **Hardened Carapace** (+1 Armor, BV +1; max one Carapace mutation per unit), **Venom Glands** (combat damage → add 1 Poison), **Additional Limbs** (+1 ATK; if it already has Additional Limbs, +2 instead).

**Hero succession:** **Royal Egg** — playable from hand when Broodmother dies. 0/5/1. Mature by feeding **5 Biomass** (multi-turn). At 5 → replace with a Brood Hero from hand, e.g. **Hive Queen** — 4 ATK / 10 HP / 2 Armor. **Spawn — Tap:** create 3 Larvae. **Consume Brood — sacrifice any number of friendly Brood units:** +1 Armor until your next turn per sacrifice.

Identity: no Housing/Training/Mana/Gold/Glory; the deck **wants some of its own units to die**; engine stress-test for tokens, sacrifice costs, transformation, attached permanent mutations, poison counters, death triggers, delayed metamorphosis, entity-stored resources.

### 4.5 UNDEAD (Necromancer)

**Graveyard — HQ (FINAL).** 10 HP | 1 Armor | **Corpse Capacity 8**.
- **Exhume — Tap:** create 1 Corpse token (Corpse Value 1) in this Graveyard.
- **Inter — Reaction, once per round:** when an **enemy non-token Unit** dies, you may put that card into your Graveyard as a Corpse instead of its normal discard pile.
- (Supersedes v1: "whenever a non-summoned Unit dies it becomes your Corpse" + "Raise Dead — Tap: exile a Corpse → summon Skeleton". Reason: multiplayer scaling and cold-start; see §6.)

**Necromancer — Hero.** Caster. 2 ATK | 5 HP | 1 Armor. Resource: **Death Power** (not Mana).
- **Harvest Soul:** whenever another non-token Unit dies → +1 Death Power (max ~5).
- **Raise Corpse — 2 Death Power, Tap:** choose a Corpse in your Graveyard → summon a Zombie based on it.
- Active corpse-claiming variants discussed: "Harvest Corpse — 1 Death Power: claim a dying unit's corpse" / "Tap: next eligible death this turn becomes your Corpse".

**Mechanics/cards:**
- **Skeleton** — 2/2/1, no Housing; **does not create a Corpse when it dies** (prevents infinite recycling).
- **Zombie raising inherits the corpse:** Zombie = original stats with **ATK −1, same HP, Armor −1**, gains Undead (e.g. Knight 5/5/3 → Zombie Knight 4/5/2). Army built from whatever opponents brought.
- **Corpse Values:** Peasant 1, Soldier 2, Archer 2, Knight 4, Hero special, Skeleton 0, summoned tokens 0.
- **Ossuary** — Building: Skeleton you control dies → +1 Bone counter; remove 3 Bones → summon a Skeleton.
- **Crypt** — Building: stores up to 3 Corpses immune to exile/steal; later restated as Corpse Capacity +4.
- **Charnel House** — whenever a non-Undead Unit dies → heal your Graveyard 1 HP.
- **Corpse Explosion** — Spell: exile a Corpse → deal damage equal to its Corpse Value to target Unit.
- **Flesh Golem** — cost: exile Corpses with combined Corpse Value ≥5 → 6 ATK / 8 HP / 2 Armor.
- **Plague Zombie** — 3/4/0. Destroyed → Plague counter on its destroyer; at end of that player's next turn, it takes 2 damage.
- **Bone Collector** — 2/4/1. Another Undead dies → gain 1 Bone.
- **Restless Dead** — 2/2/0. Destroyed → returns to play at the beginning of your next turn **unless the opponent pays 1 Gold to exile it** (deterministic replacement for a 50% chance).

**Hero succession:** the Necromancer transforms rather than resurrects. **Lich — Hero. Ascension Cost: 6 Corpses** (built on the Necromancer's corpse). 4 ATK | 7 HP | 2 Armor. **Enters play with 2 Skeletons** (on-play summon, rebalanced from task 974 — Mass Resurrection alone at a free/5 AP cost was either free-every-turn or unaffordably slow at 1 AP/turn). **Soul Drain:** whenever another Unit dies, heal 1 HP. **Mass Resurrection — 3 AP, Tap:** summon 2 Skeletons. Possible future **Phylactery** mechanic for further resurrection.

*Implementation note (task 974, 2026-08-29):* the shipped card had **Mass Resurrection** costing Tap only — no resource cost at all — so it summoned 2 free Skeletons (1 Corpse each in the shop) every single turn once AP-affordable, making Lich wildly overtuned. The original design called for a **5 Death Power** cost, but Lich's `objectType` is plain `hero`, which only carries an `ap` resource pool (Death Power is `necromancer-hero`-only, a type Lich never inherited) — so a `dp` cost would have been permanently unpayable. Fixed by costing the ability **5 AP + Tap** instead, reusing the `ap` pool every hero already accrues at +1/turn (see `gain_hero_ap` in `TurnService.cs`), which taxes the ability to roughly once every 5 turns of saving with no other AP spend. The spec's richer "exile up to 3 Corpses → summon a Skeleton for each" scaling effect is not implemented — the engine has no cost/effect mechanic yet for "spend a variable, player-chosen amount of a resource to summon that many units" (no `choice`-driven variable resource cost exists anywhere else in this file either). A future engine feature could restore that scaling; until then 5 AP + Tap → 2 Skeletons is the balance-safe simplification.

Identity: death is inventory; the Graveyard is a second hand; long bloody games favor it (bounded by capacity + once-per-round Inter).

### 4.6 SHADOW GUILD (Spies) — concept stage
- HQ: **Thieves' Guild**; Hero: **Spymaster** (no stats given).
- **Spy** — 1 ATK / 2 HP / 0 Armor. **Infiltrate — Tap:** put this Spy face-down underneath an enemy Building (leaves your battlefield). Infiltrated Spy gains **1 Intel** at the beginning of your turn.
- Intel spends: **1** — look at target player's hand; **2** — steal 2 Gold; **3** — disable the infiltrated Building for a turn; **4** — destroy it. (Sabotage example: 2 Intel → destroy Barracks in the illustrative chain.)
- **Secrets** (face-down trigger cards): **Ambush**, **Inside Man**, **False Orders** (texts in §3.8).
- Purpose: forces the engine to support **private state** and attachment-to-enemy-object relationships. Chosen over other candidates (Time Mages — schedule cards turns ahead; Machine Empire — assembly of multi-card entities; Nomads — positioning; Diplomats — deals; Shapeshifters — form-switching; Hive Mind — shared stats; Chaos — rule mutation). Machines recommended next after Shadow Guild.

### 4.7 ROBOT / MACHINE deck (hero-only)

**Landing Pad — HQ.** 10 HP | 2 Armor.
- **Recharge — Tap:** add 2 Energy to the Landing Pad.
- **Dock — Tap Hero:** transfer any amount of Energy between Hero and Landing Pad.
- **Repair Bay — 2 Energy, Tap Landing Pad + Hero:** heal the Robot 2 HP.
- **Emergency Reconstruction:** when AX-01 dies, its wreckage goes on the Pad; Reconstruction track 0/**6**; at the beginning of each turn you may pay 2 Energy → +1 Reconstruction; at 6, AX-01 returns. Card support: **Spare Parts** (+2 Reconstruction), **Emergency Protocol** (+1 Reconstruction and +3 Energy to Pad).

**AX-01 — Robot Hero.** Original: 5 ATK | 10 HP | 3 Armor | Energy 3/5. **Balance revision (proposed FINAL): start ~3 ATK | 8 HP | 2 Armor** and build power via modules; also proposed: only 1 attack/turn as its structural limiter.
- **Rule: you cannot control Units.** The Robot is the entire army. **No second Hero exists** — recovery only via Reconstruction.
- **No Gold:** entire deck runs on Energy; modules have Energy installation costs.

**Module slots:** Head, Core, Left Arm, Right Arm, Torso, Legs, Utility ×2.

**Modules:**
- **Plasma Cannon** — Arm. +3 ATK. **Fire — 2 Energy:** this attack +2 ATK. (Install ~3 Energy; also cited as +2 ATK in balance discussion.)
- **Missile Launcher** — Arm. **3 Energy, Tap Hero:** 4 damage to target Unit or Building.
- **Reinforced Plating** — Torso. +2 Armor. (Install ~2 Energy.)
- **Fusion Reactor** — Core. Energy Capacity +4; at the beginning of your turn, Hero +2 Energy. (Install ~4.)
- **Repair Nanobots** — Utility. **1 Energy, Tap:** heal Hero 1.
- **Jump Jets** — Legs. Once per turn, AX-01 may avoid retaliation after attacking (placeholder pending final combat rules).
- **Targeting Computer** — Head. +1 ATK; your ranged attacks ignore 1 Armor.
- **Heavy Fusion Cannon** — Two Arms (requires uninstalling both arm modules).
- **Siege Plating** (balance proposal) — +2 Armor; AX-01 cannot attack on the turn after it attacks.

**Permanent upgrades (non-equipment):** **Reinforced Skeleton** (Max HP +3), **Expanded Energy Grid** (Energy Capacity +2), **Combat Learning** (kill a Unit → +1 XP counter; at 3 XP → +1 base ATK).

**40-card composition sketch:** Arm weapons 8, Defensive modules 6, Reactor/Core 5, Head/Sensor 4, Leg/Mobility 4, Utility 5, Permanent upgrades 4, Emergency/Repair 4. **Zero units.**
- Reaction: **Reactive Shielding** — 2 Energy: when the Robot is attacked, +2 Armor for that attack.
- Matchup asymmetry noted: "Destroy target Unit" nearly dead vs this deck; "Destroy target Equipment" premium; Undead get almost no corpses from it.

### 4.8 ALCHEMIST GUILD / CONTROL deck — concept stage
- HQ: **Laboratory**; economy: typed **Reagents** (Fire, Acid, Frost, Poison) combined via recipes.
- Hero: **Master Alchemist** — 2 ATK | 5 HP | 1 Armor; weak body, strong control.
- Core purpose: counter concentrated single-unit power ("don't overpower the Robot; make it temporarily useless"); weak vs swarms. Strategic triangle: Robot → beaten by Control → beaten by Swarm → beaten by Robot/AoE.
- Card toolkit (see §3.11), notably: **Transmutation — 5 Reagents:** target non-Hero Unit becomes a Sheep (1/2/0, abilities disabled) until the beginning of your next turn; scaling debuffs (Crippling Curse, Armor Fracture) that get better against bigger targets; **Disable** reaction (cancel ability, costs stay paid, stays tapped).
- **Opus Magnus — 3 Reagents:** permanent +2 Attack to a **Golem** you control (a new `golem` tag on Guard/Iron/Adaptive/Stone Golem); recasting it stacks, since the bonus is a permanent property change, not a until-end-of-turn buff. Gives the otherwise pure-control Alchemist deck its own build-around win condition — pour the whole game's reagent income into one Golem (most fittingly Iron Golem, whose own flavor text already calls it "unstoppable") instead of only using reagents defensively.

### 4.9 Deck construction rules
- Deck size: **40 cards**; HQ + starting Hero are **outside the deck** (chosen starting cards).
- **Default copy limit: 4 per card.** Cards flagged **Unlimited** have no copy cap — currently **Gold Mine** and **Peasant** (a deck-construction property `DeckLimit: Unlimited` in the ontology, not engine special-casing).
- Engine-level configurable deck rules: min/max deck size, copies per card, sideboard size, allowed sets/types, banned/required cards, commander/hero requirements, faction restrictions.
- Starting hand: draw 5 (Town TCG setup).
- **Hero Limit: 1 active Hero** (Town faction; configurable — future cards could raise it, e.g. "While King's Palace is in play, your Hero Limit is increased by 1"). Reserve Heroes sit in the deck; a Shrine resurrection cannot complete while the Hero slot is occupied.
- Hero categories: **Starting Hero** (pre-game choice, begins in play), **Reserve Hero** (deck/hand card), **Dead Hero** (specific instance, potentially resurrectable).

---

## 5. ENGINE / PLATFORM ARCHITECTURE

### 5.1 Core philosophy
- Build a **configurable card-game engine** (not a TCG engine): the engine knows nothing about heroes, HP, armor, gold, poker, flops, etc. **"The engine owns execution semantics; the game definition owns game semantics."**
- Core rule model: **WHEN [event] → IF [conditions] → SELECT/CHOOSE [targets/decisions] → PAY [cost] → DO [effects] → EMIT [events]**. Even combat damage, armor reduction, death, and defeat are configured rules, not code.
- Litmus tests: Texas Hold'em and the Town TCG must both run **without custom engine code** (no `PokerGame.cs`, no `TownChief.cs`); any new concept should extend the ontology, never the engine.

### 5.2 Definition ontology (design-time)
`GameDefinition` containing: `ObjectTypeDefinition` (with inheritance, e.g. Card → Character → Hero/Unit; Card → Building → Headquarters), `PropertyDefinition` (typed: int, decimal, bool, string, enum, object/player ref, collection; defaults/min/max/validation), `ResourceDefinition` (scope: player/entity/game/team; min/max/default/reset/visibility), `TagDefinition`, `ZoneDefinition` (owner scope, visibility incl. per-role owner/others, ordering, capacity, allowed types, face-up/down, insertion position), `FlowDefinition` (Round/Turn/Phase/Step, all optional; repeat/skip/branch/terminate), `EventDefinition` (custom events with payloads), `ActionDefinition` ("available when" + choose + cost + effects — distinct from triggered rules), `AbilityDefinition`, `RuleDefinition` (subtypes: TriggerRule, ReactionRule, ReplacementRule, ContinuousRule; mandatory/optional), `ConditionDefinition` (comparison/boolean/collection operators: any/all/none/count/first/last/min/max/sum), `ExpressionDefinition` (dynamic values referencing source/target/controller/owner/active player/selected/event data/state/variables), `SelectorDefinition` (from-zone, where-filters, modes: all/first/last/top/bottom/random/N/min-max/filtered/sorted, limit), `ChoiceDefinition` (yes-no, choose one/N/up to N, object, player, target, number, mode, order, resource amount, pass; min/max constraints), `CostDefinition` (resources, tap, discard, sacrifice, HP, counters; combinable; entity-scoped vs player-scoped costs distinct), `EffectDefinition` (primitives: create/destroy/move/copy object, set/add/subtract/multiply value, add/remove tag, add/remove/transfer resource, shuffle/sort/reorder, reveal/hide, select, request choice, start/end flow node, emit event, create/remove modifier, end game, declare winner/loser), `ModifierDefinition` (target, property deltas, expiration), `CardDefinition`, `DeckDefinition`, `DeckConstructionDefinition`, `SetupDefinition`, `EndConditionDefinition`, plus later additions: `TimingDefinition`, `ReactionWindowDefinition`, `PrioritySystemDefinition`, `ResolutionPolicyDefinition`, `CapacityDefinition`, evaluators (e.g. configurable PokerHand ranking evaluator), progress tracks/lifetimes.
- "Heal", "untap", "attack", "loseGame" are **not primitives** — they compile to set/min/emit compositions.
- Semantic relationships modeled explicitly (CardInstance INSTANCE OF definition, OWNED/CONTROLLED BY player, LOCATED IN zone; Equipment ATTACHED TO character, REQUIRES slots, PROVIDES modifiers; Modifier TARGETS object, EXPIRES ON event/condition; etc.). Keep `Card` thin — intelligence lives in Rule/Ability/Effect/Selector/Event.

### 5.3 Runtime ontology
`GameInstance` containing: PlayerInstance, ObjectInstance/CardInstance, ZoneInstance, ResourceInstance, PropertyValue, ModifierInstance, EventInstance, ActionInstance, RuleExecution, **PendingChoice/PendingDecision** (first-class object; suspends rule execution until answered; server tells clients "waiting for Player 2 to choose"), StackItem, ResolutionStack, PriorityState, ReactionWindow, GameHistory. Definition vs instance strictly separated (damage changes instance state, never the definition).
- Full state serializable: players, objects, properties, resources, zones, current flow position, active player, modifiers, pending decisions, event queue, **random seed/state**, action + rule-execution history → save/load/replay/spectate/undo/AI/inspection. Detailed execution log (e.g. Call to Arms trace with per-modifier scheduling).
- **Randomness**: seeded/deterministic server-side primitives (shuffle, random int/object/player, dice, coin, random order); every result logged for exact replay.
- Rule priority & conflict resolution: priority, execution order, mandatory/optional, controller ordering, replacement precedence, deterministic simultaneous-event handling.

### 5.4 Central execution loop
`PLAYER INTENTION → ACTION → EVENT → REACTIONS/REPLACEMENTS/TRIGGERS → RESOLUTION → STATE CHANGE → NEW EVENTS → …` — designated "the heart of the engine". Event lifecycle stages: `PROPOSED → BEFORE → REACTION WINDOW → MODIFIED → RESOLVING → RESOLVED → AFTER`.

### 5.5 Templates
- Templates sit **above** the engine as **packages of definitions** (object types, properties, resources, tags, zones, events, expressions, selectors, effects, actions, rules, flow, evaluators, deck definitions, UI config, example cards). Template hierarchy with inheritance: Blank Game → {Classic Playing Cards → Poker → Texas Hold'em; Trick Taking; Draw/Discard} and Blank → Card Battler → Trading Card Game → {Advanced TCG; Town TCG}.
- **Rule modules** installable independently: 52-Card Deck, Dice, Betting, Turn System, Mana System, Attack/Defense, Armor, Health, Deck Building, Drafting, Status Effects, Stack & Priority, Trick Taking, Score System, Teams.
- Designer UX: (1) choose template, (2) friendly configuration UI, (3) Advanced Rule Designer (visual workflow-style builder with branching + player-choice nodes; expression view for advanced users). **Constraint: anything built via friendly UI must be representable/editable in the advanced rule system** (one underlying definition format).

### 5.6 Runtime platform / web architecture
- Two modes: **Designer/Admin** and **Runtime/Play**. Web UI: Game Designer (templates, cards, rules, flow, validation) + Game Client (lobby, board, hand, choices, actions, game log). Server: Definition Service, Match Service, Rule Engine, Action Validator, Event Processor, Choice Manager, State Manager, Persistence.
- **Server-authoritative**: browser never executes authoritative rules; clients submit intentions ("Activate CallToArms on #8472"); server validates legality.
- **Legal-action derivation**: backend computes currently legal actions per player from the definition (with reasons for unavailability, e.g. "requires 2 Gold"), driving a generic frontend automatically; actions needing targets return `requiresChoice` with valid options.
- Multiplayer transport: HTTP commands + **WebSockets/SignalR** push updates; spectators supported.
- **Visibility/projections**: each player receives a different projection of the same authoritative state (own hand visible; opponent hand as Unknown×N); zone-level visibility rules from the definition; Secrets extend this to per-card private state.
- **UI is a second declarative system**, separate from game logic: standard components (card, card row, hand, zone, resource counter, board, choice dialog, action menu, log) + per-game layout definitions (e.g. Hero card layout vs playing-card layout); multiple presentations per game possible (desktop/mobile/accessibility/compact).
- **Definition compilation pipeline** on load: JSON → schema validation → semantic validation (unknown property refs, cost refs to missing resources, infinite recursive rules) → reference resolution → expression compilation → rule-graph compilation → executable GameDefinition.
- Reference JSON definitions were written for **Texas Hold'em** (chips/currentBet/pot resources, deck/hand/community/burn zones, generated 52-card deck, blinds, fold/check/call/raise actions, last-player-standing and showdown rules, configurable PokerHand evaluator with the 10 standard rankings) and for **Town TCG v0.1** (object types card/character/hero/unit/building/headquarters; attack/health/armor/tapped properties; gold player-scope + actionPoints entity-scope; peasant/worker/builder tags; deck/hand/battlefield/discard zones; start/main/combat/end flow; playCard/activateAbility/attack actions; resolve-attack → armor-reduces-damage → apply-damage → destroy-zero-health → player-defeat rule chain; Town Hall, Town Chief, Paladin, Peasant card definitions).
- **AI opponents & simulation mode**: AI players use the same API; automated balance mode runs e.g. 1,000-game matchups recording win rate, game length, damage per card, gold/turn, cards played/turn, kills/turn, resource efficiency, unused resources, card lifetime, first-player advantage, HQ/Hero health at victory — producing a matchup matrix and diagnostic telemetry (e.g. "Human reaches trained army in only 31% of games"). An AI could eventually use the same admin API to invent cards/mechanics while the deterministic engine enforces them.

---

## 6. MULTIPLAYER & BALANCE PRINCIPLES

### 6.1 Multiplayer
- Player count configurable (min/max; "almost no assumption a game has two players"); teams supported; priority passes clockwise; reactions/stack work for N players.
- **Scaling rule: "Effects shouldn't scale automatically with the number of opponents unless deliberately priced in."** "Whenever an enemy does X" is dangerous in multiplayer; prefer **once per round / first time each round**, explicit taps, resource costs, or choose-one-opponent. Applied concretely to the Undead Graveyard (Inter: once per round) and Corpse Capacity 8.

### 6.2 Stat cost model (from playtest findings: Robot > Raiders > Humans)
- Structural insight: with subtractive armor, **stat concentration itself has value** — 2+2+2 ATK ≠ 6 ATK; 4×1 Armor ≠ 4 Armor. **Principle: "The marginal value of Attack and Armor increases with the amount already present on the same permanent entity; balancing cost must grow faster than linearly."**
- **Combat Value model (starting point):** base unit = 1 Attack point. Coefficients: **Armor ≈ 2× Attack; HP ≈ 0.5× Attack.** Marginal curve: start testing **1.5× per additional point** (a pure doubling curve was shown and judged too steep): marginal ATK 1, 1.5, 2.25, 3.4, 5.1, 7.6; marginal Armor 2, 3, 4.5, 6.8, 10.1, 15.2; marginal HP 0.5, 0.75, 1.13, 1.69, 2.53, 3.8. Totals: 4 ATK ≈ 8.15 CV, 5 ATK ≈ 13.25 CV; Armor totals 1→2, 2→5, 3→9.5, 4→16.25.
- `CombatValue = AttackCurve(ATK) + 2×AttackCurve(Armor) + 0.5×AttackCurve(HP) + ConcentrationPremium` — the **ATK×Armor concentration premium** penalizes entities that are simultaneously hard to hurt and armor-penetrating.
- **Evaluate combined effective stats** (base + equipment + upgrades + permanent modifiers) as one curve — a +1 Armor module on a 3-Armor Robot is priced as the 4th Armor point, not the 1st; prevents cheap stacking to 5 Armor. May surface in gameplay as escalating installation costs.
- **Normal bands:** most permanent units ATK 1–4, Armor 0–2; 5 ATK / 3 Armor = powerful; 6+ ATK / 4+ Armor = exceptional; permanent 10 ATK / 4 Armor nearly unattainable. **Temporary effects may exceed the bands** (Call to Arms is fine because it expires). HP is the cheap durability lever; Armor is the exceptional-resistance lever.
- Curve coefficients (1.5×, 2×, 0.5×) are hypotheses to be **fitted from simulation data**, not fixed truths.
- Initial rough value sketch (pre-curve): 1 Gold = 1, 1 ATK = 1, 1 HP = 0.5, 1 Armor ≈ 1.5, 1 card drawn = 2, 1 Training = 1, 1 Housing occupied = −0.5, tap requirement negative, temporary duration negative.

### 6.3 Three balancing layers against giant units
1. **Economics** — nonlinear stat pricing; 2. **Opportunity cost** — one big entity = fewer entities/actions (Robot limited to 1 attack/turn; quality vs action economy); 3. **Counterplay** — Freeze/Silence/Disarm/armor reduction/armor bypass/transform (Control faction exists specifically for this).

### 6.4 Matchup targets
- Do **not** target exact 50/50; **45–55%** is healthy for normal matchups; persistent 65–80% indicates structural problems. Balance from a full matchup matrix + telemetry, not intuition.
- Faction tempo asymmetries are intentional: Raiders strong early (tempo control, not annihilation), Humans strong late (compounding economy + card-draw infrastructure as prime Raider targets).

---

## 7. ANYTHING ELSE

- **Historical supersessions recap:** Call to Arms +1 HP → **+1 Armor**; Holy Light 4 → **3 HP**; Castle 12/4 → **10/3**; Town Hall Housing 3 → **5**; Town Chief Housing 1 → **3**; Barracks Housing +3 → **+2**; Peasant cost 2 → **1 Gold**; BP as player/turn resource → **per-building progress**; unit "Requires Barracks" → building production permission → **Training Points + cost modifiers**; Loot "gain 2" → **steal up to 2**; Stolen Treasure auto-drip → **plunder tokens + Claim Spoils**; Raider generic Leaderless/Ascension → **promotion cost on Hero card**; Overcharge 3 → **4 Mana**; Elemental Unstable upkeep → **Duration 3 Fire Elemental**; Undead auto-corpse-collection → **Exhume + once-per-round Inter + capacity 8**; Restless Dead 50% chance → **deterministic pay-1-Gold-to-exile**; Robot 5/10/3 start → **~3/8/2 + module growth**; Master Builder abilities revised twice (final: Rapid Construction +2 progress / Organize Workers untap 2 Peasants); Commander's Helmet cut from final Town 40; deck v1 → **deck v2** quantities.
- **Naming:** working names — "Town TCG" / "Town Wars" (game), Arcane Conclave, The Brood, Shadow Guild, Alchemist Guild; engine framed as "no-code programming language for card/turn-based games", "Unity for card games".
- **Ontology framing:** the system is "an ontology plus an executable rule model"; templates are specialized ontologies; hierarchy Generic Game Ontology → Card-Game Ontology → template → game. Architectural test: new concepts extend the ontology, never the engine.
- **Economy-per-faction principle:** decks shouldn't merely have different cards — they can run on entirely different economic systems (Gold+Training+Housing / Gold+aggression / Gold+Mana network / Biomass / Corpses / Energy / Reagents / Intel), all interoperable through the shared combat layer; per-faction Hero recovery is likewise unique (Shrine ritual / Glory promotion / Mana reconstitution / Royal Egg / Lich ascension / Robot reconstruction; brainstormed: Kingdom heir, Machine component rebuild, Cult ritual).
- **Telegraphing as a feature:** visible AP/Mana/Glory/progress lets opponents read incoming plays (6 Peasants + Chief at 3 AP; Conjurer holding 4 Mana; Raider at ●●● Glory; Cocoon countdown) — deliberate interactive design.
- **Playtest chronology:** the user reports the system was actually built and tested with Robot, Human (Housing not yet implemented — implement before numerical balancing), and Raider decks; findings (Raiders OP, Robot more OP, turn-2 pressure, armor/attack concentration) drove §6.
- **Not designed:** battlefield positioning/lines/movement, mulligan specifics, sideboards, first-player compensation, exhausted-Gold-Mine disposal, Dual Wield final ruling, Civilian/HQ-protection final choice, Spymaster/Laboratory stats, Reagent recipes, Warband cap, Phylactery — all explicitly open or unmentioned.
agentId: a335b260ce41d1b76 (use SendMessage with to: 'a335b260ce41d1b76' to continue this agent)
<usage>subagent_tokens: 216888
tool_uses: 16
duration_ms: 589401</usage>
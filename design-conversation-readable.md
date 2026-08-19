===== USER =====
I want to make a trading card game. Each player starts with a headquarter building and a hero. When both are dead youve lost the game. The hero and the hq building have each two special abilities. For instance: hq: town hall. 8hp 2arnor Ability 1: tap. Gain 2 gold. Ability 2: 2 gold, tap. Summon peasant. Peasant: 2attack 3hp 1armor abilities: worker, builder Hero: town chief: 4attack 5hp 2armor gains 1 action point each turn. ability 1: call to arms 3 action points: all peasants gain +1/*/+1 until end of turn. Ability 2: regenerate: 2ap,tap: heal 3hp.

===== ASSISTANT =====
That gives you a strong foundation. The interesting part is that **HQ + Hero together define the player's faction/playstyle**, while the rest of the deck develops around them.

I would formalize your example roughly like this:

### Starting board

**Town Hall — Headquarters**  
**HP:** 8 | **Armor:** 2

**Generate Taxes** — *Tap:* Gain 2 Gold.  
**Recruit Peasant** — *2 Gold, Tap:* Summon a Peasant.

**Town Chief — Hero**  
**Attack:** 4 | **HP:** 5 | **Armor:** 2  
At the beginning of your turn, gain **1 Action Point (AP)**. AP remains between turns unless spent.

**Call to Arms — 3 AP:** Peasants you control get +1 Attack/+1 HP until end of turn.  
**Regenerate — 2 AP, Tap:** Heal Town Chief for 3 HP.

**Peasant — Unit**  
**Attack:** 2 | **HP:** 3 | **Armor:** 1

**Worker** — Can perform resource-producing work.  
**Builder** — Can construct buildings.

The really promising part is the **economic engine** you've implicitly created. Your HQ produces gold → gold creates peasants → peasants can build things → buildings unlock additional units/resources/abilities → the hero modifies how you use that civilization.

That means this isn't merely a creature-battling card game. It can become a compact **RTS/city-builder expressed through cards**.

For example, a Peasant could construct a **Barracks**. The Barracks can recruit Soldiers. Or construct a **Farm**, which produces Food. A Blacksmith could improve armor. A Market could convert resources. Suddenly the player's board develops something resembling a little Warcraft/Age of Empires settlement.

I'd make an important distinction between the two starting cards:

**HQ = economy/technology.**  
It determines how your civilization develops.

**Hero = tactics/combat.**  
It determines how you exploit what you've built.

That creates interesting combinations later. You could have several heroes compatible with the same HQ. A Town Hall + Town Chief might favor mass peasants, while Town Hall + Knight Commander turns the same economic foundation into a military strategy.

Your victory condition also creates a nice dynamic:

> **You only lose when both your Hero AND Headquarters have been destroyed.**

So killing one doesn't end the game. If your Town Hall falls, your surviving hero can potentially lead a desperate last stand. If the hero dies first, the settlement can continue fighting and perhaps even have cards capable of resurrecting/replacing the hero.

One thing I'd change immediately: **Armor should probably reduce incoming damage rather than effectively behaving like extra HP**, e.g. `Damage = max(0, Attack − Armor)`. That makes 2 Armor mechanically meaningful and lets armor buffs, armor piercing, siege units, etc. become an entire design space.

This foundation could support a very deep game while keeping the basic loop surprisingly simple:

**Produce → Build → Recruit → Fight → Destroy Hero + HQ.**

And because every player starts with their HQ and Hero already in play, you eliminate one of the annoying aspects of many card games: *not drawing the cards that make your deck actually work*.

===== USER =====
Call to arms should not give +1hp but +1armoe

===== ASSISTANT =====
Yes. Then **Call to Arms** is:

**Call to Arms — 3 AP:** All Peasants you control gain **+1 Attack and +1 Armor** until end of turn.

So the Town Chief becomes:

**Town Chief — Hero**  
**Attack:** 4 | **HP:** 5 | **Armor:** 2  
Gains **1 AP** at the beginning of each turn.

**Call to Arms — 3 AP:** All Peasants gain +1 Attack/+1 Armor until end of turn.  
**Regenerate — 2 AP, Tap:** Heal 3 HP.

That also makes more thematic sense: the chief isn't magically making peasants healthier; he's **arming and organizing them for battle**.

===== USER =====
Can you now come up with two more heroes that fit with that same town? For instance, maybe a paladin or something. And what else?

===== ASSISTANT =====
Yes. I would make the three heroes deliberately push the **same Town Hall economy into three different strategies**. Since the Town Hall generates peasants, each hero should interact with that foundation differently.

### 1. Town Chief — Peasant swarm

**Town Chief**  
**4 Attack | 5 HP | 2 Armor**  
Gains **1 AP per turn**.

**Call to Arms — 3 AP**  
All Peasants you control gain **+1 Attack / +1 Armor** until end of turn.

**Regenerate — 2 AP, Tap**  
Heal this Hero for **3 HP**.

**Style:** Turns your economic Peasants into a surprisingly dangerous militia. You want lots of Peasants and can suddenly mobilize them.

---

### 2. Paladin — Protection / sustain

**Paladin**  
**3 Attack | 7 HP | 3 Armor**  
Gains **1 AP per turn**.

**Divine Shield — 2 AP**  
Choose a friendly unit or building. It gains **+2 Armor until the start of your next turn**.

**Holy Light — 3 AP, Tap**  
Heal **4 HP** to another friendly unit, Hero, or building.

**Style:** Defensive. Instead of exploiting Peasants offensively, the Paladin keeps your developing town alive. He's especially useful for protecting the Town Hall, important buildings, or expensive units.

---

### 3. Master Builder — Expansion / technology

This one would make the Town Hall play almost like an RTS economy.

**Master Builder**  
**2 Attack | 5 HP | 1 Armor**  
Gains **1 AP per turn**.

**Oversee Construction — 2 AP**  
The next building you construct this turn costs **2 less Gold**.

**Mobilize Workers — 3 AP, Tap**  
Untap up to **2 Peasants**.

**Style:** Economic engine. He's weak in combat, but he can build an enormous town much faster than the other heroes. Because Peasants have **Worker + Builder**, untapping them could produce more resources, build more structures, or perform other worker actions.

So you get three genuinely different decks from exactly the same starting HQ:

| Hero | Strategy | Peasants become... |
|---|---|---|
| **Town Chief** | Aggression/swarm | Militia |
| **Paladin** | Defense/sustain | Protected workforce |
| **Master Builder** | Economy/tech | Production engine |

I particularly like **Master Builder** as the third because Paladin + Town Chief + Master Builder roughly correspond to **defense, military, economy**. That gives the Town faction three fundamental strategic directions without needing three different HQs.

===== USER =====
I think the holy light should probably heal for three hit points.

===== ASSISTANT =====
Agreed. That keeps healing more restrained and also matches the Town Chief's Regenerate.

**Paladin**  
**3 Attack | 7 HP | 3 Armor**  
Gains **1 AP per turn**.

**Divine Shield — 2 AP**  
Choose a friendly unit or building. It gains **+2 Armor until the start of your next turn**.

**Holy Light — 3 AP, Tap**  
Heal **3 HP** to another friendly unit, Hero, or building.

The important distinction remains: **Regenerate** is cheaper but self-only, while **Holy Light** is more expensive but can save almost anything on your board.

===== USER =====
Can you now come up with two extra headquarter buildings that we can use?

===== ASSISTANT =====
I'd make the alternative HQs fundamentally change the economy, just as the heroes change how you play the Town faction. Together with **Town Hall**, these could be:

### 1. Town Hall — Population/economy
**8 HP | 2 Armor**

**Collect Taxes — Tap:** Gain **2 Gold**.

**Recruit Peasant — 2 Gold, Tap:** Summon a **Peasant**.

The flexible baseline HQ: it generates both money and workers.

### 2. Castle — Military/defense
**12 HP | 4 Armor**

**Royal Treasury — Tap:** Gain **1 Gold**.

**Raise the Guard — 3 Gold, Tap:** Summon a **Guard**.

**Guard**  
**3 Attack | 4 HP | 2 Armor**

**Defender:** While defending your HQ, this unit gains **+1 Armor**.

The Castle sacrifices economic speed for a much harder starting position. Instead of producing workers, it immediately produces proper military units.

### 3. Trading Post — Economic/commerce
**7 HP | 1 Armor**

**Commerce — Tap:** Gain **2 Gold**.

**Trade — 1 Gold, Tap:** Draw a card, then discard a card.

This would be the high-flexibility HQ. It's physically vulnerable and can't generate Peasants by itself, but it gives you much better control over your deck.

That gives you three clear starting identities:

| HQ | HP | Armor | Specialty |
|---|---:|---:|---|
| **Town Hall** | 8 | 2 | Peasants / balanced |
| **Castle** | 12 | 4 | Military / defense |
| **Trading Post** | 7 | 1 | Economy / card selection |

One thing I especially like here is that **HQ and Hero can be chosen independently**. A *Castle + Paladin* becomes an extremely defensive deck, while *Trading Post + Master Builder* becomes an economic/combo deck, and *Town Hall + Town Chief* becomes a Peasant swarm deck.

With just **3 HQs × 3 Heroes**, you already have **9 different starting combinations** before we've even designed the player's deck.

===== USER =====
But we have to consider something. An armor reduces the incoming attack by one. So if you have three attack and the other has two armor, they will only get one damage.

===== ASSISTANT =====
Exactly. That changes the balance substantially. With your armor system:

**Damage dealt = Attack − Armor**, with a minimum of presumably **0 damage**.

So:
- 3 Attack → 2 Armor = **1 damage**
- 4 Attack → 2 Armor = **2 damage**
- 2 Attack → 2 Armor = **0 damage**
- 5 Attack → 1 Armor = **4 damage**

That means my proposed **Castle with 4 Armor and 12 HP is far too durable**. Most early units couldn't damage it at all.

I'd rebalance the three HQs around armor being very powerful:

| HQ | HP | Armor | Role |
|---|---:|---:|---|
| **Town Hall** | 8 | 2 | Balanced / workers |
| **Castle** | 10 | 3 | Defensive / military |
| **Trading Post** | 7 | 1 | Commerce / cards |

Even **3 Armor** is formidable: your Town Chief at 4 Attack only deals 1 damage per hit, while a 3-Attack unit can't damage it without buffs.

This also makes **Call to Arms (+1 Attack/+1 Armor)** considerably stronger than it initially appears. A Peasant goes from:

**2 ATK / 3 HP / 1 ARM**

to

**3 ATK / 3 HP / 2 ARM**

So against another Peasant, instead of dealing `2 − 1 = 1`, it deals `3 − 1 = 2`, while receiving only `2 − 2 = 0`. That's a huge temporary combat advantage.

I actually like this armor system. It makes a single point of Armor strategically significant rather than just another inflated stat. It does mean we'll need to balance **Attack and Armor together very carefully**, and probably introduce mechanics like **Piercing**, **Siege**, or **Armor Break** later.

===== USER =====
Now I think you can get the rules of this game, but what I want to do now is make a system in which this trading card game can be configured. So I want to have a management system in which I can create rules that are specified like, when event X happens, this rule needs to apply and this needs to happen. And rules can be anything of like, just something happens or a player must choose something and then something happens. So we need to have a rule engine to configure this. And that's completely configurable in an administrative menu.

===== ASSISTANT =====
Yes. I would go one level more abstract than a normal "card editor." You want a **generic event-driven game engine**, with the trading-card game being a configuration running on that engine.

The fundamental model could be:

> **WHEN [event] → IF [conditions] → [optional choices/targets] → DO [effects]**

### Example: Call to Arms

The admin wouldn't program Call to Arms. It would configure:

```text
Ability: Call to Arms

WHEN:
  AbilityActivated

COST:
  3 ActionPoints

TARGET:
  All units
  WHERE:
    Controller = AbilityOwner
    AND Type = Peasant

DO:
  Modify Attack +1
  Modify Armor +1

UNTIL:
  EndOfTurn
```

The engine itself doesn't need to know what "Call to Arms" means.

### Example: attacking

Even fundamental game rules can use the same system:

```text
Rule: Combat Damage

WHEN:
  UnitAttacks

CHOOSE:
  Attacking player chooses:
    1 valid enemy target

CALCULATE:
  Damage = Attacker.Attack - Target.Armor
  Minimum = 0

DO:
  Target.HP -= Damage
```

Then death becomes another rule:

```text
Rule: Death

WHEN:
  HPChanged

IF:
  Entity.HP <= 0

DO:
  Destroy Entity

EMIT:
  EntityDestroyed
```

And your victory condition doesn't need to be hardcoded either:

```text
Rule: Defeat

WHEN:
  EntityDestroyed

IF:
  Player.Hero.IsDestroyed
  AND
  Player.Headquarters.IsDestroyed

DO:
  Player.LosesGame
```

This gives you something extremely powerful: **rules can trigger other rules**.

`Attack → Damage → HP Changed → Death → Entity Destroyed → Victory Check`

And cards simply hook into that event stream.

---

## The rule model

I would give every configurable rule roughly these components:

| Component | Purpose |
|---|---|
| **Trigger** | When should I evaluate this rule? |
| **Conditions** | Is the rule applicable? |
| **Cost** | What must be paid? |
| **Selector** | Which objects/players are involved? |
| **Choice** | Does someone need to make a decision? |
| **Effects** | What happens? |
| **Duration** | Permanent, this turn, until X, etc. |
| **Events** | What events does this produce? |

But there is another important component:

### Variables and expressions

The administrator needs to be able to express things such as:

```text
Damage = Source.Attack - Target.Armor

Healing = Hero.ActionPoints * 2

Gold = NumberOfPeasants

AttackBonus = NumberOfBuildings / 2
```

So effects shouldn't just accept constants such as `+1`. They should accept **expressions referencing game state**.

---

## Choices are especially important

Your observation about "the player must choose something" means that rule execution cannot always happen synchronously.

For example:

```text
WHEN:
    This card enters play

CHOOSE:
    Controller chooses one:
        [Gain 3 Gold]
        [Summon a Peasant]
        [Heal HQ for 2]

WAIT FOR:
    Player choice

DO:
    Selected effect
```

Or:

```text
CHOOSE TARGET:
    Player chooses
    exactly 2
    friendly units
    with Type = Peasant
```

The engine creates a **PendingDecision**.

The game effectively pauses that branch until the player responds.

That architecture will later support very complicated cards without special programming.

---

## Admin interface

I wouldn't initially expose the underlying rule representation. I'd build a visual rule editor resembling an automation/workflow builder:

```text
┌──────────────────────────────┐
│ WHEN                         │
│ Ability is activated         │
└──────────────┬───────────────┘
               ↓
┌──────────────────────────────┐
│ PAY                          │
│ 3 Action Points              │
└──────────────┬───────────────┘
               ↓
┌──────────────────────────────┐
│ SELECT                       │
│ All friendly Peasants        │
└──────────────┬───────────────┘
               ↓
┌──────────────────────────────┐
│ MODIFY                       │
│ Attack +1                    │
│ Armor +1                     │
└──────────────┬───────────────┘
               ↓
┌──────────────────────────────┐
│ UNTIL                        │
│ End of turn                  │
└──────────────────────────────┘
```

Then you can have branching:

```text
                    ┌─ YES → Draw card
                    │
Is Hero alive? ─────┤
                    │
                    └─ NO  → Gain 2 Gold
```

And player decisions:

```text
              ┌→ Summon Peasant
PLAYER CHOOSES┤
              └→ Gain 2 Gold
```

### Make the game schema configurable too

I would go even further.

Don't hardcode:

`Attack, HP, Armor, Gold, AP, Hero, HQ, Peasant`

Instead have an administrative **Game Definition**.

For this particular game:

```text
RESOURCE TYPES
Gold
Action Points

STATS
Attack : Number
HP     : Number
Armor  : Number

ENTITY TYPES
Player
Card
Hero
Headquarters
Unit
Building

TAGS
Peasant
Worker
Builder
Paladin
etc.

ZONES
Deck
Hand
Board
Discard
Destroyed
```

Then your rule engine operates on generic concepts:

```text
ModifyProperty
MoveEntity
CreateEntity
DestroyEntity
SelectEntity
SpendResource
GainResource
EmitEvent
RequestChoice
```

Now you've effectively stopped building *a trading-card-game engine*.

You're building a **configurable rules engine for turn-based games**.

Your current card game becomes **Game Definition #1**.

And that has another major advantage for the AI-driven game idea you were discussing earlier: an AI could eventually use exactly the same administrative API to invent cards, abilities, units, buildings, heroes and potentially even **new mechanics**, while the deterministic rules engine remains responsible for actually enforcing them.

===== USER =====
But the action points are a resource for a character card, for a unit card. So the hero, it gains an action point, but that action point is only usable by the hero itself.

===== ASSISTANT =====
Correct. Then **Action Points should not be modeled as a player resource** like Gold. They should be an **entity-scoped resource** stored on the specific card that owns them.

So the Hero could have:

```text
Town Chief
Attack: 4
HP: 5
Armor: 2
Action Points: 0
```

And a rule:

```text
WHEN:
  StartOfOwnerTurn

IF:
  ThisEntity = Town Chief

DO:
  ThisEntity.ActionPoints += 1
```

Then **Call to Arms** pays from that same Hero:

```text
COST:
  SourceEntity.ActionPoints -= 3
```

Another Hero might have 5 AP saved while the Town Chief has 1 AP. They are completely separate pools.

This suggests the configurable engine should distinguish at least three scopes for resources/properties:

- **Player-scoped:** Gold, perhaps mana, victory points, etc.
- **Entity-scoped:** Action Points, charges, ammunition, energy, experience.
- **Game-scoped:** Turn number, round number, global counters or shared resources.

And we shouldn't hardcode even that Action Points belong to Heroes. A future unit could have AP too. For example:

```text
Wizard
3 Attack | 4 HP | 1 Armor
AP: 0

Arcane Focus:
At the start of your turn, gain 2 AP.

Fireball — 4 AP:
Deal 5 damage to a target.
```

That Wizard's AP belongs only to **that particular Wizard instance**. If you have two Wizards in play, each maintains its own AP.

For the rule engine, I would therefore model resources almost like properties with an **owner scope**:

```text
ResourceDefinition
  Name: Action Points
  Scope: Entity
  Minimum: 0
  Maximum: optional
```

Whereas Gold would be:

```text
ResourceDefinition
  Name: Gold
  Scope: Player
```

That distinction is important because it makes the system much more generic without losing the exact mechanics of your game.

===== USER =====
And I want the system to be so generic that I can build many different kinds of trading card games in it.

===== ASSISTANT =====
Exactly. In that case I would avoid designing the engine around *this* game's concepts entirely. The platform should know almost nothing about heroes, HQs, attack, armor, gold, turns, or even what a "creature" is.

It should be a **TCG construction platform** where those concepts are definitions.

### The abstraction

I'd split the system into roughly these layers:

| Layer | Configures |
|---|---|
| **Game Definition** | Players, victory, phases, turn structure |
| **Data Model** | Card/entity types, stats, resources, tags |
| **Zones** | Deck, hand, battlefield, graveyard, exile, etc. |
| **Rules** | What happens when events occur |
| **Actions** | Things players/entities are allowed to initiate |
| **Effects** | Changes to game state |
| **Selectors** | How rules find cards/entities/players |
| **Choices** | Decisions players must make |
| **Cards** | Instances/templates assembled from everything above |

So even `HP` shouldn't inherently exist.

An administrator creates:

```text
Property: Health
Type: Integer
Default: 0
Minimum: 0
```

Another game might have:

```text
Power
Toughness
Mana
Sanity
Shield
Energy
Level
Experience
```

And the engine treats them identically.

### Resources need ownership/scope

Building on your AP correction, resources should support configurable scopes.

For example:

```text
Gold
Scope: Player

Action Points
Scope: Entity

Round Counter
Scope: Game

Team Energy
Scope: Team
```

Even better, internally I wouldn't make a major distinction between a *stat*, *resource*, and *counter*. They're all state values with slightly different metadata and UI behavior.

### Everything revolves around events

The engine becomes an event/state machine.

For example:

```text
TurnStarted
CardDrawn
EntityCreated
EntityTapped
AttackDeclared
DamageAboutToBeDealt
DamageDealt
PropertyChanged
EntityDestroyed
CardMoved
AbilityActivated
ChoiceMade
TurnEnded
```

But importantly, games can define **custom events** too.

A pirate TCG could define:

```text
ShipSailed
PortCaptured
TreasureBuried
```

A political TCG:

```text
ElectionStarted
VoteCast
LawPassed
GovernmentCollapsed
```

The underlying engine doesn't care what those events mean.

### Rules become the programming language

Every rule essentially becomes:

**Trigger → Conditions → Inputs/Choices → Effects → Events**

For example your armor mechanic:

```text
WHEN
    DamageCalculated

IF
    Target has Armor

SET
    Damage =
        MAX(
            Source.Attack - Target.Armor,
            0
        )
```

But another game could replace that rule with:

```text
Damage = Attack
```

or:

```text
Damage = Attack * 2 - Defense
```

without changing engine code.

### Replacement and interception rules

This is something I would design in **from the beginning**, because sophisticated TCGs need it.

Suppose:

> Divine Shield: The next time this unit would take damage, prevent that damage and destroy Divine Shield.

You need rules to interact with an event **before it resolves**:

```text
DamageProposed
        ↓
Modification/replacement rules
        ↓
DamageCalculated
        ↓
DamageApplied
        ↓
DamageDealt
```

That allows:

> Reduce damage by 2.

> Double damage.

> Redirect damage to another unit.

> Prevent damage.

> Whenever this unit takes damage, gain that much Gold.

> If this damage would kill the unit, leave it at 1 HP instead.

Without an event-resolution pipeline like this, complex cards eventually turn the backend into special-case spaghetti.

### Selectors are another major subsystem

A generic rule system needs a powerful way to say **what I'm talking about**.

For example:

> All Peasants you control.

Internally:

```text
SELECT Entity
WHERE
    Controller = Source.Controller
    AND Tags contains "Peasant"
    AND Zone = Battlefield
```

But you also need:

> Another friendly unit.

> The unit that attacked this card.

> The last card you discarded.

> Up to three enemy buildings.

> A random card from your opponent's hand.

> Every damaged Hero.

> The source of this effect.

Selectors should therefore be reusable configurable objects.

### Choices should be first-class objects

This is equally important.

A rule can resolve immediately:

```text
Gain 2 Gold
```

or suspend itself:

```text
Player must choose:
    Target
    Number
    Card
    Zone
    Mode
    Yes/No
    One of N effects
    Ordering
```

The engine creates something like:

```text
PendingDecision
```

and doesn't continue that branch until a valid answer is supplied.

This also makes online multiplayer much easier because the server can simply tell the client:

> Game is waiting for Player 2 to choose one of these three cards.

### Then your current game is just configuration

For example, your Town Hall isn't an engine concept.

It's data approximately equivalent to:

```text
EntityType: Headquarters

Properties:
    HP = 8
    Armor = 2

Abilities:

    Produce Gold
        Cost:
            Tap Source
        Effect:
            Source.Controller.Gold += 2

    Recruit Peasant
        Cost:
            Source.Controller.Gold -= 2
            Tap Source
        Effect:
            Create "Peasant"
            Controller = Source.Controller
            Zone = Battlefield
```

And the Town Chief:

```text
EntityType: Hero

Properties:
    Attack = 4
    HP = 5
    Armor = 2
    AP = 0

Rule:
    WHEN OwnerTurnStarted
    DO Source.AP += 1

Call to Arms:
    Cost: Source.AP -= 3

    Select:
        Battlefield entities
        controlled by Source.Controller
        tagged Peasant

    Effect:
        Attack +1
        Armor +1

    Duration:
        Until EndOfTurn
```

### The admin UI becomes almost a no-code game IDE

I'd actually think of it less as a "card administration system" and more as something like **Unity for card games**, except primarily configuration-driven.

You could have sections for:

**Game → Card Types → Properties → Resources → Zones → Phases → Events → Rules → Effects → Cards → Deck Rules → Victory Conditions**

Then a visual rule builder:

```text
WHEN [ Owner Turn Starts ]
            │
            ▼
IF [ Source is in Battlefield ]
            │
            ▼
ADD [ 1 ]
TO [ Source → Action Points ]
```

Advanced users could switch to an expression/rule representation, while normal designers use the visual editor.

The most important architectural principle I'd adopt is:

> **The engine owns execution semantics; the game definition owns game semantics.**

The engine knows how to select entities, evaluate expressions, request decisions, mutate state, emit events, resolve stacks/queues and enforce deterministic execution.

It **doesn't** know that a Paladin heals things, that Armor reduces Attack, that Heroes generate AP, or that destroying a Hero and HQ means defeat.

Those are all rules of the particular game.

If we maintain that boundary strictly, you could eventually create something resembling **Magic, Hearthstone, Pokémon, Yu-Gi-Oh!, your Town/Hero game, or a completely different original TCG** on the same backend without modifying the core engine.

===== USER =====
So I want this system to be usable for, let's say, a normal card game and for trading card games. So you should be able to configure the whole thing completely.

===== ASSISTANT =====
Yes. Then I would broaden the concept one more step: **don't build a configurable TCG engine; build a configurable card-game engine.** A TCG is then one category of game that can be defined with it.

The core engine should have almost no knowledge of cards beyond the fact that they are game objects.

### 1. Everything starts with the Game Definition

An administrator creates a game and defines things such as:

```text
Game: Example Game

Players:
  Minimum: 2
  Maximum: 4

Objects:
  Card
  Player

Zones:
  Deck
  Hand
  Table
  Discard pile

Turn structure:
  Start
  Main
  End

Win conditions:
  configurable rules
```

But none of these zones or phases should be mandatory.

For **War**, you might only need:

`Deck → Played pile → Won pile`

For **Poker**:

`Deck → Player hand → Community cards → Discard`

For your TCG:

`Deck → Hand → Battlefield → Graveyard`

---

### 2. Configurable object types

I'd introduce a generic concept called something like **GameObjectType**.

Your game could define:

```text
Card
Unit
Hero
Headquarters
Building
Token
Player
```

Another game could define:

```text
PlayingCard
Suit
Rank
```

And another:

```text
Creature
Spell
Artifact
Commander
```

Inheritance would be useful:

```text
GameObject
   └── Card
        ├── Unit
        │    ├── Hero
        │    └── Peasant
        ├── Building
        │    └── Headquarters
        └── Spell
```

Then a rule can target `Unit` and automatically include Heroes and Peasants.

---

### 3. Properties are completely configurable

Nothing like Attack or HP should exist in the engine.

The administrator creates properties:

```text
Attack : Integer
HP : Integer
Armor : Integer
Tapped : Boolean
FaceUp : Boolean
Suit : Enum
Rank : Integer
Color : Enum
```

Properties can exist on different object types.

This lets the exact same engine represent:

**Poker card**

```text
Suit = Hearts
Rank = Queen
FaceUp = false
```

and your **Peasant**

```text
Attack = 2
HP = 3
Armor = 1
Tapped = false
```

---

### 4. Generic collections/zones

Zones are especially important because traditional card games use them heavily.

A ZoneDefinition could specify:

```text
Name
Owner scope
Visibility
Ordering
Capacity
Allowed object types
```

For example:

**Hand**

```text
Owner: Player
Visibility:
    Owner = Visible
    Others = Hidden
Ordering = None
```

**Deck**

```text
Owner: Player
Visibility = Hidden
Ordering = Ordered
```

**Discard pile**

```text
Visibility = Public
Ordering = Ordered
```

Now you can express things like:

`Shuffle Deck`

`Draw top 2`

`Choose random card from Hand`

`Move card from Hand → Discard`

without hardcoding what a deck or hand means.

---

### 5. Generic actions

The engine provides primitive operations, not game rules.

For example:

```text
Create object
Destroy object
Move object
Copy object

Set property
Modify property

Add resource
Remove resource

Shuffle collection
Reorder collection

Select object(s)
Select player(s)

Reveal
Hide

Request choice

Emit event

Start phase
End phase

End game
```

These become the LEGO bricks from which games are constructed.

---

### 6. Rules

Then essentially everything uses:

> **WHEN → IF → SELECT/CHOOSE → DO → EMIT**

For your TCG:

```text
WHEN UnitAttacks

SET Damage =
    MAX(0, Attacker.Attack - Defender.Armor)

DO
    Defender.HP -= Damage
```

For a conventional card game:

```text
WHEN TurnStarted

DO
    Move top card
    FROM Deck
    TO CurrentPlayer.Hand
```

For something Uno-like:

```text
WHEN CardPlayed

IF
    Card.Rank = DrawTwo

DO
    NextPlayer draws 2 cards
```

The engine doesn't know what any of these rules mean.

---

### 7. Configurable turn structure

This needs to be much more generic than:

`Start → Main → Combat → End`

because some card games don't work that way.

I'd define:

```text
Game Flow
    └── Round
         └── Turn
              ├── Start Phase
              ├── Draw Phase
              ├── Action Phase
              └── End Phase
```

But all levels are optional/configurable.

You could have:

```text
Round
    ├── Deal
    ├── Betting
    ├── Reveal
    ├── Betting
    ├── Reveal
    └── Scoring
```

Or even simultaneous phases where all players make choices before resolution.

---

### 8. Player actions must also be configurable

There's an important difference between:

**Event rules**

> When X happens, do Y.

and:

**Available actions**

> While X is true, the player may do Y.

For example:

```text
ACTION: Attack

AVAILABLE WHEN:
    CurrentPhase = Combat
    Source.Controller = CurrentPlayer
    Source.Tapped = false

PLAYER CHOOSES:
    Source
    Target

DO:
    Emit AttackDeclared(Source, Target)
```

Likewise:

```text
ACTION: Play Card

AVAILABLE WHEN:
    Card.Zone = Player.Hand

PLAYER CHOOSES:
    Card

PAY:
    configured cost

DO:
    Move Card → Battlefield
```

This is critical because the engine needs to calculate:

> **What is this player currently allowed to do?**

The UI can then automatically present those possibilities.

---

### 9. A generic decision system

Choices should also be configurable:

```text
Choose one
Choose many
Choose number
Choose card
Choose player
Choose target
Choose ordering
Yes/No
Choose one mode
Pass
```

With constraints:

```text
Minimum selections: 1
Maximum selections: 3

WHERE:
    Object.Controller = CurrentPlayer
    AND Object.Type = Unit
```

This gives you most of the machinery needed for complex card games.

---

### 10. Randomness as a first-class engine concept

Traditional card games make this especially important.

The engine needs deterministic/server-controlled primitives:

```text
Shuffle
Random integer
Random object
Roll dice
Flip coin
Randomize order
```

And every random result should go into the game log so a match can be reproduced exactly.

---

### 11. The game state should be fully serializable

At any moment, the complete game could conceptually look like:

```text
GameInstance

Players
Objects
Properties
Resources
Zones
CurrentFlowPosition
ActivePlayer
PendingChoices
TemporaryEffects
EventQueue
RandomSeed
History
```

That's extremely valuable.

You could save a game, reload it, replay it, spectate it, undo actions where allowed, run AI players against it and inspect exactly why something happened.

---

## The architecture I would aim for

I'd ultimately separate it into three major things:

**CARD GAME ENGINE**

The generic runtime. It knows about events, state, objects, collections, rules, expressions, choices, randomness and execution.

↓

**GAME DEFINITION**

The configuration saying:

> "These are the rules of Poker."

or

> "These are the rules of Martien's TCG."

↓

**GAME INSTANCE**

An actual match between specific players using that definition.

And then separately:

**GAME DESIGNER / ADMIN**

A visual environment for building the Game Definition.

So eventually you could click:

**Create New Game**

and configure everything without writing code:

`Players → Objects → Properties → Zones → Resources → Flow → Actions → Events → Rules → Cards → Deck Construction → Win/Loss → UI`

Your Town/Hero game would therefore be an excellent **reference implementation and stress test**, but none of its concepts should leak into the generic engine unless they're genuinely universal abstractions.

At that point, you're essentially building a **no-code programming language specialized for card and turn-based games**, with a deterministic game engine executing the resulting program. That's the architecture I'd use as the foundation.

===== USER =====
So just as an example, could you show maybe in an XML file or a JSON file how, for instance, a card game like poker could be modeled in this same system. So Texas Hold'em, let's say.

===== ASSISTANT =====
Yes. Texas Hold'em is actually a very good test: if the same engine can model both your Hero/HQ TCG and Hold'em without adding poker-specific code, the abstraction is becoming genuinely generic.

Below is an illustrative JSON game definition. The important part is that concepts such as `hole cards`, `flop`, `betting`, `fold`, and `hand ranking` are **configuration**, not built into the engine.

```json
{
  "game": {
    "id": "texas-holdem",
    "name": "Texas Hold'em",
    "version": "1.0",

    "players": {
      "min": 2,
      "max": 10
    },

    "properties": {
      "suit": {
        "type": "enum",
        "values": ["clubs", "diamonds", "hearts", "spades"]
      },
      "rank": {
        "type": "enum",
        "values": [
          "2", "3", "4", "5", "6", "7", "8", "9",
          "10", "J", "Q", "K", "A"
        ]
      }
    },

    "resources": {
      "chips": {
        "scope": "player",
        "type": "integer",
        "min": 0,
        "default": 1000
      },

      "currentBet": {
        "scope": "player",
        "type": "integer",
        "min": 0,
        "default": 0,
        "reset": "betting-round"
      },

      "pot": {
        "scope": "game",
        "type": "integer",
        "min": 0,
        "default": 0
      }
    },

    "objectTypes": {
      "playingCard": {
        "properties": ["suit", "rank"]
      }
    },

    "zones": {
      "deck": {
        "scope": "game",
        "visibility": "hidden",
        "ordered": true,
        "allowedTypes": ["playingCard"]
      },

      "hand": {
        "scope": "player",
        "visibility": {
          "owner": "visible",
          "others": "hidden"
        },
        "allowedTypes": ["playingCard"]
      },

      "community": {
        "scope": "game",
        "visibility": "public",
        "ordered": true,
        "allowedTypes": ["playingCard"]
      },

      "burn": {
        "scope": "game",
        "visibility": "hidden",
        "allowedTypes": ["playingCard"]
      }
    },

    "deckDefinitions": {
      "standard52": {
        "objectType": "playingCard",

        "generate": {
          "forEach": {
            "suit": ["clubs", "diamonds", "hearts", "spades"],
            "rank": [
              "2", "3", "4", "5", "6", "7",
              "8", "9", "10", "J", "Q", "K", "A"
            ]
          }
        }
      }
    },

    "events": [
      "gameStarted",
      "handStarted",
      "bettingRoundStarted",
      "playerTurnStarted",
      "playerFolded",
      "betPlaced",
      "bettingRoundCompleted",
      "showdownStarted",
      "handEnded"
    ],

    "flow": {
      "repeat": "untilGameEnds",

      "hand": [
        "setup",
        "preFlop",
        "flop",
        "turn",
        "river",
        "showdown"
      ]
    },

    "phases": {
      "setup": {
        "onEnter": [
          {
            "action": "moveAll",
            "from": ["hand", "community", "burn"],
            "to": "deck"
          },
          {
            "action": "shuffle",
            "zone": "deck"
          },
          {
            "action": "rotateDealer"
          },
          {
            "action": "collectForcedBet",
            "player": "smallBlindPlayer",
            "amount": 5
          },
          {
            "action": "collectForcedBet",
            "player": "bigBlindPlayer",
            "amount": 10
          },
          {
            "action": "deal",
            "from": "deck",
            "to": "eachPlayer.hand",
            "count": 2
          }
        ]
      },

      "preFlop": {
        "run": "bettingRound"
      },

      "flop": {
        "onEnter": [
          {
            "action": "move",
            "from": "deck.top",
            "to": "burn",
            "count": 1
          },
          {
            "action": "move",
            "from": "deck.top",
            "to": "community",
            "count": 3
          }
        ],

        "run": "bettingRound"
      },

      "turn": {
        "onEnter": [
          {
            "action": "move",
            "from": "deck.top",
            "to": "burn",
            "count": 1
          },
          {
            "action": "move",
            "from": "deck.top",
            "to": "community",
            "count": 1
          }
        ],

        "run": "bettingRound"
      },

      "river": {
        "onEnter": [
          {
            "action": "move",
            "from": "deck.top",
            "to": "burn",
            "count": 1
          },
          {
            "action": "move",
            "from": "deck.top",
            "to": "community",
            "count": 1
          }
        ],

        "run": "bettingRound"
      },

      "showdown": {
        "onEnter": [
          {
            "action": "emit",
            "event": "showdownStarted"
          }
        ]
      }
    },

    "actions": {
      "fold": {
        "availableWhen": [
          "player == activePlayer",
          "player.status == active"
        ],

        "effects": [
          {
            "set": "player.status",
            "value": "folded"
          },
          {
            "emit": "playerFolded"
          }
        ]
      },

      "check": {
        "availableWhen": [
          "player == activePlayer",
          "player.currentBet == game.requiredBet"
        ],

        "effects": [
          {
            "action": "completePlayerAction"
          }
        ]
      },

      "call": {
        "availableWhen": [
          "player == activePlayer",
          "player.currentBet < game.requiredBet"
        ],

        "variables": {
          "amount": "game.requiredBet - player.currentBet"
        },

        "effects": [
          {
            "transfer": "amount",
            "from": "player.chips",
            "to": "game.pot"
          },
          {
            "add": "player.currentBet",
            "value": "amount"
          }
        ]
      },

      "raise": {
        "availableWhen": [
          "player == activePlayer"
        ],

        "choose": {
          "type": "integer",
          "name": "raiseAmount",
          "min": "game.minimumRaise",
          "max": "player.chips"
        },

        "effects": [
          {
            "action": "placeBet",
            "amount": "raiseAmount"
          },
          {
            "set": "game.requiredBet",
            "value": "player.currentBet"
          }
        ]
      }
    },

    "rules": [
      {
        "id": "only-one-player-remains",

        "when": "playerFolded",

        "if": {
          "expression": "count(players where status == active) == 1"
        },

        "then": [
          {
            "set": "winner",
            "value": "first(players where status == active)"
          },
          {
            "transfer": "game.pot",
            "to": "winner.chips"
          },
          {
            "emit": "handEnded"
          }
        ]
      },

      {
        "id": "showdown",

        "when": "showdownStarted",

        "then": [
          {
            "forEach": "activePlayers",
            "set": "player.handScore",
            "value": {
              "evaluate": "PokerHand",
              "cards": "player.hand + game.community"
            }
          },

          {
            "set": "winners",
            "value": "playersWithHighest(handScore)"
          },

          {
            "action": "splitResource",
            "resource": "game.pot",
            "between": "winners"
          },

          {
            "emit": "handEnded"
          }
        ]
      }
    ]
  }
}
```

There is one part here that exposes a particularly interesting architectural question:

```json
{
  "evaluate": "PokerHand",
  "cards": "player.hand + game.community"
}
```

I **wouldn't** build `PokerHand` into the engine. Instead, I'd make **evaluators configurable** as well.

For example, the administrator could define:

```json
{
  "evaluator": {
    "id": "PokerHand",
    "input": {
      "type": "collection",
      "objectType": "playingCard"
    },

    "select": {
      "count": 5,
      "from": "input",
      "strategy": "highestScore"
    },

    "rankings": [
      {
        "name": "Royal Flush",
        "score": 10,
        "condition": "isRoyalFlush(selection)"
      },
      {
        "name": "Straight Flush",
        "score": 9,
        "condition": "isStraight(selection) && isFlush(selection)"
      },
      {
        "name": "Four of a Kind",
        "score": 8,
        "condition": "maxSameRank(selection) == 4"
      },
      {
        "name": "Full House",
        "score": 7,
        "condition": "hasRankGroups(selection, [3,2])"
      },
      {
        "name": "Flush",
        "score": 6,
        "condition": "isFlush(selection)"
      },
      {
        "name": "Straight",
        "score": 5,
        "condition": "isStraight(selection)"
      },
      {
        "name": "Three of a Kind",
        "score": 4,
        "condition": "maxSameRank(selection) == 3"
      },
      {
        "name": "Two Pair",
        "score": 3,
        "condition": "countRankGroups(selection, 2) == 2"
      },
      {
        "name": "Pair",
        "score": 2,
        "condition": "countRankGroups(selection, 2) == 1"
      },
      {
        "name": "High Card",
        "score": 1,
        "condition": "true"
      }
    ]
  }
}
```

This highlights what I think should be the core architecture.

The engine provides **generic primitives**:

`Move`, `Shuffle`, `Select`, `Count`, `Compare`, `Set`, `Add`, `Transfer`, `Choose`, `Repeat`, `Emit event`, `Evaluate expression`, `Start/end phase`, etc.

Texas Hold'em provides the **meaning**.

Your TCG could then use exactly those same primitives to express:

```text
Town Chief gains 1 AP
```

as:

```json
{
  "when": "ownerTurnStarted",
  "then": {
    "add": "source.actionPoints",
    "value": 1
  }
}
```

And:

> Call to Arms: Spend 3 of **this Hero's** AP. All friendly Peasants get +1 Attack and +1 Armor until end of turn.

becomes roughly:

```json
{
  "cost": {
    "subtract": "source.actionPoints",
    "value": 3
  },

  "select": {
    "from": "battlefield",
    "where": [
      "entity.controller == source.controller",
      "entity.tags contains 'peasant'"
    ]
  },

  "then": [
    {
      "modify": "selected.attack",
      "value": 1,
      "duration": "endOfTurn"
    },
    {
      "modify": "selected.armor",
      "value": 1,
      "duration": "endOfTurn"
    }
  ]
}
```

That's the test I would continually apply while designing the architecture:

> **Can Texas Hold'em and our Hero/HQ game both be represented without writing custom engine code?**

If the answer is yes, we're probably putting the abstraction at approximately the right level. If implementing poker requires adding `PokerGame`, `PokerPlayer`, `CommunityCards`, `Flop`, etc. to the core C# code, we've made the engine too specific.

===== USER =====
So can you now list all the rules and settings that are defined in this system?

===== ASSISTANT =====
Yes. Based on what we've established, I would define the platform as a **generic card-game definition system**. The engine itself contains primitives; an individual game configures the following settings and rules.

## 1. Game settings

| Setting | Examples |
|---|---|
| Game ID / name / version | Texas Hold'em, Town Wars |
| Minimum players | 2 |
| Maximum players | 10 |
| Teams allowed | Yes/no |
| Starting player selection | Random, dealer, chosen |
| Game termination | Rule-driven |
| Randomness | Seeded/deterministic |
| Information model | Public/private/owner-only |

There should be almost no assumptions such as "a game has two players."

---

## 2. Object types

Define what kinds of things can exist.

Examples:

```text
Card
Unit
Hero
Building
Headquarters
Token
Player
```

Or for poker simply:

```text
PlayingCard
Player
```

Object types can inherit from other types:

```text
Card
 ├─ Unit
 │   ├─ Hero
 │   └─ Soldier
 ├─ Building
 └─ Spell
```

### Object-type settings

- Name
- Parent type
- Allowed properties
- Allowed resources
- Allowed actions
- Tags
- Zone restrictions
- Visibility
- Can be created/destroyed
- Quantity restrictions

---

## 3. Properties

Arbitrary values can be attached to objects, players or the game.

Examples:

```text
Attack = 4
HP = 5
Armor = 2

Tapped = true

Suit = Hearts
Rank = Queen
```

Supported property types should include:

- Integer
- Decimal
- Boolean
- String
- Enum
- Object reference
- Player reference
- Collection
- Date/time if ever needed

Properties can have defaults, minimums, maximums and validation rules.

---

## 4. Resources

Resources are state values intended to be gained/spent.

Most importantly, **scope is configurable**.

```text
Gold
Scope: Player

Action Points
Scope: Entity

Pot
Scope: Game

Team Mana
Scope: Team
```

Settings include:

- Scope
- Initial value
- Minimum
- Maximum
- Can go negative
- Reset conditions
- Visibility

Therefore every Town Chief has **his own AP pool**, rather than the player owning one AP pool.

---

## 5. Tags

Objects can have arbitrary classifications:

```text
Peasant
Human
Worker
Builder
Undead
Fire
Flying
Legendary
```

Rules can query them:

```text
Select all objects
WHERE
    Controller = Source.Controller
    AND Tags contains Peasant
```

---

## 6. Zones / collections

Games define their own locations.

Examples:

```text
Deck
Hand
Battlefield
Graveyard
Exile
Community
Burn pile
Discard pile
```

Each zone defines:

- Owner: player/team/game/entity
- Public/private
- Who can inspect it
- Ordered/unordered
- Face-up/down rules
- Maximum capacity
- Allowed object types
- Whether objects can enter/leave
- Default insertion position

---

# 7. Game flow

Games define their own hierarchy of:

```text
Game
 → Round
    → Turn
       → Phase
          → Step
```

None need to be mandatory.

For your TCG:

```text
Turn
 ├─ Start
 ├─ Main
 ├─ Combat
 └─ End
```

Poker could instead use:

```text
Hand
 ├─ Setup
 ├─ Pre-Flop
 ├─ Flop
 ├─ Turn
 ├─ River
 └─ Showdown
```

Flow nodes can repeat, skip, branch or terminate according to rules.

---

# 8. Events

Anything happening can produce an event.

Generic examples:

```text
GameStarted
FlowStarted
FlowEnded
PropertyChanged
ResourceChanged
ObjectCreated
ObjectMoved
ObjectDestroyed
ActionStarted
ActionCompleted
ChoiceMade
```

Games can create their own:

```text
UnitAttacked
DamageDealt
HeroKilled
CardPlayed

PlayerFolded
BetPlaced
ShowdownStarted
```

Events can carry data:

```text
DamageDealt {
    source
    target
    amount
    controller
}
```

---

# 9. Rules

This is the central system.

Every rule follows approximately:

```text
WHEN
    event

IF
    conditions

SELECT
    relevant objects

CHOOSE
    optional player decision

DO
    effects

EMIT
    optional events
```

For example:

```text
WHEN DamageCalculated

SET Damage =
    MAX(Attack - Armor, 0)
```

---

# 10. Conditions

The condition engine needs generic operators:

```text
=
!=
>
>=
<
<=

AND
OR
NOT

contains
not contains

exists
doesn't exist

in
not in
```

And collection operations:

```text
Any
All
None
Count
First
Last
Min
Max
Sum
```

So:

```text
IF
    Hero.HP <= 0
AND
    Headquarters.HP <= 0

THEN
    Player loses
```

---

# 11. Expressions/calculations

Values should be calculated dynamically.

For example:

```text
MAX(Source.Attack - Target.Armor, 0)
```

or:

```text
Count(FriendlyPeasants) * 2
```

or:

```text
Source.ActionPoints + 1
```

Expressions can reference:

- Source
- Target
- Controller
- Owner
- Active player
- Selected objects
- Event data
- Properties
- Resources
- Zones
- Game state
- Variables
- Previous results

---

# 12. Selectors

Rules need a generic query mechanism.

For example:

```text
SELECT
    objects

FROM
    Battlefield

WHERE
    Controller = Source.Controller
AND
    Type inherits Unit
AND
    Tags contains Peasant

LIMIT
    3
```

Selection modes include:

- All
- First
- Last
- Top
- Bottom
- Random
- N objects
- Minimum/maximum
- Filtered
- Sorted

---

# 13. Player choices

A rule can suspend execution and request input.

Choice types:

- Yes/no
- Choose one
- Choose N
- Choose up to N
- Choose object/card
- Choose player
- Choose target
- Choose number
- Choose mode
- Choose order
- Choose resource amount
- Pass

For example:

```text
Choose one friendly Unit.
```

creates a `PendingDecision`.

The rule continues after the player responds.

---

# 14. Actions

This is different from triggered rules.

An **Action Definition** determines what a player is currently allowed to initiate.

Examples:

```text
Play Card
Attack
Activate Ability
Fold
Check
Call
Raise
Pass
```

An action defines:

```text
AVAILABLE WHEN
    conditions

SELECT/CHOOSE
    inputs

COST
    costs

DO
    effects
```

The engine can consequently ask:

> What actions are currently available to Player 1?

This can drive the client UI automatically.

---

# 15. Costs

Costs need to be generic too.

Examples:

```text
Spend 3 Gold
Spend 2 AP
Tap this object
Discard 1 card
Sacrifice a unit
Lose 2 HP
Remove a counter
```

Multiple costs can be combined.

Crucially:

```text
Spend Source.ActionPoints
```

is different from:

```text
Spend Source.Controller.Gold
```

---

# 16. Effects

These are the fundamental building blocks of the engine.

I'd initially provide primitives such as:

```text
Create object
Destroy object
Move object
Copy object

Set value
Add value
Subtract value
Multiply value

Add/remove tag

Add/remove resource
Transfer resource

Shuffle
Sort
Reorder

Reveal
Hide

Select

Request choice

Start/end flow node

Emit event

Create temporary modifier
Remove modifier

End game
Declare winner/loser
```

Complex effects are composed from these primitives rather than implemented individually.

---

# 17. Temporary and continuous effects

Necessary for TCGs.

For example:

> Peasants get +1 Attack/+1 Armor until end of turn.

Internally:

```text
Modifier

Target:
    selected Peasants

Attack:
    +1

Armor:
    +1

Expires:
    EndOfTurn
```

Durations could include:

```text
Permanent
Until end of turn
Until start of next turn
For N turns
Until event X
While source exists
While condition is true
```

---

# 18. Replacement/interception rules

Events need a lifecycle.

For example:

```text
Attack
 ↓
Damage proposed
 ↓
Modifiers/replacements
 ↓
Damage calculated
 ↓
Damage applied
 ↓
Damage dealt
 ↓
HP changed
 ↓
Possible death
```

That enables cards saying:

> Prevent the next 3 damage.

> Double the damage.

> Ignore Armor.

> Redirect damage.

> If this would kill the unit, leave it at 1 HP.

---

# 19. Randomness

Generic primitives:

```text
Shuffle
Random object
Random player
Random number
Coin flip
Dice roll
Random ordering
```

Preferably using deterministic seeded randomness so games can be replayed exactly.

---

# 20. Card/object definitions

Actual cards are then mostly **data assembled from everything above**.

For example:

```text
Town Chief

Type:
    Hero

Properties:
    Attack = 4
    HP = 5
    Armor = 2

Resources:
    ActionPoints = 0

Rules:
    Start of owner's turn
        → Gain 1 Source.ActionPoints

Abilities:
    Call to Arms
    Regenerate
```

There shouldn't be a special `Hero` class in the engine unless the game definition created one.

---

# 21. Deck construction

For TCGs you additionally need configurable deck rules:

```text
Minimum deck size
Maximum deck size
Copies per card
Sideboard size
Allowed sets
Allowed card types
Banned cards
Required cards
Commander/Hero requirements
Faction restrictions
```

Traditional games can simply have a predefined deck.

---

# 22. Setup rules

Completely configurable:

```text
Create players
Create decks
Shuffle
Draw N cards
Place starting objects
Set resources
Choose dealer
Choose starting player
Mulligan
```

Your TCG could say:

```text
Each player:
    Put chosen HQ on Battlefield
    Put chosen Hero on Battlefield
    Shuffle deck
    Draw 5
```

Poker instead performs its poker setup.

---

# 23. Win/loss/draw rules

Nothing hardcoded.

Your game:

```text
WHEN ObjectDestroyed

IF
    player's Hero destroyed
AND
    player's HQ destroyed

DO
    Player loses
```

Another game:

```text
IF Player.Score >= 100
    Player wins
```

Poker determines the winner of each hand independently from the termination of the overall game.

---

# 24. Rule priority and conflict resolution

This will become essential once games get complicated.

Rules need things such as:

```text
Priority
Execution order
Mandatory/optional
Controller ordering
Replacement precedence
Simultaneous event handling
```

For example, if five abilities trigger when a creature dies, the system needs a deterministic method for resolving them.

---

# 25. State and history

Every game instance maintains:

```text
Players
Objects
Zones
Properties
Resources
Current flow position
Active player(s)
Modifiers
Pending decisions
Event queue
Random seed/state
Action history
Rule execution history
```

I'd make the history particularly detailed. You want to be able to inspect:

```text
Town Chief activated Call to Arms
→ Paid 3 AP
→ Selector found Peasant #17 and #21
→ Peasant #17 Attack 2 → 3
→ Peasant #17 Armor 1 → 2
→ Peasant #21 Attack 2 → 3
→ Peasant #21 Armor 1 → 2
→ 4 modifiers scheduled for expiration at EndOfTurn
```

That will be invaluable for debugging user-created games.

---

## The resulting conceptual model

I'd reduce the whole platform to approximately:

```text
GAME DEFINITION
│
├── Object Types
├── Properties
├── Resources
├── Tags
├── Zones
│
├── Players / Teams
├── Game Flow
├── Events
│
├── Expressions
├── Conditions
├── Selectors
├── Choices
│
├── Actions
├── Costs
├── Effects
├── Modifiers
├── Rules
│
├── Cards / Objects
├── Deck Definitions
├── Setup Rules
└── Win/Loss Rules

             ↓

       GENERIC ENGINE

             ↓

        GAME INSTANCE
```

The key architectural test remains: **"Gold", "Armor", "Attack", "Hero", "Peasant", "Poker", "Flop", "Fold", and "Call to Arms" should occur in game definitions, never as assumptions in the core engine.**

The core engine should mainly understand concepts such as **object, value, collection, scope, event, condition, expression, selector, action, choice, effect, modifier and rule**.

That is generic enough that you're approaching a **domain-specific programming language for constructing card games**, rather than simply a configurable TCG.

===== USER =====
So, can you now set up the definition in the same way for our trading card game?

===== ASSISTANT =====
Yes. Using the same conceptual format as the Texas Hold'em example, the first playable definition of your TCG could look like this. I would deliberately configure even things like **Armor reducing damage** and **Hero AP generation** as rules rather than engine behavior.

```json
{
  "game": {
    "id": "town-tcg",
    "name": "Town TCG",
    "version": "0.1",

    "players": {
      "min": 2,
      "max": 2
    },

    "objectTypes": {
      "card": {},

      "character": {
        "inherits": "card",
        "properties": [
          "attack",
          "health",
          "armor",
          "tapped"
        ]
      },

      "hero": {
        "inherits": "character",
        "resources": [
          "actionPoints"
        ]
      },

      "unit": {
        "inherits": "character"
      },

      "building": {
        "inherits": "card",
        "properties": [
          "health",
          "armor",
          "tapped"
        ]
      },

      "headquarters": {
        "inherits": "building"
      }
    },

    "properties": {
      "attack": {
        "type": "integer",
        "min": 0,
        "default": 0
      },

      "health": {
        "type": "integer",
        "min": 0
      },

      "armor": {
        "type": "integer",
        "min": 0,
        "default": 0
      },

      "tapped": {
        "type": "boolean",
        "default": false
      }
    },

    "resources": {
      "gold": {
        "scope": "player",
        "type": "integer",
        "min": 0,
        "default": 0
      },

      "actionPoints": {
        "scope": "entity",
        "type": "integer",
        "min": 0,
        "default": 0
      }
    },

    "tags": [
      "peasant",
      "worker",
      "builder"
    ],

    "zones": {
      "deck": {
        "scope": "player",
        "visibility": "hidden",
        "ordered": true
      },

      "hand": {
        "scope": "player",
        "visibility": {
          "owner": "visible",
          "others": "hidden"
        }
      },

      "battlefield": {
        "scope": "player",
        "visibility": "public"
      },

      "discard": {
        "scope": "player",
        "visibility": "public",
        "ordered": true
      }
    },

    "flow": {
      "repeat": "untilGameEnds",

      "turn": [
        "start",
        "main",
        "combat",
        "end"
      ]
    },

    "events": [
      "gameStarted",
      "turnStarted",
      "phaseStarted",
      "phaseEnded",
      "turnEnded",

      "cardDrawn",
      "cardPlayed",

      "abilityActivated",

      "attackDeclared",
      "damageProposed",
      "damageCalculated",
      "damageDealt",

      "healthChanged",
      "entityDestroyed",

      "resourceChanged"
    ],

    "setup": [
      {
        "action": "placeSelectedStartingCard",
        "type": "headquarters",
        "zone": "battlefield"
      },

      {
        "action": "placeSelectedStartingCard",
        "type": "hero",
        "zone": "battlefield"
      },

      {
        "action": "shuffle",
        "zone": "deck"
      },

      {
        "action": "draw",
        "count": 5
      }
    ],

    "phases": {
      "start": {
        "onEnter": [
          {
            "action": "emit",
            "event": "turnStarted"
          },

          {
            "action": "untapAll",
            "selector": {
              "zone": "battlefield",
              "controller": "activePlayer"
            }
          }
        ]
      },

      "main": {
        "allowedActions": [
          "playCard",
          "activateAbility"
        ]
      },

      "combat": {
        "allowedActions": [
          "attack",
          "activateAbility"
        ]
      },

      "end": {
        "onExit": [
          {
            "action": "removeModifiers",
            "duration": "endOfTurn"
          },

          {
            "action": "emit",
            "event": "turnEnded"
          }
        ]
      }
    },

    "actions": {
      "playCard": {
        "availableWhen": [
          "source.zone == source.controller.hand",
          "source.controller == activePlayer"
        ],

        "cost": {
          "payConfiguredCardCost": "source"
        },

        "effects": [
          {
            "move": "source",
            "to": "source.controller.battlefield"
          },

          {
            "emit": "cardPlayed"
          }
        ]
      },

      "activateAbility": {
        "availableWhen": [
          "source.controller == activePlayer",
          "source.zone == source.controller.battlefield",
          "ability.conditions == true",
          "ability.costsCanBePaid == true"
        ],

        "effects": [
          {
            "execute": "ability"
          }
        ]
      },

      "attack": {
        "availableWhen": [
          "source.controller == activePlayer",
          "source.zone == source.controller.battlefield",
          "source.attack > 0",
          "source.tapped == false"
        ],

        "choose": {
          "name": "target",
          "type": "entity",
          "where": [
            "target.controller != source.controller",
            "target.zone == battlefield"
          ]
        },

        "effects": [
          {
            "set": "source.tapped",
            "value": true
          },

          {
            "emit": {
              "event": "attackDeclared",
              "source": "source",
              "target": "target"
            }
          }
        ]
      }
    },

    "rules": [
      {
        "id": "resolve-attack",

        "when": "attackDeclared",

        "then": [
          {
            "emit": {
              "event": "damageProposed",
              "source": "event.source",
              "target": "event.target",
              "amount": "event.source.attack"
            }
          }
        ]
      },

      {
        "id": "armor-reduces-damage",

        "when": "damageProposed",

        "then": [
          {
            "setEventValue": "amount",
            "value": "max(0, event.amount - event.target.armor)"
          },

          {
            "emit": {
              "event": "damageCalculated",
              "source": "event.source",
              "target": "event.target",
              "amount": "event.amount"
            }
          }
        ]
      },

      {
        "id": "apply-damage",

        "when": "damageCalculated",

        "then": [
          {
            "subtract": "event.target.health",
            "value": "event.amount"
          },

          {
            "emit": {
              "event": "damageDealt",
              "source": "event.source",
              "target": "event.target",
              "amount": "event.amount"
            }
          }
        ]
      },

      {
        "id": "destroy-zero-health-entity",

        "when": "healthChanged",

        "if": {
          "expression": "event.entity.health <= 0"
        },

        "then": [
          {
            "move": "event.entity",
            "to": "event.entity.controller.discard"
          },

          {
            "emit": {
              "event": "entityDestroyed",
              "entity": "event.entity"
            }
          }
        ]
      },

      {
        "id": "player-defeat",

        "when": "entityDestroyed",

        "if": {
          "expression":
            "event.entity.controller.heroDestroyed == true && event.entity.controller.headquartersDestroyed == true"
        },

        "then": [
          {
            "action": "loseGame",
            "player": "event.entity.controller"
          }
        ]
      }
    ],

    "cards": {

      "townHall": {
        "name": "Town Hall",
        "type": "headquarters",

        "properties": {
          "health": 8,
          "armor": 2
        },

        "abilities": [
          {
            "id": "gainGold",
            "name": "Collect Taxes",

            "costs": [
              {
                "set": "source.tapped",
                "value": true
              }
            ],

            "effects": [
              {
                "add": "source.controller.gold",
                "value": 2
              }
            ]
          },

          {
            "id": "summonPeasant",
            "name": "Recruit Peasant",

            "costs": [
              {
                "subtract": "source.controller.gold",
                "value": 2
              },

              {
                "set": "source.tapped",
                "value": true
              }
            ],

            "effects": [
              {
                "create": "peasant",
                "controller": "source.controller",
                "zone": "battlefield"
              }
            ]
          }
        ]
      },

      "townChief": {
        "name": "Town Chief",
        "type": "hero",

        "properties": {
          "attack": 4,
          "health": 5,
          "armor": 2
        },

        "resources": {
          "actionPoints": 0
        },

        "rules": [
          {
            "when": "turnStarted",

            "if": {
              "expression":
                "activePlayer == source.controller"
            },

            "then": [
              {
                "add": "source.actionPoints",
                "value": 1
              }
            ]
          }
        ],

        "abilities": [
          {
            "id": "callToArms",
            "name": "Call to Arms",

            "costs": [
              {
                "subtract": "source.actionPoints",
                "value": 3
              }
            ],

            "select": {
              "name": "peasants",

              "from": "source.controller.battlefield",

              "where": [
                "entity.tags contains 'peasant'"
              ],

              "mode": "all"
            },

            "effects": [
              {
                "modify": "peasants.attack",
                "value": 1,
                "duration": "endOfTurn"
              },

              {
                "modify": "peasants.armor",
                "value": 1,
                "duration": "endOfTurn"
              }
            ]
          },

          {
            "id": "regenerate",
            "name": "Regenerate",

            "costs": [
              {
                "subtract": "source.actionPoints",
                "value": 2
              },

              {
                "set": "source.tapped",
                "value": true
              }
            ],

            "effects": [
              {
                "heal": "source",
                "value": 3
              }
            ]
          }
        ]
      },

      "paladin": {
        "name": "Paladin",
        "type": "hero",

        "properties": {
          "attack": 3,
          "health": 7,
          "armor": 3
        },

        "resources": {
          "actionPoints": 0
        },

        "rules": [
          {
            "when": "turnStarted",

            "if": {
              "expression":
                "activePlayer == source.controller"
            },

            "then": [
              {
                "add": "source.actionPoints",
                "value": 1
              }
            ]
          }
        ],

        "abilities": [
          {
            "id": "divineShield",
            "name": "Divine Shield",

            "costs": [
              {
                "subtract": "source.actionPoints",
                "value": 2
              }
            ],

            "choose": {
              "name": "target",
              "type": "entity",

              "where": [
                "target.controller == source.controller",
                "target.zone == battlefield"
              ]
            },

            "effects": [
              {
                "modify": "target.armor",
                "value": 2,
                "duration": "startOfNextOwnerTurn"
              }
            ]
          },

          {
            "id": "holyLight",
            "name": "Holy Light",

            "costs": [
              {
                "subtract": "source.actionPoints",
                "value": 3
              },

              {
                "set": "source.tapped",
                "value": true
              }
            ],

            "choose": {
              "name": "target",

              "type": "entity",

              "where": [
                "target.controller == source.controller",
                "target != source",
                "target.zone == battlefield"
              ]
            },

            "effects": [
              {
                "heal": "target",
                "value": 3
              }
            ]
          }
        ]
      },

      "peasant": {
        "name": "Peasant",
        "type": "unit",

        "tags": [
          "peasant",
          "worker",
          "builder"
        ],

        "properties": {
          "attack": 2,
          "health": 3,
          "armor": 1
        }
      }
    }
  }
}
```

There are a few places where I'd make this **even more generic** before implementing it.

For example, I used:

```text
"heal"
"attack"
"untapAll"
"loseGame"
```

for readability. In the production schema, most of those should probably **not be engine primitives**.

`Heal 3` is really:

```text
SET target.health =
    MIN(
        target.health + 3,
        target.maxHealth
    )
```

Likewise, "untap" is just:

```text
SET tapped = false
```

And even **damage** is not fundamentally an engine concept. It's a chain of configured events and state modifications.

There's also one property missing from our earlier design that we'd almost certainly want: **maximum health**. Otherwise, when the Paladin heals something, the engine doesn't know whether a 5-HP Hero currently at 4 HP can be healed to 5 or 7. I'd therefore probably model:

```text
baseHealth = 5
currentHealth = 5
```

rather than a single `health` property.

The really important result, though, is that the exact same generic engine that executed the Texas Hold'em definition can execute this definition:

**Poker:** shuffle → deal → choose bet → move chips → evaluate collections.

**Our TCG:** summon → attack → calculate armor → modify HP → activate abilities → accumulate entity-specific AP.

No `TownChief.cs`, `Peasant.cs`, `PokerPlayer.cs` or `TexasHoldemGame.cs` should be required. Those concepts exist entirely in the definitions.

===== USER =====
And I think that we can have different templates maybe then for different kinds of games. So we can have a regular card game with playing cards, where you can select this template and build from it. Or you can have a very complicated trading card game.

===== ASSISTANT =====
Yes. I think **templates should sit above the generic engine**, rather than creating separate engines. Every template simply preconfigures the same underlying system.

A user could start with:

| Template | Preconfigured |
|---|---|
| **Blank Game** | Nothing except basic engine primitives |
| **Classic Playing Cards** | 52-card deck, suits, ranks, deck/hand/table/discard, shuffle/deal |
| **Trick-Taking Game** | Playing cards + tricks, trump, rounds, scoring |
| **Poker-Style Game** | Playing cards + community cards, betting, chips, hand evaluation |
| **Simple Battle Card Game** | Units, attack/health, battlefield, combat |
| **Trading Card Game** | Deck construction, costs, units, effects, turns, zones |
| **Advanced TCG** | Events, triggers, modifiers, stack/priority, reactions, targeting |
| **Town TCG** | Your actual game as a complete starting definition |

The important point is that selecting **Advanced TCG** doesn't turn on special TCG code. It imports a collection of definitions.

For example, selecting **Classic Playing Cards** might automatically create:

```text id="gzn4w8"
OBJECT TYPES
Playing Card

PROPERTIES
Suit
Rank
Face Up

ZONES
Deck
Hand
Table
Discard

SUITS
♠ Spades
♥ Hearts
♦ Diamonds
♣ Clubs

RANKS
2–10
Jack
Queen
King
Ace

DECK
4 suits × 13 ranks = 52 cards

ACTIONS / EFFECTS
Shuffle
Deal
Draw
Play
Reveal
Discard
```

Then the designer starts defining the actual game.

If they select **Advanced TCG**, considerably more gets generated:

```text id="csxg1p"
OBJECT TYPES
Card
 ├── Unit
 ├── Spell
 ├── Building
 └── ...

PROPERTIES
Attack
Health
Max Health
Armor
Tapped
Cost
...

ZONES
Deck
Hand
Battlefield
Discard
Exile

FLOW
Game
 └── Turn
      ├── Start
      ├── Main
      ├── Combat
      └── End

SYSTEMS
Targeting
Combat
Temporary modifiers
Triggered abilities
Activated abilities
Costs
Resources
Status effects
Deck construction
Win/loss rules
...
```

But the designer can delete or modify all of it.

### Templates can inherit from other templates

This is where it becomes particularly useful.

Instead of maintaining everything independently:

```text id="2w2kkp"
Blank Game
│
├── Classic Playing Cards
│    ├── Poker
│    │    └── Texas Hold'em
│    ├── Trick Taking
│    └── Draw/Discard Game
│
└── Card Battler
     │
     └── Trading Card Game
          │
          ├── Advanced TCG
          │
          └── Town TCG
```

For example:

**Texas Hold'em template**

inherits:

**Poker template**

inherits:

**Classic Playing Cards template**

inherits:

**Blank Game**

That also means someone could start with Texas Hold'em and modify it:

> Start with Texas Hold'em, but everyone gets three private cards.

Change:

```text id="d7m0ab"
HoleCards = 2
```

to:

```text id="d3snwl"
HoleCards = 3
```

Or create something bizarre:

> Poker where Jacks are wild and winning a hand gives you Mana that can be spent on special abilities.

The generic engine doesn't care.

### Template packages

Architecturally, I'd make templates essentially **packages of definitions**.

A template could contain:

```text id="3yq74i"
Template
│
├── ObjectTypes
├── Properties
├── Resources
├── Tags
├── Zones
├── Events
├── Expressions
├── Selectors
├── Effects
├── Actions
├── Rules
├── Flow
├── Evaluators
├── DeckDefinitions
├── UI configuration
└── Example cards
```

This creates another useful feature: **smaller reusable modules**.

Instead of only templates, you could eventually have **rule modules**.

For example:

```text id="p3crlb"
52-Card Deck
Dice
Betting
Turn System
Mana System
Attack/Defense
Armor
Health
Deck Building
Drafting
Status Effects
Stack & Priority
Trick Taking
Score System
Teams
```

A designer could install those into their game.

So someone could say:

> Blank Game → add 52-Card Deck → add Teams → add Trick Taking → add Mana.

Now they've created the foundation for an unusual magic-based team trick-taking game without us ever having anticipated that game.

### Three levels in the designer

I think this gives the management application a very natural UX.

When creating a game:

**1. Choose template**

> What kind of game do you want to create?

**2. Configure game**

A friendly UI exposes common settings relevant to that template.

For Classic Cards:

> Number of players  
> Cards per player  
> Jokers?  
> Hand visibility  
> Turn order  
> Winning condition

For TCG:

> Starting hand  
> Deck size  
> Card types  
> Resources  
> Turn phases  
> Combat rules

**3. Advanced Rule Designer**

Then the user can go underneath all of that and manipulate the actual generic definitions:

> Events → Conditions → Selectors → Choices → Effects → Rules

This gives you both extremes: someone can create a simple conventional card game without understanding a rule engine, while an advanced designer can effectively **program a new TCG without writing conventional code**.

And I would add one architectural constraint: **anything created through the friendly template UI must be representable in and editable through the advanced rule system.** That prevents us from accidentally building two separate systems. The templates and simple editors are just higher-level interfaces for generating the same underlying game definition.

===== USER =====
So basically what we're doing is we are building an ontology for a trading card game, it seems, right?

===== ASSISTANT =====
Yes—with one qualification: you're building **an ontology plus an executable rule model** for card games.

The ontology describes **what exists and how concepts relate**:

```text
Game
├── Player
├── Game Object
│   └── Card
│       ├── Unit
│       ├── Hero
│       ├── Building
│       └── Spell
├── Zone
│   ├── Deck
│   ├── Hand
│   └── Battlefield
├── Property
├── Resource
├── Action
├── Event
├── Rule
├── Effect
└── Choice
```

But you're going beyond a conventional ontology because the definitions also specify **behavior**:

```text
WHEN event
IF condition
SELECT targets
ASK choice
PAY cost
DO effects
EMIT events
```

So I'd describe it as a **card-game domain model / ontology combined with a declarative rules language**.

And because you've decided that even concepts such as `Hero`, `Attack`, `Armor`, `Gold`, `Flop`, etc. aren't fundamental, there's actually a hierarchy:

```text
GENERIC GAME ONTOLOGY
        │
        ▼
CARD-GAME ONTOLOGY
        │
        ├── Classic-card template
        │       └── Poker
        │             └── Texas Hold'em
        │
        └── TCG template
                └── Town TCG
```

The most interesting part is that your **templates effectively become specialized ontologies**. The TCG template introduces concepts such as units, stats and abilities, while the Poker template introduces ranks, suits, betting and hand evaluation.

So yes: thinking about it as an ontology is useful. It also gives you a good architectural test:

> **If I invent a completely new card-game concept tomorrow, can I describe that concept by extending the ontology rather than changing the engine?**

If the answer is consistently yes, you've achieved the level of configurability you're aiming for.

===== USER =====
So what objects does our ontology now consist of, for instance for the trading card game?

===== ASSISTANT =====
Yes. For the **generic TCG template**, I would currently model the ontology roughly like this. Importantly, some things are *ontology objects* while others are *instances* of those objects.

```text
GameDefinition
│
├── ObjectTypeDefinition
├── PropertyDefinition
├── ResourceDefinition
├── TagDefinition
├── ZoneDefinition
│
├── FlowDefinition
│   ├── RoundDefinition
│   ├── TurnDefinition
│   ├── PhaseDefinition
│   └── StepDefinition
│
├── EventDefinition
├── ActionDefinition
├── AbilityDefinition
├── RuleDefinition
│
├── ConditionDefinition
├── ExpressionDefinition
├── SelectorDefinition
├── ChoiceDefinition
├── CostDefinition
├── EffectDefinition
├── ModifierDefinition
│
├── CardDefinition
├── DeckDefinition
├── DeckConstructionDefinition
├── SetupDefinition
└── EndConditionDefinition
```

### The important objects

| Object | Meaning |
|---|---|
| **GameDefinition** | Complete definition of one game |
| **ObjectTypeDefinition** | Defines types such as Card, Unit, Hero, Building |
| **PropertyDefinition** | Attack, HP, Armor, Tapped, etc. |
| **ResourceDefinition** | Gold, AP, Mana, Energy, etc. |
| **TagDefinition** | Peasant, Worker, Undead, Flying, etc. |
| **ZoneDefinition** | Deck, Hand, Battlefield, Discard |
| **FlowDefinition** | Structure of the game |
| **EventDefinition** | Something that can happen |
| **ActionDefinition** | Something a player/entity may initiate |
| **AbilityDefinition** | An ability attached to something |
| **RuleDefinition** | When X happens, under conditions Y, do Z |
| **ConditionDefinition** | A true/false test |
| **ExpressionDefinition** | Calculates a value |
| **SelectorDefinition** | Finds objects/players/etc. |
| **ChoiceDefinition** | Requests a decision from a player |
| **CostDefinition** | Something that must be paid/performed |
| **EffectDefinition** | Changes game state |
| **ModifierDefinition** | Temporary/continuous modification |
| **CardDefinition** | Definition of an actual card |
| **DeckDefinition** | Definition of a deck/set of cards |
| **SetupDefinition** | How a match starts |
| **EndConditionDefinition** | Winning, losing, drawing |

Then there is a second ontology for the **runtime**:

```text
GameInstance
│
├── PlayerInstance
├── ObjectInstance
│   └── CardInstance
├── ZoneInstance
├── ResourceInstance
├── PropertyValue
├── ModifierInstance
├── EventInstance
├── ActionInstance
├── RuleExecution
├── PendingChoice
└── GameHistory
```

That distinction is important.

For example, **Town Chief** is a `CardDefinition`:

```text
Town Chief
Type = Hero
Attack = 4
MaxHP = 5
Armor = 2
AP = 0
```

But when Martien plays against another player, the Town Chief actually sitting on the battlefield is a **CardInstance**:

```text
CardInstance #8472
Definition = Town Chief
Controller = Player 1
Zone = Battlefield

CurrentHP = 3
CurrentAP = 2
Tapped = false
```

The definition doesn't change when the Hero takes damage. The **instance state** changes.

### Within our particular TCG

Our game then extends the generic ontology with definitions like:

```text
Object Types
├── Card
│   ├── Character
│   │   ├── Hero
│   │   └── Unit
│   └── Building
│       └── Headquarters

Properties
├── Attack
├── CurrentHP
├── MaxHP
├── Armor
└── Tapped

Resources
├── Gold [Player scope]
└── Action Points [Entity scope]

Tags
├── Peasant
├── Worker
└── Builder

Zones
├── Deck
├── Hand
├── Battlefield
└── Discard

Flow
└── Turn
    ├── Start
    ├── Main
    ├── Combat
    └── End
```

And actual cards are instances of those **definitions**, not new engine concepts:

```text
CardDefinition
├── Town Hall
├── Town Chief
├── Paladin
└── Peasant
```

For example:

```text
Town Hall
    IS A Headquarters

Headquarters
    IS A Building

Building
    IS A Card
```

while:

```text
Town Chief
    IS A Hero

Hero
    IS A Character

Character
    IS A Card
```

And relationships become just as important as objects:

```text
CardDefinition
    HAS ObjectType

ObjectType
    HAS PropertyDefinitions

CardInstance
    INSTANCE OF CardDefinition
    OWNED BY Player
    CONTROLLED BY Player
    LOCATED IN Zone

Ability
    ATTACHED TO CardDefinition
    HAS Costs
    HAS Conditions
    HAS Effects

Rule
    TRIGGERED BY Event
    HAS Conditions
    USES Selectors
    MAY REQUEST Choice
    EXECUTES Effects

Modifier
    TARGETS Object
    MODIFIES Property
    EXPIRES ON Event/Condition
```

That last part is why calling it an **ontology** becomes genuinely useful. We're no longer just listing database tables. We're explicitly describing the *semantic relationships* between the concepts.

And I would actually keep **`Card` itself relatively thin**. Most of the intelligence should reside in `Rule`, `Ability`, `Effect`, `Selector`, `Event`, etc. That way a new mechanic usually becomes a **new configuration of existing ontology objects**, rather than requiring a new C# class.

===== USER =====
And we can make a system that can now handle this whole definition and play this trading card game. Let's say we have a system with a web UI, and you can load a definition and then start playing that game.

===== ASSISTANT =====
Yes. The system would have two major modes: **Designer/Admin** and **Runtime/Play**.

The Admin side loads or creates a `GameDefinition`: object types, properties, resources, zones, phases, actions, rules, cards, setup, victory conditions, and so on. The Runtime side takes that definition, validates it, creates a `GameInstance`, and executes the rules deterministically.

A clean architecture would look like this:

```text
WEB UI
├── Game Designer
│   ├── Templates
│   ├── Cards
│   ├── Rules
│   ├── Flow
│   └── Validation
│
└── Game Client
    ├── Lobby
    ├── Board
    ├── Hand
    ├── Choices
    ├── Actions
    └── Game Log

             │
             ▼

GAME API / SERVER
├── Definition Service
├── Match Service
├── Rule Engine
├── Action Validator
├── Event Processor
├── Choice Manager
├── State Manager
└── Persistence

             │
             ▼

GAME DEFINITION
+
GAME INSTANCE STATE
```

The key is that the browser should **not execute authoritative game rules**. The server does that. The client mainly renders state and submits intentions such as:

```text
Activate ability CallToArms on CardInstance #8472
```

or:

```text
Attack with #12 targeting #25
```

The server then checks whether that action is currently legal according to the loaded definition.

For example, starting your current TCG might work like this:

```text
POST /games/town-tcg/matches

Players:
    Player A
    Player B

Selections:
    Player A:
        HQ = Town Hall
        Hero = Town Chief

    Player B:
        HQ = Town Hall
        Hero = Paladin
```

The runtime loads `town-tcg.json`, creates the match, runs the configured setup rules and produces something conceptually like:

```text
GameInstance #1027

Turn:
    Player A

Phase:
    Start

Player A
    Gold: 0

    Battlefield:
        Town Hall
            HP 8/8
            Armor 2
            Tapped false

        Town Chief
            HP 5/5
            Attack 4
            Armor 2
            AP 1
            Tapped false

Player B
    Gold: 0

    Battlefield:
        Town Hall
        Paladin
```

The engine then calculates the legal actions for Player A.

Perhaps:

```text
AVAILABLE ACTIONS

Town Hall
    ✓ Collect Taxes
    ✗ Recruit Peasant
      Reason: requires 2 Gold

Town Chief
    ✓ Attack
    ✗ Call to Arms
      Reason: requires 3 AP
    ✗ Regenerate
      Reason: requires 2 AP
```

That is a very useful design principle:

> The backend should not merely reject illegal actions; it should be able to **derive the currently legal actions from the game definition**.

That allows the frontend itself to remain quite generic.

For example, it receives:

```json
{
  "action": "activateAbility",
  "source": "townHall-123",
  "ability": "collectTaxes",
  "label": "Collect Taxes",
  "available": true
}
```

and simply displays the appropriate button.

If an action requires a target, the server can return:

```json
{
  "action": "attack",
  "source": "townChief-123",
  "requiresChoice": {
    "type": "entity",
    "min": 1,
    "max": 1,
    "validOptions": [
      "enemy-townHall-456",
      "enemy-paladin-789"
    ]
  }
}
```

The frontend doesn't need to understand the combat rules. It knows:

> The player needs to choose one of these objects.

That also makes the UI configurable.

### A single runtime cycle

Suppose the Town Hall uses **Collect Taxes**.

Client sends:

```text
Activate:
    Town Hall → Collect Taxes
```

Server:

```text
1. Find ability definition.
2. Verify Town Hall is controlled by player.
3. Verify ability is currently available.
4. Verify Town Hall isn't tapped.
5. Pay cost:
      TownHall.Tapped = true
6. Execute effect:
      Player.Gold += 2
7. Emit ResourceChanged.
8. Process triggered rules.
9. Persist new game state.
10. Return state delta + new legal actions.
```

Then the browser immediately updates.

Now the player may have:

```text
Gold: 2
```

and the legal-action calculation changes:

```text
Town Hall
    ✗ Collect Taxes
      Town Hall is tapped

    ✗ Recruit Peasant
      Town Hall is tapped
```

After the next untap, Recruit Peasant becomes available.

### Player choices

Suppose the Paladin activates Divine Shield.

The rule engine reaches:

```text
CHOOSE one friendly entity
```

Instead of continuing, it creates:

```text
PendingChoice #583

Player:
    Player B

Source:
    Paladin #74

Choice:
    Select exactly 1 entity

Valid:
    Paladin #74
    Town Hall #75
    Peasant #91
```

The web UI highlights those cards.

Player clicks the Peasant.

The client sends:

```text
ResolveChoice #583
Selected = Peasant #91
```

The server resumes the suspended rule execution and applies:

```text
Peasant Armor +2
until start of owner's next turn
```

This means **PendingChoice should be a real runtime object**, not some frontend trick.

### WebSockets would fit well

For multiplayer, I would probably use normal HTTP commands plus WebSockets/SignalR for game updates.

Conceptually:

```text
Player A ─┐
          │
Player B ─┼── WebSocket/SignalR ── Game Server
          │
Spectator ┘
```

When Player A performs something, Player B immediately gets a state update.

Critically, each player may receive a **different projection of the same game state**.

Player A might receive:

```text
My hand:
    Soldier
    Farm
    Blacksmith
```

Player B receives:

```text
Opponent hand:
    Unknown
    Unknown
    Unknown
```

The server maintains the authoritative state and applies visibility rules defined by the game.

### The renderer can also become definition-driven

Initially, we can have standard visual components:

```text
Card
Card row
Hand
Zone
Resource counter
Board
Choice dialog
Action menu
Game log
```

And the Game Definition tells the UI what to show.

For example:

```text
Hero Card Layout

Top:
    Name

Center:
    Artwork

Stats:
    Attack
    Health
    Armor

Resources:
    Action Points

Bottom:
    Abilities
```

Poker could use the exact same application but load a different UI configuration:

```text
Playing Card Layout

Center:
    Rank
    Suit
```

So there are effectively **two declarative systems**:

```text
GAME LOGIC DEFINITION
What the game does

UI DEFINITION
How the game is presented
```

They should remain separate.

The same game could eventually have different presentations: desktop, mobile, accessibility mode, tabletop-style, compact tournament mode, etc.

### Definition compilation

I would also avoid interpreting raw JSON from scratch on every action. When a definition is loaded, run something like:

```text
JSON definition
      ↓
Schema validation
      ↓
Semantic validation
      ↓
Reference resolution
      ↓
Expression compilation
      ↓
Rule graph compilation
      ↓
Executable GameDefinition
```

For example, validation catches:

```text
CallToArms references property "armor"
```

but `armor` does not exist.

Or:

```text
Ability costs 3 AP
```

but the source type has no `actionPoints` resource.

Or even dangerous structures such as infinitely recursive event rules.

That makes the runtime much safer and faster.

### The end result

You could literally have a **Games** screen:

```text
MY GAMES

Town Wars
[Edit] [Play]

Texas Hold'em
[Edit] [Play]

Crazy Eights
[Edit] [Play]

My Experimental TCG
[Edit] [Play]

+ Create Game
```

Click **Play** and the exact same runtime application loads the corresponding definition.

So, yes: we're now at the point where the architecture is no longer hypothetical. We can define a concrete platform where:

**Definition → Compiler/Validator → Generic Rule Engine → Game Instance → Generic Web Client**

And your Town TCG can be the first serious reference game that forces us to prove that combat, scoped AP, armor, summoning, temporary buffs, targets, player choices and victory conditions all work without special-purpose game code.

===== USER =====
And the game engine should also allow for, let's say, things like instants from like Magic the Gathering. So we should be able to have options that if a player has a card that they can play as a response to another player doing something, that all of that is handled by the system. So that's quite an event-based system as well.

===== ASSISTANT =====
Exactly. This introduces a crucial concept we haven't modeled deeply enough yet: **actions don't necessarily resolve immediately**.

The engine needs an **interrupt/reaction/priority system**. Magic's stack can then be one configuration of that generic mechanism rather than something hardcoded as "Magic-style instants."

### 1. Proposed actions become events

Suppose Player A attacks:

```text
Player A:
    Attack(Unit A → Unit B)
```

The engine should not immediately apply damage. Instead:

```text
Action proposed
      ↓
Action validated
      ↓
Reaction window opened
      ↓
Players receive priority
      ↓
Possible responses
      ↓
Resolve
```

Player B might have a card saying:

> **Divine Intervention — Instant**  
> Give target friendly unit +3 Armor until end of turn.

So:

```text
A: Attack Unit B
       ↓
B: Divine Intervention → Unit B
```

But then Player A might respond to *that*:

```text
A: Attack Unit B
       ↓
B: Divine Intervention
       ↓
A: Counterspell → Divine Intervention
```

And potentially B responds again.

---

## 2. Generic Stack

I'd introduce a runtime object:

```text
ResolutionStack
```

containing `StackItem`s:

```text
StackItem
├── Source
├── Controller
├── Action/Ability
├── Targets
├── Choices
├── Parameters
├── CostsPaid
└── ResolutionState
```

Example:

```text
TOP

#3 Counterspell
   Target: #2

#2 Divine Intervention
   Target: Peasant #72

#1 Attack
   Attacker: Soldier #31
   Target: Peasant #72

BOTTOM
```

Resolution occurs top-down:

```text
Counterspell
↓
Divine Intervention cancelled
↓
Attack resolves
```

---

## 3. But the stack itself should be configurable

This is important given the generic nature of our engine.

Some games might say:

> Responses resolve last-in-first-out.

Another could say:

> Reactions happen immediately.

Another:

> Everyone secretly chooses a reaction and they're revealed simultaneously.

Another:

> Only the defending player can react.

So I'd add something like a:

**ResolutionPolicyDefinition**

```text
Resolution policy:
    Immediate

or

Resolution policy:
    Stack
    Order: LIFO

or

Resolution policy:
    Queue
    Order: FIFO

or

Resolution policy:
    Simultaneous
```

The TCG template could ship with a conventional stack implementation.

---

# 4. Priority

For a Magic-like game, we also need a **PrioritySystemDefinition**.

For example:

```text
Action proposed
      ↓
Active player receives priority
      ↓
Player may:
    Play valid reaction
    Activate valid ability
    Pass
      ↓
Next player receives priority
      ↓
...
```

When everybody consecutively passes:

```text
Resolve top StackItem
```

Then priority can reopen.

This allows two, three, four or more players without hardcoding Player A/Player B.

---

# 5. Reaction windows

Not every event should necessarily allow responses.

We therefore need:

```text
ReactionWindowDefinition
```

For example:

```text
AttackDeclared
    reactionsAllowed = true

DamageProposed
    reactionsAllowed = true

DamageApplied
    reactionsAllowed = false
```

Or a simpler game could say:

```text
CardPlayed
    reactionsAllowed = true

EverythingElse
    reactionsAllowed = false
```

---

# 6. Reaction eligibility

The engine then calculates available reactions exactly like normal available actions.

Suppose Player A declares an attack.

The engine asks:

```text
For Player B:

What actions are legal
during ReactionWindow(AttackDeclared)?
```

Maybe the answer is:

```text
Divine Intervention
Counter Attack
Paladin → Divine Shield
Pass
```

The web client receives those options.

Player B doesn't need to know *why* they're available.

---

# 7. Cards define when they can be played

Instead of hardcoding an `Instant` class, we could have something like:

```json id="nks8c7"
{
  "name": "Divine Intervention",

  "playTiming": {
    "allowedDuring": [
      "ownPriority",
      "opponentPriority"
    ],

    "reactionTo": [
      "attackDeclared",
      "damageProposed"
    ]
  },

  "choose": {
    "target": {
      "type": "character",
      "controller": "self"
    }
  },

  "effects": [
    {
      "modify": "target.armor",
      "value": 3,
      "duration": "endOfTurn"
    }
  ]
}
```

Now we've effectively created an Instant without the core engine knowing what an Instant is.

---

# 8. Events themselves need a lifecycle

I'd consequently make events richer than simply:

```text
Event happens
→ execute rules
```

Instead:

```text
PROPOSED
    ↓
BEFORE
    ↓
REACTION WINDOW
    ↓
MODIFIED
    ↓
RESOLVING
    ↓
RESOLVED
    ↓
AFTER
```

This gives us several different mechanics.

### Reaction

> "When an enemy attacks, you MAY play this."

### Trigger

> "Whenever an enemy attacks, gain 1 Gold."

### Replacement

> "If this unit would take damage, prevent it."

### After-effect

> "Whenever this unit takes damage, draw a card."

Those are actually different relationships with an event.

---

# 9. Triggered abilities versus reactions

This distinction is important.

A **trigger** automatically notices something:

```text
WHEN:
    Friendly Peasant dies

DO:
    Gain 1 Gold
```

A **reaction** gives someone an opportunity:

```text
WHEN:
    Friendly Peasant would die

PLAYER MAY:
    Pay 2 AP

DO:
    Prevent destruction
```

So `RuleDefinition` should probably support:

```text
Mandatory trigger
Optional trigger
Replacement
Reaction
Continuous rule
```

---

# 10. Replacement effects

This also lets us model very sophisticated TCG mechanics.

Suppose:

> **Last Stand:** If your Hero would be destroyed, instead set its HP to 1. Destroy this card.

The event is initially:

```text
Destroy(TownChief)
```

A replacement rule catches it:

```text
WHEN:
    EntityDestroyed

STAGE:
    Before

IF:
    Entity = Controller.Hero

INSTEAD:
    Cancel original event
    Set Hero.HP = 1
    Destroy LastStand
```

So the engine needs concepts such as:

```text
Cancel event
Replace event
Modify event
Redirect event
```

---

# 11. This also changes the game-state machine

A match can now be in states such as:

```text
WaitingForAction
WaitingForReaction
WaitingForPriority
WaitingForChoice
ResolvingStack
ResolvingRule
GameEnded
```

This is especially important for the web application.

If we're waiting for Player B:

```text
Player A sees:

"Waiting for opponent..."
```

Player B sees:

```text
Opponent attacks your Paladin.

Available responses:

[Divine Shield]
[Play Divine Intervention]
[Pass]
```

If B plays Divine Intervention, Player A may then get another response window.

---

# 12. We should add these concepts to the ontology

Our ontology therefore grows with:

```text
GameDefinition
│
├── EventDefinition
│
├── RuleDefinition
│   ├── TriggerRule
│   ├── ReactionRule
│   ├── ReplacementRule
│   └── ContinuousRule
│
├── ActionDefinition
├── TimingDefinition
├── ReactionWindowDefinition
├── PrioritySystemDefinition
└── ResolutionPolicyDefinition
```

And runtime:

```text
GameInstance
│
├── EventInstance
├── ActionInstance
├── StackItem
├── ResolutionStack
├── PriorityState
├── ReactionWindow
├── PendingChoice
└── RuleExecution
```

There is an even more fundamental architecture emerging here:

```text
PLAYER INTENTION
      ↓
ACTION
      ↓
EVENT
      ↓
REACTIONS / REPLACEMENTS / TRIGGERS
      ↓
RESOLUTION
      ↓
STATE CHANGE
      ↓
NEW EVENTS
      ↓
...
```

That should probably become the **heart of the engine**.

Then Magic-style Instants aren't an awkward feature we bolt on later. They're simply actions that become legal during particular event/reaction windows.

And that same machinery can support counterspells, traps, interrupts, secrets, defensive reactions, triggered abilities, damage prevention, redirection and even simultaneous decisions across very different card games.

===== USER =====
So, I now want you to help me think about the first deck that we will make. So this deck starts with the headquarters and with the town chief, with the town hall and the town chief. And now we need to make units around it. So the town hall makes peasants. The peasants are normally workers, but they can be made stronger by the hero. The hero itself is not super strong, so they are dependent on items to make themselves stronger. So I'm thinking that the first buildings that we would build, that we would have as cards, things that are quickly built, are things like a tavern, where you can hire a little bit stronger units. So the tavern must in some way enable the production of units, but it must not be like the single point of, the single building that you need for it, because we want to of course have a whole deck full of cards. So we're probably looking for multiple buildings that make, enable the production of units, like a tavern, a barracks, and maybe something else, an archery range. And at the same time, when we build more of that same building, if you have the second card of that same building, it should still be useful. So the buildings should in some way still add value even though you have built them already.

===== ASSISTANT =====
Yes. I think the key is to avoid making buildings simple **unlock requirements**. A Barracks shouldn't mean "now Soldier cards work." Every additional Barracks should improve your military infrastructure.

I'd make the first deck a **growing medieval town**: Peasants establish infrastructure, infrastructure gives access to stronger people/equipment, and the Town Chief turns the otherwise weak population into an emergency fighting force.

### A useful building pattern

Give production buildings **two functions**:

> **Function 1: Recruitment** — provides a way to obtain its associated units.  
> **Function 2: Infrastructure bonus** — additional copies improve those units or make recruitment more efficient.

For the first three:

| Building | Role | Typical recruits |
|---|---|---|
| **Tavern** | Cheap flexible recruitment | Mercenary, Brawler, Scout |
| **Barracks** | Professional infantry | Militia, Soldier, Guard |
| **Archery Range** | Ranged military | Archer, Hunter, Crossbowman |

But I wouldn't necessarily make them generate unlimited units the way Town Hall generates Peasants. That would make unit cards in your deck less important.

Instead, buildings could interact with **unit cards in your hand/deck**.

For example:

### Tavern
**5 HP | 1 Armor**  
**Build cost:** perhaps 2 Gold + Tap a Builder.

**Hire — Tap:** The next **Mercenary** unit you play this turn costs 1 less Gold.

**Busy Night — Passive:** Whenever you play a Mercenary, gain 1 Gold if you control another Tavern.

Now Tavern #1 is useful, but Tavern #2 isn't redundant. Multiple Taverns create a stronger mercenary economy.

### Barracks
**6 HP | 2 Armor**

**Train — Tap:** The next **Soldier** unit you play this turn costs 1 less Gold.

**Military Infrastructure — Passive:** Soldiers enter play with **+1 Armor this turn for each additional Barracks you control**, perhaps capped at +1 or +2 for balance.

I'd be careful with permanent Armor because your armor mechanic is extremely powerful.

### Archery Range
**5 HP | 1 Armor**

**Train Archer — Tap:** Reduce the cost of the next Archer you play by 1.

**Practice Range — Passive:** If you control 2+ Archery Ranges, your Archers receive some small ranged benefit—perhaps the first attack by an Archer each turn gets **+1 Attack**.

This creates a nice progression without requiring cards to say:

> "Requires Archery Range."

Instead, you *can* play an Archer without one, but building the appropriate infrastructure makes an Archer-focused strategy substantially better.

## But there's an even more interesting option

Since Peasants already have **Worker + Builder**, we can make them the foundation of the entire recruitment system.

For example, perhaps certain buildings can **train Peasants into specialized units**.

You begin with:

**Peasant**  
2 ATK / 3 HP / 1 Armor  
Worker, Builder

Then:

```text
Barracks + Peasant
        ↓
     Soldier

Archery Range + Peasant
        ↓
      Archer
```

That gives the Town Hall much greater strategic importance.

It continuously produces the raw population from which your civilization develops.

A Barracks could therefore say:

**Train Soldier — 2 Gold, Tap this building and a Peasant:** Transform that Peasant into a Soldier.

And the Archery Range:

**Train Archer — 2 Gold, Tap this building and a Peasant:** Transform that Peasant into an Archer.

The Tavern works differently because you're hiring outsiders rather than training citizens:

**Hire Mercenary — 3 Gold, Tap:** Summon a Mercenary.

Now the buildings have genuinely different economic identities.

### Multiple copies become naturally useful

This solves your duplicate-building problem elegantly.

Suppose you have:

**1 Barracks**

You can train one Peasant per turn.

**2 Barracks**

You can train two.

**3 Barracks**

You can train three.

No artificial "+1 because you have another Barracks" rule is even necessary. **The building's activated ability itself creates the scaling.**

That's probably cleaner.

The same applies to Archery Ranges.

And because buildings tap when used, the physical number of production buildings represents your **production capacity**, almost like an RTS.

That fits this game extremely well.

## Then I'd make the three buildings mechanically different

**Tavern — Hiring**

Doesn't require Peasants. Spend more Gold and get independent units quickly.

```text
Gold → Mercenary
```

**Barracks — Training**

Cheap conversion of existing Peasants into durable melee units.

```text
Peasant + Gold → Soldier
```

**Archery Range — Training**

Converts Peasants into ranged/high-attack but fragile units.

```text
Peasant + Gold → Archer
```

And later:

**Stable**

```text
Soldier + Gold → Knight
```

**Workshop**

```text
Resources/Gold → Siege equipment
```

**Temple**

```text
Peasant + Gold → Priest
```

**Mage Tower**

```text
Peasant/Apprentice + lots of Gold → Mage
```

Now you're creating an actual **technology tree out of cards**.

## This also gives the Town Chief an interesting dilemma

Remember Call to Arms:

> **3 AP: All Peasants get +1 Attack/+1 Armor until end of turn.**

Every Peasant you train into a Soldier or Archer is **no longer a Peasant**.

So the player has to decide:

> Do I specialize my population into stronger units?

or:

> Do I maintain a large Peasant population because my Town Chief can mobilize them?

That's excellent strategic tension.

A player with:

**Town Hall + Town Chief + 6 Peasants**

looks economically weak militarily—but after Call to Arms suddenly has six:

**3 Attack / 3 HP / 2 Armor**

fighters for a turn.

Meanwhile, the Barracks player may have converted most of those Peasants into professional troops and therefore gets much less value from Call to Arms.

## Items should form the other half of this deck

Your observation about the Town Chief being dependent on equipment also suggests another building:

### Blacksmith
Rather than producing units, it produces/improves **equipment**.

It could support cards such as:

**Iron Sword** — +1 Attack  
**Shield** — +1 Armor  
**Heavy Armor** — +2 Armor, perhaps with a drawback  
**Warhammer** — +2 Attack against Buildings

That gives us an emerging first-deck structure:

```text
                    TOWN HALL
                        │
                     Peasant
                  /      |       \
                 /       |        \
          BARRACKS    ARCHERY    Buildings
              │        RANGE       │
           Soldier       │       Economy
                       Archer

TAVERN ─────────────→ Mercenaries

BLACKSMITH ─────────→ Equipment
                           │
                           ▼
                      TOWN CHIEF
```

I think this is a much stronger identity for the first deck than simply filling it with unrelated creatures. **The deck represents building a town from scratch.** Your starting Town Hall produces population; the population constructs the town; the town trains/hire/equips increasingly specialized forces; and the Town Chief can either lead those forces personally through equipment or mobilize the remaining Peasants in an emergency.

That gives us a coherent framework from which we can start designing the actual first **30–40 cards and their quantities**, rather than inventing cards individually.

===== USER =====
Yeah, so instead of gold, let's say a barracks can produce a training point every turn. And these training points are an additional resource that's needed to produce units.

===== ASSISTANT =====
Yes. That improves the system considerably because **Gold represents the town's general economy, while Training Points represent military production capacity**.

And I would make Training Points **player-scoped**, unlike the Hero's entity-scoped AP.

For example:

**Barracks**  
**6 HP | 2 Armor**

**Training — Tap:** Gain **1 Training Point**.

Then a Soldier might cost:

**Soldier**  
**3 Attack | 4 HP | 2 Armor**  
**Cost:** 2 Gold + 1 Training Point

This means you don't necessarily need a rule saying *"You must control a Barracks to play Soldiers."* The resource itself creates that dependency:

> No military infrastructure → no Training Points → can't recruit trained military units.

That elegantly solves several problems at once.

It also makes duplicate Barracks inherently useful:

```text
1 Barracks → up to 1 Training Point/turn
2 Barracks → up to 2 Training Points/turn
3 Barracks → up to 3 Training Points/turn
```

And it opens up a broader **resource-production architecture** for the deck.

For example:

| Building | Produces | Used for |
|---|---|---|
| **Town Hall** | Gold / Peasants | General economy |
| **Barracks** | Training | Soldiers, Guards |
| **Archery Range** | Marksmanship | Archers, Crossbowmen |
| **Tavern** | Influence | Mercenaries, Scouts |
| **Blacksmith** | Crafting | Weapons, Armor |
| **Stable** | Horsemanship | Cavalry |

Although I wouldn't immediately create six different resources—that could become cumbersome. We could instead make some buildings share resources.

For the first deck, I particularly like:

**Gold** — general player resource  
**Training Points** — military player resource  
**Action Points** — entity-specific resource on Heroes/other applicable characters

Then Barracks and Archery Range could both produce Training Points, but give different secondary advantages.

For example:

**Barracks**  
**6 HP | 2 Armor**  
**Tap:** Gain 1 Training Point.  
Perhaps trained **melee** units cost less Training while this exists.

**Archery Range**  
**5 HP | 1 Armor**  
**Tap:** Gain 1 Training Point.  
Perhaps enables/renders efficient **ranged** units.

Then a card could have:

```text
Soldier
Cost: 2 Gold + 1 Training
```

while:

```text
Crossbowman
Cost: 3 Gold + 2 Training
```

This is also a good example of why the generic engine needs the resource ontology we designed:

```text
Gold
Scope: Player

Training Points
Scope: Player

Action Points
Scope: Entity
```

They're all the same generic `ResourceDefinition`; their meaning comes entirely from this particular game's definition.

One design decision we should make next is whether **Training Points accumulate between turns**. If they do, a Barracks is building up a military reserve; if they reset each turn, Training represents *production capacity*. I lean toward **resetting unused Training Points at the end of the turn**, because then multiple Barracks represent actual concurrent military infrastructure rather than eventually allowing a player with one Barracks to save up indefinitely and recruit an entire army at once.

===== USER =====
I know something else. Let's make it so that the town hall itself can house a maximum of three units. So that's the hero and two other units. And then you need another building that provides housing. So for instance, the barracks could produce housing, and they could produce training points. And soldiers that are trained in the barracks require a training point. So you have units that you can just play. You can play them without any requirements. You just pay the gold and you can play them. And you have units that require, what do I say, training points. So you need the barracks to be able to get these training points. And some units may actually state requires a barracks to build.

===== ASSISTANT =====
Yes. That gives the town-building aspect much more structure. I would separate **housing capacity**, **resources**, and **requirements** as three independent mechanics.

Your starting position becomes:

**Town Hall**  
**8 HP | 2 Armor**  
**Housing: 3**

**Tap:** Gain 2 Gold.  
**2 Gold + Tap:** Summon a Peasant.

The Town Chief occupies one housing slot, so at game start:

```text
Housing: 1 / 3

Town Chief     1
Empty          2
Empty          3
```

You can initially add only two units. That creates immediate pressure to expand the town.

### Barracks

I'd then make the Barracks something like:

**Barracks**  
**6 HP | 2 Armor**  
**Cost:** perhaps 3 Gold  
**Housing: +3**

**Train — Tap:** Gain 1 Training Point.

Now one Barracks does three things simultaneously:

1. Expands your population capacity.
2. Creates Training Points.
3. Satisfies `Requires: Barracks` requirements.

And a second Barracks remains extremely valuable:

```text
Town Hall          +3 housing
Barracks           +3
Barracks           +3
-----------------------
Total                9
```

It can also produce twice as many Training Points.

### Three categories of units emerge

This is the part I particularly like. We don't need one universal recruitment mechanism.

**1. Ordinary units**

Simply:

> Pay the cost → play the unit.

For example:

**Farmhand**  
2 ATK | 3 HP | 1 Armor  
**Cost: 2 Gold**

No infrastructure required.

---

**2. Trained units**

Require the Training resource:

**Soldier**  
3 ATK | 4 HP | 2 Armor  
**Cost: 2 Gold + 1 Training**

It doesn't necessarily matter where the Training came from.

This means future cards could say:

> Gain 1 Training.

without requiring a Barracks.

---

**3. Infrastructure-specific units**

Some units explicitly require a building:

**Heavy Infantry**  
4 ATK | 5 HP | 2 Armor  
**Cost: 4 Gold + 2 Training**  
**Requirement: Control a Barracks**

That's a separate condition from paying Training Points.

So destroying someone's last Barracks could suddenly prevent them from deploying Heavy Infantry even if they have saved Training.

### Housing should probably be generic

I wouldn't make every unit necessarily consume exactly one housing.

That gives us another useful stat:

```text
HousingCost
```

Most humans:

```text
HousingCost: 1
```

But later:

**Knight:** 2 Housing  
**War Elephant:** 3 Housing  
**Dragon:** perhaps 5 Housing

And potentially:

**Rat Swarm:** 0 Housing

So the engine calculates:

```text
AvailableHousing =
    TotalHousingCapacity
    -
    Sum(HousingCost of controlled units)
```

Then playing a unit has an implicit condition:

```text
IF AvailableHousing >= Unit.HousingCost
```

Your Hero can simply have:

```text
HousingCost: 1
```

which explains why the starting Town Chief occupies one of the Town Hall's three spaces.

### This makes buildings strategically important targets

That's where this starts becoming especially interesting.

Suppose I have:

```text
Town Hall       +3
Barracks        +3
Cottages        +4

Total Housing: 10
Used Housing:    9
```

You destroy my Cottages.

What happens?

My capacity becomes:

```text
6 capacity
9 occupied
```

We need a rule for **overpopulation**.

I would *not* immediately kill three units. A cleaner rule could be:

> Existing units remain, but you cannot deploy additional housing-consuming units while population exceeds capacity.

So:

```text
Housing: 9 / 6
OVER CAPACITY

Play Soldier: ✗
Play Peasant: ✗
```

This makes destroying housing damaging without creating bizarre instant deaths.

We could later have cards that punish overpopulation.

### Dedicated housing

This also gives us an obvious cheap building:

**Cottages**  
**4 HP | 0 Armor**  
**Cost: 2 Gold**  
**Housing: +4**

No activated ability.

It's cheap and efficient, but extremely vulnerable.

Now the player chooses between:

**Barracks**  
Expensive, tougher, +3 Housing, produces Training.

versus:

**Cottages**  
Cheap, vulnerable, +4 Housing, does nothing else.

That's a meaningful construction decision.

### The first town economy is taking shape

We now effectively have:

```text
                    TOWN HALL
                  Gold + Peasants
                  Housing: 3
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
       Cottages     Barracks       Tavern
       Housing      Housing        Housing?
                       +
                    Training
                       │
                       ▼
                    Soldiers
```

And this produces several independent constraints:

**Gold**  
Can I afford this?

**Housing**  
Do I have room for this?

**Training Points**  
Do I have the military capacity to train this?

**Building requirements**  
Do I have the infrastructure required for this particular unit?

That's much richer than simply having every card cost Gold.

It also means the first deck can contain units at several technological levels: cheap civilians and adventurers that can be played immediately, trained troops that need Training Points, and advanced troops that explicitly require particular infrastructure.

The nice part is that none of this requires changing our generic engine. `Housing` is a capacity/property, `Training` is a player-scoped resource, and `Requires Barracks` is simply a condition/selector rule in the game definition.

===== USER =====
So actually the barracks should not say produce, like the unit should not say requires barracks to produce, but the barracks should have an ability that it can produce units of a certain type. Let's say soldier kind of units are produced when you have a barracks.

===== ASSISTANT =====
Yes. That's cleaner. The **production permission belongs to the building**, rather than being repeated as a requirement on every Soldier card.

I'd introduce a generic concept such as **Production Capability**.

### Barracks

**Barracks**  
**6 HP | 2 Armor**  
**Housing: +3**

**Military Training — Tap:** Gain 1 Training Point.

**Produce — Passive:** You may produce **Soldier-type units** from your hand.

Then a Soldier card itself only needs:

**Soldier**  
**3 Attack | 4 HP | 2 Armor**  
**Housing: 1**  
**Cost:** 2 Gold + 1 Training  
**Type/Tags:** Unit, Soldier

It doesn't know anything about Barracks.

The engine determines whether it can be played by asking:

```text
Player wants to produce Soldier

1. Is Soldier in player's hand?        ✓
2. Can something produce "Soldier"?    ✓ Barracks
3. Can player pay 2 Gold?              ✓
4. Can player pay 1 Training?          ✓
5. Is 1 Housing available?             ✓

→ Soldier can be produced.
```

Without the Barracks:

```text
Can something produce "Soldier"?       ✗

→ Action unavailable
```

### This makes multiple production buildings interesting

The Barracks could perhaps only produce **one Soldier per turn** because production taps it.

For example:

> **Produce Soldier — Tap Barracks:** Play one Soldier unit from your hand, paying its costs.

Now:

```text
1 Barracks = maximum 1 Soldier produced/turn
2 Barracks = maximum 2 Soldiers produced/turn
3 Barracks = maximum 3 Soldiers produced/turn
```

That gives duplicate Barracks value beyond housing and Training.

There is a slight issue, though: if the Barracks must tap to generate Training **and** tap to produce a Soldier, one Barracks cannot normally do both during the same turn. That could actually be an excellent mechanic.

You have to choose:

> **Train:** Tap → Gain 1 Training Point

or

> **Produce:** Tap → Produce a Soldier

So initially you might spend a turn accumulating Training:

```text
Turn 1:
Barracks → Training +1
```

Then next turn:

```text
Turn 2:
Barracks → Produce Soldier
Spend 1 Training
```

With two Barracks:

```text
Barracks A → Training +1
Barracks B → Produce Soldier
```

Now **multiple Barracks dramatically improve your military production chain** without needing arbitrary stacking bonuses.

### Different buildings become producers of different unit classes

That gives us a very clean system:

**Town Hall**
> Produces: Peasant

**Tavern**
> Produces: Mercenary / Adventurer

**Barracks**
> Produces: Soldier

**Archery Range**
> Produces: Archer

**Stable**
> Produces: Cavalry

**Temple**
> Produces: Cleric

**Mage Tower**
> Produces: Mage

And units can have multiple tags.

For example:

**Ranger**

```text
Types:
Unit
Archer
Adventurer
```

That means perhaps either a **Tavern OR Archery Range** can produce it.

### Production should therefore be selector-based

For our generic ontology, I wouldn't implement:

```text
Barracks → produces Soldiers
```

as a special relationship.

I'd make `Produce` an action with a selector:

```text
PRODUCE

SELECT:
    Card from Controller.Hand

WHERE:
    Type = Unit
    AND Tags contains Soldier

COST:
    Tap Source
    Pay SelectedCard.Cost

DO:
    Move SelectedCard
    Hand → Battlefield
```

The Barracks definition just configures that selector.

An Archery Range changes one line:

```text
Tags contains Archer
```

A Tavern:

```text
Tags contains Mercenary
OR
Tags contains Adventurer
```

And this opens up much more interesting buildings later.

For example:

**Military Academy**

> Produce any `Soldier OR Archer` unit.

**Royal Castle**

> Produce any `Soldier`, `Knight` or `Royal Guard`.

**Necromancer's Crypt**

> Produce `Undead` units from your **discard pile** instead of your hand.

It's still exactly the same generic production action:

> **Select eligible object → pay costs → move/create it into the appropriate zone.**

So I think this correction is important: **units don't declare which building they require. Buildings declare which units they are capable of producing.** That keeps the unit definitions independent and makes the production infrastructure much more flexible.

===== USER =====
So you can tap the archery range to reduce the cost of in training points of an archery unit by one this turn, the next archery unit. So that means that they just become one cheaper. It's the same mechanism as the barracks, only now the archery range doesn't produce it, but it is used to produce the, to reduce the training points.

===== ASSISTANT =====
Yes. Then I'd simplify the model: **Barracks and Archery Range don't "produce" units at all.** Units are played normally from the hand; infrastructure modifies whether/how efficiently they can be trained.

The distinction becomes:

**Barracks = produces Training Points.**  
**Archery Range = makes Archer units cheaper to train.**

For example:

### Barracks
**6 HP | 2 Armor | Housing +3**

**Training — Tap:** Gain **1 Training Point**.

So multiple Barracks naturally scale:

`1 Barracks → 1 Training`  
`2 Barracks → 2 Training`  
`3 Barracks → 3 Training`

### Archery Range
**5 HP | 1 Armor**

**Archery Training — Tap:** The next **Archer** unit you play this turn costs **1 less Training Point**.

Minimum Training cost would normally be 0.

So an:

**Archer**  
3 Attack | 3 HP | 1 Armor  
Cost: **2 Gold + 2 Training**  
Housing: 1  
Tags: `Unit, Archer`

normally costs:

> 2 Gold + 2 Training

Tap one Archery Range:

> 2 Gold + **1 Training**

Tap two Archery Ranges:

> 2 Gold + **0 Training**

That also solves the duplicate-building issue beautifully: every additional Archery Range can reduce another Training Point, or potentially be saved for another Archer depending on exactly how we define the modifier.

I'd make each activation apply specifically to **one next unit**, rather than globally:

> **Tap: The next Archer unit you play this turn costs 1 less Training Point.**

Then if you have two Archery Ranges, you have a choice:

- tap both → one expensive Archer gets −2 Training;
- tap one → first Archer gets −1, then tap the second before playing another Archer → second gets −1;
- don't tap them yet → preserve your options.

This also creates a very nice general concept for the ontology:

```text
CostModifier
├── Resource: TrainingPoints
├── Amount: -1
├── Filter: Card has tag Archer
├── AppliesTo: NextMatchingAction
├── Uses: 1
└── Expires: EndOfTurn
```

So **Archery Range isn't actually a special engine mechanic**. Its ability creates a temporary `CostModifier`.

That same mechanism later gives us:

**Blacksmith:** Tap → next Equipment costs 1 less Gold.

**Stable:** Tap → next Cavalry unit costs 1 less Training.

**Temple:** Tap → next Cleric costs 1 less Gold.

**Experienced Commander:** Your next Soldier this turn costs 1 less Training.

And now I think our first buildings are starting to get distinct purposes rather than all being variations of "unit factory":

**Town Hall** → Gold + Peasants + starting housing  
**Barracks** → Housing + generates Training  
**Archery Range** → Training-cost efficiency for Archers  
**Tavern** → could specialize in cheap/untrained units that primarily cost Gold  
**Cottages/Houses** → efficient dedicated Housing  
**Blacksmith** → equipment and/or equipment-cost efficiency

That creates a much more interesting town where **buildings form an economic network supporting the cards in your hand**, instead of buildings simply being prerequisites you have to draw before you're allowed to play particular units.

===== USER =====
And so the barracks can also, let's say, house two units, and some units also cost multiple rooms, let's say.

===== ASSISTANT =====
Yes. I'd call the mechanic **Housing** and the amount a unit consumes **Housing Cost** (or **Space** if you want the card text shorter).

So we now have:

**Town Hall**  
8 HP | 2 Armor  
**Housing: 3**  
Tap → Gain 2 Gold.  
2 Gold + Tap → Summon a Peasant.

The Town Chief occupies **1 Housing**, so you start at **1/3**.

**Barracks**  
6 HP | 2 Armor  
**Housing: +2**  
**Tap:** Gain 1 Training Point.

So after building one Barracks:

> Housing capacity: **5**  
> Town Chief occupies: **1**  
> Available: **4**

Then units have different Housing Costs:

| Unit | Housing | Reason |
|---|---:|---|
| Peasant | 1 | Normal civilian |
| Archer | 1 | Normal soldier |
| Soldier | 1 | Normal soldier |
| Heavy Infantry | 2 | Larger logistical burden |
| Knight | 2 | Knight + horse/equipment |
| Siege Crew | 2 | Crew and equipment |
| Giant | 3 | Exceptional unit |

This gives us another independent balancing axis. A powerful unit doesn't only need to cost more Gold/Training—it can consume more of your town's limited population capacity.

For example:

**Knight**  
5 Attack | 5 HP | 3 Armor  
**Cost:** 5 Gold + 3 Training  
**Housing:** 2

Even if you have enough Gold and Training, you still need two free Housing.

And this creates an interesting strategic difference between **wide** and **tall** armies:

> 4 Peasants = 4 Housing  
> 2 Knights = 4 Housing

The Town Chief strongly rewards the first strategy because four Peasants can all receive Call to Arms, while another Hero might eventually reward elite high-Housing units.

In the generic engine, this doesn't need a special housing subsystem either. It can be expressed as a capacity:

```text id="syqkmj"
CapacityDefinition:
    id: Housing

Providers:
    Town Hall: +3
    Barracks: +2

Consumers:
    Town Chief: 1
    Peasant: 1
    Soldier: 1
    Knight: 2
```

with the generic rule:

```text id="bslzvw"
Can deploy unit IF:

CurrentHousingUsed
+ Unit.HousingCost
<=
CurrentHousingCapacity
```

That suggests **Capacity** should probably become another first-class concept in our ontology alongside Resources. Unlike Training or Gold, Housing isn't normally *spent*—it is **occupied and released** as units enter and leave play. That distinction will be useful for many other games too.

===== USER =====
So we could also say that the headquarters provides five housing and that the hero has three housing cost.

===== ASSISTANT =====
Yes. I actually prefer that balance because it makes the **Hero feel like a significant resident of the town**, rather than mechanically equivalent to one Peasant.

So:

**Town Hall — Headquarters**  
**8 HP | 2 Armor | Housing: 5**

- **Tap:** Gain 2 Gold.
- **2 Gold + Tap:** Summon a Peasant.

**Town Chief — Hero**  
**4 Attack | 5 HP | 2 Armor | Housing Cost: 3**

- Gains 1 personal AP each turn.
- **Call to Arms — 3 AP:** Peasants get +1 Attack/+1 Armor until end of turn.
- **Regenerate — 2 AP, Tap:** Heal 3 HP.

That means the starting position is:

> **Housing: 3/5 used → 2 available**

So the Town Hall can initially accommodate the Hero plus **two normal 1-Housing units**—exactly the same practical starting capacity as before, but with a much better scaling model.

Then:

**Barracks — Housing +2**

would bring you to:

> **3/7 used → 4 available**

This also gives us more room to differentiate units later. A Peasant/Soldier/Archer might cost 1 Housing, a Knight 2, a Hero 3, and something truly massive 4–5.

It also means different starting Heroes could have different **Housing Costs**, which becomes another balancing lever. A powerful Paladin might occupy 4 Housing while a weaker economic Hero occupies 2, for example.

===== USER =====
So this also means that different builds could have cards that do not require housing costs at all. So not all decks are dependent on housing costs.

===== ASSISTANT =====
Exactly. And that's an important distinction: **Housing should be a mechanic of certain cards/factions, not a universal rule saying every unit consumes Housing.**

For example, the Town deck could be heavily dependent on Housing because it's building an actual settlement:

> Humans → need somewhere to live → consume Housing.

But another deck could operate completely differently.

A **Necromancer deck** might summon Skeletons and Zombies with:

> **Housing Cost: 0**

Instead, it might depend on corpses in the discard pile.

A **Demon deck** might use a different constraint:

> Summoning powerful Demons costs **Sacrifice** rather than Housing.

A **Nature deck** could generate creatures based on Forests.

A **Machine deck** might use something like **Power Capacity**:

```text id="bylqxr"
Generator provides: 5 Power Capacity

Automaton consumes: 1 Power
War Machine consumes: 3 Power
```

Conceptually that's actually the same generic engine mechanism as Housing, but a completely different game mechanic from the player's perspective.

This suggests an improvement to our ontology. Instead of the engine having a special:

```text id="zmlmmp"
Housing
```

system, it should have a generic **CapacityDefinition**.

For your Town deck:

```text id="3xlzlo"
Capacity: Housing

Town Hall:
    Provides 5

Barracks:
    Provides 2

Town Chief:
    Consumes 3

Peasant:
    Consumes 1
```

For Machines:

```text id="0i7rvz"
Capacity: Power

Generator:
    Provides 5

Automaton:
    Consumes 1

War Machine:
    Consumes 3
```

And a card can simply consume **nothing**:

```text id="3k5kmm"
Skeleton

Housing consumption: 0
Power consumption: 0
```

Or potentially consume multiple capacities:

```text id="3rxfmz"
Steam Knight

Housing: 1
Power: 2
```

This gives us three meaningfully different kinds of economic values in the generic ontology:

| Concept | Example | Behavior |
|---|---|---|
| **Resource** | Gold, Training | Gain it, store it, spend it |
| **Entity Resource** | Hero AP | Same, but belongs to one particular entity |
| **Capacity** | Housing, Power | Provided and occupied; normally not spent |

That distinction is useful.

It also gives us a very good principle for deck design: **different decks shouldn't merely have different cards—they can operate on different economic systems.**

Your Town Chief deck might be:

> Gold + Training + Housing + Peasants

while another deck could be:

> Mana + Graveyard + Sacrifices

and another:

> Energy + Power Capacity + Machines

Yet all three can fight each other because their internal economic machinery doesn't have to be symmetrical.

That would make deck identity much deeper than the usual "red cards do damage, blue cards cast spells" distinction.

===== USER =====
So on top of it, we want to add extra heroes to the game, so that when our hero dies, we can build another hero. We also want to have a building that gives us the option to resurrect our hero. Let's say you have a shrine, and the shrine has the option to resurrect the hero, but this should cost multiple turns because it's, yeah, the hero should not come back very easily.

===== ASSISTANT =====
Yes. That gives us two distinct recovery paths after losing a Hero, and I think they should have quite different economics:

**replace the Hero** versus **resurrect the original Hero**.

Your loss condition remains: you only lose when **both your HQ and Hero are dead**, but "Hero dead" now means you are in a dangerous state rather than permanently hero-less.

### Hero replacement

I'd let the deck contain additional **Hero cards**. If the Town Chief dies, you can eventually deploy another Hero, provided you can meet that Hero's normal costs and Housing requirements.

For example:

**Paladin — Hero**  
3 Attack | 7 HP | 3 Armor  
Housing: 3  
Cost: perhaps **6 Gold + 3 Training**

You cannot have two active Heroes normally:

> **Hero Limit: 1**

When Town Chief dies, that slot becomes available, so the Paladin in your hand can become your new active Hero.

This gives Hero cards value without allowing a board full of Heroes.

### Shrine

For resurrection, I like your idea that it takes several turns. I would implement this through **progress counters**, because that's useful for our generic engine too.

**Shrine**  
**5 HP | 1 Armor**  
Cost: perhaps 4 Gold  
Housing: 0

**Resurrection — 3 Gold, Tap:** If your Hero is dead, begin a Resurrection Ritual for that Hero with **3 Ritual counters**.

Then, for example:

> **At the start of each of your turns, remove 1 Ritual counter. When the last is removed, resurrect that Hero.**

So:

```text
Town Chief dies
       ↓
Build/use Shrine
       ↓
Resurrection Ritual [3]
       ↓
next turn [2]
       ↓
next turn [1]
       ↓
next turn [0]
       ↓
Town Chief returns
```

That gives the opponent a large window in which to react.

### Destroying the Shrine should matter

I'd make the ritual **attached to the Shrine**, rather than merely starting a global timer.

So if your opponent destroys the Shrine while:

> Resurrection Ritual: 1 turn remaining

the ritual fails.

That makes resurrection interactive rather than inevitable.

The opponent now has a strategic choice:

> Attack the HQ and try to win while the Hero is dead?

or:

> Destroy the Shrine before the Hero returns?

That's exactly the kind of board-level decision we want.

### The resurrected Hero

I'd bring the Hero back at **full HP**, but with:

- 0 AP
- untapped
- no temporary modifiers
- no equipment unless equipment explicitly survives death

So Town Chief returns as:

> 4 Attack / 5 HP / 2 Armor / 0 AP

Because AP accumulates over turns, resurrecting him doesn't immediately restore Call to Arms.

### A generic "progress" mechanic

This also exposes another useful ontology primitive. Rather than hardcoding resurrection timers, I'd introduce something like:

**ProgressTrack / Counter**

```text
Resurrection Ritual
Progress required: 3

WHEN OwnerTurnStarts:
    Progress += 1

WHEN Progress >= 3:
    Resurrect TargetHero
```

That same mechanism could later represent:

> Building a Castle takes 2 turns.

> Research Gunpowder: 4 research progress.

> Dragon Egg hatches after 3 turns.

> Siege weapon needs 2 turns to assemble.

> Spell channels for 3 turns.

So the Shrine is another good stress test for our generic engine.

### It also creates an interesting Hero system

I would distinguish three concepts:

**Starting Hero** — chosen before the game and starts on the battlefield.

**Reserve Hero** — Hero card in your deck/hand that can replace a dead Hero.

**Dead Hero** — the specific Hero that died and can potentially be resurrected.

That means if Town Chief dies, you could face a real strategic choice:

> Spend resources immediately deploying the Paladin?

or

> Build/protect a Shrine and wait three turns to get Town Chief back?

And perhaps if you deploy the Paladin first, the Shrine can still resurrect Town Chief—but because **Hero Limit = 1**, the resurrection cannot complete until your Hero slot becomes available. That creates some potentially very interesting interactions.

I would therefore make **Hero Limit itself configurable**, just like Housing:

> Town faction: maximum 1 active Hero.

Another future deck could legitimately have:

> Hero Limit: 2

or a card saying:

> "While King's Palace is in play, your Hero Limit is increased by 1."

Again, no special-case engine code is necessary.

===== USER =====
Now the next thing is we want to have equipment. And equipment can be placed on a hero or a unit to make them stronger. And you can have different kinds of equipment. You can have a weapon, you can have a shield, you can have an armor. And maybe you can have a helmet, like different kinds, but you cannot have two armors or multiple weapons. Maybe you can have two weapons that are possible to be wielded together, but a weapon can also be dual-wield. So for instance, a broadsword is dual-wield, and probably a bow and arrow should also be dual-wields.

===== ASSISTANT =====
Yes. I'd make **equipment slots** the underlying mechanic, rather than hardcoding "one weapon, one armor." Then individual equipment specifies which slots it occupies.

For this Town deck, a character could have:

| Slot | Capacity |
|---|---:|
| Main Hand | 1 |
| Off Hand | 1 |
| Body | 1 |
| Head | 1 |

Potentially later: accessory, boots, mount, etc.

A character doesn't necessarily need to have every slot. A human Hero could have all four, while a Peasant might only support hands and body.

### Weapons

The important distinction you're describing is between **one-handed** and **two-handed** weapons.

I'd probably avoid calling a Broadsword "dual-wield", because *dual wielding* normally means wielding **two separate weapons simultaneously**. What you mean for a bow is **two-handed**.

For example:

**Short Sword**  
Equipment — Weapon  
Occupies: **1 Hand**  
Equipped character gets **+1 Attack**.

That allows:

> Sword + Shield  
> Sword + Sword

assuming that character is allowed to dual-wield.

**Broadsword**  
Equipment — Weapon, Two-Handed  
Occupies: **Main Hand + Off Hand**  
Equipped character gets **+2 Attack**.

**Bow**  
Equipment — Weapon, Two-Handed, Ranged  
Occupies: **Main Hand + Off Hand**  
Perhaps +2 Attack and enables some ranged mechanic.

So Bow + Shield isn't normally possible because both hands are already occupied.

### Shield

**Wooden Shield**  
Equipment — Shield  
Occupies: **Off Hand**  
Equipped character gets **+1 Armor**.

Then:

```text id="t0dl47"
Town Chief

Main Hand: Short Sword     +1 Attack
Off Hand: Wooden Shield    +1 Armor
Body:      Leather Armor   +1 Armor
Head:      Empty
```

Town Chief becomes:

> **5 Attack | 5 HP | 4 Armor**

Given our damage system, 4 Armor is extremely powerful, so equipment bonuses will need careful balancing.

### Armor

**Leather Armor**  
Equipment — Armor  
Occupies: **Body**  
+1 Armor

**Plate Armor** could later be:

> +2 Armor  
> perhaps costs Training, reduces something else, or has a minimum requirement.

You can't equip Leather Armor and Plate Armor simultaneously because both require:

```text id="syzz3v"
Body × 1
```

The slot system automatically prevents it.

### Helmet

Likewise:

**Iron Helmet**  
Equipment — Helmet  
Occupies: **Head**

Perhaps rather than another +1 Armor—which could cause Armor inflation—it might do something more specialized:

> The first damage this character receives each turn is reduced by 1.

or:

> +1 HP / protection against certain effects.

That gives helmets a distinct purpose from body armor.

### Equipping should probably have a cost

I'd also make **Equip Cost** separate from the card's play/acquisition cost.

For example:

**Broadsword**

> Cost: 3 Gold  
> Equip: 1 Gold  
> Weapon — Two-Handed  
> +2 Attack

You might play the Broadsword into an equipment/inventory zone first:

```text id="m2c38j"
Hand
 ↓
Equipment/Inventory
 ↓
Character
```

This gives you more possibilities. Equipment doesn't necessarily disappear just because the person carrying it dies.

Perhaps:

> Town Chief dies → Broadsword returns to your Equipment zone.

Then the resurrected Town Chief or replacement Paladin can equip it later.

That's thematically satisfying and makes equipment a persistent investment.

### Equipping and swapping

A generic rule might be:

> Equip only during your Main Phase unless otherwise stated.

If you equip something into an occupied slot, you first have to unequip the old item.

For example:

```text id="hwz4ak"
Town Chief currently:

Main Hand → Sword
Off Hand → Shield

Player wants to equip Broadsword.

Broadsword requires:
Main Hand + Off Hand

Therefore:
Sword → Inventory
Shield → Inventory
Broadsword → Equipped
```

Potentially unequipping could also cost something, but I'd keep that simple initially.

### Dual wielding

Then we can actually introduce **Dual Wield** as a character capability.

Normally perhaps:

> A character may equip only one Weapon, even if they have two hands.

But:

**Dual Wield**

> This character may equip a one-handed Weapon in both Main Hand and Off Hand.

That means a future Rogue could do:

```text id="chcqom"
Main Hand: Dagger +1 Attack
Off Hand:  Dagger +1 Attack
```

while Town Chief might not automatically be able to do that.

Alternatively, if we want everyone to be able to use two one-handed weapons, we don't need a Dual Wield keyword at all.

### This fits our ontology very nicely

We add definitions such as:

```text id="vk4b7p"
EquipmentDefinition
EquipmentSlotDefinition
EquipmentRequirement
```

But even more generically, equipment is still a game object with relationships:

```text id="9t9g5j"
Equipment
    ATTACHED TO Character

Character
    PROVIDES Slots

Equipment
    REQUIRES Slots

Equipment
    PROVIDES Modifiers
```

For example:

```text id="m6efvc"
Broadsword

Type:
    Equipment

Tags:
    Weapon
    TwoHanded

RequiresSlots:
    MainHand: 1
    OffHand: 1

Modifiers:
    EquippedEntity.Attack +2
```

And:

```text id="xhwcrv"
Wooden Shield

Type:
    Equipment

Tags:
    Shield

RequiresSlots:
    OffHand: 1

Modifiers:
    EquippedEntity.Armor +1
```

This is better than building a special "weapon system", because the same generic **attachment + slot + modifier** mechanism could later model things that aren't medieval equipment at all:

> Spaceship weapon slots  
> Vehicle upgrades  
> Pokémon-like held items  
> Magical enchantments  
> Robot modules  
> Building upgrades

So for our first Town deck, I'd start conservatively with **four equipment slots: Main Hand, Off Hand, Body, Head**, and let the cards themselves determine whether they occupy one or multiple slots.

===== USER =====
So now, given all this, I want you to create a deck with all units having equal value, with the town hall and the town chief as hero and building, and another hero, probably one of the ones that we created earlier, maybe the paladin or something. That should be the second hero that is also in the deck, and maybe even a third hero. And then the other cards you can have up to four of each card. And yeah, we want all the cards to synergize with each other. Different kinds of units, different kinds of, what was it, do I say, different kinds of equipment. And yeah, that's it.

===== ASSISTANT =====
Yes. I would make the first deck a **40-card Town deck**, plus the **Town Hall + Town Chief** as starting cards outside the deck. I interpret "equal value" as every card being balanced around comparable **cost-to-impact**, rather than every unit having identical stats.

A core identity emerges: **build a town → generate Gold/Training/Housing → turn Peasants into an army → equip key characters → recover or replace your Hero.**

## Starting cards

**Town Hall — Headquarters**  
8 HP | 2 Armor | **Housing 5**

- **Tap:** Gain 2 Gold.
- **2 Gold, Tap:** Summon a Peasant.

**Town Chief — Starting Hero**  
4 ATK | 5 HP | 2 Armor | **Housing 3**  
Gains 1 personal AP each turn.

- **Call to Arms — 3 AP:** Peasants you control get +1 ATK/+1 Armor until end of turn.
- **Regenerate — 2 AP, Tap:** Heal 3 HP.

This means you start with **3/5 Housing occupied**, leaving room for two ordinary units.

---

# The 40-card deck

### Buildings — 11

**4× Barracks**  
6 HP | 2 Armor | **Housing +2**  
**Tap:** Gain 1 Training Point.

This is the backbone of the professional-army route. Additional Barracks mean more Housing and more Training generation.

**3× Archery Range**  
5 HP | 1 Armor  
**Tap:** The next Archer you play this turn costs 1 less Training.  
Multiple activations can stack.

**2× Tavern**  
5 HP | 1 Armor | Housing +1  
**Tap:** The next Mercenary you play this turn costs 1 less Gold.

This creates the alternative to Training: hire people instead.

**2× Shrine**  
5 HP | 1 Armor  
**Resurrection Ritual — 3 Gold, Tap:** Begin resurrecting your dead Hero. At the beginning of each of your next three turns, add one Resurrection progress. At 3, return that Hero to play at full HP and 0 AP. If the Shrine is destroyed, its ritual is lost.

---

# Units — 15

### 4× Peasant
**2 ATK | 3 HP | 1 Armor | Housing 1**  
**Cost: 2 Gold**  
Tags: Peasant, Worker, Builder

These are separate from the unlimited Peasants the Town Hall can summon, which gives you a faster way to establish a population.

And they're unusually valuable with the Town Chief because Call to Arms turns them temporarily into:

> **3 ATK | 3 HP | 2 Armor**

### 3× Soldier
**3 ATK | 4 HP | 2 Armor | Housing 1**  
**Cost: 2 Gold + 1 Training**

Solid professional infantry.

### 3× Archer
**4 ATK | 3 HP | 1 Armor | Housing 1**  
**Cost: 2 Gold + 2 Training**

**Ranged:** When attacking a non-Ranged character, that character doesn't deal combat damage back to the Archer.

That makes the Archery Range's Training reduction significant.

### 2× Town Guard
**2 ATK | 5 HP | 2 Armor | Housing 1**  
**Cost: 3 Gold + 1 Training**

**Guard:** While untapped, enemy characters cannot attack your Hero or Headquarters while they could attack this unit.

This gives the Town Chief protection while AP accumulates.

### 2× Mercenary
**4 ATK | 4 HP | 1 Armor | Housing 1**  
**Cost: 4 Gold**

No Training required.

This is important: if you don't draw Barracks early, the deck still functions. Gold can buy immediate military strength.

### 1× Knight
**5 ATK | 5 HP | 3 Armor | Housing 2**  
**Cost: 5 Gold + 3 Training**

A high-end unit. No special text necessary—the stats are the payoff.

---

# Reserve Heroes — 2

### 1× Paladin
**3 ATK | 7 HP | 3 Armor | Housing 3**  
**Cost: 6 Gold + 2 Training**  
Gains 1 AP each turn.

**Divine Shield — 2 AP:** A friendly character gets +2 Armor until the beginning of your next turn.

**Holy Light — 3 AP, Tap:** Heal another friendly character or building for **3 HP**.

This is the defensive replacement for the Town Chief.

### 1× Master Builder
**2 ATK | 5 HP | 1 Armor | Housing 2**  
**Cost: 4 Gold**  
Gains 1 AP each turn.

I'd change his abilities somewhat now that we know more about the game:

**Rapid Construction — 2 AP:** Your next Building this turn costs 2 less Gold.

**Expansion — 3 AP, Tap:** Target building provides **+2 Housing** while the Master Builder remains in play.

So if the Town Chief dies, switching to the Master Builder radically changes your strategy rather than merely giving you a weaker substitute.

Only **one Hero may be active at once**.

---

# Equipment — 12

This gives equipment a substantial role, which fits your idea that Heroes themselves aren't overwhelmingly powerful.

### 3× Short Sword
**Cost: 2 Gold | Main Hand OR Off Hand**

Equipped character gets **+1 ATK**.

### 2× Broadsword
**Cost: 3 Gold | Main Hand + Off Hand**

Equipped character gets **+2 ATK**.

### 2× Wooden Shield
**Cost: 2 Gold | Off Hand**

Equipped character gets **+1 Armor**.

### 2× Leather Armor
**Cost: 3 Gold | Body**

Equipped character gets **+1 Armor**.

### 2× Longbow
**Cost: 3 Gold | Main Hand + Off Hand**

Equipped character gets **+1 ATK and Ranged**.

This is especially interesting because you could turn the Town Chief—or even a Peasant—into a ranged character.

### 1× Commander's Helmet
**Cost: 3 Gold | Head**

Equipped character gets **+1 HP**.

If equipped to a Hero:

> At the beginning of your turn, if this Hero has 0 AP, gain 1 additional AP.

That makes it particularly valuable for rebuilding momentum after resurrection/replacement without simply producing unlimited extra AP.

---

## Complete deck

| Qty | Card | Category |
|---:|---|---|
| — | Town Hall | Starting HQ |
| — | Town Chief | Starting Hero |
| 4 | Barracks | Building |
| 3 | Archery Range | Building |
| 2 | Tavern | Building |
| 2 | Shrine | Building |
| 4 | Peasant | Unit |
| 3 | Soldier | Unit |
| 3 | Archer | Unit |
| 2 | Town Guard | Unit |
| 2 | Mercenary | Unit |
| 1 | Knight | Unit |
| 1 | Paladin | Hero |
| 1 | Master Builder | Hero |
| 3 | Short Sword | Equipment |
| 2 | Broadsword | Equipment |
| 2 | Wooden Shield | Equipment |
| 2 | Leather Armor | Equipment |
| 2 | Longbow | Equipment |
| 1 | Commander's Helmet | Equipment |
| **40** | **Total deck** | |

## Why the synergies work

There are essentially three interlocking routes through the same deck.

**Peasant route:** Town Hall → Peasants → Town Chief → Call to Arms. You can overwhelm the opponent without needing sophisticated infrastructure.

**Professional army route:** Town Hall → Barracks → Training + Housing → Soldiers/Guards/Knights. Archery Ranges then make the Archer branch much more efficient.

**Gold route:** Town Hall → Gold → Tavern → Mercenaries. This lets you fight even if your Training infrastructure is destroyed.

And equipment crosses all three strategies. A Peasant can become dangerous with a Sword and Shield; a Soldier can become an elite fighter; and your Town Chief can gradually turn from a mediocre 4/5/2 Hero into the centerpiece of the army.

There is also a recovery loop:

```text id="rdyk2x"
                    TOWN CHIEF
                       │
                     dies
                       │
             ┌─────────┴─────────┐
             ▼                   ▼
          SHRINE             NEW HERO
             │              /         \
       resurrect Chief   Paladin   Master Builder
             │
             ▼
       original strategy
```

One balance issue I would specifically test in the first prototype is **Armor**. Because Armor subtracts directly from every attack, 3–4 Armor can become enormously powerful. Consequently, I'd keep +Armor equipment scarce and avoid casually putting +2 Armor on equipment until we've actually played some matches.

This 40-card deck is enough to run the **first real simulated matches** and start adjusting Gold income, Training costs, Housing pressure and card stats from actual gameplay rather than theory.

===== USER =====
You've forgotten to add the cost of the buildings.

===== ASSISTANT =====
You're right. The buildings need explicit Gold costs, otherwise the economy can't be balanced properly. I'd set the first version like this:

| Building | Cost | Stats / Capacity | Ability |
|---|---:|---|---|
| **Barracks** | **3 Gold** | 6 HP, 2 Armor, +2 Housing | Tap: gain 1 Training Point |
| **Archery Range** | **3 Gold** | 5 HP, 1 Armor | Tap: next Archer played this turn costs 1 less Training |
| **Tavern** | **2 Gold** | 5 HP, 1 Armor, +1 Housing | Tap: next Mercenary played this turn costs 1 less Gold |
| **Shrine** | **4 Gold** | 5 HP, 1 Armor | 3 Gold + Tap: begin a 3-turn resurrection ritual |

I like these costs because **Barracks and Archery Range are core military infrastructure**, so 3 Gold feels like a meaningful but achievable investment. The Tavern is simpler and more immediately accessible, so 2 Gold. The Shrine should be expensive because resurrecting your Hero is potentially game-saving, so it costs 4 Gold to build and another 3 Gold to begin the ritual.

The starting HQ remains:

**Town Hall — Starting Headquarters**  
8 HP | 2 Armor | Housing 5  
**Tap:** Gain 2 Gold.  
**2 Gold + Tap:** Summon a Peasant.

That means there's an important early-game choice. If you tap the Town Hall for Gold, you can't use it that turn to make a Peasant. And after producing 2 Gold, you can immediately afford a Tavern, but you need to save toward a Barracks or Archery Range.

I'd also add a general rule that **buildings enter play untapped unless the card says otherwise**, at least initially. Otherwise infrastructure would become unnecessarily slow. memcite

===== USER =====
And the building to be built costs a number of building points. And a builder can be tapped to produce one building point.

===== ASSISTANT =====
Yes. That makes **construction itself a second constraint**, analogous to Training for military units.

I'd define **Building Points (BP)** as a player-scoped resource generated by Builders:

**Builder — keyword/ability**  
**Tap:** Gain **1 Building Point**.

Since the Peasant has `Builder`, every Peasant can do this.

So the Peasant now has a meaningful economic choice each turn: remain available for combat, or tap to contribute toward construction.

### Buildings now have two costs

For example:

| Building | Gold | Building Points | HP | Armor | Housing |
|---|---:|---:|---:|---:|---:|
| **Tavern** | 2 | 2 BP | 5 | 1 | +1 |
| **Barracks** | 3 | 3 BP | 6 | 2 | +2 |
| **Archery Range** | 3 | 3 BP | 5 | 1 | — |
| **Shrine** | 4 | 4 BP | 5 | 1 | — |

So to build a Barracks you need:

> **3 Gold + 3 Building Points**

If you have three Peasants, all three could tap:

```text
Peasant #1 → Tap → +1 BP
Peasant #2 → Tap → +1 BP
Peasant #3 → Tap → +1 BP

Building Points: 3
Gold: 3

→ Build Barracks
```

### Should Building Points accumulate?

Here I'd do the opposite of what I suggested for Training: I think **Building Points should expire at the end of the turn**.

That makes them represent **labor capacity**, rather than a stockpiled currency.

So a Barracks requiring 3 BP means:

> You need three Builder activations during that turn.

That makes large buildings genuinely require a developed workforce.

It also creates excellent tension with the Town Chief.

Your Peasants can:

**Build**

> Tap → +1 BP

**Work**

> potentially produce some economic resource later

**Fight**

> stay untapped so they can attack/defend

**Respond to Call to Arms**

> become 3 Attack / 3 HP / 2 Armor for the turn

You cannot do everything simultaneously.

### Construction can become more sophisticated later

A very large structure might cost:

**Castle**  
8 Gold + **8 BP**

If you only have four Builders, perhaps it cannot be constructed in a single turn.

That suggests we could eventually have **multi-turn construction**:

```text
Castle
Required construction: 8 BP

Turn 1:
4 Builders → 4/8

Turn 2:
4 Builders → 8/8

Castle completed.
```

But I would *not* introduce that into the first deck yet. For now:

> Generate BP during your turn → spend BP on Buildings → unused BP disappears at end of turn.

It's much easier to play.

### The economy now has four different concepts

| Mechanic | Scope | Generated by | Purpose |
|---|---|---|---|
| **Gold** | Player resource | Town Hall etc. | General payment |
| **Building Points** | Player/turn resource | Builders | Construct Buildings |
| **Training Points** | Player resource | Barracks | Train advanced Units |
| **Action Points** | Individual entity resource | Hero itself | Hero abilities |
| **Housing** | Capacity | Town Hall, Barracks, etc. | Limits population |

And importantly, they behave differently.

**Gold** is money.  
**BP** is available labor.  
**Training** is military preparation.  
**AP** represents an individual character's accumulated capability.  
**Housing** is capacity, not something you spend.

This is becoming a fairly coherent little economy: **Town Hall creates money and population → population supplies construction labor → construction creates infrastructure → infrastructure supplies Housing and Training → those support stronger units → the Town Chief can instead turn the original workforce into a temporary army.** memcite

===== USER =====
The building points can be spent over multiple turns. So, you first play the building, then you tap a builder. The next turn you tap the builder again, and then you complete the building if it costs two building points.

===== ASSISTANT =====
Yes. That's better: **Building Points aren't a player resource at all. They are construction progress stored on the individual building being constructed.**

So when you play a building, you pay its Gold cost and put it onto the battlefield as **Under Construction**.

For example:

### Tavern
**Cost:** 2 Gold  
**Construction:** 2 BP  
**5 HP | 1 Armor | Housing +1**

When played:

```text
Tavern
Construction: 0/2
Status: Under Construction
```

A Peasant with **Builder** can then:

> **Build — Tap:** Add 1 Building Point to a building you control that is Under Construction.

Turn 1:

```text
Peasant → Tap

Tavern: 1/2 BP
```

Turn 2, the Peasant untaps:

```text
Peasant → Tap

Tavern: 2/2 BP
→ Construction completed
```

Only then does the Tavern become operational.

### Buildings therefore have two different costs

I'd revise our buildings approximately like this:

| Building | Gold | Construction | HP | Armor | Housing |
|---|---:|---:|---:|---:|---:|
| **Tavern** | 2 | 2 BP | 5 | 1 | +1 |
| **Barracks** | 3 | 3 BP | 6 | 2 | +2 |
| **Archery Range** | 3 | 3 BP | 5 | 1 | — |
| **Shrine** | 4 | 4 BP | 5 | 1 | — |

And multiple Builders accelerate construction.

Three Peasants could all tap to work on the same Barracks:

```text
Barracks: 0/3

Peasant A → +1
Peasant B → +1
Peasant C → +1

Barracks: 3/3
→ COMPLETED
```

Or one Peasant can spend three turns building it.

That creates exactly the RTS-like economy we've been moving toward.

### Under-construction buildings

I'd make an unfinished building physically exist on the battlefield but **provide none of its normal benefits yet**.

So an unfinished Barracks at 2/3:

> doesn't provide +2 Housing  
> can't generate Training  
> can't use its other abilities

But I'd allow the opponent to attack it.

That creates an interesting vulnerability: you've already spent the Gold and committed Builder turns, but the enemy can destroy the project before completion.

We might even say an unfinished building has its normal HP/Armor, or potentially reduced Armor while under construction. I'd start with normal defensive stats for simplicity.

### This makes Builder a proper mechanic

The Peasant becomes:

**Peasant**  
2 ATK | 3 HP | 1 Armor | Housing 1

**Worker** — [we can define its economic ability later]

**Builder — Tap:** Choose one Building you control that is Under Construction. Add **1 Construction Progress** to it.

And technically, I wouldn't even call the stored value "Building Points" internally. I'd distinguish:

**Building Points** = amount of construction work an effect provides.

**Construction Requirement** = amount needed by the building.

**Construction Progress** = amount currently accumulated on this particular building instance.

So:

```text
Peasant
Building Power: 1

Barracks
Construction Required: 3

Barracks #183
Construction Progress: 2
```

That gives us room for better builders later:

**Master Builder**

> Tap: Add **2 Building Points**.

or equipment:

**Builder's Tools**

> Equipped Builder produces +1 Building Point when building.

or effects:

**All Hands to Work**

> Every untapped Peasant may immediately contribute 1 BP to a Building.

This also means construction is naturally supported by our generic ontology: the Barracks instance simply has a **progress track**, and reaching its configured threshold triggers `ConstructionCompleted`. No special multi-turn timer is necessary.

===== USER =====
A peasant is also a worker unit. And I know a building that can be placed for free. It's a gold mine, it has five gold coins, and you can tap a worker unit to harvest one gold coin.

===== ASSISTANT =====
Yes. That gives **Worker** a concrete purpose alongside Builder, and introduces a finite-resource building.

### Gold Mine — Building
**Gold cost:** 0  
**Construction:** 0 BP — enters play completed  
**Gold reserves:** 5

**Harvest:** Tap a **Worker** you control → remove 1 Gold Coin from this Gold Mine and gain **1 Gold**.

So the mine itself does **not** tap; the Worker does. That means several Workers can harvest the same mine in one turn.

For example:

```text
Gold Mine: 5/5 Gold

Peasant A → Tap → harvest
Gold Mine: 4/5
Player: +1 Gold

Peasant B → Tap → harvest
Gold Mine: 3/5
Player: +1 Gold
```

Once it reaches:

```text
Gold Mine: 0/5
```

it can no longer be harvested. We can decide later whether an exhausted mine remains on the battlefield or is automatically discarded.

### Peasant

This now makes the Peasant quite an interesting basic unit:

**Peasant**  
**2 ATK | 3 HP | 1 Armor | Housing 1**

**Worker** — May be tapped to perform Worker actions, such as harvesting a Gold Mine.

**Builder** — Tap: Add 1 Construction Progress to a Building you control that is under construction.

So every turn a Peasant represents a genuine allocation decision:

> **Harvest** → +1 Gold  
> **Build** → +1 construction progress  
> **Fight / remain available** → military use

And because tapping is the common cost, one Peasant can't normally do multiple jobs in the same turn.

### This also gives the Town Hall an interesting role

The Town Hall can:

> **Tap → gain 2 Gold**

or:

> **2 Gold + Tap → summon a Peasant**

So spending 2 Gold on a Peasant can become an economic investment. That Peasant can subsequently work Gold Mines, construct infrastructure, or become part of the Town Chief's militia.

The economy now has a nice feedback structure:

```text
                  TOWN HALL
                     │
                 2 Gold
                     │
                     ▼
                  PEASANT
                 /   |    \
                /    |     \
           Harvest  Build   Fight
              │       │       │
              ▼       ▼       ▼
          Gold Mine  Town   Call to Arms
              │    Buildings
              ▼
             Gold
```

And the **Gold Mine being free is important**. Drawing one doesn't require you to already have an economy before you can establish an economy.

I'd probably allow up to **4 Gold Mines** in the deck like any other ordinary card. Four mines represent a maximum of **20 harvestable Gold**, but extracting all of it still costs twenty Worker activations. So the real resource isn't merely the Gold Mine—it's also the **Peasants' time**. That creates exactly the sort of interconnected economy this deck is developing.

===== USER =====
Now you can have more gold mines in the deck, because a gold mine is a special kind of card that you can have unlimited of.

===== ASSISTANT =====
Yes. Then **Gold Mine becomes a basic/economy card with no deck-copy limit**, analogous to how some TCGs exempt fundamental resource cards from the normal copy restriction.

So our deck-building rules become:

> **Normal cards:** maximum 4 copies of the same card.  
> **Unlimited cards:** no maximum; cards explicitly marked `Unlimited` can occur any number of times.

### Gold Mine
**Building — Mine — Unlimited**  
**Cost:** 0 Gold  
**Construction:** 0  
**Gold reserves:** 5

**Harvest:** Tap a Worker you control → remove 1 Gold from Gold Mine; gain 1 Gold.

This also means `Unlimited` should be a configurable deck-construction property in our ontology:

```text id="ed3vfb"
DeckRule:
    DefaultMaxCopies: 4

GoldMine:
    DeckLimit: Unlimited
```

Rather than making "Gold Mine" special in the engine.

It gives deck construction an interesting trade-off too. You could put **10 Gold Mines** into a 40-card deck, but then a quarter of your deck consists of mines rather than units, buildings or equipment. So they're unlimited, but including too many has a natural opportunity cost.

And because every mine still requires **Worker activations** to extract its five Gold, drawing several mines doesn't automatically make you rich—you still need enough Peasants or other Worker units to exploit them.

===== USER =====
Just like peasants, you can also have peasants in your deck. And the peasants can maybe be played for one gold or two gold or something.

===== ASSISTANT =====
Yes. I would definitely allow **Peasant cards in the deck**, in addition to the Town Hall's ability to create them. That gives you two routes to grow your workforce.

I think **1 Gold** is probably the better starting price.

### Peasant
**Unit — Human / Peasant / Worker / Builder — Unlimited?**  
**Cost:** **1 Gold**  
**2 ATK | 3 HP | 1 Armor**  
**Housing:** 1

**Worker** — Can be tapped to perform Worker actions, such as harvesting a Gold Mine.

**Builder** — Tap: add 1 Construction Progress to a Building you control.

That creates a useful comparison:

**Play Peasant from hand:**  
Costs **1 Gold + a card from your hand**, but doesn't require tapping the Town Hall.

**Town Hall ability:**  
**2 Gold + Tap Town Hall → create a Peasant**, but doesn't consume a card from your hand.

Those aren't redundant. You're exchanging different resources.

For example, an explosive economic opening could be:

```text id="oj3quz"
Start:
Town Chief
Town Hall
Gold Mine
Peasant in hand

Gold Mine → free

Town Hall → Tap → +2 Gold

Pay 1 Gold → Play Peasant

Peasant → eventually harvest/build
```

We should probably say newly played units **cannot tap on the turn they enter play** unless they have something like `Haste`. Otherwise a 1-Gold Peasant immediately harvesting a Gold Mine effectively refunds itself, making economic acceleration potentially too easy.

And I think there's a strong argument for making **Peasant another Unlimited card**, just like Gold Mine. They're essentially the basic population/resource card of this particular faction.

So the special unlimited cards for the first deck could be:

> **Gold Mine — Unlimited**  
> **Peasant — Unlimited**

Everything else remains maximum four copies.

That would make deck construction quite interesting because players decide their own economic ratio: lots of Mines but few Workers, lots of Peasants but insufficient Mines, or a smaller economy with many combat/equipment cards.

===== USER =====
Then another special card, a bag of gold, that gives three gold. But from this one you can only have four.

===== ASSISTANT =====
Yes. That gives us a nice distinction between **long-term economy** and **immediate liquidity**.

### Bag of Gold
**Card type:** Resource / Treasure  
**Cost:** 0  
**Deck limit:** 4

**Effect:** Gain **3 Gold**, then discard this card.

So unlike a Gold Mine, it requires no Worker and produces all its value immediately:

**Gold Mine**
- Unlimited copies
- 5 potential Gold
- Requires 5 Worker activations to extract everything
- Long-term economic card

**Bag of Gold**
- Maximum 4 copies
- 3 Gold immediately
- No Worker required
- One-use card

This creates some good openings. For example:

```text id="ufwmmp"
Play Bag of Gold → +3 Gold
Play Peasant → -1 Gold
Play Barracks → -3 Gold

Peasant starts constructing Barracks.
```

Assuming you had another Gold available, etc.

I would also make `Resource` or `Treasure` a normal configurable card type rather than giving Bag of Gold special engine behavior:

```text id="mk8ncc"
Bag of Gold

Type: Resource
Tags: Treasure

Cost:
    0

OnPlay:
    Controller.Gold += 3
    Move Source → Discard

DeckLimit:
    4
```

This also gives us a whole future category of one-shot economic cards: supplies, food, building materials, captured treasure, tax collection, and so forth.

===== USER =====
But the peasant can only start constructing the barracks one turn after it comes into play.

===== ASSISTANT =====
Correct. A newly played Peasant should have **summoning sickness**—or whatever term we eventually choose—so it cannot use abilities requiring it to tap on the turn it enters play.

So if the Peasant has:

**Worker — Tap:** Harvest 1 Gold from a Gold Mine.  
**Builder — Tap:** Add 1 construction progress to a Building under construction.

neither can be used immediately.

For example:

```text
TURN 1

Play Bag of Gold
→ +3 Gold

Play Peasant
→ -1 Gold

Play Barracks
→ pay its Gold cost
→ Barracks enters Under Construction (0/3)

Peasant cannot build yet.
```

Then:

```text
TURN 2

Peasant becomes ready.

Tap Peasant → Builder
Barracks: 0/3 → 1/3
```

With that same Peasant alone:

```text
Turn 2 → 1/3
Turn 3 → 2/3
Turn 4 → 3/3 → Barracks completed
```

Additional Builders can accelerate it.

I'd make the underlying rule broader than just Peasants:

> **A unit cannot attack or activate an ability with a Tap cost during the turn in which it entered your battlefield, unless an ability explicitly allows it.**

That means the same rule covers **attacking, building, harvesting**, and future tap abilities.

We can later give the mechanic a keyword such as **Ready**, **Haste**, or **Active** to bypass this restriction.

===== USER =====
So now the strategy becomes, you first build some peasants quick, you use them to start building buildings. And when they are done harvesting resources and building, you can use Call to Arm, and then use your peasants to attack the enemy or to defend.

===== ASSISTANT =====
Exactly. That gives the **Town Chief deck a very clear economic-to-military conversion loop**. The same Peasants are your economy, construction workforce, and emergency army.

The central decision each turn becomes:

> **What should each Peasant do this turn?**

A Peasant can effectively serve one role because harvesting/building taps it:

| Use | Result |
|---|---|
| **Harvest** | Tap → extract 1 Gold from a Gold Mine |
| **Build** | Tap → add 1 construction progress |
| **Attack** | Use it as a combat unit |
| **Defend** | Keep it available for the opponent's turn |

Then **Call to Arms** creates the transition from economy to warfare.

Suppose you have five Peasants. During peaceful development, you might use:

```text
Peasant 1 → Harvest
Peasant 2 → Harvest
Peasant 3 → Build Barracks
Peasant 4 → Build Barracks
Peasant 5 → remain available
```

But once your infrastructure is established, those same five Peasants are no longer needed as intensively for construction.

The Town Chief saves up **3 AP**, activates Call to Arms, and suddenly:

**5 × Peasant**

normally:

> 2 ATK / 3 HP / 1 Armor

become:

> **3 ATK / 3 HP / 2 Armor**

until end of turn.

That's potentially **15 total Attack** coming from what was your workforce.

There's an important tactical trade-off, though. If you've already tapped three Peasants to mine Gold and build that turn, they're unavailable to attack. So if you're planning a Call to Arms turn, you probably intentionally **don't use your workforce economically**:

```text
Previous turns:
Harvest → Build → Expand → Train

Attack turn:
Don't harvest
Don't build
        ↓
Call to Arms
        ↓
Peasant army attacks
```

That creates telegraphing too. Your opponent might notice:

> "He has six Peasants and the Town Chief just reached 3 AP."

Now they know a major attack could be coming.

It also gives the Town Chief a strong identity compared with the Paladin. The **Town Chief converts economic capacity into temporary military capacity**. The Paladin instead protects valuable developed units and buildings.

And there's another nice emergent effect: as the game progresses and your buildings finish, **Builder labor naturally becomes available for warfare**. Early game, Peasants are desperately needed to establish the town. Later, the town is built, so the same population can be mobilized.

That's very close to the economic/military tension of an RTS, but expressed through a card game—and it emerged from only a few simple mechanics rather than needing a complicated special rule.

===== USER =====
Now come up with the full deck again.

===== ASSISTANT =====
Yes. With the mechanics we've now established, I'd make the first version a **40-card deck**, with **Town Hall + Town Chief outside the deck as the two starting cards**.

The deck's identity is: **establish economy → expand the town → train specialists → mobilize the workforce when necessary.**

### Starting cards

**Town Hall — Headquarters**  
8 HP | 2 Armor | **Housing 5**

- **Tap:** Gain 2 Gold.
- **2 Gold + Tap:** Create a Peasant.
- The Town Chief initially consumes 3 of the 5 Housing.

**Town Chief — Hero**  
4 ATK | 5 HP | 2 Armor | **Housing 3**  
Gains **1 AP each turn**.

- **Call to Arms — 3 AP:** All your Peasants gain +1 ATK/+1 Armor until end of turn.
- **Regenerate — 2 AP, Tap:** Heal 3 HP.

---

## 40-card deck

| # | Card | Copies | Cost | Main purpose |
|---|---|---:|---|---|
| 1 | Gold Mine | 7 | Free | Worker economy |
| 2 | Bag of Gold | 3 | Free | Gain 3 Gold |
| 3 | Peasant | 6 | 1 Gold | Worker/Builder |
| 4 | Barracks | 3 | 3 Gold, 3 BP | Training + Housing |
| 5 | Archery Range | 2 | 3 Gold, 3 BP | Archer efficiency |
| 6 | Tavern | 2 | 2 Gold, 2 BP | Mercenary support |
| 7 | Shrine | 1 | 4 Gold, 4 BP | Hero resurrection |
| 8 | Soldier | 3 | 2 Gold + 1 Training | Infantry |
| 9 | Archer | 2 | 2 Gold + 2 Training | Ranged unit |
| 10 | Town Guard | 2 | 3 Gold + 1 Training | Defense |
| 11 | Mercenary | 2 | 4 Gold | No Training needed |
| 12 | Knight | 1 | 5 Gold + 3 Training | Elite unit |
| 13 | Paladin | 1 | 6 Gold + 2 Training | Replacement Hero |
| 14 | Master Builder | 1 | 4 Gold | Replacement Hero |
| 15 | Short Sword | 1 | 2 Gold | +1 ATK |
| 16 | Broadsword | 1 | 3 Gold | +2 ATK, two-handed |
| 17 | Wooden Shield | 1 | 2 Gold | +1 Armor |
| 18 | Leather Armor | 1 | 3 Gold | +1 Armor |
| 19 | Longbow | 1 | 3 Gold | +1 ATK + Ranged |
|  | **Total** | **40** | | |

Gold Mine and Peasant are **Unlimited** cards for deck-building purposes, even though this particular deck uses 7 and 6. All ordinary cards have a maximum of four copies.

---

## Economy cards

### Gold Mine ×7
**Building — Mine — Unlimited**  
**Cost: 0 | Construction: 0**  
Starts with **5 Gold reserves**.

**Harvest:** Tap a Worker → remove 1 Gold from this Mine and gain 1 Gold.

Multiple Workers can harvest the same Mine.

### Bag of Gold ×3
**Treasure**  
**Cost: 0**

Play → Gain **3 Gold**, then discard Bag of Gold.

Deck limit: 4.

### Peasant ×6
**Unit — Peasant / Worker / Builder — Unlimited**  
**1 Gold**  
**2 ATK | 3 HP | 1 Armor | Housing 1**

**Worker:** May perform Worker actions.

**Builder — Tap:** Add **1 construction progress** to a Building under construction.

Like other units, a Peasant cannot attack or use Tap abilities on the turn it enters play.

---

# Buildings

### Barracks ×3
**3 Gold | Construction 3**  
**6 HP | 2 Armor | Housing +2**

**Military Training — Tap:** Gain **1 Training Point**.

Training accumulates until spent.

This is your primary route into professional troops.

### Archery Range ×2
**3 Gold | Construction 3**  
**5 HP | 1 Armor**

**Archery Training — Tap:** Your next Archer played this turn costs **1 less Training**.

Multiple Archery Range activations can stack.

### Tavern ×2
**2 Gold | Construction 2**  
**5 HP | 1 Armor | Housing +1**

**Recruitment — Tap:** Your next Mercenary played this turn costs **1 less Gold**.

### Shrine ×1
**4 Gold | Construction 4**  
**5 HP | 1 Armor**

**Resurrection Ritual — 3 Gold, Tap:** Choose your dead Hero and begin a resurrection ritual.

The ritual requires **3 turns of progress**. When completed, return that Hero at full HP and 0 AP.

If the Shrine is destroyed before completion, the ritual fails.

---

# Military units

### Soldier ×3
**2 Gold + 1 Training**  
**3 ATK | 4 HP | 2 Armor | Housing 1**

Straightforward professional infantry. Efficient stats are its advantage.

### Archer ×2
**2 Gold + 2 Training**  
**4 ATK | 3 HP | 1 Armor | Housing 1**

**Ranged:** When this attacks a non-Ranged unit, that unit doesn't deal combat damage back.

The high Training cost makes Archery Ranges valuable.

### Town Guard ×2
**3 Gold + 1 Training**  
**2 ATK | 5 HP | 2 Armor | Housing 1**

**Guard:** While this unit is untapped, opponents must attack a Guard before they can attack your Hero or Headquarters.

### Mercenary ×2
**4 Gold**  
**4 ATK | 4 HP | 1 Armor | Housing 1**

Requires no Training.

This is important because destroying your Barracks shouldn't make your deck incapable of fighting.

### Knight ×1
**5 Gold + 3 Training**  
**5 ATK | 5 HP | 3 Armor | Housing 2**

An expensive late-game unit with no complicated ability. Its efficiency comes from raw power.

---

# Replacement Heroes

Only **one Hero can be active at a time**.

### Paladin ×1
**6 Gold + 2 Training**  
**3 ATK | 7 HP | 3 Armor | Housing 3**  
Gains 1 AP per turn.

**Divine Shield — 2 AP:** Target friendly character gains +2 Armor until the beginning of your next turn.

**Holy Light — 3 AP, Tap:** Heal another friendly character or Building for **3 HP**.

This changes the deck from workforce aggression toward defense.

### Master Builder ×1
**4 Gold**  
**2 ATK | 5 HP | 1 Armor | Housing 2**  
Gains 1 AP per turn.

**Rapid Construction — 2 AP:** Add 2 construction progress to a Building.

**Organize Workers — 3 AP, Tap:** Untap up to two Peasants.

This one now fits the deck particularly well. Those Peasants can subsequently build or harvest again.

---

# Equipment

I'd keep equipment relatively scarce in this first version because Heroes and units can use the same equipment.

### Short Sword ×1
**2 Gold — One Hand**  
+1 ATK.

### Broadsword ×1
**3 Gold — Two Hands**  
+2 ATK.

### Wooden Shield ×1
**2 Gold — Off Hand**  
+1 Armor.

### Leather Armor ×1
**3 Gold — Body**  
+1 Armor.

### Longbow ×1
**3 Gold — Two Hands**  
+1 ATK and grants **Ranged**.

---

## The resulting progression

The deck now has a very natural early/mid/late-game development:

```text id="nm5gzr"
TOWN HALL
   │
   ├── Gold
   └── Peasants
          │
          ├──────── Harvest Gold Mines
          │
          ├──────── Construct Buildings
          │
          └──────── Call to Arms
                         ↓
                    Temporary Army

BUILDINGS
   │
   ├── Barracks ─────── Training
   │                       │
   │                       ├── Soldiers
   │                       ├── Guards
   │                       └── Knights
   │
   ├── Archery Range ─── cheaper Archers
   │
   ├── Tavern ────────── cheaper Mercenaries
   │
   └── Shrine ────────── resurrect Hero
```

What I particularly like is that the **Peasant remains relevant throughout the entire game**.

Early game:

> Peasant = economic investment.

Midgame:

> Peasant = Builder/Worker establishing your infrastructure.

Late game:

> Buildings are finished, Gold Mines may be exhausted, so those same Peasants become increasingly available for **Call to Arms**.

That means your weakest 1-Gold card doesn't become dead weight later. Its role naturally changes as the town matures.

One thing I'd test immediately is whether **7 Mines + 6 Peasants + 3 Bags of Gold = 16 economy cards** is slightly too much for a 40-card deck. But that's precisely the sort of number I'd determine through the first few simulated games rather than trying to perfect theoretically. memcite

===== USER =====
Okay, awesome. Let's now imagine a completely different kind of deck. This deck uses, let's, to make it easy, make it also use gold. But this one does not use peasants that are summoned in the headquarters, but a very different way to gather the gold.

===== ASSISTANT =====
A good opposite to the Town deck would be a **Raider/Orc-style deck**: instead of building an economy with Workers and Mines, it gets Gold primarily through **combat, pillaging and destroying things**.

That would demonstrate exactly what we wanted from the game system: both decks share `Gold`, but obtain and use it completely differently.

## Raider deck concept

The economic loop would be:

```text
TOWN DECK

Peasants → Work → Gold
    ↓
Buildings
    ↓
Training
    ↓
Army


RAIDER DECK

Attack → Damage/Destroy → Gold
              ↓
         More Raiders
              ↓
          More Attack
```

The Town deck gets stronger by investing time.

The Raider deck gets stronger by maintaining momentum.

### Starting HQ: Raider Camp

**Raider Camp — Headquarters**  
**9 HP | 1 Armor**  
No Housing mechanic.

**Spoils of War — Passive:** The first time each turn one of your units damages an enemy Hero or Headquarters, gain **1 Gold**.

**Raid Party — 2 Gold, Tap:** Draw a Raider unit, then discard a card.

So unlike the Town Hall:

> Town Hall → directly creates Gold.

the Raider Camp says:

> Go attack somebody if you want money.

And importantly, this deck simply **doesn't use Housing**.

---

## Starting Hero: Warchief

**Warchief — Hero**  
**5 ATK | 6 HP | 2 Armor**  
Gains 1 personal AP each turn.

**Bloodlust — 3 AP:** Your attacking units gain **+1 Attack** until end of turn.

**Pillage — 2 AP:** The next enemy Building destroyed this turn gives you **3 Gold**.

This Hero wants you constantly attacking rather than spending turns developing infrastructure.

---

# Their basic economy

Instead of Peasants + Gold Mines, their fundamental cards could be things like:

### Loot ×4

**Treasure**

Gain **2 Gold**.

Not quite as powerful as the Town deck's Bag of Gold, because we'd want Raiders to earn their real money through aggression.

### Scavenger ×4

**2 Gold**  
**2 ATK | 3 HP | 1 Armor**

**Scavenge:** Whenever this unit participates in destroying an enemy unit, gain **1 Gold**.

Now the unit itself is economic infrastructure.

Compare:

```text
Peasant
↓
Tap
↓
Mine
↓
Gold
```

versus:

```text
Scavenger
↓
Attack
↓
Kill
↓
Gold
```

That's a fundamentally different gameplay loop.

---

# Pillaging buildings

I'd make this deck particularly dangerous to the Town deck's infrastructure.

### Pillager

**3 Gold**  
**3 ATK | 4 HP | 1 Armor**

**Pillage:** +2 Attack when attacking Buildings.

**Loot:** When this destroys a Building, gain **2 Gold**.

Suddenly your opponent's Barracks, Tavern, Shrine and Archery Range aren't merely tactical targets—they're your **economic resources**.

That's a great asymmetric interaction.

---

# Their "Gold Mine"

We could even give them something thematic that produces finite Gold without Workers.

### Stolen Treasure

**Cost: 1 Gold**

Enters with **4 Gold counters**.

At the beginning of your turn:

> Remove one Gold counter → gain 1 Gold.

No Worker required.

So:

```text
Town Gold Mine
5 potential Gold
Requires Worker activations

Stolen Treasure
4 potential Gold
Automatic
Costs 1 Gold initially
```

Both decks use Gold, but completely differently.

---

# No Housing

This is where the deck becomes an excellent demonstration of the system we've designed.

Raider units simply have:

```text
Housing Cost: none
```

Instead, I'd give the Raider deck another limiting mechanism: **Warband**.

For example, the Raider Camp might say:

> You may control at most **5 Raider units**.

And certain cards increase that:

**War Banner**

> Your maximum Warband size increases by 2.

But we don't necessarily even need that. Gold/card availability might provide enough natural limitation.

I'd initially try the deck with **no capacity mechanic whatsoever**.

That makes the contrast stronger:

**Town:**  
Gold + Housing + Construction + Training

**Raiders:**  
Gold + aggression

The Raider deck is much simpler economically but has to expose its units to combat to maintain income.

---

# Different buildings

They could still have buildings, but they wouldn't require Builders.

For example:

### War Tent

**3 Gold**  
**5 HP | 1 Armor**

Enters play immediately.

**Tap:** The next Raider you play this turn costs 1 less Gold.

### Fighting Pit

**4 Gold**  
**6 HP | 1 Armor**

**Tap:** Target Raider gets +1 Attack this turn.

### Trophy Rack

**2 Gold**  
**4 HP | 0 Armor**

Whenever you destroy an enemy unit with 4+ Attack:

> Gain 1 Gold.

Again, their buildings are **purchased**, not constructed.

No Building Points.

---

# And equipment can overlap

This is where cross-deck compatibility becomes interesting.

The Raider Warchief can still equip:

> Short Sword  
> Broadsword  
> Wooden Shield  
> Leather Armor  
> Longbow

because those aren't necessarily Town-specific.

But Raiders could introduce new equipment:

**Battle Axe — 3 Gold — Two-handed**  
+2 Attack.  
+1 additional Attack against Buildings.

**Looted Shield — 2 Gold — Off Hand**  
+1 Armor.

Now a Town deck could potentially include Battle Axe too, unless deck/faction restrictions prevent it.

---

## The strategic contrast

We would now have two genuinely different decks using the same underlying rules engine:

| | Town Chief | Raider Warchief |
|---|---|---|
| Primary resource | Gold | Gold |
| Gold generation | Work/mines/Town Hall | Combat/pillaging |
| Basic unit | Peasant | Scavenger/Raider |
| Housing | Yes | No |
| Construction | Builders + progress | No |
| Training | Yes | No |
| Development | Infrastructure | Momentum |
| Hero strategy | Mobilize workforce | Amplify aggression |
| Weakness | Slow setup | Economy collapses if attacks fail |
| Long game | Strong | Weaker |
| Early game | Vulnerable | Strong |

And this creates a very interesting matchup.

The **Town player wants time**:

> Mines → Peasants → Barracks → Training → professional army.

The **Raider player wants to deny time**:

> Attack → steal Gold → destroy infrastructure → finance more attackers.

So the decks don't merely have different artwork and abilities. They effectively play **different games against each other while still participating in the same shared combat system**.

That is exactly the kind of asymmetry that would prove the generic game engine and ontology are doing what we intended. memcite

===== USER =====
but why would they use loot in that deck if they can use bag of gold instead. the loot card should maybe say steal 2 gold from another player? and you can only play it during your turn so if the enemy doesnt have any god there is nothing to steal

===== ASSISTANT =====
Exactly. If **Bag of Gold gives 3 Gold unconditionally**, a card that simply gives 2 Gold is dominated unless it has some other advantage. Making Loot actually *loot the opponent* gives it a distinct role.

I'd define it like this:

### Loot
**Action — Raid**  
**Cost: 0**  
**Deck limit: 4**  
**Timing:** Your turn only.

**Effect:** Steal up to **2 Gold** from target opponent.

"Steal" should mean the Gold actually moves between players rather than being generated:

```text
Opponent Gold: -X
Your Gold:     +X

X = min(2, opponent's current Gold)
```

So:

| Opponent has | You steal |
|---:|---:|
| 0 Gold | 0 |
| 1 Gold | 1 |
| 2+ Gold | 2 |

This makes **Loot situationally stronger than Bag of Gold** because it's a swing of up to 4 Gold in relative economic position:

**Bag of Gold:**  
You +3 → relative advantage **+3**

**Loot:**  
You +2 / opponent −2 → relative advantage **+4**

But it has a major weakness: if the opponent has no Gold when you can play it, **Loot does nothing**.

That also creates interesting counterplay. If you're playing against Raiders and they're holding cards, you might deliberately spend your Gold rather than stockpile it.

There's an important rules detail I'd preserve from what you said:

> **Loot can only be played during your own turn.**

So it isn't an Instant/reaction. The Raider cannot wait for you to generate Gold and steal it before you spend it. They have to hope you end your turn with Gold remaining.

This gives us another useful generic card concept:

**Steal Resource**

rather than:

**Opponent loses 2 Gold + you gain 2 Gold.**

Those can be mechanically different. If an opponent has an effect saying "your Gold cannot be stolen," it should prevent both sides of the transfer.

And this makes the Raider economy more coherent:

> **Bag of Gold** remains a generic card either deck can use for reliable liquidity.

> **Loot** is a Raider-style economic warfare card: potentially more strategically valuable, but dependent on the opponent having something worth stealing.

I'd absolutely allow the Raider deck to contain **Bag of Gold as well**. Generic cards shouldn't become artificially unavailable merely because a faction has thematic alternatives. The interesting deck-building question becomes whether you prefer the reliability of Bag of Gold or the disruption and higher relative value of Loot.

===== USER =====
Stolen Treasure    -- remove up to 4 gold from another player. for each gold add a token on stolen treasure

===== ASSISTANT =====
Yes. That's much better than having **Stolen Treasure** generate Gold by itself. It becomes a way to capture the opponent's wealth, but you still need to extract it.

### Stolen Treasure
**Building — Treasure**  
**Cost:** perhaps **2 Gold**  
**Construction:** None  
**Deck limit:** 4

**Plunder — When Stolen Treasure enters play:** Remove up to **4 Gold** from target opponent. For each Gold removed this way, place **1 Gold Token** on Stolen Treasure.

So:

```text
Opponent has 7 Gold
→ Remove 4
→ Stolen Treasure gets 4 Gold Tokens

Opponent has 2 Gold
→ Remove 2
→ Stolen Treasure gets 2 Gold Tokens

Opponent has 0 Gold
→ Remove 0
→ Stolen Treasure gets 0 Gold Tokens
```

But I'd make an important distinction from **Loot**.

**Loot**

> Steal up to 2 Gold.

The Gold immediately becomes yours.

**Stolen Treasure**

> Remove up to 4 Gold from the opponent and put that amount *on the card*.

You haven't actually gained spendable Gold yet.

Then we need an extraction mechanism. For the Raider deck, something like:

**Claim Spoils — Tap Stolen Treasure:** Remove 1 Gold Token from Stolen Treasure. Gain 1 Gold.

That would mean the opponent suffers the economic damage immediately, but the Raider gets the economic benefit gradually.

So stealing 4 creates:

```text
Opponent:
-4 Gold immediately

Stolen Treasure:
● ● ● ●

Following turns:
Tap → ● ● ● → +1 Gold
Tap → ● ●   → +1 Gold
Tap → ●     → +1 Gold
Tap →       → +1 Gold
```

That also creates counterplay: **destroy Stolen Treasure before all the Gold is collected**, and the Raider loses the remaining captured wealth.

I particularly like that because it makes the card meaningfully different from Loot:

| | Loot | Stolen Treasure |
|---|---|---|
| Removes enemy Gold | Up to 2 | Up to 4 |
| You get Gold immediately | Yes | No |
| Can be destroyed | No | Yes |
| Long-term value | None | Up to 4 Gold |
| Risk | Enemy may have no Gold | Enemy may destroy treasure |

And I'd probably make **Stolen Treasure cost 1 Gold rather than 2**. Paying 2 Gold for a card that could capture nothing and then takes four turns to recover its maximum value seems too weak. At **1 Gold**, it's a meaningful high-risk economic attack.

===== USER =====
It still needs a new way to bring a new hero into the game. Not a shrine but some other completely unique way

===== ASSISTANT =====
Yes. I would make the Raider deck's Hero replacement mechanic almost the inverse of the Town deck's resurrection.

For Raiders, **a new leader earns the right to become Warchief through combat**.

### Chieftain's Challenge

When your Hero dies, you gain the status **Leaderless**.

While Leaderless, any Raider unit you control can earn **Glory**:

> Whenever a Raider destroys an enemy unit, put 1 Glory counter on that Raider.

When a Raider reaches **3 Glory**, you may promote it into a Hero from your hand.

For example:

**Orc Berserker**
4 ATK | 4 HP | 1 Armor

kills three enemies:

```text
Berserker
Glory: ●●●
```

Now:

> **Ascension — 3 Glory:** Play a Raider Hero from your hand, replacing this unit. Ignore the Hero's normal Gold cost.

So perhaps your second Hero is:

### Bloodfang — Hero
**6 ATK | 6 HP | 1 Armor**

Gains 1 AP each turn.

**Frenzy — 2 AP:** Bloodfang gets +2 Attack this turn.

**Blood Price — 3 AP:** Sacrifice another friendly unit → heal Bloodfang 3 HP and gain 1 Gold.

This creates a completely different recovery story.

The Town deck says:

> **Our leader has fallen. Build/protect a Shrine and bring them back.**

The Raider deck says:

> **Our leader has fallen. Whoever proves themselves in battle becomes the new leader.**

### But I'd make it even more interesting

Instead of simply replacing the Raider with a Hero card, let the successful Raider **become the Hero**.

For example, Hero cards could have:

> **Claim Leadership — Requires a Raider with 3 Glory.**

You play Bloodfang *onto* that Raider. The Raider is removed and Bloodfang enters in its place.

Potentially, equipment could even remain attached:

```text
Orc Berserker
+ Battle Axe
+ 3 Glory
      ↓
CLAIMS LEADERSHIP
      ↓
Bloodfang
+ Battle Axe
```

Now the player has literally watched a basic unit fight its way up through the ranks and become the faction's new Hero.

And this is mechanically important for the broader game: **Hero recovery doesn't have to be a universal mechanic at all.**

Different decks can define entirely different paths:

```text
Town
Hero dies
→ Shrine
→ multi-turn resurrection

Raiders
Hero dies
→ Raider earns 3 Glory
→ claims leadership
→ new Hero

Necromancers
Hero dies
→ sacrifice creatures
→ resurrect as undead Hero

Kingdom
Hero dies
→ successor/heir takes throne

Machines
Hero dies
→ collect components
→ rebuild Hero

Cult
Hero dies
→ complete ritual
→ summon new leader
```

That is exactly the kind of asymmetry that will make both the card game **and the generic engine** interesting.

===== USER =====
I think the glory part comes from the hero card itself. So you habe the hero in your hand and it says: cost: 3 glory. And 3 glory is earned by promoting.a unit that has participated in killing 3 enemies to that hero. The hero will take on the equipment that unit has

===== ASSISTANT =====
Yes. That's cleaner. **Glory belongs to the candidate unit**, while the **Hero card defines Glory as its promotion cost**.

So this isn't a general Raider rule saying "at 3 Glory become a Hero." Different Hero cards can require different amounts of Glory.

### Glory

A Raider unit gains **1 Glory** whenever it participates in killing an enemy.

```text
Raider kills enemy       → +1 Glory
Raider assists in kill   → +1 Glory
```

The Glory counters stay on that particular unit.

Then a Hero in your hand might say:

### Bloodfang, Raider Chief — Hero
**Promotion Cost: 3 Glory**

**Promote:** During your turn, choose a Raider unit you control with at least 3 Glory. Play Bloodfang from your hand by promoting that unit.

The transformation is effectively:

```text
Veteran Raider
4 ATK / 4 HP / 1 Armor
Glory: ●●●

Equipment:
Battle Axe
Leather Armor

          ↓ PROMOTE ↓

Bloodfang, Raider Chief
6 ATK / 6 HP / 2 Armor

Equipment:
Battle Axe
Leather Armor
```

The original unit leaves play and the Hero takes its place.

Critically, **all compatible equipment transfers to the new Hero**.

### I'd make Glory a real resource on the unit

This fits the ontology very nicely because it resembles the Town Chief's AP but behaves differently:

```text
Action Points
Scope: Entity
Generated: +1 per turn
Spent: Abilities

Glory
Scope: Entity
Generated: Participates in killing enemy
Spent: Promotion
```

So if Bloodfang costs 3 Glory, the selected unit pays those 3 Glory as the promotion cost.

Another Hero could therefore work differently:

**Gorak the Conqueror — Hero**  
**Promotion Cost: 5 Glory**

and be substantially stronger.

Or:

**Skull Shaman — Hero**  
**Promotion Cost: 2 Glory from a Shaman-type unit**

The requirement comes from the Hero card itself.

### Equipment inheritance is particularly good

I'd generalize promotion as an entity replacement operation that can specify which state transfers:

```text
PROMOTE

Source:
    Selected Unit

Target:
    Hero card from hand

Cost:
    Source.Glory >= Hero.PromotionCost

Transfer:
    Equipment = yes
    Damage = no
    Status effects = no
    Glory = no
    Temporary modifiers = no

Replace:
    Unit → Hero
```

So equipment remains because the *person is being promoted*, rather than a completely unrelated Hero appearing.

That also creates a very nice emergent story. A cheap Raider could start with a basic Axe, survive several battles, accumulate Glory, acquire better equipment, and eventually become your new Warchief.

And I would allow promotion **even while your original Hero is alive**, subject to the one-Hero limit? Probably **no** for this deck: the Hero card should say something like:

> **Promotion — You may play this only if you control no Hero.**

That makes it specifically their Hero-recovery/succession mechanic.

So when their Warchief dies, the opponent has created a temporary vulnerability—but if there's already a Raider sitting there with ●●● Glory, the opponent knows a new Hero can emerge on the Raider player's next turn.

===== USER =====
I would like to now see a wizard deck. That is structured around mana. So the hero and the hq gather mana and the hero is a wizard who can also reload mana at the hq and other units as well. So the hq becomes a way to swap mana between caster units. And swapping mana causes the unit to be tapped. The hero can summon certain kind of units and the units can also summon units or cast spekls. And some of them gather mana in various ways. And magic armors and weapons last for some turns only.

===== ASSISTANT =====
Yes. This can be radically different from both the Town and Raider decks. The Wizard deck's central idea should be **mana circulation**: mana exists on individual caster entities, and the HQ acts as the network through which mana can be stored and redistributed.

I would call the deck **The Arcane Conclave**.

### Starting cards

**Arcane Nexus — Headquarters**  
8 HP | 2 Armor  
Mana capacity: **8**  
Starts with **3 Mana**.

**Channel — Tap:** Gain 2 Mana.

**Mana Transfer:** Tap a Caster you control → transfer any amount of Mana between that Caster and the Arcane Nexus, up to the receiving entity's Mana capacity.

The important part is that mana isn't primarily a player resource:

```text
Wizard ─────┐
Apprentice ─┼──► Arcane Nexus ──► another Caster
Seer ───────┘
```

So an Apprentice might generate Mana, deposit it in the Nexus, and your Hero later withdraw it. But **the unit performing the transfer taps**.

---

### Starting Hero — Archmage

**Archmage — Hero / Wizard / Caster**  
3 ATK | 5 HP | 1 Armor  
Mana capacity: **6**  
Starts with **3 Mana**.

**Meditate — Tap:** Gain 2 Mana.

**Summon Familiar — 2 Mana, Tap:** Summon an Arcane Familiar.

Notice that the Archmage has no AP system. **Mana replaces AP entirely for this Hero.**

That also means the Wizard player's fundamental decision is:

> Cast something? Generate Mana? Transfer Mana? Attack?

because all of those may compete for the same tap.

---

# Mana-producing units

This is where I'd make the deck particularly asymmetric.

### Apprentice Mage
**2 Gold**  
2 ATK | 3 HP | 0 Armor  
Mana capacity: 3  
Starts with 0 Mana.

**Study — Tap:** Gain 1 Mana.

**Spark — 2 Mana:** Deal 1 damage to any character.

So the Apprentice can slowly charge itself and eventually cast.

### Mana Seer
**3 Gold**  
1 ATK | 3 HP | 1 Armor  
Mana capacity: 4

**Foresee — At the beginning of your turn:** Gain 1 Mana.

Unlike the Apprentice, this doesn't require tapping. It's slower economically but remains available.

### Mana Leech
**3 Gold**  
2 ATK | 3 HP | 0 Armor  
Mana capacity: 3

**Mana Drain:** Whenever Mana Leech deals combat damage to a Caster, transfer 1 Mana from that Caster to Mana Leech if possible.

Now combat itself can generate/steal Mana.

---

# Summoning chains

I'd make one of the Wizard deck's defining characteristics that **cards can create other units**.

The Archmage summons:

### Arcane Familiar
2 ATK | 2 HP | 0 Armor  
Mana capacity: 2

**Arcane Bond:** When summoned, you may transfer 1 Mana from its summoner to the Familiar.

**Sacrifice — Tap:** Transfer all Mana from Familiar to its summoner, then destroy Familiar.

So the Familiar can effectively become a little Mana battery.

But other units have their own summons.

### Conjurer
**4 Gold**  
2 ATK | 4 HP | 1 Armor  
Mana capacity: 5

**Summon Elemental — 4 Mana, Tap:** Summon an Elemental.

### Elemental
5 ATK | 4 HP | 1 Armor

**Unstable:** At the beginning of your turn, its summoner must pay 1 Mana or destroy Elemental.

Now powerful summoned units have an ongoing magical maintenance cost.

---

# Spell cards

This deck should naturally contain considerably more one-shot cards than the Town deck.

### Fireball
**Spell**  
**3 Mana**

Deal **4 damage** to target character or Building.

The Caster paying the Mana casts the spell.

### Arcane Shield
**Instant Spell**  
**2 Mana**

Target character gets **+2 Armor until end of turn**.

Because it's an Instant, it can be played during an appropriate reaction window.

### Mana Surge
**Spell**  
**1 Mana**

Transfer up to 3 Mana from your Arcane Nexus to target Caster **without tapping that Caster**.

This breaks the normal transfer rule and is therefore valuable.

### Counterspell
**Instant Spell**  
**3 Mana**

Cancel target Spell being cast.

Now our stack/reaction engine gets properly exercised.

---

# Temporary magical equipment

Your idea that magical weapons and armor expire is particularly appropriate here. I'd implement them as Equipment with **Duration counters**.

### Arcane Blade
**2 Mana**  
Weapon — One Hand  
**Duration: 3 turns**

Equipped character gets **+2 Attack**.

At the beginning of your turn:

> Remove one Duration counter.

When the last is removed:

> Destroy Arcane Blade.

### Mage Armor
**3 Mana**  
Armor — Body  
**Duration: 3 turns**

Equipped character gets **+2 Armor**.

### Staff of Power
**3 Mana**  
Weapon — Two Hands  
**Duration: 4 turns**

Equipped Caster gets:

> **Mana capacity +2**

and:

> Whenever this character generates Mana itself, generate +1 additional Mana.

### Boots of Haste
**2 Mana**  
Equipment — Feet  
**Duration: 2 turns**

Once each turn:

> After equipped character taps to cast a Spell, untap it.

Potentially very powerful, hence the short duration.

---

# More unusual mana generation

I'd deliberately make different Wizards generate Mana differently.

For example:

### Pyromancer
3 ATK | 3 HP | 0 Armor  
Mana capacity: 4

> Whenever an enemy character takes spell damage, Pyromancer gains 1 Mana. Maximum once per turn.

### Death Mage
2 ATK | 4 HP | 0 Armor  
Mana capacity: 5

> Whenever another unit dies, Death Mage gains 1 Mana.

### Storm Mage
3 ATK | 3 HP | 1 Armor  
Mana capacity: 4

> Whenever you cast an Instant during another player's turn, gain 1 Mana.

Now choosing which Casters you deploy changes how your entire mana economy behaves.

---

## The core mana cycle

This gives the deck a very different economic system:

```text
                    ARCANE NEXUS
                     Mana Storage
                    /     |      \
                   /      |       \
                  ▼       ▼        ▼
             Archmage  Apprentice  Conjurer
                │          │          │
                │          │          │
              Spells     Spells     Summons
                │                     │
                ▼                     ▼
             Combat                Elementals
```

Suppose:

```text
Arcane Nexus     4/8 Mana
Archmage         1/6
Apprentice       3/3
Conjurer         0/5
```

You want to summon an Elemental, which requires the Conjurer to have 4 Mana.

You could:

**1. Tap Apprentice** → deposit its 3 Mana into Nexus.

```text
Nexus: 7
Apprentice: 0 [Tapped]
```

**2. Tap Conjurer** → withdraw 4.

```text
Nexus: 3
Conjurer: 4 [Tapped]
```

But now there's a problem:

> The Conjurer is tapped, so it can't tap to Summon Elemental this turn.

That's excellent.

Mana transfer has a **tempo cost**.

You therefore need to prepare your mana distribution ahead of time—or use a card like Mana Surge to bypass the normal restriction.

---

## I'd add one more important rule

Normally:

> **Mana remains on the entity between turns.**

So Mana isn't reset.

That means you can charge an Apprentice:

```text
Turn 1: 1/3
Turn 2: 2/3
Turn 3: 3/3
```

and decide later whether to spend it personally or deposit it into the Nexus.

If the Apprentice dies while holding 3 Mana, that Mana is **lost**.

Suddenly mana placement itself becomes strategically important.

Keeping 8 Mana safely in the HQ protects it—but requires tapping Casters to withdraw it.

Keeping Mana directly on your Wizards allows immediate responses—but risks losing it if they're killed.

That's a very strong strategic distinction.

### Hero recovery should also be magical

And following our principle that every deck should solve this differently, I wouldn't give Wizards a Shrine or Glory.

I'd give the Arcane Nexus something like **Arcane Reconstitution**:

> When your Archmage dies, its magical essence is stored in the Nexus.

Perhaps the dead Hero becomes an **Essence** attached to the Nexus.

Then:

> **Pay 6 Mana from the Nexus over any number of turns → return that Hero.**

Other Casters can therefore literally channel their Mana through the HQ to reconstruct their dead master.

So:

```text
Town:
Workers → Shrine → time → Hero

Raiders:
Kills → Glory → promotion → Hero

Wizards:
Casters → Mana → Nexus → reconstitution → Hero
```

That makes three decks already feel like three substantially different games, despite sharing the same underlying card engine. memcite

===== USER =====
Some Units can also last for a number of turns only.

===== ASSISTANT =====
Absolutely. That fits especially well with **summoned magical units**. I'd make `Duration` a completely generic property, usable by units, equipment, effects, buildings, or anything else.

For the Wizard deck, there could be a distinction between **permanent Casters** and **temporary Summons**.

For example:

**Fire Elemental — Summoned Unit**  
5 ATK | 4 HP | 1 Armor  
**Duration: 3**

At the beginning of its controller's turn, remove 1 Duration counter. When the last is removed, destroy/sacrifice the Fire Elemental.

So:

```text
Summoned → Duration 3

Next turn → 2
Next turn → 1
Next turn → 0 → disappears
```

That lets us make summoned creatures substantially stronger for their Mana cost because you're purchasing **temporary board power** rather than a permanent unit.

Another could work differently:

**Arcane Guardian**  
3 ATK | 6 HP | 3 Armor  
**Duration: 2**

It's essentially a magical emergency defender: extremely durable, but temporary.

And:

**Lightning Spirit**  
6 ATK | 2 HP | 0 Armor  
**Duration: 1**

A nearly disposable burst attacker.

More interestingly, duration doesn't necessarily have to mean "number of turns." The generic engine could support different expiration conditions:

```text
Duration
├── 3 ControllerTurns
├── 2 Rounds
├── UntilEndOfTurn
├── UntilStartOfNextTurn
├── UntilCasterDies
├── UntilSourceLeavesPlay
├── UntilUsed
└── Permanent
```

That gives us possibilities like:

**Summoned Familiar**

> Lasts until its summoning Wizard dies.

**Illusionary Warrior**

> Lasts until it receives damage.

**Phoenix**

> Lasts 3 turns, then transforms into Phoenix Egg.

**Animated Weapon**

> Lasts until the spell that animated it ends.

And the same underlying mechanism handles the temporary magical equipment you proposed:

> Mage Armor — 3 turns  
> Arcane Blade — 3 turns  
> Fire Elemental — 3 turns  
> Arcane Shield — until end of turn

So I'd now explicitly add **Lifetime/Expiration** to our generic ontology. `Duration` can be one way of defining a lifetime, rather than something unique to Wizard cards.

This also makes the Wizard deck's identity clearer: **it can generate enormous temporary power, but maintaining board presence is difficult**. The Town deck spends time building things that remain; the Wizard deck spends Mana creating powerful things that may literally disappear several turns later.

===== USER =====
And what cards does the wizard deck have to gain mana quickly? Maybe a card that gives mana and spreads it over the units

===== ASSISTANT =====
Yes. Since **mana normally lives on individual Casters**, fast mana should be powerful partly because it bypasses the slow *generate → deposit → withdraw* process.

I'd give the Wizard deck several different acceleration cards rather than one obvious best card.

| Card | Cost | Effect |
|---|---:|---|
| **Mana Burst** | 0 Gold | Generate **3 Mana** and distribute it as you choose among Casters you control. |
| **Arcane Rain** | 1 Gold | Every Caster you control gains **1 Mana**. |
| **Nexus Surge** | 1 Gold | Arcane Nexus gains **4 Mana**. |
| **Mana Convergence** | 2 Mana | Redistribute all Mana among your Casters and HQ however you choose, without tapping them. |
| **Overcharge** | 0 | Target Caster gains **3 Mana**; at end of turn it takes 2 damage. |

I especially like the first two.

### Mana Burst
**Spell — Arcane**  
**Deck limit: 4**

> Generate **3 Mana. Distribute those Mana among any number of friendly Casters.**

So:

```text
Archmage       2/6
Apprentice     0/3
Conjurer       2/5

Play Mana Burst

Choose:
Archmage       +1
Apprentice     +1
Conjurer       +1
```

or:

```text
Conjurer +3
```

provided its Mana Capacity allows it.

That's much better than simply "gain 3 Mana", because **where the Mana goes matters** in this deck.

### Arcane Rain
This one could reward building a board full of Wizards.

> **Each friendly Caster gains 1 Mana.**

With only the Archmage:

> +1 Mana

With Archmage + three Casters:

> **+4 Mana**

So it's weak early and potentially extremely efficient later. It synergizes naturally with the deck rather than just being a generic resource card.

### I'd also add a Mana Potion

This would interact with equipment/items:

**Mana Potion — Consumable**  
**1 Gold**

> Choose a Caster. It gains **2 Mana**. Destroy Mana Potion.

That gives the deck reliable targeted acceleration.

And we can have a riskier card:

### Forbidden Knowledge
**Spell**

> Target Caster gains **4 Mana**.  
> It takes **2 damage**.

That's especially interesting because Armor shouldn't reduce this if we define it as direct/self-inflicted damage rather than an attack.

### The most Wizard-like one: Mana Communion

I'd definitely include something along these lines:

**Mana Communion — Spell**  
**2 Mana**

> Redistribute any amount of Mana among your Casters and Arcane Nexus.

Normally moving Mana requires tapping the Caster involved. Communion bypasses that.

Imagine:

```text
Nexus          5
Archmage       0
Pyromancer     3
Conjurer       0
```

Opponent attacks.

Normally you can't suddenly move Nexus Mana onto the Archmage without the appropriate transfer action.

But with Mana Communion as an Instant:

```text
Nexus          5 → 2
Archmage       0 → 3
```

Archmage can now cast a **3-Mana Counterspell**.

That makes the Wizard player dangerous even when their Mana appears to be in the "wrong" place.

So the Wizard deck gets several forms of Mana manipulation:

**Generation** — create new Mana.  
**Distribution** — generate Mana directly on particular Casters.  
**Transfer** — move existing Mana through the Nexus.  
**Redistribution** — reorganize existing Mana without normal tap costs.  
**Drain** — take Mana from enemies.  
**Overcharge** — gain Mana at some other cost.

That's a much more interesting resource system than treating Mana as simply another version of Gold.

===== USER =====
Overcharge needs to give 4 mana

===== ASSISTANT =====
Agreed. **Overcharge** should be the high-risk, high-output Mana accelerator:

### Overcharge
**Spell — Arcane**  
**Cost: 0**  
**Deck limit: 4**

Choose a Caster you control. It gains **4 Mana**, up to its Mana Capacity.

**At the end of your turn:** That Caster takes **2 damage**.

So compared with the other acceleration:

- **Mana Burst:** +3 Mana distributed freely.
- **Arcane Rain:** +1 Mana to every friendly Caster.
- **Mana Potion:** +2 Mana to one Caster.
- **Overcharge:** **+4 Mana to one Caster, but 2 damage.**

That makes Overcharge particularly good for suddenly powering an expensive summon or spell, while creating a real risk of killing a fragile Apprentice or other Caster.

===== USER =====
Now come up with the whole caster deck

===== ASSISTANT =====
Yes. I'd make the first **Arcane Conclave** deck 40 cards plus its starting HQ and Hero. Unlike the Town deck, it has **no Housing, Training or Construction system**. Its complexity is entirely about **where Mana is located, how it moves, and when you spend it**.

## Starting cards

### Arcane Nexus — Headquarters
**8 HP | 2 Armor | Mana 3/8**

**Channel — Tap:** Gain **2 Mana**.

**Mana Exchange:** Tap a Caster you control → transfer any amount of Mana between that Caster and the Arcane Nexus, respecting Mana Capacity.

The Nexus is therefore simultaneously your Mana bank and Mana router.

### Archmage — Starting Hero
**3 ATK | 5 HP | 1 Armor | Mana 3/6**

**Meditate — Tap:** Gain **2 Mana**.

**Summon Familiar — 2 Mana, Tap:** Summon an **Arcane Familiar**.

The Archmage has no AP. Its abilities consume its personal Mana.

---

# Arcane Conclave — 40 cards

| Qty | Card | Type | Cost |
|---:|---|---|---|
| 4 | Apprentice Mage | Caster | 2 Gold |
| 3 | Mana Seer | Caster | 3 Gold |
| 2 | Pyromancer | Caster | 3 Gold |
| 2 | Conjurer | Caster | 4 Gold |
| 2 | Mana Leech | Caster | 3 Gold |
| 1 | Storm Mage | Caster | 4 Gold |
| 1 | Grand Summoner | Hero | 5 Gold + 4 Nexus Mana |
| 4 | Mana Burst | Spell | 0 |
| 3 | Arcane Rain | Spell | 1 Gold |
| 3 | Overcharge | Spell | 0 |
| 2 | Mana Communion | Instant | 2 Mana |
| 2 | Fireball | Spell | 3 Mana |
| 2 | Counterspell | Instant | 3 Mana |
| 2 | Arcane Shield | Instant | 2 Mana |
| 1 | Mage Armor | Magic Armor | 3 Mana |
| 1 | Arcane Blade | Magic Weapon | 2 Mana |
| 1 | Staff of Power | Magic Weapon | 3 Mana |
| 1 | Mana Potion | Consumable | 1 Gold |
| **40** | | | |

Summoned tokens such as Familiars and Elementals exist outside the deck.

---

# Casters

### Apprentice Mage ×4
**2 ATK | 3 HP | 0 Armor | Mana 0/3**  
**Cost: 2 Gold**

**Study — Tap:** Gain 1 Mana.

**Arcane Spark — 2 Mana, Tap:** Deal 1 damage to target character.

This is your basic Mana worker.

---

### Mana Seer ×3
**1 ATK | 3 HP | 1 Armor | Mana 0/4**  
**Cost: 3 Gold**

**Attunement:** At the beginning of your turn, gain 1 Mana.

**Foresight — 2 Mana, Tap:** Look at the top 2 cards of your deck. Put one on top and one on the bottom.

Unlike the Apprentice, the Seer generates Mana without tapping.

---

### Pyromancer ×2
**3 ATK | 3 HP | 0 Armor | Mana 1/4**  
**Cost: 3 Gold**

**Feed the Flame:** The first time each turn one of your spells deals damage, Pyromancer gains 1 Mana.

**Flame Bolt — 2 Mana, Tap:** Deal 2 damage to a unit.

This creates the offensive Mana engine.

---

### Mana Leech ×2
**2 ATK | 4 HP | 1 Armor | Mana 0/3**  
**Cost: 3 Gold**

Whenever Mana Leech deals combat damage to a Caster:

> Transfer 1 Mana from that Caster to Mana Leech.

So against another Mana deck, it's particularly dangerous.

---

### Conjurer ×2
**2 ATK | 4 HP | 1 Armor | Mana 0/5**  
**Cost: 4 Gold**

**Summon Fire Elemental — 4 Mana, Tap**

Create:

**Fire Elemental**  
5 ATK | 4 HP | 1 Armor  
**Duration: 3 turns**

This is where the deck converts stored Mana into temporary board power.

---

### Storm Mage ×1
**3 ATK | 4 HP | 1 Armor | Mana 1/5**  
**Cost: 4 Gold**

**Stormcharge:** The first time you cast an Instant during another player's turn, Storm Mage gains 1 Mana.

**Lightning — 3 Mana, Tap:** Deal 3 damage to a character.

This pushes the player toward keeping Mana available during the opponent's turn.

---

# Second Hero

### Grand Summoner ×1
**Hero — Wizard/Caster**  
**3 ATK | 6 HP | 1 Armor | Mana 2/8**  
**Cost: 5 Gold + 4 Mana from Arcane Nexus**

This can only be played while you have no active Hero.

**Greater Summoning — 5 Mana, Tap:** Summon an **Arcane Guardian**.

**Mana Bond — Passive:** Summoned units you control gain +1 Duration when summoned.

**Arcane Guardian**

> 4 ATK | 7 HP | 2 Armor  
> Duration 2

With Grand Summoner:

> Duration **3**

This Hero therefore shifts the deck heavily toward summoned creatures.

---

# Mana acceleration

### Mana Burst ×4
**Spell — Cost 0**

Generate **3 Mana** and distribute it however you choose among friendly Casters.

It cannot exceed their individual Mana capacities.

---

### Arcane Rain ×3
**Spell — 1 Gold**

Every friendly Caster gains **1 Mana**.

This rewards having many Casters.

---

### Overcharge ×3
**Spell — Cost 0**

Target friendly Caster gains **4 Mana**.

At the end of your turn, that Caster takes **2 damage**.

This is the deck's explosive acceleration.

---

### Mana Communion ×2
**Instant — 2 Mana**

Redistribute any amount of Mana among your Casters and Arcane Nexus without tapping them.

This can be played in response to something.

For example:

> Opponent casts Fireball → Mana Communion → move 3 Mana to Archmage → Counterspell.

---

# Offensive and defensive magic

### Fireball ×2
**Spell — 3 Mana**

Deal **4 damage** to target character or Building.

---

### Counterspell ×2
**Instant — 3 Mana**

Cancel target Spell being cast.

This goes directly into the reaction/stack system we designed earlier.

---

### Arcane Shield ×2
**Instant — 2 Mana**

Target friendly character gets **+2 Armor until end of turn**.

Because Armor subtracts from every incoming attack, this can be extremely powerful at the right moment.

---

# Magical equipment

These are deliberately temporary. The Wizard deck can create powerful equipment cheaply, but unlike the Town deck's physical equipment, the magic eventually dissipates.

### Mage Armor ×1
**Magic Equipment — Body**  
**3 Mana**

Equipped character gets:

> **+2 Armor**

**Duration: 3 turns**

---

### Arcane Blade ×1
**Magic Equipment — One Hand**  
**2 Mana**

> **+2 Attack**

**Duration: 3 turns**

---

### Staff of Power ×1
**Magic Equipment — Two Hands**  
**3 Mana**

Equipped Caster gets:

> **Mana Capacity +2**

Whenever that Caster generates Mana through one of its own abilities:

> Generate **+1 additional Mana**.

**Duration: 4 turns.**

---

### Mana Potion ×1
**Consumable — 1 Gold**

Target Caster gains **2 Mana**.

Discard Mana Potion.

---

# Summoned units

These aren't part of the 40-card deck.

### Arcane Familiar
**2 ATK | 2 HP | 0 Armor | Mana 0/2**

When summoned, its summoner may transfer 1 Mana to it.

**Return Mana — Tap, sacrifice Familiar:** Transfer all Mana on Familiar to its summoner.

---

### Fire Elemental
**5 ATK | 4 HP | 1 Armor**

**Duration: 3**

---

### Arcane Guardian
**4 ATK | 7 HP | 2 Armor**

**Duration: 2**

The Grand Summoner makes that Duration 3.

---

## Hero resurrection: Arcane Reconstitution

I'd put this on the **Arcane Nexus itself**, rather than requiring a card draw.

When your Hero dies, place its **Arcane Essence** on your Nexus.

**Reconstitute:** During your turn, you may move Mana from the Nexus onto that Essence.

When it reaches **6 Mana**, remove those 6 Mana and return the Hero at full HP but **0 Mana**.

Importantly, this can happen over multiple turns:

```text
Archmage dies
      ↓
Arcane Essence 0/6

Turn 1 → invest 2 → 2/6
Turn 2 → invest 1 → 3/6
Turn 3 → invest 3 → 6/6
      ↓
Archmage returns
```

That creates an enormous economic decision. The six Mana being used to rebuild your Hero isn't being used for Fireballs, Counterspells or Elementals.

---

## How the deck actually plays

The interesting thing is that **Mana placement is almost a game within the game**.

Imagine:

```text
Arcane Nexus       6/8
Archmage           2/6
Apprentice         3/3
Pyromancer         1/4
Conjurer           0/5
```

You have **12 Mana in total**, but that's misleading.

The Conjurer can't summon its Elemental because its Mana is in the wrong places.

You could tap it and withdraw 4 from the Nexus:

```text
Conjurer → 4/5
```

but now it's tapped and can't summon.

So perhaps this turn you load the Conjurer, planning to summon next turn.

But the opponent sees that:

> Conjurer has 4 Mana.

They know what's coming and may kill it, causing all four Mana to disappear.

Alternatively you keep Mana safely in the Nexus and use **Mana Communion** at the critical moment.

That's the central skill of this deck.

The three decks now have genuinely different economic rhythms:

| Town | Raiders | Arcane Conclave |
|---|---|---|
| Gold | Gold | Gold + Mana |
| Peasants work | Combat earns/steals wealth | Casters generate Mana |
| Buildings take time | Immediate/aggressive | Mana needs positioning |
| Housing | No Housing | No Housing |
| Training | Glory/promotion | Mana Capacity |
| Permanent development | Momentum | Temporary magical power |
| Physical equipment | Looted equipment | Temporary magic equipment |
| Shrine resurrection | Glory promotion | Mana reconstitution |

The Wizard deck therefore isn't simply "the Town deck with Mana instead of Training." Its defining resource exists **on the individual pieces on the board**, can be moved around, can be lost when a Caster dies, and determines what reactions are available. That should make it play very differently.

===== USER =====
Now I want another deck that is based on yet comoletely new mechanics

===== ASSISTANT =====
I'd go much further with the fourth deck and make it operate on a mechanic none of the previous decks really use: **the Swarm**. No Housing, Training, Mana, construction, or Glory. Its economy is based on **biomass, consuming its own units, reproduction, and evolution**.

Call it **The Brood**.

### Starting HQ — The Hive

**The Hive — Headquarters**  
10 HP | 1 Armor

The Hive doesn't generate Gold.

**Consume — Tap:** Sacrifice one friendly Organic unit. Put Biomass counters on the Hive equal to that unit's Biomass Value.

**Spawn — Remove 1 Biomass:** Summon a Larva.

Biomass isn't a player resource. It's physically stored on Hive-type entities, similar to Mana being stored on Casters.

### Starting Hero — Broodmother

**Broodmother — Hero / Brood**  
3 ATK | 8 HP | 1 Armor

**Lay Eggs — Tap:** Create 2 Egg tokens.

**Devour — Sacrifice a friendly unit:** Heal Broodmother equal to that unit's Biomass Value.

No AP. No Gold ability. Her abilities revolve around creatures.

---

## Eggs create delayed production

**Egg**

0 ATK | 2 HP | 0 Armor

Cannot attack.

At the beginning of your next turn:

> Hatch → replace this Egg with a Larva.

So unlike the Town deck buying Peasants:

```text
Town:
Gold → Peasant

Brood:
Broodmother → Egg → wait → Larva
```

The opponent can destroy Eggs before they hatch.

---

## Larvae are the basic resource

**Larva**

1 ATK | 1 HP | 0 Armor  
Biomass Value: 1

Larvae are simultaneously:

- weak combat units;
- food for the Broodmother;
- Biomass for the Hive;
- raw material for evolution.

That's the deck's fundamental decision.

A Larva isn't merely a creature. It's a **living resource**.

---

# Evolution

Instead of playing stronger creatures normally, many Brood cards **evolve existing creatures**.

### Ravager
**Evolution Cost: Larva + 1 Biomass**

Replace target Larva with Ravager.

**Ravager**  
4 ATK | 3 HP | 1 Armor  
Biomass Value: 2

So:

```text
Larva
  +
1 Biomass
  ↓
Ravager
```

The card from your hand becomes the new entity.

---

### Carapace Drone
**Evolution Cost: Larva + 2 Biomass**

2 ATK | 5 HP | **3 Armor**  
Biomass Value: 2

**Protector:** Your Eggs cannot be attacked while you control an untapped Carapace Drone.

---

### Spitter
**Evolution Cost: Larva + 1 Biomass**

3 ATK | 3 HP | 0 Armor

**Ranged**

Whenever Spitter damages a unit:

> Put a **Poison counter** on it.

---

# Poison introduces another mechanic

Poison doesn't deal conventional damage immediately.

At the end of the poisoned unit's controller's turn:

> It takes 1 damage for each Poison counter, then remove one Poison counter.

So:

```text
Knight
5 HP
Poison ●●●

End turn:
-3 HP
Poison ●●

Next:
-2 HP
Poison ●

Next:
-1 HP
```

Armor could perhaps **not reduce Poison damage**, because it's not an attack.

Now the Brood has a way around heavily armored Town units.

---

# Mutation

Here's where I'd make this deck especially different.

Some cards aren't Equipment. They're **permanent biological mutations** attached to creatures.

### Hardened Carapace
**Mutation**

Attach to an Organic unit.

> +1 Armor  
> Biomass Value +1

Maximum one `Carapace` mutation per unit.

---

### Venom Glands
**Mutation**

Whenever this creature deals combat damage:

> Add 1 Poison.

---

### Additional Limbs
**Mutation**

> +1 Attack.

If the creature already has Additional Limbs:

> +2 Attack instead.

Unlike Wizard magical equipment, Mutations **don't expire**.

And unlike Town equipment, they generally **can't be unequipped and transferred**.

If the creature dies, the mutation dies with it.

---

# Metamorphosis

We can then have multi-stage creatures.

### Cocoon
A card might say:

**Metamorphosis — 3 Biomass:** Replace a Ravager with a Cocoon.

**Cocoon**  
0 ATK | 6 HP | 2 Armor  
Duration: 2 turns

When its Duration reaches zero:

> Transform into Brood Behemoth.

### Brood Behemoth

7 ATK | 9 HP | 3 Armor  
Biomass Value: 5

So the opponent sees:

```text
Ravager
   ↓
COCOON [2]
   ↓
COCOON [1]
   ↓
BROOD BEHEMOTH
```

and has two turns to destroy the Cocoon.

That's effectively this deck's equivalent of "construction", but it uses an entirely different mechanism.

---

# Death is productive

I'd then make death itself feed the deck.

### Corpse Harvester

3 ATK | 4 HP | 1 Armor

Whenever another Organic unit you control dies:

> Put 1 Biomass on the Hive.

Now sacrificing creatures becomes much more efficient.

And:

### Cannibal Drone

4 ATK | 4 HP | 1 Armor

**Devour — Tap:** Sacrifice another friendly Organic unit. Cannibal Drone gets **+X Attack this turn**, where X is that unit's Biomass Value.

Your own army becomes ammunition.

---

# Swarm mechanics

Some cards should reward sheer quantity.

### Hive Mind
**Ongoing Effect**

If you control:

**3+ Brood units:** They get +1 Attack when attacking.

**6+ Brood units:** They additionally get +1 Armor.

**10+ Brood units:** Your Larvae get +1 Attack permanently while Hive Mind remains.

Now going extremely wide becomes viable.

---

# Splitting creatures

Another mechanic:

### Bloated Spawn

3 ATK | 3 HP | 0 Armor

When Bloated Spawn dies:

> Summon **2 Larvae**.

So killing it doesn't necessarily reduce the Brood's board presence.

And:

### Brood Carrier

2 ATK | 6 HP | 1 Armor

When this takes damage and survives:

> Once per turn, create an Egg.

Attacking it can literally make the swarm larger.

---

# Hero replacement should again be completely different

The Broodmother doesn't resurrect.

Instead, she has laid potential successors.

### Royal Egg

When the Broodmother dies:

> You may play Royal Egg from your hand.

**Royal Egg**  
0 ATK | 5 HP | 1 Armor

Requires **5 Biomass** to mature.

Biomass can be added over multiple turns.

At 5:

> Replace Royal Egg with a Brood Hero from your hand.

For example:

### Hive Queen — Hero

4 ATK | 10 HP | 2 Armor

**Spawn — Tap:** Create 3 Larvae.

**Consume Brood — Sacrifice any number of friendly Brood units:** Hive Queen gets +1 Armor until your next turn for each unit sacrificed.

So the succession cycle is:

```text
BROODMOTHER DIES
       ↓
   Royal Egg
       ↓
Feed it Biomass
       ↓
     1/5
     2/5
     3/5
     4/5
     5/5
       ↓
   HIVE QUEEN
```

Again, the opponent has an opportunity to destroy the successor before it matures.

---

## What makes the four decks fundamentally different

We now have four different resource/tempo architectures:

| Town | Raiders | Wizards | Brood |
|---|---|---|---|
| Build economy | Attack economy | Mana network | Biological economy |
| Gold Mines | Steal Gold | Generate Mana | Generate organisms |
| Workers | Raiders | Casters | Larvae |
| Construction | Pillaging | Mana transfer | Evolution |
| Housing | None | Mana Capacity | Biomass |
| Training | Glory | Summoning | Mutation |
| Permanent buildings | Combat momentum | Temporary magic | Reproduction |
| Equipment | Loot/equipment | Temporary enchantment | Permanent mutation |
| Call to Arms | Promotion | Reconstitution | Metamorphosis |
| Preserve units | Kill enemies | Manage Mana | **Sacrifice own units** |

The particularly interesting inversion is:

> **Town wants its units to survive. Brood sometimes wants its own units to die.**

A Peasant dying means lost economic capacity.

A Bloated Spawn dying might be exactly what the Brood player wanted because it produces two Larvae, triggers the Corpse Harvester, generates Biomass, and potentially triggers other death effects.

That would be an excellent fourth test deck for the generic engine because it forces the ontology to support **tokens, sacrifice costs, transformations, attached permanent mutations, poison counters, death triggers, evolution/replacement, delayed metamorphosis, and resources stored on specific entities**—without introducing special Brood-specific logic into the engine.

===== USER =====
Now another one: the undead. Their hq is a graveyard. Any ideas

===== ASSISTANT =====
Yes. I'd make the Undead deck's defining concept **death as accumulation**. Other decks lose resources when units die; the Undead player is building a resource pool in the Graveyard.

Crucially, I would *not* use Gold as its primary economy. The **Graveyard itself is the economy, hand extension, and production engine**.

## Starting HQ — Graveyard

**Graveyard — Headquarters**  
**10 HP | 1 Armor**

Whenever a non-summoned Unit dies, put that card **under the Graveyard as a Corpse** instead of its normal discard destination.

**Raise Dead — Tap:** Exile a Corpse from your Graveyard → summon a **Skeleton**.

So death gradually fills your HQ:

```text
GRAVEYARD
────────────
Corpses: 4

Knight
Peasant
Mercenary
Archer
```

Those aren't merely counters. **Which corpses are there can matter.**

That gives us lots of design space.

### Starting Hero — Necromancer

**Necromancer — Hero / Caster**  
**2 ATK | 5 HP | 1 Armor**

I'd give him **Death Power** rather than Mana.

**Harvest Soul:** Whenever another non-token Unit dies, Necromancer gains 1 Death Power. Maximum perhaps 5.

**Raise Corpse — 2 Death Power, Tap:** Choose a Corpse in your Graveyard. Summon a Zombie based on it.

That gives us two different uses of corpses:

> Graveyard consumes a corpse → cheap generic Skeleton.

> Necromancer uses a corpse → stronger Zombie.

## Skeletons

**Skeleton**  
**2 ATK | 2 HP | 1 Armor**  
No Housing.

When Skeleton dies:

> It does **not** create another Corpse.

Otherwise we'd create an infinite recycling loop.

Skeletons are disposable.

---

## Zombies could inherit their corpse

This is where I'd make the deck really distinctive.

Suppose the Town player's:

**Knight**  
5 ATK / 5 HP / 3 Armor

dies.

Its card becomes a Corpse under your Graveyard.

The Necromancer raises it:

**Zombie Knight**

Perhaps:

> Original Attack −1  
> Original HP  
> Original Armor −1  
> Undead

So:

```text
Knight Corpse
5 ATK / 5 HP / 3 Armor
       ↓
   NECROMANCY
       ↓
Zombie Knight
4 ATK / 5 HP / 2 Armor
```

Now the Undead deck partially **builds its army out of whatever everyone else brought to the game**.

Against Town, you get undead humans.

Against Raiders, undead Orcs.

Against Wizards, undead Casters.

Against Brood, horrifying undead creatures.

That gives the deck enormous replayability.

---

# Graveyard buildings

I'd introduce structures that interact with corpses rather than conventional economy.

### Ossuary

**Building**

Whenever a Skeleton you control dies:

> Put 1 **Bone counter** on Ossuary.

**Remove 3 Bone:** Summon a Skeleton.

Now even destroyed Skeletons contribute something without becoming Corpses themselves.

### Crypt

**Building**

Store up to 3 Corpses here.

Corpses in the Crypt **cannot be exiled or stolen by opponents**.

That immediately implies future anti-Graveyard cards.

### Charnel House

Whenever a non-Undead Unit dies:

> Heal your Graveyard 1 HP.

Now killing things literally strengthens your HQ.

---

# Corpse quality

I'd actually give every eligible Unit:

```text
CorpseValue
```

Usually based on its power.

For example:

| Unit | Corpse Value |
|---|---:|
| Peasant | 1 |
| Soldier | 2 |
| Knight | 4 |
| Hero | perhaps special |
| Skeleton | 0 |
| Summoned Elemental | 0 |

Then certain effects can consume **Corpse Value** rather than number of cards.

### Corpse Explosion

**Spell**

Exile a Corpse.

Deal damage equal to its Corpse Value to target Unit.

Suddenly the enemy's dead Knight isn't just something you can resurrect. You can **detonate the corpse** for 4 damage.

---

# Abominations

The next mechanic should be combining corpses.

### Flesh Golem

Instead of paying Gold:

> **Exile Corpses with combined Corpse Value ≥5.**

Summon:

**Flesh Golem**  
6 ATK | 8 HP | 2 Armor

So:

```text
Peasant corpse     1
Soldier corpse     2
Archer corpse      2
                    ─
                    5

         ↓ STITCH TOGETHER ↓

          FLESH GOLEM
```

Or simply consume one extremely valuable corpse.

This gives you a strategic choice over every corpse:

> Skeleton?

> Zombie?

> Corpse Explosion?

> Save it for Flesh Golem?

The Graveyard becomes almost a **second hand of cards**.

---

# Death effects

Naturally, Undead should care heavily about dying.

### Plague Zombie

3 ATK | 4 HP | 0 Armor

When destroyed:

> Put a **Plague counter** on the Unit that destroyed it.

At the end of that player's next turn:

> Take 2 damage.

### Bone Collector

2 ATK | 4 HP | 1 Armor

Whenever another Undead dies:

> Gain 1 Bone.

### Restless Dead

2 ATK | 2 HP | 0 Armor

When destroyed:

> 50% chance isn't suitable for deterministic play unless we want randomness.

Better:

> Return Restless Dead to play at the beginning of your next turn unless the opponent pays 1 Gold to exile it.

Now the opponent must actively ensure it stays dead.

---

# Hero recovery

This deck absolutely should **not** simply resurrect its Necromancer normally.

I'd make death transform the Hero.

When the Necromancer dies, you may eventually play:

### Lich — Hero

But becoming a Lich requires consuming souls.

**Lich — Hero**  
**Ascension Cost: 6 Death Power + exile 3 Corpses**

The dead Necromancer becomes the basis of the transformation:

```text
NECROMANCER
     ↓ dies
Necromancer Corpse
     ↓
+ 6 Death Power
+ 3 Corpses
     ↓
    LICH
```

### Lich
**4 ATK | 7 HP | 2 Armor**

**Soul Drain:** Whenever another Unit dies, heal 1 HP.

**Mass Resurrection — 5 Death Power, Tap:** Exile up to three Corpses → summon a Skeleton for each.

And perhaps the Lich introduces a **Phylactery** mechanic later, giving it an additional resurrection route.

---

## The deck's central loop

The whole faction becomes:

```text
                 THINGS DIE
                     ↓
            ┌──── GRAVEYARD ────┐
            │         │          │
            ▼         ▼          ▼
         Corpses    Souls       Bones
            │
      ┌─────┼─────────┐
      ▼     ▼         ▼
 Skeleton Zombie  Explosion
      │
      ▼
 Skeleton dies
      │
     Bones
      │
      ▼
 more Skeletons

CORPSES + CORPSES
       ↓
   ABOMINATIONS
```

And this creates another excellent asymmetry.

The Town player sees:

> "My Knight died."

Bad.

The Raider sees:

> "An enemy died."

Potential Glory/Gold.

The Wizard sees:

> "A unit died."

Maybe relevant to a Death Mage.

The Brood sees:

> "One of my creatures died."

Potential Biomass/reproduction.

The Undead player sees **everyone's deaths as inventory**.

That's what I would make the defining identity of the deck: **the longer and bloodier the game becomes, the more resources accumulate in the Graveyard.** A massive battle that wipes both armies can actually leave the Undead player in the strongest position, because the battlefield may be empty while their HQ is now sitting on ten Corpses.

===== USER =====
But in multiway this deck may be too overpowered. And, how does the deck gets its initial corpses? Maybe the graveyard can be tapped to generate corpses

===== ASSISTANT =====
Yes. Both issues can be solved together by making **Corpses a controlled resource**, rather than automatically giving the Undead every unit that dies anywhere.

In a 4-player game, "whenever any unit dies → your Graveyard gets it" would scale far too strongly with player count.

I'd change the Graveyard to this:

### Graveyard — Headquarters
**10 HP | 1 Armor**  
**Corpse capacity: perhaps 8**

**Dig Up the Dead — Tap:** Add **1 Corpse** to your Graveyard.

That solves the starting problem immediately. Even with no combat, the Necromancer can slowly build a corpse supply.

But enemy deaths should still matter because that's thematically central. I'd distinguish **generic Corpses** from actual dead cards.

### Two ways to obtain corpses

**1. Generic Corpse**

The Graveyard generates these:

> Tap Graveyard → create 1 Corpse token.

A Corpse token could have:

> Corpse Value: 1

Reliable, but slow.

**2. Claimed Corpse**

When an actual unit dies, the Undead player shouldn't automatically receive it. Instead, the Necromancer or another card must explicitly **claim** it.

For example:

### Necromancer

**Harvest Corpse — 1 Death Power:** When a non-token unit dies, claim its Corpse.

Or perhaps:

> **Harvest Corpse — Tap:** The next eligible unit that dies this turn becomes a Corpse in your Graveyard.

Now there's an opportunity cost. You can't passively vacuum up every death occurring between three opponents.

### Even cleaner: once per round

We could have a Graveyard rule:

> **The first time each round an enemy non-token Unit dies, you may create 1 Corpse.**

That scales much better:

```text id="8mijrg"
2-player game → max 1 passive corpse/round
4-player game → still max 1 passive corpse/round
8-player game → still max 1 passive corpse/round
```

rather than:

```text id="jpbz5q"
More opponents
→ more units
→ more deaths
→ exponentially better Undead deck
```

But I think active corpse harvesting is even more interesting because it creates decisions.

## I'd make the starting Graveyard like this

**Graveyard — Headquarters**  
**10 HP | 1 Armor**  
**Corpse Capacity: 8**

**Exhume — Tap:** Create **1 Corpse token** in this Graveyard.

**Inter — Reaction, once per round:** When an enemy non-token Unit dies, you may put that card into your Graveyard as a Corpse instead of its normal discard pile.

Now it has a reliable baseline economy:

> Graveyard → Corpse every turn.

And a limited way of stealing particularly valuable corpses:

> One actual dead enemy per round.

That introduces a very interesting choice in multiplayer.

Suppose during the round:

> Peasant dies.

You could claim it.

But perhaps you don't.

Then later:

> Knight dies.

Now:

> **That's the corpse I want.**

So the Undead player has to decide when to use their once-per-round Inter.

### Corpse capacity matters too

I'd definitely introduce a maximum Graveyard capacity.

Say:

> **8 Corpses**

When full, you can't add another unless you first exile/discard one.

That prevents the Undead player from simply accumulating 25 corpses in a long multiplayer match.

Other cards could expand it:

**Crypt**

> Graveyard Corpse Capacity +4.

Now even the Undead deck has an infrastructure progression, but it's based on **storage of death**, not Housing or Mana.

And this produces a nice opening:

```text id="8q8c10"
START

Graveyard: 0/8 Corpses
Necromancer

Turn 1:
Graveyard Tap
→ Corpse 1/8

Turn 2:
Graveyard Tap
→ Corpse 2/8

Necromancer now has enough
material to do something useful.
```

So the Undead deck doesn't require an opponent to cooperate by dying before its engine works.

The multiplayer balancing principle I'd use throughout the whole game is broader:

> **Effects shouldn't generally scale automatically with the number of opponents unless that's deliberately priced into the card.**

So "whenever an enemy does X" is dangerous in multiplayer. `Once per round`, `first time each round`, explicit tapping, resource costs, or choosing one opponent are useful ways for our engine to keep cards functional from 1v1 through larger multiplayer games.

===== USER =====
So if I want yet a completely different deck what should i now add?

===== ASSISTANT =====
At this point, I would deliberately avoid another deck whose core loop is simply "generate a different resource → spend it on units." You already have several economic architectures.

The biggest unexplored design space is **information, positioning, time, and rule manipulation**.

A particularly different fifth/sixth faction could be a **Kingdom of Spies / Assassins** deck based on **Secrets and Infiltration**.

### The Shadow Guild

Its HQ could be a **Thieves' Guild**. Its Hero is the **Spymaster**.

The central mechanic is **Infiltration**. Your units don't primarily want to defeat enemy units in combat. They want to infiltrate enemy Heroes, buildings and HQs.

For example:

**Spy**  
1 ATK | 2 HP | 0 Armor

**Infiltrate — Tap:** Put this Spy face-down underneath an enemy Building.

The Spy is now *inside* that Building rather than on your battlefield.

While infiltrated, it might accumulate **Intel**:

> At the beginning of your turn, an infiltrated Spy gains 1 Intel.

Then you can spend that Intel on effects associated with whatever was infiltrated.

For example:

```text
Spy infiltrates Barracks
        ↓
      Intel 1
        ↓
      Intel 2
        ↓
SABOTAGE
Destroy Barracks
```

Or:

> **1 Intel:** Look at target player's hand.

> **2 Intel:** Steal 2 Gold.

> **3 Intel:** Disable the infiltrated Building for a turn.

> **4 Intel:** Destroy it.

Suddenly the deck isn't trying to win conventional battles.

### Secrets

I'd combine this with face-down cards.

**Secret** cards are played face-down, and the opponent doesn't know what they are.

For example:

**Ambush — Secret**

Trigger:

> When an enemy attacks your Hero.

Reveal:

> Deal 3 damage to the attacker.

Another:

**Inside Man — Secret**

Trigger:

> When an opponent activates a Building.

Reveal:

> Cancel the ability and gain 1 Intel.

Another:

**False Orders**

> When an enemy Unit attacks, change its attack target to another legal target.

Now the opponent has to reason about **unknown information**.

That's a mechanic none of our existing decks really exploit.

---

But there are several other radically different directions worth considering:

| Deck concept | Core mechanic | What makes it fundamentally different |
|---|---|---|
| **Shadow Guild** | Infiltration + Secrets | Hidden information |
| **Time Mages** | Future/past | Schedule cards several turns ahead |
| **Machine Empire** | Assembly | Combine cards into larger entities |
| **Nomads** | Movement/locations | Board positioning becomes central |
| **Diplomats** | Deals/influence | Manipulates other players, especially multiplayer |
| **Shapeshifters** | Transformation | Same cards switch between forms |
| **Hive Mind** | Shared stats | Units form networks rather than act independently |
| **Chaos** | Rule mutation | Temporarily changes fundamental game rules |

Of these, I think **Shadow Guild** adds the most to the game right now because it forces our generic engine to support something we haven't needed much yet: **private state**.

Until now, nearly everything has been public:

```text
HP
Gold
Mana
Corpses
Housing
Glory
Equipment
Construction progress
Duration
```

Secrets introduce:

```text
Owner knows card
Opponent knows "something is there"
Opponent does NOT know what
        ↓
trigger occurs
        ↓
reveal
        ↓
reaction/effect
```

And infiltration introduces another new relationship:

```text
Spy
  ↓ attached/infiltrated
Enemy Building
  ↓
accumulates Intel
  ↓
uses abilities against host
```

That would be extremely valuable for testing the generic engine.

After that, I'd make **Machines**. Machines could introduce a completely different ontology challenge: **multiple cards physically combining into one game entity**—chassis + weapon + engine + pilot, for example. That would push the engine in another genuinely new direction rather than just introducing another resource type.

===== USER =====
What about a deck that consists of the hero only? For instance a hero robot and a landing pad

===== ASSISTANT =====
Yes. That could be one of the most radically different decks: **no normal units at all**. You start with one Robot Hero and a Landing Pad, and essentially spend the whole game upgrading one increasingly dangerous machine.

It would play almost like a **boss character** against another player's civilization.

## Starting HQ — Landing Pad

**Landing Pad**  
**10 HP | 2 Armor**

Instead of Housing or unit production, it provides **Energy and Repair**.

**Recharge — Tap:** Add 2 Energy to the Landing Pad.

**Dock — Tap Hero:** Transfer any amount of Energy between the Hero and Landing Pad.

**Repair Bay — 2 Energy, Tap Landing Pad + Hero:** Heal Robot Hero for 2 HP.

The Landing Pad becomes the Robot's home base.

---

# Starting Hero — AX-01

**AX-01 — Robot Hero**  
**5 ATK | 10 HP | 3 Armor**  
**Energy Capacity: 5**

Starts with perhaps **3 Energy**.

Unlike other decks:

> **You cannot control Units.**

The Robot itself is the entire army.

But it has far more equipment capacity than a normal character.

### Robot slots

```text
Head          ×1
Core          ×1
Left Arm      ×1
Right Arm     ×1
Torso         ×1
Legs          ×1
Utility       ×2
```

So instead of:

> play Soldier → play Archer → play Knight

your progression is:

> AX-01 → weapons → armor → reactor → targeting system → propulsion → giant war machine.

---

## The deck is mostly Modules

For example:

### Plasma Cannon
**Arm Module**

+3 Attack.

**Fire — 2 Energy:** This attack gets +2 Attack.

---

### Missile Launcher
**Arm Module**

**3 Energy, Tap Hero:** Deal 4 damage to target Unit or Building.

Duration? None. It's physical equipment.

---

### Reinforced Plating
**Torso Module**

+2 Armor.

---

### Fusion Reactor
**Core Module**

Hero's Energy Capacity +4.

At the beginning of your turn:

> Hero gains 2 Energy.

Now the deck's progression starts accelerating.

---

### Repair Nanobots
**Utility Module**

**1 Energy, Tap:** Heal Hero 1 HP.

---

### Jump Jets
**Leg Module**

Perhaps:

> Once per turn, AX-01 may avoid retaliation after attacking.

Or whatever movement/combat mechanic we eventually settle on.

---

### Targeting Computer
**Head Module**

> +1 Attack.

And perhaps:

> Your ranged attacks ignore 1 Armor.

Now you're essentially constructing a build.

---

# The really interesting part: Modules can replace Modules

Suppose AX-01 currently has:

```text
Left Arm: Plasma Cannon
Right Arm: Shield Generator
Core: Basic Reactor
Torso: Reinforced Plating
Head: Targeting Computer
Legs: Jump Jets
Utility: Repair Nanobots
```

Then you draw:

**Heavy Fusion Cannon — Two Arms**

It requires:

> Left Arm + Right Arm

So you have to remove both existing Arm modules to install it.

Exactly like our Equipment slot system, but much more central to this deck.

---

# Upgrade cards

I'd also introduce **permanent Hero upgrades** that aren't equipment.

### Reinforced Skeleton

Hero gains:

> **Maximum HP +3**

Permanent.

### Expanded Energy Grid

> **Energy Capacity +2**

### Combat Learning

Whenever AX-01 destroys a Unit:

> Put an Experience counter on AX-01.

At 3 Experience:

> +1 base Attack.

Now the Hero itself evolves throughout the match.

---

# No second Hero

I would actually break another convention here.

This deck should have:

> **Only one Hero.**

If AX-01 dies, you don't summon a replacement Hero.

Instead, the Landing Pad can **reconstruct it**.

### Emergency Reconstruction

When AX-01 dies, put its wreckage on the Landing Pad.

The Landing Pad gets:

```text
Reconstruction: 0/6
```

At the beginning of each turn perhaps:

> Pay 2 Energy → +1 Reconstruction.

At 6:

> AX-01 returns.

But that may take far too long. Better could be modules contributing reconstruction.

Perhaps the deck contains **Repair Drones**—but those would technically be units, violating our concept.

Instead, cards could provide:

**Spare Parts**

> Add 2 Reconstruction to your destroyed Robot.

**Emergency Protocol**

> Add 1 Reconstruction and 3 Energy to Landing Pad.

This means the deck's resurrection mechanism is largely **card-driven**.

---

# But the loss condition gets fascinating

Normally:

> HQ dead + Hero dead = lose.

For this deck:

```text
AX-01 alive
Landing Pad destroyed
→ still alive

AX-01 destroyed
Landing Pad alive
→ can rebuild

AX-01 destroyed
Landing Pad destroyed
→ GAME OVER
```

That's perfect.

It creates a boss-fight dynamic for the opponent:

> Do I attack the enormous Robot?

or

> Do I somehow get past it and destroy the Landing Pad so it can't keep repairing/reconstructing?

---

# The deck could have no conventional Gold

I'd actually eliminate Gold entirely.

The Robot deck could run purely on **Energy**.

The Landing Pad and Reactor modules produce Energy. Modules themselves could have an **Installation Cost** measured in Energy.

For example:

> Plasma Cannon — Install: 3 Energy  
> Fusion Reactor — Install: 4 Energy  
> Reinforced Plating — Install: 2 Energy

That makes this deck completely economically independent from the others.

---

## Its 40-card deck could look roughly like this

| Category | Cards |
|---|---:|
| Arm weapons | 8 |
| Defensive modules | 6 |
| Reactor/Core modules | 5 |
| Head/Sensor modules | 4 |
| Leg/Mobility modules | 4 |
| Utility modules | 5 |
| Permanent upgrades | 4 |
| Emergency/Repair cards | 4 |
| **Total** | **40** |

Zero Units.

And that's particularly interesting for our engine because effects like:

> "Destroy target Unit"

are nearly useless against this deck.

While:

> "Destroy target Equipment"

becomes extremely valuable.

Likewise, the Undead player's Graveyard gets almost no corpses from the Robot deck.

That means this isn't merely mechanically different—it changes the **value of the opponent's cards depending on the matchup**, which is exactly what highly asymmetric deck design can produce.

The Robot deck effectively says: **you brought an army; I brought one machine.**

===== USER =====
Yeah, so I've built the system, and I tested with a couple of decks, and it works roughly good, but the game is totally unbalanced. I now have the robot deck, the human deck, but it didn't take housing into account, and the pillagers deck, the raiders deck. And actually the raiders, they are overpowered, which is funny because that's not what I had expected. But what happened, or actually the robot deck is even more overpowering, because the robot is so strong that it kills all units instantly.

===== ASSISTANT =====
That result actually exposes something important about the combat system. The problem is probably **not just individual card values**; the current rules structurally favor high-attack entities and early aggression.

With your armor rule, this becomes especially pronounced:

> **Damage = Attack − Armor**

If the Robot has, say, 5 ATK, then against the Human deck:

| Target | HP | Armor | Robot damage | Result |
|---|---:|---:|---:|---|
| Peasant | 3 | 1 | 4 | dies |
| Archer | 3 | 1 | 4 | dies |
| Soldier | 4 | 2 | 3 | survives barely |
| Town Guard | 5 | 2 | 3 | survives |

So a single Robot attack can erase cards that required Gold, Housing, Training, and possibly several turns of infrastructure to deploy. **The Human player pays enormous setup costs for units that the Robot trades against almost for free.**

And the missing Housing rule makes that harder to diagnose, because Humans aren't currently paying their intended population constraint either. I'd implement Housing before doing serious numerical balancing.

### The Robot needs a different weakness

I wouldn't primarily solve this by making the Robot weak. The fantasy of the deck is:

> **one extremely powerful machine versus an army.**

So it *should* kill a Peasant or Archer easily.

The balancing mechanism should instead be **action economy**.

If the Robot can attack only once per turn, then:

> Robot kills 1 Peasant → Human still has 5 others.

The Human deck should beat the Robot through **quantity and action advantage**, not by making individual Peasants capable of fighting it.

That means something like this is desirable:

```text
Robot
10 HP / 3 Armor / 5 Attack
1 attack per turn

versus

6 Peasants
18 combined HP
12 combined Attack
6 separate actions
```

The Robot has enormous *quality*. Humans have enormous *action economy*.

Equipment can then make the Robot choose its specialization. A Plasma Cannon might let it annihilate one target, while a weaker area weapon could damage several units.

### The Raider problem is probably different

Raiders have a potential **positive feedback loop**:

> attack → gain/steal Gold → buy stronger board → attack more → gain more Gold.

That's inherently dangerous because the thing they do to win the game is **also generating their economy**.

The Human player has to sacrifice tempo:

> Peasant taps to harvest → Peasant isn't attacking.

Raiders may currently have the opposite:

> Raider attacks → damages opponent **and** earns resources.

That's two benefits from the same action.

I'd preserve the mechanic but make successful raiding less automatic. For example, Gold should come from specific triggers such as:

> damage the enemy HQ → gain 1 Gold;

> destroy a Building with a Pillager → gain Gold;

> Loot → steal existing enemy Gold.

Rather than most Raider units simply producing money by fighting.

### We should establish a balancing currency

Now that you have a working engine, I'd stop balancing cards intuitively and create a rough **value model**.

For example, initially assign approximate values such as:

```text
1 Gold              = 1 value
1 Attack             = 1 value
1 HP                 = 0.5 value
1 Armor              = ~1.5 value
1 card drawn         = 2 value
1 Training           = 1 value
1 Housing occupied   = -0.5 value
Tap requirement      = negative value
Temporary duration   = negative value
```

Those numbers aren't supposed to be correct yet. Your simulations will **discover the correct coefficients**.

Then we can evaluate cards by something like:

> resources invested + setup time + card cost versus expected board impact.

This would immediately reveal something like:

**Peasant**

> 1 Gold + 1 Housing + card → 2/3/1 + Worker + Builder

versus:

**Robot**

> starting card → 5/10/3 + modules + no Housing

The Robot's enormous starting value is okay only if the Human player's **HQ + starting economy + ability to generate many cards/units** compensates for it.

### Your engine can balance itself

And because you've already built the configurable game system, you're actually in an unusually good position now.

I'd add an automated **simulation/balance mode**.

Have AI-controlled players run, say:

> Human vs Robot — 1,000 games  
> Human vs Raiders — 1,000  
> Robot vs Raiders — 1,000

Record not just win rates but:

- win rate;
- average game length;
- average damage per card;
- Gold generated per turn;
- cards played per turn;
- units killed per turn;
- resource efficiency;
- average unused resources;
- turns cards remain alive;
- first-player advantage;
- average HQ/Hero health at victory.

Then you don't want merely:

> Human 50%, Robot 50%.

You want a **matchup matrix**:

| | Human | Robot | Raiders |
|---|---:|---:|---:|
| Human | — | 48% | 52% |
| Robot | 52% | — | 49% |
| Raiders | 48% | 51% | — |

Something roughly in that neighborhood.

More importantly, when Robot has a 75% win rate against Humans, the telemetry can tell us **why**:

> Robot average kills/attack = 0.94  
> Human average unit lifetime = 1.7 turns  
> Human infrastructure break-even = 4.2 turns  
> Human reaches trained army only 31% of games

Then we know not to randomly change card numbers.

And I'd specifically avoid targeting exact 50/50 matchups. A bit of asymmetric advantage is interesting. Something like **45–55%** for ordinary matchups is perfectly healthy; persistent 65–80% matchups indicate structural problems.

The fact that your first actual implementation produced surprising results—Raiders stronger than expected and Robot even stronger—is useful. It means you're already past purely theoretical card design. **Now the simulations can start informing the rules**, and that's where the game can become genuinely balanced rather than merely looking balanced on paper. memcite

===== USER =====
Yeah, but the problem is that the robots can already attack at turn two, just like the raider war chief, which means that they can be putting constant pressure and that the human town people are never able to build their town properly. And also, single units that are stronger are very overpowering because of the armor advantage and attack advantage. Because if they have higher attack, they're more easy to penetrate other people's armors. And also a single strong unit has higher armor, which makes them almost impenetrable. So the balancing act should then be such that it becomes exponentially more difficult to add that extra attack damage or extra armor to a single unit. So that one unit cannot have, let's say, four armor and ten attack damage. That that is, well, it should be extremely hard to achieve, even with the robots.

===== ASSISTANT =====
Yes. You've identified a structural property of the combat mathematics, rather than merely an overpowered Robot card.

With:

**Damage = max(0, Attack − Armor)**

Attack and Armor have **increasing strategic value when concentrated on one entity**. A 6/4 unit isn't remotely equivalent to several units whose combined stats are 6/4.

For example, three 2-ATK Peasants attacking 4 Armor:

> 2 − 4 = 0 each → **0 total damage**

while one 6-ATK attacker:

> 6 − 4 = **2 damage**

So I agree with your conclusion: **concentrating stats should become progressively more expensive.**

### I'd introduce a nonlinear stat-cost curve

As a first balancing model, something approximately like:

| Attack | Marginal cost of next ATK |
|---:|---:|
| 1 → 2 | +1 |
| 2 → 3 | +1 |
| 3 → 4 | +2 |
| 4 → 5 | +3 |
| 5 → 6 | +4 |
| 6 → 7 | +6 |
| 7 → 8 | +8 |
| 8 → 9 | +11 |
| 9 → 10 | +15 |

So going from 2 → 3 Attack is ordinary.

Going from 5 → 6 is a serious upgrade.

Going from 9 → 10 is absurdly expensive.

You could model Armor even more aggressively because of how your damage system works:

| Armor | Relative cost |
|---:|---:|
| 0 | 0 |
| 1 | 1 |
| 2 | 3 |
| 3 | 6 |
| 4 | 11 |
| 5 | 18 |
| 6 | 28 |

That immediately changes how we think about our earlier Robot.

A Robot with:

> **5 Attack / 10 HP / 3 Armor**

isn't just "a bit stronger" than a Soldier at:

> **3 Attack / 4 HP / 2 Armor**

That extra Armor point crosses an important threshold, while the extra Attack penetrates the Soldier's Armor much more effectively.

### Attack and Armor also interact multiplicatively

There's another layer I'd include in the valuation.

High Attack + high Armor **on the same entity** deserves an additional premium.

A glass cannon:

> 7 ATK / 5 HP / 0 Armor

can be balanced.

A tank:

> 2 ATK / 10 HP / 5 Armor

can potentially be balanced because it doesn't kill things efficiently.

But:

> **7 ATK / 10 HP / 5 Armor**

is much more problematic than simply adding the independent prices of those stats. It both wins attacks **and** prevents retaliation.

So the balance model should include a **concentration premium**.

Conceptually:

```text
Card value =
    AttackValue(ATK)
  + ArmorValue(Armor)
  + HPValue(HP)
  + abilities
  + AttackArmorSynergyPenalty
```

That last term grows when both Attack and Armor are high.

---

## I'd establish "normal" combat bands

For the first balance pass, I'd keep most permanent units around:

**Attack:** 1–4  
**Armor:** 0–2

Then:

**5 Attack** = powerful  
**3 Armor** = powerful  
**6+ Attack** = exceptional  
**4+ Armor** = exceptional

A permanent **10 Attack / 4 Armor** entity should be almost unheard of.

Temporary effects can break these boundaries because they expire.

That makes your Call to Arms mechanic particularly sensible:

> Peasant 2/3/1 → temporarily 3/3/2.

You're getting a significant combat spike, but you're not permanently creating 3-Armor Peasants.

---

## Robot should start weaker and build toward power

This also suggests that our original Robot design starts too high.

Instead of:

> Robot starts 5 ATK / 10 HP / 3 Armor

I'd consider something closer to:

**AX-01**  
**3 ATK | 8 HP | 2 Armor**

That's already a formidable starting Hero.

Its modules then create specialization.

**Plasma Cannon:** +2 ATK

→ 5 ATK / 2 Armor

**Heavy Armor:** +1 Armor

→ 3 ATK / 3 Armor

But equipping **both** should become expensive.

And something like:

> +2 Armor

should be extremely expensive or carry a downside.

For example:

**Siege Plating**  
+2 Armor  
**AX-01 cannot attack on the turn after it attacks.**

Now 4 Armor isn't simply "better."

---

## The turn-two aggression is a separate problem

This shouldn't be solved entirely through stats.

If every starting Hero can attack on turn two, the Town deck—which deliberately has an investment/setup economy—is structurally disadvantaged.

You need some form of **early-game protection or tempo asymmetry**.

One elegant solution is to make the Town Hall itself good at protecting a developing settlement.

For example:

**Town Walls — passive on Town Hall**

> While you control no completed non-HQ Buildings, your Town Hall and Town Chief have +1 Armor.

But that doesn't protect Peasants enough.

I think a better solution is a general **HQ protection zone**:

> Units cannot be attacked directly while their Headquarters remains standing unless they have attacked or another effect exposes them.

Then Raiders can pressure the Town, but they have to attack:

> Town Hall / defensive units / exposed attackers

rather than simply killing every Peasant the moment it appears.

However, that's a significant combat-rule change.

Another possibility is giving Peasants:

**Civilian**

> This unit cannot be attacked while it has not attacked, unless the opponent controls a card that can target Civilians.

That would make Raiders good at destroying infrastructure without simply slaughtering the Human economy on turn two.

I'd test both approaches.

---

### Most importantly: don't balance by total stats

Your simulations have demonstrated why:

```text
2 ATK + 2 ATK + 2 ATK
```

does **not** equal:

```text
6 ATK
```

and:

```text
1 Armor on four units
```

doesn't equal:

```text
4 Armor on one unit.
```

With subtractive Armor, **stat concentration itself has value**.

So I'd make this an explicit principle of your balancing system:

> **The marginal value of Attack and Armor increases with the amount already present on the same permanent entity. Therefore their balancing cost must increase faster than linearly.**

That one rule should influence virtually every card we design from here onward. And it explains very well why your Robot deck became the monster in actual play even though its raw numbers didn't initially look absurd.

===== USER =====
So I think the problem between the human and the raiders is more easily solved, because the raiders do not have these options to be extremely strong very quickly. Their economy just needs to be a little bit more balanced so they can easily, more easily, pillage gold, while at the same time their units should be made maybe a little less strong so that they cannot completely overwhelm an opponent early game that easy.

===== ASSISTANT =====
Yes. I think Raiders are fundamentally a **numbers/economy tuning problem**, whereas the Robot exposes a deeper combat-system problem.

For Raiders, I wouldn't remove the aggressive identity. I'd actually make their economy **more dependent on successful pillaging**, while reducing their starting combat efficiency.

The intended cycle should be:

**weak/moderate Raiders → successful raids → Gold → larger warband → stronger pressure**

rather than:

**strong Raiders → immediately dominate → also get Gold for dominating → snowball.**

A good first rebalance would be:

| Aspect | Current problem | Direction |
|---|---|---|
| Raider base stats | Too efficient early | −1 ATK or −1 Armor on several cheap Raiders |
| Raider starting Hero | Too threatening turn 2 | Reduce ATK, perhaps 5 → 4 |
| Loot | Situational | Keep: steal up to 2 Gold |
| Stolen Treasure | Interesting but conditional | Keep: capture up to 4 enemy Gold as tokens |
| Pillaging | Should define economy | Increase rewards specifically for HQ/building attacks |
| Passive Gold | Makes aggression snowball | Reduce/remove |
| Expensive Raiders | Need payoff | Keep them strong, but require pillage economy |

For example, instead of a generic Raider being:

> **3 ATK / 4 HP / 2 Armor — 2 Gold**

make it something closer to:

> **2 ATK / 4 HP / 1 Armor — 2 Gold**

But give it:

> **Pillage:** When this deals combat damage to an enemy Headquarters, gain 1 Gold.

Now it's not particularly good at wiping out Human units. Its purpose is to **get through and steal resources**.

A dedicated Pillager might be:

> **3 ATK / 3 HP / 1 Armor**  
> +2 ATK when attacking Buildings.  
> When it destroys a Building, gain 2 Gold.

That's dangerous to a developed Human town without being a 5-ATK monster against everything.

And I'd make the Warchief something like **4 ATK / 6 HP / 2 Armor** rather than letting him become the early-game wrecking ball himself. His abilities should make *Raiders better at raiding* rather than simply making himself an enormous fighter.

This produces an important distinction:

> **Raiders should be good at attacking economic targets, not universally good at combat.**

That gives Humans counterplay. Soldiers and Town Guards should generally beat equivalently priced Raiders in straight combat because the Human player paid the additional costs of **Peasants → construction → Barracks → Training → Housing** to field them.

But Raiders get to skip all of that infrastructure.

So I'd actually establish this balance rule:

> **A 3-Gold Raider should be weaker in direct combat than a Human unit requiring 3 Gold plus Training/infrastructure.**

The Raider's compensation is **tempo**. It can arrive earlier and potentially finance itself by pillaging.

That gives the matchup the shape you originally wanted:

```text
EARLY
Raiders >>> Humans
pressure / pillage / disrupt

MID
Raiders ≈ Humans
Human military comes online

LATE
Humans >>> Raiders
developed town + trained army + equipment
```

But "Raiders >>> Humans" early should mean **they control the tempo**, not "they can simply kill everything the Human player puts down."

That's the distinction I'd tune for first. Once Housing is actually enforced and the Raider economy is tied more tightly to pillaging while their raw unit stats come down slightly, I suspect that matchup will become much easier to balance than Robot vs Human. memcite

===== USER =====
It also means that the human town must have a strong defense and rebuilding capacity, also early on in the game. So that if an opponent attacks early, that it may cost some peasants or anything, but they would still have to have like another defense layer.

===== ASSISTANT =====
Yes. I think that's the missing half of the Human identity. The Human deck shouldn't beat early aggression by having stronger early units. It should be **difficult to completely dismantle**. Raiders can burn the outskirts and kill workers, but taking the actual town should require sustained effort.

I'd give the Human deck three defensive layers:

1. **Peasants/workforce** — expendable first losses.
2. **Town defenses** — walls/guards that buy time.
3. **Town Hall + rebuilding** — resilient core that lets the economy recover.

A particularly fitting card would be:

### Wooden Palisade
**Building — Defense**  
**Cost: 1 Gold | Construction: 1**  
**5 HP | 2 Armor**

**Fortification:** While untapped, enemies cannot attack your non-Defense Buildings unless they first destroy a Defense Building.

This means a Raider can't simply go:

> turn 2 → destroy Barracks → turn 3 → destroy Archery Range → Human never develops.

They have to break through the Palisade.

And because it requires only **1 construction**, a single Peasant can complete one quickly.

### Town Guard should be the unit equivalent

**Town Guard**  
2 ATK | 5 HP | 2 Armor  
Cost: 3 Gold + 1 Training

**Guard:** Enemies cannot attack your Hero while an untapped Guard is available as a legal target.

So once the town develops, its defenses shift from crude fortifications to professional defenders.

### The Town Hall itself should help rebuild

I'd modify its second ability slightly. Currently:

> 2 Gold + Tap → summon Peasant.

That's actually already your rebuilding mechanism, but **2 Gold may be too expensive after you've just had your economy destroyed**.

I'd add something like:

**Repopulate — Passive:** If you control no Peasants at the beginning of your turn, create one Peasant.

That is very powerful but extremely thematic.

The Raiders can slaughter your workforce, but they cannot permanently lock you out of playing the game:

```text
Raiders attack
↓
Peasants die
↓
Mines stop producing
Buildings stop progressing

NEXT TURN

Town Hall → Repopulate
↓
1 Peasant
↓
Harvest / rebuild
↓
economy slowly restarts
```

It doesn't make the attack meaningless. Going from five Peasants to one is devastating. It just prevents a **death spiral** where losing your workers means you can no longer generate the resources required to replace your workers.

### I'd also add a rebuilding card

**Rebuild the Town**  
Action — maximum 4

> Choose a destroyed Building from your discard pile. Play it under construction. Its Gold cost is reduced by 2.

It still needs its normal construction progress.

So if Raiders destroy your Barracks, you don't magically get it back. But your surviving Peasants can reconstruct it more cheaply.

This is exactly the kind of card that becomes more useful **the further behind you are**, which is healthy for preventing runaway games.

### And perhaps a cheap defensive Peasant action

Because Peasants are Builders, let fortifications interact with them:

**Man the Walls:** An untapped Peasant defending a Defense Building gives that Building **+1 Armor for that attack** and becomes tapped.

Now your Peasants have another choice:

> Mine  
> Build  
> Attack  
> Defend normally  
> **Man fortifications**

That reinforces the whole Human theme.

The result should be that an early Raider attack is still successful:

> kills two Peasants, steals Gold, damages a Palisade.

That's a **good raid**.

But:

> kills all Peasants → destroys Barracks → kills Hero → destroys Town Hall

should be extremely difficult in the first few turns.

The Human faction's strategic identity can therefore be **resilience**: individually mediocre units and slow infrastructure, but layered defenses, cheap repairs, replacement workers and reconstruction make the town progressively harder to eradicate.

That also makes the matchup more interesting for Raiders: **raid Humans for value, but don't expect every raid to become a conquest.**

===== USER =====
I think the human deck, when done right, should be able to produce lots of gold quickly, but they would be vulnerable early game if they don't buy defense as well. And later on in the game, they will probably run out of cards, because you only draw one card a turn. So they need to have late game options for drawing cards.

===== ASSISTANT =====
Yes. That gives the Human deck a much clearer identity: **the strongest conventional economy, but it takes time and cards to construct it**. The Human player's challenge is balancing growth, defense, and eventually card supply.

The economic curve should look roughly like:

> **Early:** vulnerable, limited Gold → **Mid:** rapidly expanding Gold → **Late:** abundant Gold but card-starved.

That last part is important: eventually the Human player can have 15 Gold sitting around but nothing useful in hand.

### Early game: growth versus defense

A Human opening might involve:

> Gold Mine → Peasant → start Barracks

which maximizes development but leaves you exposed.

Or:

> Gold Mine → Peasant → Palisade

which delays military development but makes a Raider rush much less dangerous.

Or even:

> multiple Peasants → multiple Mines

which could produce enormous Gold quickly but is essentially saying to the opponent: **attack me now.**

That's a good strategic trade-off. I wouldn't give Humans too much automatic defense because it would remove this choice. They should have **good defensive cards**, but the player has to actually invest in them.

### Midgame: Humans should become economically scary

A developed Human town might have:

```text
4 Peasants harvesting → +4 Gold
Town Hall             → +2 Gold
some economic building → +?
                         ────────
                         6+ Gold/turn
```

And perhaps later we introduce upgrades such as:

**Marketplace — Building**  
3 Gold | 3 Construction  
**Tap:** Gain 1 Gold for every two Workers you control, maximum 3.

Or:

**Improved Mine — Building/Upgrade**

> Gold Mines contain +2 Gold when played.

But I wouldn't overdo passive Gold. The core Human fantasy is that **Workers produce the economy**.

### Late game: card draw becomes their bottleneck

This is actually excellent because it gives the Human economy a natural diminishing return without imposing artificial Gold limits.

Eventually:

```text
Gold: 17
Training: 6
Housing available: 5

Hand:
1 card

Draw:
1 card/turn
```

The town has tremendous productive capacity, but lacks things to produce.

So Humans should be able to build **knowledge/commercial infrastructure** that converts their mature economy into cards.

I'd introduce something like:

### Marketplace
**Building**  
**4 Gold | Construction 3**  
5 HP | 1 Armor

**Trade — 2 Gold, Tap:** Draw a card.

That's simple and strong. You're converting your economic advantage into card advantage.

A second Marketplace remains useful because each has its own tap.

Two Marketplaces:

> Pay 4 Gold → draw 2 additional cards.

That's exactly what a rich late-game town wants.

### Library

And I'd make another building more selective:

**Library**  
**4 Gold | Construction 4**  
4 HP | 1 Armor

**Research — 2 Gold, Tap:** Look at the top 3 cards of your deck. Put one into your hand and the others on the bottom in any order.

So:

**Marketplace** = quantity.

**Library** = quality.

That also makes duplicate buildings useful without making them identical.

### Merchant

We could even introduce another Worker-like unit:

**Merchant**  
1 ATK | 3 HP | 0 Armor | Housing 1  
3 Gold

**Trade — Tap:** Pay 1 Gold → draw a card, then discard a card.

It doesn't create card advantage, but lets you cycle away useless Mines or early-game cards.

That becomes particularly important late game. Drawing a Gold Mine on turn 15 when you already have 20 Gold might be nearly useless. The Merchant turns it into another chance at finding your Knight, equipment, Paladin, etc.

### This also solves the "too many Gold Mines" problem elegantly

Because Gold Mines and Peasants are Unlimited, players can choose their own economic density.

But there's a natural consequence:

> Put 12 Gold Mines + 10 Peasants into your 40-card deck → extremely reliable economy, but you've used **22 deck slots** on economy.

You'll eventually draw a lot of low-impact cards.

Marketplace, Library and Merchant help mitigate that, but themselves cost deck slots and resources.

So deck construction becomes another balancing layer.

### I'd make the Human progression roughly four stages

**Foundation**

> Peasants + Mines + Town Hall

**Protection**

> Palisades + defensive units

**Development**

> Barracks + Archery Range + Training + Housing + equipment

**Prosperity**

> Marketplace + Library + large Gold income + card draw + elite units

Then the Human deck becomes increasingly dangerous if allowed to develop:

```text
TURN 1–3
Weak town
Workers exposed

TURN 4–7
Infrastructure forming
Defenses appearing

TURN 8–12
Strong economy
Professional army

TURN 12+
Gold surplus
Marketplace/Library
multiple cards per turn
rebuilding capacity
```

That's a much better asymmetry against Raiders. **Raiders are under pressure to exploit their tempo advantage before the Human economy compounds. Humans are under pressure not to become so greedy that they die before that happens.**

And importantly, the late-game card-draw buildings give Raiders excellent strategic targets. Once the Human player has plenty of Gold, destroying another Mine isn't necessarily important anymore. Destroying their **Marketplace or Library** may be how you stop them from turning that wealth into an endless stream of units and equipment.

===== USER =====
So it should mean that units that have more armor, let's say if a unit has one more armor, it should be exponentially more expensive than a unit with one less armor. And the same applies to attack damage. And also, but an armor should roughly cost, let's say, twice as much as an attack damage. And a hit point should cost maybe half as much as an attack damage.

===== ASSISTANT =====
Yes. I think that's the right direction, but there's an important distinction: the **total value** of a stat should grow exponentially/nonlinearly, not simply "each Armor is worth 2 Attack." Otherwise stat concentration remains too cheap.

We can create a universal **Combat Value (CV)** model and use it as the starting point for balancing every unit, Hero, Robot, and equipment card.

Take **1 Attack as the base value**. Then your ratios are:

> Armor ≈ 2× Attack value  
> HP ≈ 0.5× Attack value

But apply an escalating curve to each successive point.

For a first version, I'd use a simple doubling curve:

| Stat level | ATK point | HP point | Armor point |
|---:|---:|---:|---:|
| 1st | 1 | 0.5 | 2 |
| 2nd | 2 | 1 | 4 |
| 3rd | 4 | 2 | 8 |
| 4th | 8 | 4 | 16 |
| 5th | 16 | 8 | 32 |
| 6th | 32 | 16 | 64 |

That's probably **too exponential** for actual balancing, but it demonstrates the principle. A softer curve will likely work better.

I'd start testing something like a **1.5× marginal curve** instead:

| Stat | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---:|---:|---:|---:|---:|---:|
| marginal ATK | 1 | 1.5 | 2.25 | 3.4 | 5.1 | 7.6 |
| marginal Armor | 2 | 3 | 4.5 | 6.8 | 10.1 | 15.2 |
| marginal HP | 0.5 | 0.75 | 1.13 | 1.69 | 2.53 | 3.8 |

The key word is **marginal**. A 4-ATK unit costs the sum of the first four Attack points:

> 1 + 1.5 + 2.25 + 3.4 ≈ **8.15 CV**

A 5-ATK unit:

> 8.15 + 5.1 ≈ **13.25 CV**

So that single jump from 4 → 5 ATK costs almost as much as the first three Attack points combined.

### Armor becomes very expensive

Using that same curve:

**1 Armor:** 2 CV  
**2 Armor:** 5 CV total  
**3 Armor:** 9.5 CV total  
**4 Armor:** 16.25 CV total

That's appropriate for your combat system because 4 Armor is extraordinary. It makes all attacks of 4 or lower deal zero damage.

So the difference between:

> 3 Armor → 4 Armor

should indeed be vastly more expensive than:

> 0 Armor → 1 Armor.

### HP remains the cheapest way to make something tougher

This gives you another desirable consequence.

Suppose a designer wants a tanky unit. Instead of casually increasing Armor:

> 4 HP / 2 Armor → 4 HP / **3 Armor**

which dramatically changes which attackers can hurt it, the cheaper option is:

> **7 HP / 2 Armor**

It survives longer but remains penetrable.

That's healthy for the game.

It means the system naturally encourages:

**HP for ordinary durability.**  
**Armor for exceptional damage resistance.**

### Equipment should use exactly the same valuation

This is especially important for the Robot.

If AX-01 already has 3 Armor, a module giving:

> +1 Armor

is **not worth the same amount** as +1 Armor on a 0-Armor Peasant.

The Robot is going:

> 3 → 4 Armor

so the upgrade should be priced according to the **fourth Armor point**.

Similarly:

> Robot has 5 ATK.  
> Plasma Cannon gives +2 ATK.

You're purchasing Attack points **6 and 7**, not Attack points 1 and 2.

Those should be extremely expensive.

This single rule directly attacks the Robot problem.

### I'd also calculate effective stats dynamically

The balance engine should evaluate:

```text id="xkg8as"
Base stats
+ equipment
+ permanent upgrades
+ permanent modifiers
```

as **one combined stat curve**.

Otherwise players can circumvent nonlinear pricing by stacking cheap equipment:

> Robot base Armor 2  
> Armor module +1  
> Shield +1  
> Upgrade +1

If each card is priced as an isolated "+1 Armor", you've accidentally allowed cheap **5 Armor**.

Instead, the fifth Armor point should have fifth-point economics regardless of where it came from.

This could even translate into gameplay through **escalating installation costs** for Robot modules or equipment penalties.

### One additional factor

I'd still add a smaller **ATK × Armor concentration premium**, because an entity that's simultaneously hard to damage *and* penetrates everything is disproportionately powerful.

So conceptually:

```text id="iy6obq"
CombatValue =
    AttackCurve(Attack)
  + 2 × AttackCurve(Armor)
  + 0.5 × AttackCurve(HP)
  + ConcentrationPremium
```

where the same underlying nonlinear curve is being weighted differently.

That gives us a genuine mathematical baseline for card generation. We can then run your actual game simulations and fit the curve—perhaps **1.35×** turns out better than **1.5×**, and perhaps Armor is actually **2.4× Attack** under your damage rules.

The important part is the architecture: **stat value is nonlinear, Armor has a much higher coefficient than Attack, HP has a lower coefficient, and all bonuses are evaluated against the entity's resulting total stats rather than independently.** That directly addresses what your first playtests exposed.

===== USER =====
Then we should probably have a deck that also has abilities to control, to fight against single very strong units, for instance by disabling them or ignoring their armor or having nerfing effects.

===== ASSISTANT =====
Yes. And I would make that an entire faction identity rather than just sprinkling anti-Robot cards everywhere.

A natural deck would be a **Control / Alchemist deck**: individually weak units whose specialty is making powerful enemies ineffective. It should be particularly good against "one giant threat", while being weaker against swarms.

## The Alchemist Guild

**HQ: Laboratory**  
Its economy could revolve around **Reagents**. Rather than generating another generic mana-like resource, Reagents could be actual types—e.g. Fire, Acid, Frost, Poison—and recipes combine them into effects.

**Hero: Master Alchemist**  
Weak body, perhaps:

**2 ATK | 5 HP | 1 Armor**

but extremely strong control abilities.

The fundamental principle is:

> *Don't overpower the Robot. Make the Robot temporarily useless.*

### Effects specifically good against concentrated power

**Freeze**

> Target unit becomes tapped and **doesn't untap during its controller's next turn**.

Against a Peasant that's mediocre.

Against a 30-value Robot, it's devastating.

**Corrosive Acid**

> Target character gets **−2 Armor until end of turn**.

A 1-Armor unit goes to 0.

But against:

> Robot: 4 Armor → **2 Armor**

it opens the Robot up to ordinary attacks.

**Armor Dissolver**

> Damage from your next attack against target character **ignores Armor**.

This is exactly the sort of effect that prevents Armor from becoming an unbeatable strategy.

### Weakness

The Alchemist should struggle against quantity.

If the enemy has:

> one Robot

freezing it essentially disables their entire army.

But if the Human player has:

> 6 Peasants  
> 2 Soldiers  
> 2 Archers  
> Town Chief

freezing one Soldier isn't nearly as impressive.

That naturally gives us a strategic triangle:

```text id="imnrxo"
ROBOT
high concentrated power
       ↓ vulnerable to
CONTROL
disable / debuff / bypass
       ↓ vulnerable to
SWARM
too many targets
       ↓ vulnerable to
ROBOT / AoE
```

Not necessarily hard rock-paper-scissors, but meaningful tendencies.

## Some abilities I'd definitely want in the game

**Disarm**  
Disable target Weapon for 2 turns.

That's particularly nasty against equipment-heavy Heroes and Robots without destroying the equipment permanently.

**Rust**  
Target character gets −1 Armor. If it's a Robot, perhaps −2 instead.

I'd avoid too many faction-specific bonuses, though. Generic mechanics are cleaner.

**Weaken**

> Target gets −2 Attack until your next turn.

Again, because Attack is nonlinear, reducing:

> 7 → 5

should be much more valuable than reducing:

> 3 → 1.

That's actually an interesting consequence of our balance model.

### Percentage-like effects become useful

We could have:

**Crippling Curse**

> Halve target character's Attack, rounded down, until end of turn.

Against:

> Peasant 2 ATK → 1

only −1.

Against:

> Robot 8 ATK → 4

−4.

So the card naturally scales against concentrated power without explicitly mentioning Robots.

That's elegant anti-boss design.

Likewise:

**Armor Fracture**

> Target loses half its Armor, rounded up, until end of turn.

Again:

> 1 Armor → 0  
> 2 → 1  
> 4 → 2  
> 6 → 3

The stronger the target, the more value the control player gets.

### Transformation is another excellent counter

A powerful spell could say:

**Transmutation — 5 Reagents**

> Target non-Hero Unit becomes a **Sheep** until the beginning of your next turn.

Sheep:

> 1 ATK / 2 HP / 0 Armor  
> Abilities disabled.

That absolutely destroys a giant temporary threat for a turn—but doesn't permanently remove it.

For Heroes, perhaps transformation isn't allowed, or Heroes receive a weaker version.

## Control should also interact with abilities

We need more than stat debuffs.

**Silence**

> Target character loses all activated abilities until your next turn.

**Suppress**

> Target passive ability doesn't function this turn.

**Seal**

> Target character cannot gain counters/resources until your next turn.

**Ground**

> Target cannot use movement/evasion abilities.

**Dispel**

> Destroy a temporary magical Equipment or ongoing magical effect.

That last one gives Alchemists/Control a particularly interesting matchup against Wizards.

## This is important for the overall game

I wouldn't rely solely on the nonlinear cost curve to prevent giant units from becoming dominant.

We need **three balancing layers**:

**Economics:**  
7 Attack + 4 Armor is extraordinarily expensive.

**Opportunity cost:**  
Putting all that value into one entity means you have fewer entities/actions.

**Counterplay:**  
The opponent can Freeze, Silence, Disarm, reduce Armor, bypass Armor, transform, etc.

Then building a giant Robot is still exciting:

> "I finally have an 8 ATK / 4 Armor monster."

But it isn't synonymous with winning.

The opponent might respond:

> **Freeze.**

And your enormous investment spends an entire turn doing nothing.

That's precisely why control mechanics are valuable: **the more value a player concentrates into one entity, the more dangerous single-target disruption becomes.**

===== USER =====
Also, players need to have cards that in Magic: The Gathering are known as instants that you can play against any action of your opponent.

===== ASSISTANT =====
Yes. I would make **reaction cards a fundamental game-wide card type**, not something specific to the control deck. The Control deck can specialize in them, but every deck should have some access to reactions.

The important design decision is that they should be able to respond not only to attacks or spells, but potentially **any interruptible action**.

### Reaction

I'd call the generic type `Reaction`—with "Instant" perhaps available as a template-specific name.

Whenever a player performs an interruptible action:

> **Action declared → Reaction window → reactions → resolve**

For example:

```text
Robot declares attack on Soldier
        ↓
Human: Fortify
Soldier +2 Armor
        ↓
Robot: Armor Piercing
Attack ignores 2 Armor
        ↓
Human: Hold the Line
Redirect attack to Town Guard
        ↓
Resolve reactions backwards
        ↓
Resolve original attack
```

That gives us the familiar stack-like behavior.

### Every deck gets thematic reactions

**Human — Protect the Town**  
**2 Gold — Reaction**

Play when one of your Buildings is attacked.

> Target Peasant may tap to intercept the attack.

---

**Human — Emergency Repairs**  
**2 Gold — Reaction**

> Prevent the next 3 damage to target Building this turn.

---

**Raiders — Dirty Trick**  
**1 Gold — Reaction**

When one of your Raiders attacks or is attacked:

> Target enemy gets −1 Armor for this combat.

---

**Raiders — Grab the Loot!**  
**Reaction**

When an opponent would gain Gold:

> Steal 1 of that Gold instead.

This could create some hilarious interactions with Human economy.

---

**Wizard — Counterspell**  
**3 Mana — Reaction**

> Cancel target Spell.

Notice that this doesn't necessarily cancel an attack. The reaction needs an appropriate target.

---

**Wizard — Blink**  
**2 Mana — Reaction**

When a friendly Caster becomes the target of an action:

> Remove it from play. Return it at the end of the current action.

The attack/spell loses its target.

---

**Undead — Not Dead Yet**  
**Reaction**

When an Undead unit would die:

> It remains at 1 HP instead.

---

**Robot — Reactive Shielding**  
**2 Energy — Reaction**

When Robot receives an attack:

> +2 Armor for that attack.

Again, potentially very strong because of Armor's nonlinear value, so this needs careful pricing.

---

**Control — Disable**
This deck should probably be the king of reactions.

When an opponent activates an ability:

> Cancel that ability. The entity remains tapped and any resource cost is still paid.

That is *much* nastier than Counterspell.

---

### But "respond to anything" needs one important restriction

I'd separate:

**Actions** from **state changes**.

You can react to:

> Attack declared  
> Spell cast  
> Ability activated  
> Card played  
> Building activated  
> Equipment equipped  
> Resource transfer initiated  
> Unit summoned

But not necessarily to every internal consequence:

> HP changes  
> counter removed  
> duration decreases  
> unit reaches 0 HP

Otherwise reaction windows recursively appear around practically every engine operation.

So the ontology could define:

```text
Action
    interruptible: true/false
```

For example:

```text
Attack
interruptible = true

CastSpell
interruptible = true

ActivateAbility
interruptible = true

PlayCard
interruptible = true

StateBasedDeath
interruptible = false

DurationTick
interruptible = false
```

Individual cards can override this.

### And reactions themselves can be reacted to

That's important.

Player A:

> Fireball.

Player B:

> Counterspell.

Player A:

> Counter-Counterspell.

Player B:

> Arcane Shield? No, because Fireball is currently countered unless the counter is itself countered.

Then resolve **last-in-first-out**:

```text
STACK

3  Counterspell B ← resolves last? No, top first
2  Counterspell A
1  Fireball

        ↓

resolve #3
resolve #2 if still valid
resolve #1 if still valid
```

More precisely, the latest reaction resolves first.

### Multiplayer needs priority

With four players, you can't simply ask everyone simultaneously.

Something like:

> Active player performs action → priority passes clockwise.

Each player can:

> React  
> Pass

When everyone consecutively passes:

> resolve top item.

Then priority opens again if appropriate.

That gives your engine a completely generic mechanism.

And importantly, **cards don't have to be Reaction cards to be usable as reactions**. Timing should really be a property:

```text
Card: Fireball
Timing: Main

Card: Counterspell
Timing: Reaction

Card: Arcane Shield
Timing:
  - Main
  - Reaction
```

This is more generic for your configurable engine than hard-coding `Instant` as a special class.

It also gives you an important balancing dimension. A card that gives **+2 Armor as a Reaction** is significantly more valuable than an otherwise identical card that can only be played during your own main phase, because the Reaction version can wait until the player knows exactly where the opponent is attacking.
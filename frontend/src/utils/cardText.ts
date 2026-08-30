import { CostDto, ConditionDto, EffectDto, ChoiceDefinition, TriggerDto } from '../types/game';

// Turns definition JSON into readable rules text for the card inspector.

type NameOf = (cardId: string) => string;

function resName(id?: string | null): string {
  if (!id) return 'resource';
  if (id === 'gold') return 'Gold';
  if (id === 'energy') return 'Energy';
  if (id === 'ap') return 'AP';
  if (id === 'loot') return 'Loot Token';
  return id;
}

function propName(id?: string | null): string {
  if (id === 'attack') return 'Attack';
  if (id === 'armor') return 'Armor';
  if (id === 'currentHp') return 'HP';
  if (id === 'maxHp') return 'Max HP';
  return id ?? 'stat';
}

export function explainCost(c: CostDto): string {
  switch (c.type) {
    case 'tap': return 'Tap this card';
    case 'resource':
      return c.scope === 'player'
        ? `Pay ${c.amount} ${resName(c.resourceId)}`
        : `Spend ${c.amount} ${resName(c.resourceId)} from this card`;
    case 'sacrifice': return 'Sacrifice this card';
    case 'crew': {
      const n = c.amount ?? 1;
      const who = c.tag ? c.tag.charAt(0).toUpperCase() + c.tag.slice(1) : 'unit';
      return `Crew ${n} — tap ${n} untapped ${who}${n > 1 ? 's' : ''}`;
    }
    default: return c.type;
  }
}

export function explainCondition(c: ConditionDto): string {
  switch (c.type) {
    case 'not_tapped': return 'Only while untapped';
    case 'is_tapped': return 'Only while tapped';
    case 'resource_gte': return `Requires at least ${c.amount} ${resName(c.resourceId)}`;
    case 'resource_lte': return `Requires at most ${c.amount} ${resName(c.resourceId)}`;
    case 'has_tag': return `Only if this card is a ${c.tag}`;
    case 'is_phase': return `Only during the ${c.phase} phase`;
    case 'own_hero_destroyed': return 'Only while your hero is destroyed';
    case 'is_attached': return 'Only while infiltrated / attached';
    case 'not_attached': return 'Only while free on the battlefield';
    default: return c.type;
  }
}

export function explainChoice(ch: ChoiceDefinition): string {
  const who = ch.controller === 'self' ? 'friendly' : ch.controller === 'opponent' ? 'enemy' : 'any';
  const what = ch.attachmentsOnly ? 'attached card' : ch.tag ?? ch.objectType ?? 'card';
  const count = ch.min === ch.max ? `${ch.min}` : `${ch.min}–${ch.max}`;
  const where = ch.hostController === 'self'
    ? ` on a ${ch.hostObjectType ?? 'card'} you control`
    : ch.hostController === 'opponent' ? ` on an enemy ${ch.hostObjectType ?? 'card'}` : '';
  const hidden = ch.includeFaceDown ? ' (even face-down)' : '';
  const cap = ch.maxPropertyId ? ` with ${propName(ch.maxPropertyId)} ${ch.maxPropertyValue ?? 0} or less` : '';
  return `Target: ${count} ${who} ${what}${ch.max > 1 ? 's' : ''}${cap}${where}${hidden}`;
}

export function explainEffect(e: EffectDto, nameOf: NameOf): string {
  const target = e.scope === 'target' ? 'the target'
    : e.scope === 'player' ? 'you'
    : e.scope === 'host' ? 'the card this is attached to'
    : 'this card';
  switch (e.type) {
    case 'gain_resource':
      return e.scope === 'player'
        ? `Gain ${e.amount} ${resName(e.resourceId)}`
        : `This card gains ${e.amount} ${resName(e.resourceId)}`;
    case 'set_resource': return `Set ${resName(e.resourceId)} of ${target} to ${e.amount}`;
    case 'set_property': return `Set ${propName(e.propertyId)} of ${target} to ${e.amount}`;
    case 'modify_property': {
      const sign = (e.amount ?? 0) >= 0 ? '+' : '';
      return `Permanently ${sign}${e.amount} ${propName(e.propertyId)} to ${target}`;
    }
    case 'tap': return `Tap ${target}`;
    case 'untap': return `Untap ${target}`;
    case 'summon': return `Summon a ${nameOf(e.cardId ?? '')}${e.ignoreHousing ? ' (ignores Housing)' : ''}`;
    case 'destroy': return `Destroy ${target}`;
    case 'heal': return `Heal ${target} for ${e.amount} HP (up to its maximum)`;
    case 'damage': return `Deal ${e.amount} damage to ${target} (reduced by its Armor)`;
    case 'buff_tag_until_end_of_turn':
      return `All your ${e.tag} cards get +${e.amount} ${propName(e.propertyId)} until end of turn`;
    case 'buff_target_until_end_of_turn':
      return `${e.scope === 'self' ? 'This card gets' : 'The target gets'} +${e.amount} ${propName(e.propertyId)} until end of turn`;
    case 'transform': return `Transform the target into a ${nameOf(e.cardId ?? '')} (fresh stats, cannot act this turn)`;
    case 'steal_resource': return `Steal up to ${e.amount} ${resName(e.resourceId)} from the opponent (nothing if they have none)`;
    case 'plunder_resource': return `Remove up to ${e.amount} ${resName(e.resourceId)} from the opponent and store that many tokens on this card`;
    case 'revive_hero': return 'Return your destroyed hero to the battlefield with full HP (it cannot act this turn)';
    case 'infiltrate': return 'Slip face-down into the target enemy building. While inside: cannot attack, block or be attacked — only its infiltration abilities work';
    case 'direct_damage': return `Deal ${e.amount} direct damage to ${target} (ignores Armor)`;
    case 'reveal': return 'Reveal the target face-down card';
    case 'reveal_attachments': return 'Search the target building: everything hiding inside is revealed';
    case 'destroy_infiltrators': return 'Flush out the target building: every enemy card attached to it (even hidden) is destroyed';
    case 'damage_infiltrators': return `Enemy cards hiding in the target building are revealed and take ${e.amount} direct damage each`;
    case 'gain_bank_resource': return `Add ${e.amount} ${resName(e.resourceId)} to your HQ stockpile`;
    case 'damage_enemy_units': {
      const scopeTxt = e.line ? ` on the ${e.line} line` : '';
      const frail = e.maxHp ? ` with ${e.maxHp} or less max HP` : '';
      return `Deal ${e.amount} damage to every enemy unit${scopeTxt}${frail} (reduced by Armor)`;
    }
    case 'afflict_enemy_units': {
      const scopeTxt = e.line ? ` on the ${e.line} line` : '';
      const frail = e.maxHp ? ` with ${e.maxHp} or less max HP` : '';
      return `Every enemy unit${scopeTxt}${frail} gains ${e.amount} ${resName(e.resourceId)}`;
    }
    default: return e.type;
  }
}

export function explainTrigger(t: TriggerDto, nameOf: NameOf): string {
  const when: Record<string, string> = {
    onKill: 'Whenever this card destroys an enemy in combat',
    onDestroyBuilding: 'Whenever this card destroys an enemy building',
    onFriendlyDamageHqOrHero: 'Whenever any of your cards damages the enemy hero or HQ',
  };
  const effects = t.effects.map(e => explainEffect(e, nameOf)).join('; ');
  return `${when[t.event] ?? t.event}${t.oncePerTurn ? ' (once per turn)' : ''}: ${effects}`;
}

// ---- Hover tooltips ------------------------------------------------------

export const HOUSING_TIP =
  'Housing — units need living space. Your capacity comes from buildings (Town Hall +5, House +3, Tavern +2); each unit occupies 1. At the cap you cannot play or recruit more units until you build housing — and losing a building can lock recruitment until you are back under capacity. Heroes need no housing.';

export const HOUSING_COST_TIP = (n: number) =>
  `Needs ${n} housing while on the battlefield. If your housing is full, this card cannot enter play.`;

export const HOUSING_PROVIDED_TIP = (n: number) =>
  `Provides ${n} housing while on the battlefield. Lose this building and the capacity goes with it.`;

export const STAT_TIPS: Record<string, string> = {
  attack: 'Attack — damage this card deals in combat. Damage to the target = Attack minus the target’s Armor.',
  armor: 'Armor — reduces every hit taken by this amount. Damage taken = attacker’s Attack minus this Armor.',
  hp: 'Hit Points — when they reach 0, this card is destroyed and goes to the discard pile.',
  ap: 'Action Points — heroes gain 1 at the start of their turn. Spent on hero abilities; unspent AP carries over.',
  loot: 'Loot Tokens — plundered gold stored on this card. Claim Spoils converts 1 token into 1 Gold. Destroy this card to deny the rest.',
};

export const TAG_TIPS: Record<string, string> = {
  ranged: 'Ranged — always reaches the enemy front line, even from your back line. From your front line it reaches both enemy lines. From your back line it can bombard the enemy back line once their front is empty — but only while your own front line is held.',
  retaliate: 'Retaliate — when attacked by an enemy within its own reach, this card strikes back for its Attack (reduced by the attacker’s Armor). Cards without this keyword never strike back when defending.',
  guard: 'Guard — while this card is untapped and within reach, enemies cannot attack your Hero or Headquarters. Kill or exhaust the guards first.',
  blocking: 'Blocking — while this card is untapped and within reach, enemies must attack it before they can attack any other unit on this line.',
  fortification: 'Fortification — while this card is untapped and within reach, other buildings are protected from attack.',
  archer: 'Archer — trained at the Archery Range; tapping a Range makes your next Archer this turn cost 1 less Training.',
  knight: 'Knight — elite heavy cavalry. Expensive in gold and training, devastating on the field.',
  peasant: 'Peasant — the town workforce. Can be trained into a Soldier (Barracks) or Archer (Archery Range), and boosted by Call to Arms.',
  worker: 'Worker — can perform resource-producing work.',
  builder: 'Builder — can construct buildings.',
  soldier: 'Soldier — professional Town infantry.',
  mercenary: 'Mercenary — an outsider hired at the Tavern for gold, no Peasant required.',
  raider: 'Raider — boosted by the Warchief’s Bloodlust and the Fighting Pit’s Rally.',
  cleave: 'Cleave — when this unit’s attack kills a unit, the excess damage carries into the next enemy unit on that line (reduced by its Armor).',
  splash: 'Splash — when this unit attacks a unit, every other enemy unit on that line also takes 1 damage (reduced by its Armor).',
};

export const TYPE_TIPS: Record<string, string> = {
  hero: 'Hero — your leader. You only lose the game when BOTH your hero and your headquarters are destroyed.',
  headquarters: 'Headquarters — your base. You only lose the game when BOTH your HQ and your hero are destroyed.',
  unit: 'Unit — fights on the battlefield. Cannot move or attack the turn it arrives.',
  building: 'Building — placed in a line but cannot move or attack. Provides abilities.',
  spell: 'Spell — resolves its effect immediately when played, then goes to the discard pile.',
  item: 'Item — applies a permanent bonus to the chosen card, then goes to the discard pile.',
  module: 'Module — installs onto your hero and occupies an equipment slot. Its bonuses last while installed; it is destroyed with the hero.',
  treasure: 'Treasure — a building holding stored tokens. Destroy it to deny the owner the remaining value.',
};

export const STATUS_TIPS: Record<string, string> = {
  tapped: 'Tapped — already used this turn. Untaps automatically at the start of its controller’s next turn.',
  new: 'Summoning sickness — arrived this turn; cannot move or attack until its controller’s next turn.',
  moved: 'Moved this turn — cannot attack or use abilities until next turn.',
};

export const LINE_TIP_FRONT =
  'Front Line — melee cards attack from here. Holding it shields your back line, and lets your back-line ranged cards bombard the enemy back line once the enemy front is empty.';
export const LINE_TIP_BACK =
  'Back Line — where all cards spawn. If your front line is empty, melee here can defensively strike the enemy front line, and your ranged cards can only hit the enemy front.';

export function playCostTip(cost: number, resource: string): string {
  return `Play cost — pay ${cost} ${resName(resource)} to play this card from your hand during your Main Phase.`;
}

export function slotTip(slot: string): string {
  return `Occupies the ${slot} slot on your hero. Installing into a full slot dismantles the oldest module there.`;
}

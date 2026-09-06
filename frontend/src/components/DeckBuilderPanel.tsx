import { ReactNode, useEffect, useMemo, useState } from 'react';
import { GameDefinitionFull, CardDefinitionDto, ObjectStateDto, ObjectTypeDto } from '../types/game';
import { CardView } from './CardView';
import { CardDetailModal } from './CardDetailModal';
import { isDeckEligible } from '../utils/deckEligibility';

// The one deck-builder editor, shared by My Decks (multiplayer) and the Campaign page
// (task 1050): real card views for the pool and the deck being built, full filter row,
// inspect modal, and HQ/hero pickers scoped to the headquarters-/hero-type cards
// actually in the deck — pick from what you put in, exactly like the campaign flow.

function isOrExtends(objectType: string, root: string, types: ObjectTypeDto[]): boolean {
  const byId: Record<string, ObjectTypeDto> = {};
  for (const t of types) byId[t.id] = t;
  let cur: string | undefined = objectType;
  while (cur) {
    if (cur === root) return true;
    cur = byId[cur]?.parentType ?? undefined;
  }
  return false;
}

// Sentinel faction id for cards that belong to no preconstructed deck.
const UNAFFILIATED_FACTION = '__unaffiliated__';

// The role/archetype tags the unit-type filter exposes, distinct from the mechanical
// keyword tags (retaliate, cleave, splash) that must never appear as filter options.
const UNIT_TYPE_TAG_LABELS: Record<string, string> = {
  peasant: 'Peasant',
  worker: 'Worker',
  builder: 'Builder',
  soldier: 'Soldier',
  ranged: 'Ranged',
  mercenary: 'Mercenary',
  raider: 'Raider',
  guard: 'Guard',
  archer: 'Archer',
  knight: 'Knight',
  'resource-node': 'Resource Node',
};

// A single numeric cost for filtering: playCost as-is, or the sum of playCosts'
// resource amounts for multi-resource cards (e.g. Soldier's gold+training).
function costOf(card: CardDefinitionDto): number | null {
  if (card.playCost !== null && card.playCost !== undefined) return card.playCost;
  if (card.playCosts) {
    const values = Object.values(card.playCosts);
    if (values.length > 0) return values.reduce((a, b) => a + b, 0);
  }
  return null;
}

// The deck builder shows cards using the same CardView/CardDetailModal components the live
// game uses, so it needs an ObjectStateDto-shaped stand-in for a static CardDefinitionDto
// (there is no live game object here — nothing is on a battlefield/in a hand).
function toObjectState(card: CardDefinitionDto): ObjectStateDto {
  return {
    id: card.id,
    definitionId: card.id,
    name: card.name,
    objectType: card.objectType,
    ownerId: '',
    controllerId: '',
    zoneId: 'pool',
    properties: card.properties,
    resources: {},
    tags: card.tags,
    isTapped: false,
    isDestroyed: false,
    line: '',
    hasMovedThisTurn: false,
    hasSummoningSickness: false,
    icon: card.icon,
    housingCost: card.housingCost,
    housingProvided: card.housingProvided,
    underConstruction: false,
    constructionProgress: 0,
  };
}

export interface DeckBuilderPanelProps {
  gameDef: GameDefinitionFull;
  deck: Record<string, number>;
  onDeckChange: (next: Record<string, number>) => void;
  hqId: string;
  heroId: string;
  // Toggle semantics: parents deselect when the current pick is passed again, and both
  // treat '' as "clear the pick" (used when the picked card leaves the deck).
  onSelectHq: (id: string) => void;
  onSelectHero: (id: string) => void;
  /**
   * Campaign mode: only these cards (the player's earned collection) appear in the pool,
   * and per-card copies are additionally capped at the owned count. Omitted (My Decks):
   * every deck-eligible card in the catalog is available.
   */
  ownedCounts?: Record<string, number>;
  hqLabel?: ReactNode;
  heroLabel?: ReactNode;
  /** Shown inside an empty HQ/hero picker so players know the options come from the deck. */
  hqEmptyHint?: ReactNode;
  heroEmptyHint?: ReactNode;
}

export function DeckBuilderPanel({
  gameDef,
  deck,
  onDeckChange,
  hqId,
  heroId,
  onSelectHq,
  onSelectHero,
  ownedCounts,
  hqLabel = '🏛 Headquarters (optional)',
  heroLabel = '★ Hero (optional)',
  hqEmptyHint = 'Add a headquarters card to your deck to pick it here.',
  heroEmptyHint = 'Add a hero card to your deck to pick it here.',
}: DeckBuilderPanelProps) {
  const [cardFilter, setCardFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [factionFilter, setFactionFilter] = useState('');
  const [unitTypeFilter, setUnitTypeFilter] = useState('');
  const [minCostFilter, setMinCostFilter] = useState('');
  const [maxCostFilter, setMaxCostFilter] = useState('');
  const [inspectId, setInspectId] = useState<string | null>(null);

  const deckRules = gameDef.deckRules;
  const objectTypes = gameDef.objectTypes ?? [];
  const maxCopies = deckRules?.maxCopies ?? 4;
  const maxDeckSize = deckRules?.maxDeckSize ?? 60;

  const cardDefs = useMemo(() => {
    const map: Record<string, CardDefinitionDto> = {};
    for (const c of gameDef.cards ?? []) map[c.id] = c;
    return map;
  }, [gameDef]);

  const deckTotal = Object.values(deck).reduce((a, b) => a + b, 0);
  const deckEntries = Object.entries(deck).filter(([, v]) => v > 0);

  // Copies allowed of one card: the game's maxCopies, further capped at the owned
  // count when building from a collection (campaign).
  function maxFor(cardId: string): number {
    if (!ownedCounts) return maxCopies;
    return Math.min(maxCopies, ownedCounts[cardId] ?? 0);
  }

  function addCard(cardId: string) {
    const inDeck = deck[cardId] ?? 0;
    if (inDeck >= maxFor(cardId) || deckTotal >= maxDeckSize) return;
    onDeckChange({ ...deck, [cardId]: inDeck + 1 });
  }

  function removeCard(cardId: string) {
    const inDeck = deck[cardId] ?? 0;
    if (inDeck <= 0) return;
    const next = { ...deck };
    if (inDeck === 1) delete next[cardId];
    else next[cardId] = inDeck - 1;
    onDeckChange(next);
  }

  // HQ/hero options come from the deck being built: headquarters-/hero-type cards
  // (via the type-hierarchy walk, so faction subtypes like nexus, graveyard-hq,
  // hive-hq, laboratory-hq, homestead-hq, caster-hero, necromancer-hero count too)
  // the player actually put copies of in this deck.
  const hqCards = useMemo(() =>
    deckEntries
      .map(([id]) => cardDefs[id])
      .filter((c): c is CardDefinitionDto => !!c && isOrExtends(c.objectType, 'headquarters', objectTypes)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [deck, cardDefs, objectTypes]);

  const heroCards = useMemo(() =>
    deckEntries
      .map(([id]) => cardDefs[id])
      .filter((c): c is CardDefinitionDto => !!c && isOrExtends(c.objectType, 'hero', objectTypes)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [deck, cardDefs, objectTypes]);

  // A picked HQ/hero whose last copy left the deck can no longer be selected — clear it
  // so no phantom pick survives. The panel only renders with a loaded gameDef, so this
  // never runs against a half-loaded catalog.
  useEffect(() => {
    if (hqId && !hqCards.some(c => c.id === hqId)) onSelectHq('');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hqId, hqCards]);

  useEffect(() => {
    if (heroId && !heroCards.some(c => c.id === heroId)) onSelectHero('');
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [heroId, heroCards]);

  // Pool: the whole deck-eligible catalog, or (campaign) only cards the player owns.
  // Heroes have no playCost (picked via heroOptions, not purchased) and some HQ subtypes
  // (e.g. hive-hq with playCosts:{}) also lack one — include both via the type hierarchy.
  const eligibleCards = useMemo(() => {
    const all = gameDef.cards ?? [];
    if (ownedCounts) return all.filter(c => (ownedCounts[c.id] ?? 0) > 0);
    return all.filter(c =>
      isDeckEligible(c)
      || isOrExtends(c.objectType, 'hero', objectTypes)
      || isOrExtends(c.objectType, 'headquarters', objectTypes)
    );
  }, [gameDef, ownedCounts, objectTypes]);

  const typeOptions = useMemo(() => {
    const typeNames: Record<string, string> = {};
    for (const t of objectTypes) typeNames[t.id] = t.name;
    const ids = Array.from(new Set(eligibleCards.map(c => c.objectType)));
    return ids
      .map(id => ({ id, name: typeNames[id] ?? id }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [eligibleCards, objectTypes]);

  // A card's faction = the canonical faction of every preconstructed deck whose
  // cards/hqOptions/heroOptions list includes its id (task 1524: multiple precon decks —
  // e.g. Blackrock Raiders and Warbond Raiders — share one real faction). A card can
  // belong to zero factions (unaffiliated) or several.
  const cardFactions = useMemo(() => {
    const map: Record<string, string[]> = {};
    for (const precon of gameDef.decks ?? []) {
      const faction = precon.faction || precon.id;
      const memberIds = new Set<string>([
        ...Object.keys(precon.cards ?? {}),
        ...(precon.hqOptions ?? []),
        ...(precon.heroOptions ?? []),
      ]);
      for (const id of memberIds) {
        const factions = (map[id] ??= []);
        if (!factions.includes(faction)) factions.push(faction);
      }
    }
    return map;
  }, [gameDef]);

  // One option per real faction (not per precon deck). The faction's display name is the
  // base deck's name (the deck whose id equals the faction id, e.g. "raiders" -> Blackrock
  // Raiders); decks without a faction field fall back to being their own option.
  const factionOptions = useMemo(() => {
    const labels = new Map<string, string>();
    for (const d of gameDef.decks ?? []) {
      const faction = d.faction || d.id;
      if (!labels.has(faction) || d.id === faction) {
        labels.set(faction, d.id === faction ? d.name : faction);
      }
    }
    return [
      ...Array.from(labels, ([id, name]) => ({ id, name })),
      { id: UNAFFILIATED_FACTION, name: 'Unaffiliated' },
    ];
  }, [gameDef]);

  const unitTypeOptions = useMemo(() => {
    const present = new Set<string>();
    for (const c of eligibleCards) {
      for (const t of c.tags) {
        if (UNIT_TYPE_TAG_LABELS[t]) present.add(t);
      }
    }
    return Array.from(present)
      .map(tag => ({ tag, name: UNIT_TYPE_TAG_LABELS[tag] }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [eligibleCards]);

  const pool = useMemo(() => {
    const filter = cardFilter.trim().toLowerCase();
    const minCost = minCostFilter.trim() === '' ? null : Number(minCostFilter);
    const maxCost = maxCostFilter.trim() === '' ? null : Number(maxCostFilter);
    return eligibleCards
      .filter(c => !filter || c.name.toLowerCase().includes(filter))
      .filter(c => !typeFilter || isOrExtends(c.objectType, typeFilter, objectTypes))
      .filter(c => {
        if (!factionFilter) return true;
        const factions = cardFactions[c.id] ?? [];
        if (factionFilter === UNAFFILIATED_FACTION) return factions.length === 0;
        return factions.includes(factionFilter);
      })
      .filter(c => !unitTypeFilter || c.tags.includes(unitTypeFilter))
      .filter(c => {
        if (minCost === null && maxCost === null) return true;
        const cost = costOf(c);
        if (cost === null) return false;
        if (minCost !== null && cost < minCost) return false;
        if (maxCost !== null && cost > maxCost) return false;
        return true;
      })
      .sort((a, b) => a.objectType.localeCompare(b.objectType) || a.name.localeCompare(b.name));
  }, [eligibleCards, cardFilter, typeFilter, factionFilter, unitTypeFilter, cardFactions, minCostFilter, maxCostFilter]);

  const filtersActive =
    !!cardFilter || !!typeFilter || !!factionFilter || !!unitTypeFilter || !!minCostFilter || !!maxCostFilter;

  function clearFilters() {
    setCardFilter('');
    setTypeFilter('');
    setFactionFilter('');
    setUnitTypeFilter('');
    setMinCostFilter('');
    setMaxCostFilter('');
  }

  function cardName(id: string): string {
    return cardDefs[id]?.name ?? id;
  }

  return (
    <>
      <div className="deck-picker-section">
        <div className="deck-picker-label">{hqLabel}</div>
        <div className="deck-picker-row">
          {hqCards.length === 0 && <span className="deck-hint">{hqEmptyHint}</span>}
          {hqCards.map(c => (
            <div
              key={c.id}
              className={`deck-picker-card ${hqId === c.id ? 'deck-picker-selected' : ''}`}
              onClick={() => onSelectHq(c.id)}
              title={c.id}
            >
              <span className="deck-picker-icon">{c.icon ?? '🏛'}</span>
              <span className="deck-picker-name">{c.name}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="deck-picker-section">
        <div className="deck-picker-label">{heroLabel}</div>
        <div className="deck-picker-row">
          {heroCards.length === 0 && <span className="deck-hint">{heroEmptyHint}</span>}
          {heroCards.map(c => (
            <div
              key={c.id}
              className={`deck-picker-card ${heroId === c.id ? 'deck-picker-selected' : ''}`}
              onClick={() => onSelectHero(c.id)}
              title={c.id}
            >
              <span className="deck-picker-icon">{c.icon ?? '★'}</span>
              <span className="deck-picker-name">{c.name}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="deck-editor-layout">
        <div className="deck-editor-current">
          <div className="deck-section-title">This deck — click a card to inspect it, click ➖ to remove</div>
          {deckEntries.length === 0 && (
            <div className="deck-empty">Empty — add cards from the pool below.</div>
          )}
          <div className="deck-card-grid">
            {deckEntries
              .sort(([a], [b]) => cardName(a).localeCompare(cardName(b)))
              .map(([id, count]) => {
                const def = cardDefs[id];
                if (!def) return null;
                return (
                  <CardView
                    key={id}
                    card={toObjectState(def)}
                    actions={[]}
                    isSelectableTarget={false}
                    isSelectedTarget={false}
                    playCost={def.playCost}
                    playCostResource={def.playCostResource}
                    onAction={() => {}}
                    onSelectTarget={() => {}}
                    onInspect={() => setInspectId(id)}
                    deckControl={{
                      count,
                      canAdd: count < maxFor(id) && deckTotal < maxDeckSize,
                      onAdd: () => addCard(id),
                      onRemove: () => removeCard(id),
                    }}
                  />
                );
              })}
          </div>
        </div>

        <div className="deck-collection">
          <div className="deck-section-title">Card pool — click a card to inspect it, click ➕ to add</div>
          <input
            className="player-name-input deck-filter"
            placeholder="Search cards..."
            value={cardFilter}
            onChange={e => setCardFilter(e.target.value)}
          />
          <div className="deck-filter-row">
            <select
              className="deck-select"
              value={typeFilter}
              onChange={e => setTypeFilter(e.target.value)}
            >
              <option value="">All types</option>
              {typeOptions.map(t => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
            <select
              className="deck-select"
              value={factionFilter}
              onChange={e => setFactionFilter(e.target.value)}
            >
              <option value="">All factions</option>
              {factionOptions.map(f => (
                <option key={f.id} value={f.id}>{f.name}</option>
              ))}
            </select>
            <select
              className="deck-select"
              value={unitTypeFilter}
              onChange={e => setUnitTypeFilter(e.target.value)}
            >
              <option value="">All unit types</option>
              {unitTypeOptions.map(u => (
                <option key={u.tag} value={u.tag}>{u.name}</option>
              ))}
            </select>
            <input
              className="player-name-input deck-filter-cost"
              type="number"
              min={0}
              placeholder="Min cost"
              value={minCostFilter}
              onChange={e => setMinCostFilter(e.target.value)}
            />
            <input
              className="player-name-input deck-filter-cost"
              type="number"
              min={0}
              placeholder="Max cost"
              value={maxCostFilter}
              onChange={e => setMaxCostFilter(e.target.value)}
            />
            {filtersActive && (
              <button className="back-btn deck-clear-btn" onClick={clearFilters}>Clear filters</button>
            )}
          </div>
          <div className="deck-card-grid">
            {pool.map(card => {
              const inDeck = deck[card.id] ?? 0;
              const canAdd = inDeck < maxFor(card.id) && deckTotal < maxDeckSize;
              return (
                <CardView
                  key={card.id}
                  card={toObjectState(card)}
                  actions={[]}
                  isSelectableTarget={false}
                  isSelectedTarget={false}
                  playCost={card.playCost}
                  playCostResource={card.playCostResource}
                  onAction={() => {}}
                  onSelectTarget={() => {}}
                  onInspect={() => setInspectId(card.id)}
                  deckControl={{
                    count: inDeck,
                    canAdd,
                    onAdd: () => addCard(card.id),
                    onRemove: () => removeCard(card.id),
                  }}
                />
              );
            })}
          </div>
        </div>
      </div>

      {inspectId && cardDefs[inspectId] && (() => {
        const def = cardDefs[inspectId];
        const inDeck = deck[inspectId] ?? 0;
        const canAdd = inDeck < maxFor(inspectId) && deckTotal < maxDeckSize;
        return (
          <CardDetailModal
            card={toObjectState(def)}
            def={def}
            attachments={[]}
            nameOf={cardName}
            actions={[]}
            onAction={() => {}}
            onClose={() => setInspectId(null)}
            onInspect={() => {}}
            deckControl={{
              count: inDeck,
              canAdd,
              onAdd: () => addCard(inspectId),
              onRemove: () => removeCard(inspectId),
            }}
          />
        );
      })()}
    </>
  );
}

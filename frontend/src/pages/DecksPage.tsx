import { useEffect, useMemo, useState, useCallback } from 'react';
import { GameDefinitionFull, CardDefinitionDto, ObjectStateDto, ObjectTypeDto, SavedDeck } from '../types/game';
import { BASE } from '../config';
import { CardView } from '../components/CardView';
import { CardDetailModal } from '../components/CardDetailModal';

interface DecksPageProps {
  onOpenLobby: () => void;
}

const GAME_ID = 'town-tcg';

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

function isDeckEligible(card: CardDefinitionDto): boolean {
  return (card.playCost !== null && card.playCost !== undefined)
    || !!(card.playCosts && Object.keys(card.playCosts).length > 0);
}

function deckSize(deck: Record<string, number>): number {
  return Object.values(deck).reduce((a, b) => a + b, 0);
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

export function DecksPage({ onOpenLobby }: DecksPageProps) {
  const [gameDef, setGameDef] = useState<GameDefinitionFull | null>(null);
  const [decks, setDecks] = useState<SavedDeck[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  // null = viewing the list; '' = building a brand-new deck; a deck id = editing that deck
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editHq, setEditHq] = useState('');
  const [editHero, setEditHero] = useState('');
  const [editCards, setEditCards] = useState<Record<string, number>>({});
  const [cardFilter, setCardFilter] = useState('');
  const [inspectId, setInspectId] = useState<string | null>(null);

  useEffect(() => {
    fetch(`${BASE}api/definitions/${GAME_ID}`)
      .then(r => r.json())
      .then((def: GameDefinitionFull) => setGameDef(def))
      .catch(() => setError('Failed to load the card catalog. Is the backend running?'));
  }, []);

  const loadDecks = useCallback(() => {
    setLoading(true);
    fetch(`${BASE}api/decks?gameId=${GAME_ID}`, { credentials: 'include' })
      .then(r => r.json())
      .then(data => setDecks(data.decks ?? []))
      .catch(() => setError('Failed to load your decks. Is the backend running?'))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => { loadDecks(); }, [loadDecks]);

  const deckRules = gameDef?.deckRules;
  const objectTypes = gameDef?.objectTypes ?? [];
  const cardDefs = useMemo(() => {
    const map: Record<string, CardDefinitionDto> = {};
    for (const c of gameDef?.cards ?? []) map[c.id] = c;
    return map;
  }, [gameDef]);

  const hqCards = useMemo(
    () => (gameDef?.cards ?? []).filter(c => isOrExtends(c.objectType, 'headquarters', objectTypes)),
    [gameDef, objectTypes],
  );
  const heroCards = useMemo(
    () => (gameDef?.cards ?? []).filter(c => isOrExtends(c.objectType, 'hero', objectTypes)),
    [gameDef, objectTypes],
  );

  const pool = useMemo(() => {
    const filter = cardFilter.trim().toLowerCase();
    return (gameDef?.cards ?? [])
      .filter(isDeckEligible)
      .filter(c => !filter || c.name.toLowerCase().includes(filter))
      .sort((a, b) => a.objectType.localeCompare(b.objectType) || a.name.localeCompare(b.name));
  }, [gameDef, cardFilter]);

  const maxCopies = deckRules?.maxCopies ?? 4;
  const maxDeckSize = deckRules?.maxDeckSize ?? 60;
  const minDeckSize = deckRules?.minDeckSize ?? 40;
  const editTotal = deckSize(editCards);
  const editEntries = Object.entries(editCards).filter(([, v]) => v > 0);

  function startNew() {
    setEditingId('');
    setEditName('');
    setEditHq('');
    setEditHero('');
    setEditCards({});
    setCardFilter('');
    setError('');
  }

  function startEdit(deck: SavedDeck) {
    setEditingId(deck.id);
    setEditName(deck.name);
    setEditHq(deck.hqId ?? '');
    setEditHero(deck.heroId ?? '');
    setEditCards({ ...deck.cards });
    setCardFilter('');
    setError('');
  }

  function cancelEdit() {
    setEditingId(null);
    setInspectId(null);
    setError('');
  }

  function addCard(cardId: string) {
    const inDeck = editCards[cardId] ?? 0;
    if (inDeck >= maxCopies || editTotal >= maxDeckSize) return;
    setEditCards({ ...editCards, [cardId]: inDeck + 1 });
  }

  function removeCard(cardId: string) {
    const inDeck = editCards[cardId] ?? 0;
    if (inDeck <= 0) return;
    const next = { ...editCards };
    if (inDeck === 1) delete next[cardId];
    else next[cardId] = inDeck - 1;
    setEditCards(next);
  }

  function selectHq(id: string) {
    setEditHq(prev => (prev === id ? '' : id));
  }

  function selectHero(id: string) {
    setEditHero(prev => (prev === id ? '' : id));
  }

  const nameValid = editName.trim().length > 0;
  const sizeValid = editTotal >= minDeckSize && editTotal <= maxDeckSize;
  const canSave = nameValid && sizeValid && !saving;

  async function saveEditing() {
    if (!canSave || editingId === null) return;
    setSaving(true);
    setError('');
    try {
      const isNew = editingId === '';
      const res = await fetch(`${BASE}api/decks${isNew ? '' : `/${editingId}`}`, {
        method: isNew ? 'POST' : 'PUT',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          gameId: GAME_ID,
          name: editName.trim(),
          hqId: editHq || null,
          heroId: editHero || null,
          cards: editCards,
        }),
      });
      if (!res.ok) throw new Error((await res.text()) || 'Failed to save the deck');
      setEditingId(null);
      loadDecks();
    } catch (err: any) {
      setError(err.message ?? 'Unknown error');
    } finally {
      setSaving(false);
    }
  }

  async function deleteDeck(deck: SavedDeck) {
    if (!window.confirm(`Delete "${deck.name}"? This cannot be undone.`)) return;
    setError('');
    try {
      const res = await fetch(`${BASE}api/decks/${deck.id}`, {
        method: 'DELETE',
        credentials: 'include',
      });
      if (!res.ok && res.status !== 204) throw new Error((await res.text()) || 'Failed to delete the deck');
      if (editingId === deck.id) setEditingId(null);
      loadDecks();
    } catch (err: any) {
      setError(err.message ?? 'Unknown error');
    }
  }

  function cardIcon(id: string): string {
    return cardDefs[id]?.icon ?? '🃏';
  }
  function cardName(id: string): string {
    return cardDefs[id]?.name ?? id;
  }

  return (
    <div className="lobby-page">
      <div className="lobby-card lobby-wide campaign-card">
        <h1 className="lobby-title">Town Wars</h1>
        <p className="lobby-subtitle">
          {editingId !== null
            ? (editingId === '' ? 'Build a new deck' : `Editing "${editName}"`)
            : 'My Decks — build, save, and reuse named decks'}
        </p>

        {error && <div className="error-box">{error}</div>}

        {editingId === null && (
          <>
            <button className="campaign-btn" onClick={startNew}>🃏 New Deck</button>

            {loading && <div className="deck-hint">Loading your decks...</div>}

            {!loading && decks.length === 0 && (
              <div className="deck-empty">No saved decks yet — build one to get started.</div>
            )}

            <div className="deck-list">
              {decks.map(d => (
                <div key={d.id} className="deck-list-item" style={{ cursor: 'default' }}>
                  <span className="deck-card-icon">{d.hqId ? cardIcon(d.hqId) : '🃏'}</span>
                  <span className="deck-card-name">{d.name}</span>
                  <span className="deck-list-count">{deckSize(d.cards)} cards</span>
                  <button className="back-btn" onClick={() => startEdit(d)}>Edit</button>
                  <button className="back-btn" onClick={() => deleteDeck(d)}>Delete</button>
                </div>
              ))}
            </div>
          </>
        )}

        {editingId !== null && (
          <div className="deck-builder">
            <div className="form-group">
              <label>Deck name</label>
              <input
                type="text"
                className="player-name-input"
                value={editName}
                onChange={e => setEditName(e.target.value)}
                placeholder="Name this deck"
              />
            </div>

            <div className="deck-builder-header">
              <div className={`deck-size-indicator ${sizeValid ? '' : 'deck-warning'}`}>
                Deck: <strong>{editTotal}</strong> / {minDeckSize}-{maxDeckSize} cards, max {maxCopies} copies each
                {!sizeValid && (
                  <span className="deck-warning">
                    {' '}⚠ {editTotal < minDeckSize ? `needs at least ${minDeckSize}` : `over the ${maxDeckSize} limit`}
                  </span>
                )}
              </div>
            </div>

            <div className="deck-picker-section">
              <div className="deck-picker-label">🏛 Headquarters (optional)</div>
              <div className="deck-picker-row">
                {hqCards.map(c => (
                  <div
                    key={c.id}
                    className={`deck-picker-card ${editHq === c.id ? 'deck-picker-selected' : ''}`}
                    onClick={() => selectHq(c.id)}
                    title={c.id}
                  >
                    <span className="deck-picker-icon">{c.icon ?? '🏛'}</span>
                    <span className="deck-picker-name">{c.name}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="deck-picker-section">
              <div className="deck-picker-label">★ Hero (optional)</div>
              <div className="deck-picker-row">
                {heroCards.map(c => (
                  <div
                    key={c.id}
                    className={`deck-picker-card ${editHero === c.id ? 'deck-picker-selected' : ''}`}
                    onClick={() => selectHero(c.id)}
                    title={c.id}
                  >
                    <span className="deck-picker-icon">{c.icon ?? '★'}</span>
                    <span className="deck-picker-name">{c.name}</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="deck-builder-columns">
              <div className="deck-collection">
                <div className="deck-section-title">Card pool — click a card to inspect it, click ➕ to add</div>
                <input
                  className="player-name-input deck-filter"
                  placeholder="Search cards..."
                  value={cardFilter}
                  onChange={e => setCardFilter(e.target.value)}
                />
                <div className="deck-card-grid">
                  {pool.map(card => {
                    const inDeck = editCards[card.id] ?? 0;
                    const canAdd = inDeck < maxCopies && editTotal < maxDeckSize;
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

              <div className="deck-current">
                <div className="deck-section-title">This deck — click a card to inspect it, click ➖ to remove</div>
                {editEntries.length === 0 && (
                  <div className="deck-empty">Empty — add cards from the pool on the left.</div>
                )}
                <div className="deck-card-grid">
                  {editEntries
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
                            canAdd: count < maxCopies && editTotal < maxDeckSize,
                            onAdd: () => addCard(id),
                            onRemove: () => removeCard(id),
                          }}
                        />
                      );
                    })}
                </div>
              </div>
            </div>

            {inspectId && cardDefs[inspectId] && (() => {
              const def = cardDefs[inspectId];
              const inDeck = editCards[inspectId] ?? 0;
              const canAdd = inDeck < maxCopies && editTotal < maxDeckSize;
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

            <div className="mode-select">
              <button className="start-btn" disabled={!canSave} onClick={saveEditing}>
                {saving ? 'Saving...' : 'Save Deck'}
              </button>
              <button className="back-btn" onClick={cancelEdit}>Cancel</button>
            </div>
          </div>
        )}

        {editingId === null && (
          <button className="back-btn" onClick={onOpenLobby} style={{ marginTop: 16 }}>
            ⚔️ Back to Lobby
          </button>
        )}
      </div>
    </div>
  );
}

import { useEffect, useState, useCallback } from 'react';
import { GameDefinitionFull, SavedDeck } from '../types/game';
import { BASE } from '../config';
import { DeckBuilderPanel } from '../components/DeckBuilderPanel';

interface DecksPageProps {
  onOpenLobby: () => void;
}

const GAME_ID = 'town-tcg';

function deckSize(deck: Record<string, number>): number {
  return Object.values(deck).reduce((a, b) => a + b, 0);
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
  const maxCopies = deckRules?.maxCopies ?? 4;
  const maxDeckSize = deckRules?.maxDeckSize ?? 60;
  const minDeckSize = deckRules?.minDeckSize ?? 40;
  const editTotal = deckSize(editCards);

  function startNew() {
    setEditingId('');
    setEditName('');
    setEditHq('');
    setEditHero('');
    setEditCards({});
    setError('');
  }

  function startEdit(deck: SavedDeck) {
    setEditingId(deck.id);
    setEditName(deck.name);
    setEditHq(deck.hqId ?? '');
    setEditHero(deck.heroId ?? '');
    setEditCards({ ...deck.cards });
    setError('');
  }

  function cancelEdit() {
    setEditingId(null);
    setError('');
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
    return gameDef?.cards.find(c => c.id === id)?.icon ?? '🃏';
  }

  return (
    <div className="lobby-page">
      <div className={`lobby-card lobby-wide ${editingId !== null ? 'deck-page-wide' : 'campaign-card'}`}>
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

        {editingId !== null && gameDef && (
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

            <DeckBuilderPanel
              gameDef={gameDef}
              deck={editCards}
              onDeckChange={setEditCards}
              hqId={editHq}
              heroId={editHero}
              onSelectHq={selectHq}
              onSelectHero={selectHero}
            />

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

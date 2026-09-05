import React, { useEffect, useState } from 'react';
import {
  GameDefinitionSummary,
  GameDefinitionFull,
  CardDefinitionDto,
  CreateMatchResponse,
  SavedDeck,
  ObjectTypeDto,
} from '../types/game';
import { BASE } from '../config';
import { isDeckEligible } from '../utils/deckEligibility';

interface LobbyPageProps {
  onMatchCreated: (matchId: string, seat: string) => void;
  onOpenCampaign: () => void;
  onOpenDecks: () => void;
  // Account-level privilege: only admin accounts see the per-player Admin toggle
  // (the server refuses admin seats from non-admin accounts regardless).
  canUseAdminMode: boolean;
  // Pre-selects the match mode — used by the "Play vs Computer" landing-screen entry point
  // so it drops straight into bot mode instead of defaulting to hotseat.
  initialMode?: 'hotseat' | 'seats' | 'bot';
}

interface PlayerDeckState {
  name: string;
  isAdmin: boolean;
  deckId: string;
  hqId: string;
  heroId: string;
  deck: Record<string, number>;
}

function statsSummary(card: CardDefinitionDto): string {
  const parts: string[] = [];
  const atk = card.properties?.['attack'];
  const hp = card.properties?.['maxHp'];
  const armor = card.properties?.['armor'];
  if (atk) parts.push(`⚔${atk}`);
  if (hp) parts.push(`❤${hp}`);
  if (armor) parts.push(`🛡${armor}`);
  return parts.join(' ');
}

function typeLabel(objectType: string): string {
  switch (objectType) {
    case 'hero': return 'Hero';
    case 'headquarters': return 'HQ';
    case 'unit': return 'Unit';
    case 'building': return 'Building';
    case 'spell': return 'Spell';
    default: return objectType;
  }
}

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

// Hero-/headquarters-type cards the player has actually put copies of in their own
// deck — these are the "reserve" picks a Hero/HQ dropdown may offer beyond the
// faction's default.
function reserveOptions(deck: Record<string, number>, fullDef: GameDefinitionFull, root: string): CardDefinitionDto[] {
  const types = fullDef.objectTypes ?? [];
  return Object.entries(deck)
    .filter(([, count]) => count > 0)
    .map(([id]) => fullDef.cards.find(c => c.id === id))
    .filter((c): c is CardDefinitionDto => !!c && isOrExtends(c.objectType, root, types));
}

export function LobbyPage({ onMatchCreated, onOpenCampaign, onOpenDecks, canUseAdminMode, initialMode }: LobbyPageProps) {
  const [definitions, setDefinitions] = useState<GameDefinitionSummary[]>([]);
  const [selectedGame, setSelectedGame] = useState('');
  const [fullDef, setFullDef] = useState<GameDefinitionFull | null>(null);
  const [players, setPlayers] = useState<PlayerDeckState[]>([
    { name: 'Player 1', isAdmin: false, deckId: '', hqId: '', heroId: '', deck: {} },
    { name: 'Player 2', isAdmin: false, deckId: '', hqId: '', heroId: '', deck: {} },
  ]);

  function addPlayer() {
    if (players.length >= 8) return;
    const n = players.length + 1;
    setPlayers(prev => [...prev, { name: `Player ${n}`, isAdmin: false, deckId: '', hqId: '', heroId: '', deck: {} }]);
  }

  function removePlayer() {
    if (players.length <= 2) return;
    setPlayers(prev => prev.slice(0, -1));
  }
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [mode, setMode] = useState<'hotseat' | 'seats' | 'bot'>(initialMode ?? 'hotseat');
  const [savedDecks, setSavedDecks] = useState<SavedDeck[]>([]);

  useEffect(() => {
    fetch(`${BASE}api/definitions`)
      .then(r => r.json())
      .then((data: GameDefinitionSummary[]) => {
        setDefinitions(data);
        if (data.length > 0) setSelectedGame(data[0].id);
      })
      .catch(() => setError('Failed to load game definitions. Is the backend running?'));
  }, []);

  // Saved decks are scoped to the logged-in account (same identity as the Campaign
  // builder and My Decks page) so a deck built there can be picked here.
  useEffect(() => {
    if (!selectedGame) return;
    fetch(`${BASE}api/decks?gameId=${selectedGame}`, { credentials: 'include' })
      .then(r => r.json())
      .then(data => setSavedDecks(data.decks ?? []))
      .catch(() => setSavedDecks([]));
  }, [selectedGame]);

  useEffect(() => {
    if (!selectedGame) return;
    fetch(`${BASE}api/definitions/${selectedGame}`)
      .then(r => r.json())
      .then((def: GameDefinitionFull) => {
        setFullDef(def);
        // Seed each player with a precon deck (P1 gets the first, P2 the second, etc.)
        setPlayers(prev => prev.map((p, i) => {
          const precon = def.decks[i % Math.max(def.decks.length, 1)];
          if (precon) return { ...p, deckId: precon.id, hqId: precon.hq, heroId: precon.hero, deck: { ...precon.cards } };
          return { ...p, deckId: '', hqId: '', heroId: '', deck: { ...(def.deckRules?.defaultDeck ?? {}) } };
        }));
      })
      .catch(() => setError('Failed to load game definition details'));
  }, [selectedGame]);

  const deckRules = fullDef?.deckRules;

  function cardPool(isAdmin: boolean): CardDefinitionDto[] {
    if (!fullDef) return [];
    return isAdmin
      ? fullDef.cards
      : fullDef.cards.filter(isDeckEligible);
  }

  function deckSize(deck: Record<string, number>): number {
    return Object.values(deck).reduce((a, b) => a + b, 0);
  }

  function setCount(playerIdx: number, cardId: string, delta: number) {
    setPlayers(prev => prev.map((p, i) => {
      if (i !== playerIdx) return p;
      const current = p.deck[cardId] ?? 0;
      let next = current + delta;
      if (next < 0) next = 0;
      if (!p.isAdmin && deckRules) {
        if (next > deckRules.maxCopies) next = deckRules.maxCopies;
        if (delta > 0 && deckSize(p.deck) >= deckRules.maxDeckSize) return p;
      }
      const deck = { ...p.deck };
      if (next === 0) delete deck[cardId];
      else deck[cardId] = next;

      // If the deck copy backing the currently-picked Hero/HQ just ran out, fall back
      // to this faction's default — unless the pick is also a faction alternate
      // (hqOptions/heroOptions), which stays valid without a deck copy.
      const precon = fullDef?.decks.find(d => d.id === p.deckId);
      let hqId = p.hqId;
      let heroId = p.heroId;
      if (next === 0 && cardId === hqId && cardId !== precon?.hq && !precon?.hqOptions?.includes(cardId))
        hqId = precon?.hq ?? '';
      if (next === 0 && cardId === heroId && cardId !== precon?.hero && !precon?.heroOptions?.includes(cardId))
        heroId = precon?.hero ?? '';

      return { ...p, deck, hqId, heroId };
    }));
  }

  function setName(playerIdx: number, name: string) {
    setPlayers(prev => prev.map((p, i) => (i === playerIdx ? { ...p, name } : p)));
  }

  function selectDeck(playerIdx: number, deckId: string) {
    const precon = fullDef?.decks.find(d => d.id === deckId);
    setPlayers(prev => prev.map((p, i) => {
      if (i !== playerIdx) return p;
      return {
        ...p, deckId,
        hqId: precon?.hq ?? p.hqId,
        heroId: precon?.hero ?? p.heroId,
        deck: precon ? { ...precon.cards } : p.deck,
      };
    }));
  }

  function selectSavedDeck(playerIdx: number, deckId: string) {
    const saved = savedDecks.find(d => d.id === deckId);
    if (!saved) return;
    setPlayers(prev => prev.map((p, i) => {
      if (i !== playerIdx) return p;
      return {
        ...p,
        deckId: '', // this is a custom saved deck, not one of the precons
        hqId: saved.hqId ?? '',
        heroId: saved.heroId ?? '',
        deck: { ...saved.cards },
      };
    }));
  }

  function setPlayerField(playerIdx: number, field: 'hqId' | 'heroId', value: string) {
    setPlayers(prev => prev.map((p, i) => (i === playerIdx ? { ...p, [field]: value } : p)));
  }

  function toggleAdmin(playerIdx: number) {
    setPlayers(prev => prev.map((p, i) => {
      if (i !== playerIdx) return p;
      const isAdmin = !p.isAdmin;
      let deck = p.deck;
      // When leaving admin mode, clamp the deck back to legal limits
      if (!isAdmin && deckRules && fullDef) {
        deck = {};
        let total = 0;
        for (const [cardId, count] of Object.entries(p.deck)) {
          const card = fullDef.cards.find(c => c.id === cardId);
          if (!card || card.playCost === null || card.playCost === undefined) continue;
          const clamped = Math.min(count, deckRules.maxCopies, deckRules.maxDeckSize - total);
          if (clamped <= 0) continue;
          deck[cardId] = clamped;
          total += clamped;
        }
      }

      // Clamping may have dropped the reserve copy backing the current Hero/HQ pick —
      // fall back to this faction's default, unless the pick is a faction alternate
      // (hqOptions/heroOptions) or no faction is selected (custom saved deck).
      const precon = fullDef?.decks.find(d => d.id === p.deckId);
      let hqId = p.hqId;
      let heroId = p.heroId;
      if (precon && hqId && hqId !== precon.hq && !precon.hqOptions?.includes(hqId) && !(deck[hqId] > 0))
        hqId = precon.hq ?? '';
      if (precon && heroId && heroId !== precon.hero && !precon.heroOptions?.includes(heroId) && !(deck[heroId] > 0))
        heroId = precon.hero ?? '';

      return { ...p, isAdmin, deck, hqId, heroId };
    }));
  }

  async function handleStart() {
    if (!selectedGame) return;
    setLoading(true);
    setError('');

    try {
      const res = await fetch(`${BASE}api/matches`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          gameId: selectedGame,
          players: players.map((p, i) => ({
            id: `p${i + 1}`,
            name: mode === 'bot' && i > 0 ? (players.length === 2 ? 'Computer' : `Computer ${i}`) : p.name,
            deckId: p.deckId || undefined,
            hqId: p.hqId || undefined,
            heroId: p.heroId || undefined,
            deck: p.deck,
            isAdmin: p.isAdmin,
            isBot: mode === 'bot' && i > 0,
          })),
        }),
      });

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(txt || 'Failed to create match');
      }

      const data: CreateMatchResponse = await res.json();
      onMatchCreated(data.matchId, mode === 'hotseat' ? '' : 'p1');
    } catch (err: any) {
      setError(err.message ?? 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  const namesValid = players.every(p => p.name.trim().length > 0);

  return (
    <div className="lobby-page">
      <div className="lobby-card lobby-wide">
        <h1 className="lobby-title">Town Wars</h1>
        <p className="lobby-subtitle">Trading Card Game Engine</p>

        <button className="campaign-btn" onClick={onOpenCampaign}>
          🏰 Campaign — learn the game mission by mission
        </button>
        <button className="campaign-btn" onClick={onOpenDecks}>
          🃏 My Decks — build and manage saved decks
        </button>

        {error && <div className="error-box">{error}</div>}

        <div className="form-group">
          <label>Game</label>
          <select value={selectedGame} onChange={e => setSelectedGame(e.target.value)}>
            {definitions.length === 0 && <option value="">Loading...</option>}
            {definitions.map(d => (
              <option key={d.id} value={d.id}>
                {d.name} v{d.version}
              </option>
            ))}
          </select>
        </div>

        {deckRules && (
          <div className="lobby-note">
            <strong>Deck rules:</strong> max {deckRules.maxDeckSize} cards,
            max {deckRules.maxCopies} copies per card.
            Admins ignore all limits and can use every card.
          </div>
        )}

        <div className="player-count-row">
          <span className="player-count-label">Players: {players.length}</span>
          <button className="count-btn" onClick={removePlayer} disabled={players.length <= 2}>−</button>
          <button className="count-btn" onClick={addPlayer} disabled={players.length >= 8}>+</button>
          <span className="player-count-hint">2–8 players, ring FFA</span>
        </div>

        <div className="deck-builders">
          {players.map((player, idx) => {
            const pool = cardPool(player.isAdmin);
            const size = deckSize(player.deck);
            const overLimit = !player.isAdmin && deckRules && size > deckRules.maxDeckSize;
            return (
              <div key={idx} className="deck-builder">
                <div className="deck-builder-header">
                  <input
                    type="text"
                    className="player-name-input"
                    value={player.name}
                    onChange={e => setName(idx, e.target.value)}
                    placeholder={`Player ${idx + 1}`}
                  />
                  {canUseAdminMode && (
                    <label className="admin-toggle">
                      <input
                        type="checkbox"
                        checked={player.isAdmin}
                        onChange={() => toggleAdmin(idx)}
                      />
                      Admin
                    </label>
                  )}
                </div>

                {fullDef && fullDef.decks.length > 0 && (
                  <div className="deck-select-row">
                    <select
                      className="deck-select"
                      value={player.deckId}
                      onChange={e => selectDeck(idx, e.target.value)}
                    >
                      {fullDef.decks.map(d => (
                        <option key={d.id} value={d.id}>{d.name}</option>
                      ))}
                    </select>
                    {(() => {
                      const cardName = (id: string) => fullDef.cards.find(c => c.id === id)?.name ?? id;
                      // Hero/HQ options: the faction's default and alternates (hqOptions/
                      // heroOptions), plus any hero-/headquarters-type card the player has
                      // actually added to their own deck (a reserve copy with a play cost).
                      const precon = fullDef.decks.find(d => d.id === player.deckId);
                      const factionHqs = precon
                        ? (precon.hqOptions?.length ? precon.hqOptions : [precon.hq])
                        : [];
                      const factionHeroes = precon
                        ? (precon.heroOptions?.length ? precon.heroOptions : [precon.hero])
                        : [];
                      const hqOptionIds = Array.from(new Set(
                        [player.hqId, ...factionHqs, ...reserveOptions(player.deck, fullDef, 'headquarters').map(c => c.id)]
                          .filter((id): id is string => !!id)
                      ));
                      const heroOptionIds = Array.from(new Set(
                        [player.heroId, ...factionHeroes, ...reserveOptions(player.deck, fullDef, 'hero').map(c => c.id)]
                          .filter((id): id is string => !!id)
                      ));
                      return (
                        <div className="deck-precon-info">
                          <select
                            className="hq-select"
                            value={player.hqId}
                            onChange={e => setPlayerField(idx, 'hqId', e.target.value)}
                          >
                            {hqOptionIds.map(id => <option key={id} value={id}>🏛 {cardName(id)}</option>)}
                          </select>
                          <select
                            className="hq-select"
                            value={player.heroId}
                            onChange={e => setPlayerField(idx, 'heroId', e.target.value)}
                          >
                            {heroOptionIds.map(id => <option key={id} value={id}>★ {cardName(id)}</option>)}
                          </select>
                        </div>
                      );
                    })()}
                  </div>
                )}

                {savedDecks.length > 0 && (
                  <div className="deck-select-row">
                    <select
                      className="deck-select"
                      value=""
                      onChange={e => { if (e.target.value) selectSavedDeck(idx, e.target.value); }}
                    >
                      <option value="">🃏 Load a saved deck...</option>
                      {savedDecks.map(d => (
                        <option key={d.id} value={d.id}>{d.name} ({deckSize(d.cards)})</option>
                      ))}
                    </select>
                  </div>
                )}

                <div className={`deck-size ${overLimit ? 'over-limit' : ''}`}>
                  Deck: {size}{!player.isAdmin && deckRules ? ` / ${deckRules.maxDeckSize}` : ''} cards
                  {player.isAdmin && <span className="admin-badge">ADMIN — no limits</span>}
                </div>

                <div className="card-pool">
                  {pool.map(card => {
                    const count = player.deck[card.id] ?? 0;
                    const maxed = !player.isAdmin && deckRules
                      ? count >= deckRules.maxCopies || size >= deckRules.maxDeckSize
                      : false;
                    return (
                      <div key={card.id} className={`pool-card ${count > 0 ? 'in-deck' : ''}`}>
                        <div className="pool-card-info">
                          <span className="pool-card-name">
                            {card.icon ? `${card.icon} ` : ''}{card.name}
                          </span>
                          <span className="pool-card-meta">
                            {typeLabel(card.objectType)}
                            {card.playCost !== null && card.playCost !== undefined
                              ? ` · ${card.playCost}g`
                              : ' · free'}
                            {statsSummary(card) ? ` · ${statsSummary(card)}` : ''}
                          </span>
                          {(card.abilities?.length > 0 || card.onPlay) && (
                            <span className="pool-card-abilities">
                              {card.onPlay
                                ? `On play: ${card.onPlay.name}`
                                : card.abilities.map(a => a.name).join(', ')}
                            </span>
                          )}
                        </div>
                        <div className="pool-card-controls">
                          <button
                            className="count-btn"
                            disabled={count === 0}
                            onClick={() => setCount(idx, card.id, -1)}
                          >
                            −
                          </button>
                          <span className="pool-card-count">{count}</span>
                          <button
                            className="count-btn"
                            disabled={maxed}
                            onClick={() => setCount(idx, card.id, 1)}
                          >
                            +
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>

        <div className="mode-select">
          <label className={`mode-option ${mode === 'hotseat' ? 'mode-active' : ''}`}>
            <input
              type="radio"
              name="mode"
              checked={mode === 'hotseat'}
              onChange={() => setMode('hotseat')}
            />
            <strong>Hotseat</strong> — both players share this screen and take turns
          </label>
          <label className={`mode-option ${mode === 'seats' ? 'mode-active' : ''}`}>
            <input
              type="radio"
              name="mode"
              checked={mode === 'seats'}
              onChange={() => setMode('seats')}
            />
            <strong>Multiple browsers</strong> — you play as Player 1; each other player gets their own invite link. Hands stay hidden.
          </label>
          <label className={`mode-option ${mode === 'bot' ? 'mode-active' : ''}`}>
            <input
              type="radio"
              name="mode"
              checked={mode === 'bot'}
              onChange={() => setMode('bot')}
            />
            <strong>vs Computer</strong> — you play as Player 1; all other players are computer opponents.
          </label>
        </div>

        <button
          className="start-btn"
          onClick={handleStart}
          disabled={loading || !selectedGame || !namesValid}
        >
          {loading ? 'Starting...' : 'Start Game'}
        </button>
      </div>
    </div>
  );
}

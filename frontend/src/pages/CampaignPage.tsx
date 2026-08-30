import { useEffect, useState, useMemo } from 'react';
import { CampaignOverview, CampaignMission, GameDefinitionFull, CardDefinitionDto } from '../types/game';
import { BASE } from '../config';

interface CampaignPageProps {
  onMissionStarted: (matchId: string, seat: string) => void;
  onOpenLobby: () => void;
  onOpenVsComputer: () => void;
}

const GAME_ID = 'town-tcg';

const CAMPAIGN_META: Record<string, { label: string; icon: string }> = {
  town:    { label: 'Town Campaign',          icon: '🏰' },
  raiders: { label: 'Raiders Campaign',       icon: '⚔️' },
  mages:   { label: 'Mage Guild Campaign',    icon: '🔮' },
  undead:  { label: 'Undead Campaign',        icon: '💀' },
  thieves: { label: 'Thieves Guild Campaign', icon: '🗡️' },
  hive:    { label: 'Hive Campaign',          icon: '🐛' },
};

const CAMPAIGN_ORDER = ['town', 'raiders', 'mages', 'undead', 'thieves', 'hive'];

function loadDeck(): Record<string, number> {
  try { return JSON.parse(localStorage.getItem('campaignDeck') ?? '{}'); } catch { return {}; }
}
function saveDeck(deck: Record<string, number>) {
  localStorage.setItem('campaignDeck', JSON.stringify(deck));
}
function loadHq(): string {
  return localStorage.getItem('campaignHq') ?? '';
}
function loadHero(): string {
  return localStorage.getItem('campaignHero') ?? '';
}

export function CampaignPage({ onMissionStarted, onOpenLobby, onOpenVsComputer }: CampaignPageProps) {
  const [overview, setOverview] = useState<CampaignOverview | null>(null);
  const [gameDef, setGameDef] = useState<GameDefinitionFull | null>(null);
  const [error, setError] = useState('');
  const [starting, setStarting] = useState('');
  const [activeTab, setActiveTab] = useState<string>('town');
  const [customDeck, setCustomDeck] = useState<Record<string, number>>(loadDeck);
  const [customHq, setCustomHq] = useState<string>(loadHq);
  const [customHero, setCustomHero] = useState<string>(loadHero);
  const [deckFilter, setDeckFilter] = useState('');

  useEffect(() => {
    fetch(`${BASE}api/definitions/${GAME_ID}`)
      .then(r => r.json())
      .then((def: GameDefinitionFull) => setGameDef(def))
      .catch(() => {});
  }, []);

  useEffect(() => {
    fetch(`${BASE}api/campaign?gameId=${GAME_ID}`, { credentials: 'include' })
      .then(r => r.json())
      .then(setOverview)
      .catch(() => setError('Failed to load the campaign. Is the backend running?'));
  }, []);

  const cardNames = useMemo(() => {
    const names: Record<string, string> = {};
    if (gameDef) for (const c of gameDef.cards) names[c.id] = `${c.icon ? c.icon + ' ' : ''}${c.name}`;
    return names;
  }, [gameDef]);

  const cardDefs = useMemo(() => {
    const map: Record<string, CardDefinitionDto> = {};
    if (gameDef) for (const c of gameDef.cards) map[c.id] = c;
    return map;
  }, [gameDef]);

  const maxCopies = gameDef?.deckRules?.maxCopies ?? 4;
  const deckTotal = Object.values(customDeck).reduce((s, v) => s + v, 0);
  const minDeck = gameDef?.deckRules?.maxDeckSize ?? 40;

  function addCard(cardId: string) {
    const owned = overview?.profile.collection[cardId] ?? 0;
    const inDeck = customDeck[cardId] ?? 0;
    if (inDeck >= Math.min(maxCopies, owned)) return;
    const next = { ...customDeck, [cardId]: inDeck + 1 };
    setCustomDeck(next);
    saveDeck(next);
  }

  function removeCard(cardId: string) {
    const inDeck = customDeck[cardId] ?? 0;
    if (inDeck <= 0) return;
    const next = { ...customDeck };
    if (inDeck === 1) delete next[cardId];
    else next[cardId] = inDeck - 1;
    setCustomDeck(next);
    saveDeck(next);
  }

  function clearDeck() {
    setCustomDeck({});
    saveDeck({});
  }

  function selectHq(id: string) {
    const next = customHq === id ? '' : id;
    setCustomHq(next);
    localStorage.setItem('campaignHq', next);
  }

  function selectHero(id: string) {
    const next = customHero === id ? '' : id;
    setCustomHero(next);
    localStorage.setItem('campaignHero', next);
  }

  async function startMission(missionId: string) {
    setStarting(missionId);
    setError('');
    try {
      const body: Record<string, unknown> = { gameId: GAME_ID, missionId };
      if (deckTotal > 0) body.customDeck = customDeck;
      if (customHq) body.customHq = customHq;
      if (customHero) body.customHero = customHero;
      const res = await fetch(`${BASE}api/campaign/start`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!res.ok) throw new Error(await res.text());
      const data = await res.json();
      onMissionStarted(data.matchId, data.playerId);
    } catch (err: any) {
      setError(err.message ?? 'Failed to start the mission');
    } finally {
      setStarting('');
    }
  }

  const collection = overview ? Object.entries(overview.profile.collection) : [];
  const unlocked = overview?.multiplayerUnlocked ?? false;
  const have = overview?.collectionCount ?? 0;
  const needed = overview?.requiredCards ?? 40;

  const missionsByCampaign: Record<string, CampaignMission[]> = {};
  for (const camp of CAMPAIGN_ORDER) missionsByCampaign[camp] = [];
  if (overview) {
    for (const m of overview.missions) {
      if (missionsByCampaign[m.campaign]) missionsByCampaign[m.campaign].push(m);
    }
  }

  function isCampaignUnlocked(campaign: string): boolean {
    return missionsByCampaign[campaign]?.some(m => m.unlocked) ?? false;
  }

  const hqCards = useMemo(() =>
    (gameDef?.cards ?? []).filter(c => c.objectType === 'headquarters'),
    [gameDef]);

  const heroCards = useMemo(() =>
    (gameDef?.cards ?? []).filter(c => c.objectType === 'hero'),
    [gameDef]);

  // Cards available to put in deck = cards in collection
  const collectionCards = useMemo(() => {
    if (!overview) return [];
    return Object.entries(overview.profile.collection)
      .filter(([, count]) => count > 0)
      .map(([id, owned]) => ({ id, owned, def: cardDefs[id] }))
      .filter(c => !deckFilter || (c.def?.name ?? c.id).toLowerCase().includes(deckFilter.toLowerCase()))
      .sort((a, b) => (a.def?.objectType ?? '').localeCompare(b.def?.objectType ?? '') || (a.def?.name ?? a.id).localeCompare(b.def?.name ?? b.id));
  }, [overview, cardDefs, deckFilter]);

  function MissionList({ missions }: { missions: CampaignMission[] }) {
    return (
      <div className="mission-list">
        {missions.map(m => (
          <div
            key={m.id}
            className={`mission-row ${m.completed ? 'mission-completed' : ''} ${!m.unlocked ? 'mission-locked' : ''}`}
          >
            <div className="mission-info">
              <div className="mission-name">
                {m.completed ? '✅ ' : m.unlocked ? '⚔️ ' : '🔒 '}
                {m.name}
              </div>
              <div className="mission-desc">{m.description}</div>
              <div className="mission-rewards">
                Rewards: {m.rewards.map(r => cardNames[r] ?? r).join(', ')}
              </div>
            </div>
            <button
              className="start-btn mission-start-btn"
              disabled={!m.unlocked || starting !== ''}
              onClick={() => startMission(m.id)}
            >
              {starting === m.id ? 'Starting...' : m.completed ? 'Replay' : 'Start'}
            </button>
          </div>
        ))}
      </div>
    );
  }

  function DeckBuilder() {
    const deckEntries = Object.entries(customDeck).filter(([, v]) => v > 0);
    return (
      <div className="deck-builder">
        <div className="deck-builder-header">
          <div className="deck-size-indicator">
            Deck: <strong>{deckTotal}</strong> kaarten
            {deckTotal === 0 && <span className="deck-hint"> (leeg — missie gebruikt standaard kaarten)</span>}
            {deckTotal > 0 && deckTotal < 10 && <span className="deck-warning"> ⚠ minimaal 10 aanbevolen</span>}
          </div>
          {deckTotal > 0 && (
            <button className="back-btn deck-clear-btn" onClick={clearDeck}>Leeg deck</button>
          )}
        </div>

        {/* HQ picker */}
        <div className="deck-picker-section">
          <div className="deck-picker-label">
            🏛 Headquarters{customHq ? '' : <span className="deck-hint"> (niet gekozen — missie gebruikt standaard)</span>}
          </div>
          <div className="deck-picker-row">
            {hqCards.map(c => (
              <div
                key={c.id}
                className={`deck-picker-card ${customHq === c.id ? 'deck-picker-selected' : ''}`}
                onClick={() => selectHq(c.id)}
                title={c.id}
              >
                <span className="deck-picker-icon">{c.icon ?? '🏛'}</span>
                <span className="deck-picker-name">{c.name}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Hero picker */}
        <div className="deck-picker-section">
          <div className="deck-picker-label">
            ★ Hero{customHero ? '' : <span className="deck-hint"> (niet gekozen — missie gebruikt standaard)</span>}
          </div>
          <div className="deck-picker-row">
            {heroCards.map(c => (
              <div
                key={c.id}
                className={`deck-picker-card ${customHero === c.id ? 'deck-picker-selected' : ''}`}
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
          {/* Collection */}
          <div className="deck-collection">
            <div className="deck-section-title">Jouw collectie — klik om toe te voegen</div>
            <input
              className="player-name-input deck-filter"
              placeholder="Zoek kaart..."
              value={deckFilter}
              onChange={e => setDeckFilter(e.target.value)}
            />
            {collectionCards.length === 0 && (
              <div className="deck-empty">Nog geen kaarten in je collectie. Win missies om kaarten te verdienen.</div>
            )}
            <div className="deck-card-grid">
              {collectionCards.map(({ id, owned, def }) => {
                const inDeck = customDeck[id] ?? 0;
                const canAdd = inDeck < Math.min(maxCopies, owned);
                return (
                  <div
                    key={id}
                    className={`deck-card-item ${canAdd ? 'deck-card-addable' : 'deck-card-full'}`}
                    onClick={() => addCard(id)}
                    title={canAdd ? 'Klik om toe te voegen' : `Maximum bereikt (${inDeck}/${Math.min(maxCopies, owned)})`}
                  >
                    <span className="deck-card-icon">{def?.icon ?? '🃏'}</span>
                    <span className="deck-card-name">{def?.name ?? id}</span>
                    <span className="deck-card-type">{def?.objectType ?? ''}</span>
                    <span className="deck-card-count">{inDeck > 0 ? `${inDeck}/${owned}` : `0/${owned}`}</span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Current deck */}
          <div className="deck-current">
            <div className="deck-section-title">Huidig deck — klik om te verwijderen</div>
            {deckEntries.length === 0 && (
              <div className="deck-empty">Deck is leeg — voeg kaarten toe vanuit je collectie.</div>
            )}
            <div className="deck-list">
              {deckEntries
                .sort(([a], [b]) => (cardDefs[a]?.objectType ?? '').localeCompare(cardDefs[b]?.objectType ?? '') || (cardDefs[a]?.name ?? a).localeCompare(cardDefs[b]?.name ?? b))
                .map(([id, count]) => (
                  <div key={id} className="deck-list-item" onClick={() => removeCard(id)} title="Klik om te verwijderen">
                    <span className="deck-card-icon">{cardDefs[id]?.icon ?? '🃏'}</span>
                    <span className="deck-card-name">{cardDefs[id]?.name ?? id}</span>
                    <span className="deck-list-count">×{count}</span>
                  </div>
                ))}
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="lobby-page">
      <div className="lobby-card lobby-wide campaign-card">
        <h1 className="lobby-title">Town Wars</h1>
        <p className="lobby-subtitle">
          Campaign — fight your way through six factions, earn your collection, unlock multiplayer
        </p>

        <button className="campaign-btn" onClick={onOpenVsComputer}>
          🤖 Play vs Computer — no unlock required, win to earn a random card
        </button>

        {error && <div className="error-box">{error}</div>}

        {overview && (
          <div className={`unlock-progress ${unlocked ? 'unlock-done' : ''}`}>
            <div className="unlock-progress-text">
              {unlocked
                ? `🔓 Multiplayer unlocked! Collection: ${have} cards`
                : `🔒 Multiplayer unlocks at ${needed} cards — you have ${have}. Win missions to grow your collection.`}
            </div>
            <div className="unlock-progress-bar">
              <div
                className="unlock-progress-fill"
                style={{ width: `${Math.min(100, (have / needed) * 100)}%` }}
              />
            </div>
          </div>
        )}

        {overview && (
          <>
            <div className="campaign-tabs">
              {CAMPAIGN_ORDER.map(camp => {
                const meta = CAMPAIGN_META[camp];
                const campUnlocked = isCampaignUnlocked(camp);
                return (
                  <button
                    key={camp}
                    className={`campaign-tab ${activeTab === camp ? 'active' : ''} ${!campUnlocked ? 'campaign-tab-locked' : ''}`}
                    onClick={() => campUnlocked && setActiveTab(camp)}
                    title={campUnlocked ? '' : 'Complete the previous campaign to unlock'}
                  >
                    {campUnlocked ? meta.icon : '🔒'} {meta.label}
                  </button>
                );
              })}
              <button
                className={`campaign-tab ${activeTab === 'deck' ? 'active' : ''}`}
                onClick={() => setActiveTab('deck')}
              >
                🃏 Mijn Deck{deckTotal > 0 ? ` (${deckTotal})` : ''}
              </button>
            </div>

            {activeTab === 'deck' && <DeckBuilder />}

            {CAMPAIGN_ORDER.map(camp => {
              const campUnlocked = isCampaignUnlocked(camp);
              if (activeTab !== camp) return null;
              if (!campUnlocked) {
                return (
                  <div key={camp} className="campaign-locked-msg">
                    🔒 Complete the previous campaign to unlock the {CAMPAIGN_META[camp].label}.
                  </div>
                );
              }
              return <MissionList key={camp} missions={missionsByCampaign[camp]} />;
            })}
          </>
        )}

        {overview && collection.length > 0 && activeTab !== 'deck' && (
          <div className="campaign-collection">
            <h3>Your collection ({have} cards)</h3>
            <div className="collection-cards">
              {collection.map(([id, count]) => (
                <span key={id} className="collection-card">
                  {cardNames[id] ?? id} ×{count}
                </span>
              ))}
            </div>
          </div>
        )}

        <button
          className={unlocked ? 'start-btn' : 'back-btn'}
          disabled={!unlocked}
          onClick={onOpenLobby}
          title={unlocked ? '' : `Unlocks at ${needed} cards`}
        >
          {unlocked
            ? '⚔️ Multiplayer Lobby'
            : `🔒 Multiplayer — ${have}/${needed} cards`}
        </button>
      </div>
    </div>
  );
}

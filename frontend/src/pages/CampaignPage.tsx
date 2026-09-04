import { useEffect, useState, useMemo, useCallback } from 'react';
import { CampaignOverview, CampaignMission, GameDefinitionFull, SavedDeck } from '../types/game';
import { BASE } from '../config';
import { DeckBuilderPanel } from '../components/DeckBuilderPanel';

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
  const [savedDecks, setSavedDecks] = useState<SavedDeck[]>([]);
  const [savedDeckId, setSavedDeckId] = useState('');

  useEffect(() => {
    fetch(`${BASE}api/definitions/${GAME_ID}`)
      .then(r => r.json())
      .then((def: GameDefinitionFull) => setGameDef(def))
      .catch(() => {});
  }, []);

  // Saved decks are scoped to the logged-in account — the same "My Decks" deck builder
  // used by the Lobby, so a deck built/saved there can be used to fight campaign missions.
  const loadSavedDecks = useCallback(() => {
    fetch(`${BASE}api/decks?gameId=${GAME_ID}`, { credentials: 'include' })
      .then(r => r.json())
      .then(data => setSavedDecks(data.decks ?? []))
      .catch(() => setSavedDecks([]));
  }, []);

  useEffect(() => { loadSavedDecks(); }, [loadSavedDecks]);

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

  const deckTotal = Object.values(customDeck).reduce((s, v) => s + v, 0);

  function changeDeck(next: Record<string, number>) {
    setCustomDeck(next);
    saveDeck(next);
    setSavedDeckId(''); // manual edit diverges from whichever saved deck was loaded
  }

  function clearDeck() {
    changeDeck({});
  }

  function useSavedDeck(deckId: string) {
    setSavedDeckId(deckId);
    if (!deckId) return;
    const saved = savedDecks.find(d => d.id === deckId);
    if (!saved) return;
    setCustomDeck(saved.cards);
    saveDeck(saved.cards);
    const hq = saved.hqId ?? '';
    const hero = saved.heroId ?? '';
    setCustomHq(hq);
    localStorage.setItem('campaignHq', hq);
    setCustomHero(hero);
    localStorage.setItem('campaignHero', hero);
  }

  // id === '' is the panel auto-clearing a pick whose card left the deck — that is not
  // a manual edit, so it doesn't mark the loaded saved deck as diverged.
  function selectHq(id: string) {
    const next = customHq === id ? '' : id;
    setCustomHq(next);
    localStorage.setItem('campaignHq', next);
    if (id !== '') setSavedDeckId('');
  }

  function selectHero(id: string) {
    const next = customHero === id ? '' : id;
    setCustomHero(next);
    localStorage.setItem('campaignHero', next);
    if (id !== '') setSavedDeckId('');
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

  // The campaign uses the same real-cards deck builder as My Decks (DeckBuilderPanel),
  // restricted to the player's earned collection, with the HQ/hero picked from the cards
  // in the deck being built — the campaign convention the shared panel implements.
  // Rendered as a plain call (not <DeckBuilder />): a component type defined inside the
  // page gets a new identity every render, which would remount DeckBuilderPanel — and
  // wipe its filter/inspect state — on every card add/remove.
  function renderDeckBuilder() {
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

        {savedDecks.length > 0 && (
          <div className="deck-picker-section">
            <div className="deck-picker-label">📚 My Decks</div>
            <select
              className="player-name-input"
              value={savedDeckId}
              onChange={e => useSavedDeck(e.target.value)}
            >
              <option value="">🃏 Load a saved deck...</option>
              {savedDecks.map(d => (
                <option key={d.id} value={d.id}>
                  {d.name} ({Object.values(d.cards).reduce((a, b) => a + b, 0)} cards)
                </option>
              ))}
            </select>
          </div>
        )}

        {gameDef && overview && (
          <DeckBuilderPanel
            gameDef={gameDef}
            deck={customDeck}
            onDeckChange={changeDeck}
            hqId={customHq}
            heroId={customHero}
            onSelectHq={selectHq}
            onSelectHero={selectHero}
            ownedCounts={overview.profile.collection}
            hqLabel={<>🏛 Headquarters{customHq ? '' : <span className="deck-hint"> (niet gekozen — missie gebruikt standaard)</span>}</>}
            heroLabel={<>★ Hero{customHero ? '' : <span className="deck-hint"> (niet gekozen — missie gebruikt standaard)</span>}</>}
            hqEmptyHint="Voeg een headquarters-kaart uit je collectie toe aan je deck om hem hier te kiezen."
            heroEmptyHint="Voeg een hero-kaart uit je collectie toe aan je deck om hem hier te kiezen."
          />
        )}
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

            {activeTab === 'deck' && renderDeckBuilder()}

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

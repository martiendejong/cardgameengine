import { useEffect, useState } from 'react';
import { CampaignOverview, GameDefinitionFull } from '../types/game';
import { BASE } from '../config';

interface CampaignPageProps {
  onMissionStarted: (matchId: string, seat: string) => void;
  onOpenLobby: () => void;
}

const GAME_ID = 'town-tcg';

export function CampaignPage({ onMissionStarted, onOpenLobby }: CampaignPageProps) {
  const [profileName, setProfileName] = useState(
    () => localStorage.getItem('campaignProfile') ?? '',
  );
  const [overview, setOverview] = useState<CampaignOverview | null>(null);
  const [cardNames, setCardNames] = useState<Record<string, string>>({});
  const [error, setError] = useState('');
  const [starting, setStarting] = useState('');

  useEffect(() => {
    fetch(`${BASE}api/definitions/${GAME_ID}`)
      .then(r => r.json())
      .then((def: GameDefinitionFull) => {
        const names: Record<string, string> = {};
        for (const c of def.cards) names[c.id] = `${c.icon ? c.icon + ' ' : ''}${c.name}`;
        setCardNames(names);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!profileName.trim()) { setOverview(null); return; }
    localStorage.setItem('campaignProfile', profileName);
    const t = setTimeout(() => {
      fetch(`${BASE}api/campaign?gameId=${GAME_ID}&profile=${encodeURIComponent(profileName)}`)
        .then(r => r.json())
        .then(setOverview)
        .catch(() => setError('Failed to load the campaign. Is the backend running?'));
    }, 300);
    return () => clearTimeout(t);
  }, [profileName]);

  async function startMission(missionId: string) {
    setStarting(missionId);
    setError('');
    try {
      const res = await fetch(`${BASE}api/campaign/start`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ gameId: GAME_ID, missionId, profile: profileName }),
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

  return (
    <div className="lobby-page">
      <div className="lobby-card lobby-wide campaign-card">
        <h1 className="lobby-title">Town Wars</h1>
        <p className="lobby-subtitle">
          {overview ? 'Campaign — defend the town, learn the game, earn your collection'
            : 'Welcome, commander. Enter your name to begin.'}
        </p>

        {error && <div className="error-box">{error}</div>}

        <div className="form-group">
          <label>Commander name</label>
          <input
            type="text"
            className="player-name-input campaign-profile-input"
            value={profileName}
            onChange={e => setProfileName(e.target.value)}
            placeholder="Enter your name to load your progress"
          />
        </div>

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
          <div className="mission-list">
            {overview.missions.map(m => (
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
        )}

        {overview && collection.length > 0 && (
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

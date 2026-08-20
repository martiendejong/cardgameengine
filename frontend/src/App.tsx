import { useState, useEffect } from 'react';
import { LobbyPage } from './pages/LobbyPage';
import { GamePage } from './pages/GamePage';
import { CampaignPage } from './pages/CampaignPage';
import './App.css';

interface Session {
  matchId: string;
  seat: string; // player id for a fixed seat, '' for hotseat (omniscient)
}

const GAME_ID = 'town-tcg';

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [view, setView] = useState<'loading' | 'lobby' | 'campaign'>('loading');

  // New players start in the campaign; the lobby is for unlocked profiles.
  // A second browser window can still join any match via ?match=...&player=...
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const match = params.get('match');
    const player = params.get('player');
    if (match) {
      setSession({ matchId: match, seat: player ?? '' });
      return;
    }
    if (params.get('campaign')) {
      setView('campaign');
      return;
    }
    const profile = localStorage.getItem('campaignProfile');
    if (!profile) {
      setView('campaign');
      return;
    }
    fetch(`/api/campaign?gameId=${GAME_ID}&profile=${encodeURIComponent(profile)}`)
      .then(r => r.json())
      .then(data => setView(data.multiplayerUnlocked ? 'lobby' : 'campaign'))
      .catch(() => setView('campaign'));
  }, []);

  function handleMatchCreated(matchId: string, seat: string) {
    if (seat) {
      const url = new URL(window.location.href);
      url.searchParams.set('match', matchId);
      url.searchParams.set('player', seat);
      window.history.replaceState(null, '', url.toString());
    }
    setSession({ matchId, seat });
  }

  function handleMissionStarted(matchId: string, seat: string) {
    const url = new URL(window.location.href);
    url.searchParams.set('match', matchId);
    url.searchParams.set('player', seat);
    url.searchParams.set('campaign', '1');
    window.history.replaceState(null, '', url.toString());
    setSession({ matchId, seat });
  }

  function handleLeave() {
    const params = new URLSearchParams(window.location.search);
    const backToCampaign = params.get('campaign') !== null;
    window.history.replaceState(
      null, '',
      window.location.pathname + (backToCampaign ? '?campaign=1' : ''),
    );
    setSession(null);
    setView(backToCampaign ? 'campaign' : 'lobby');
  }

  function openCampaign() {
    window.history.replaceState(null, '', window.location.pathname + '?campaign=1');
    setView('campaign');
  }

  function openLobby() {
    window.history.replaceState(null, '', window.location.pathname);
    setView('lobby');
  }

  if (session) {
    return (
      <GamePage
        matchId={session.matchId}
        seat={session.seat}
        onLeave={handleLeave}
      />
    );
  }

  if (view === 'loading') {
    return <div className="lobby-page"><div className="lobby-card"><p>Loading...</p></div></div>;
  }

  if (view === 'campaign') {
    return <CampaignPage onMissionStarted={handleMissionStarted} onOpenLobby={openLobby} />;
  }

  return <LobbyPage onMatchCreated={handleMatchCreated} onOpenCampaign={openCampaign} />;
}

export default App;

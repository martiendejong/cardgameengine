import { useState, useEffect } from 'react';
import { LobbyPage } from './pages/LobbyPage';
import { GamePage } from './pages/GamePage';
import { CampaignPage } from './pages/CampaignPage';
import './App.css';

interface Session {
  matchId: string;
  seat: string; // player id for a fixed seat, '' for hotseat (omniscient)
}

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [view, setView] = useState<'lobby' | 'campaign'>('lobby');

  // Allow a second browser window to join via ?match=...&player=...
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const match = params.get('match');
    const player = params.get('player');
    if (match) setSession({ matchId: match, seat: player ?? '' });
    else if (params.get('campaign')) setView('campaign');
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

  function closeCampaign() {
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

  if (view === 'campaign') {
    return <CampaignPage onMissionStarted={handleMissionStarted} onBack={closeCampaign} />;
  }

  return <LobbyPage onMatchCreated={handleMatchCreated} onOpenCampaign={openCampaign} />;
}

export default App;

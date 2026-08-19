import { useState, useEffect } from 'react';
import { LobbyPage } from './pages/LobbyPage';
import { GamePage } from './pages/GamePage';
import './App.css';

interface Session {
  matchId: string;
  seat: string; // player id for a fixed seat, '' for hotseat (omniscient)
}

function App() {
  const [session, setSession] = useState<Session | null>(null);

  // Allow a second browser window to join via ?match=...&player=...
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const match = params.get('match');
    const player = params.get('player');
    if (match) setSession({ matchId: match, seat: player ?? '' });
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

  function handleLeave() {
    window.history.replaceState(null, '', window.location.pathname);
    setSession(null);
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

  return <LobbyPage onMatchCreated={handleMatchCreated} />;
}

export default App;

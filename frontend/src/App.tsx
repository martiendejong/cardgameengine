import React, { useState } from 'react';
import { LobbyPage } from './pages/LobbyPage';
import { GamePage } from './pages/GamePage';
import './App.css';

interface MatchInfo {
  matchId: string;
  players: { id: string; name: string }[];
}

function App() {
  const [match, setMatch] = useState<MatchInfo | null>(null);

  function handleMatchCreated(matchId: string, players: { id: string; name: string }[]) {
    setMatch({ matchId, players });
  }

  function handleLeave() {
    setMatch(null);
  }

  if (match) {
    return (
      <GamePage
        matchId={match.matchId}
        players={match.players}
        onLeave={handleLeave}
      />
    );
  }

  return <LobbyPage onMatchCreated={handleMatchCreated} />;
}

export default App;

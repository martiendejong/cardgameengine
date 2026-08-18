import React, { useEffect, useState } from 'react';
import { GameDefinitionSummary, CreateMatchResponse } from '../types/game';

interface LobbyPageProps {
  onMatchCreated: (matchId: string, players: { id: string; name: string }[]) => void;
}

export function LobbyPage({ onMatchCreated }: LobbyPageProps) {
  const [definitions, setDefinitions] = useState<GameDefinitionSummary[]>([]);
  const [selectedGame, setSelectedGame] = useState('');
  const [player1Name, setPlayer1Name] = useState('Player 1');
  const [player2Name, setPlayer2Name] = useState('Player 2');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    fetch('/api/definitions')
      .then(r => r.json())
      .then((data: GameDefinitionSummary[]) => {
        setDefinitions(data);
        if (data.length > 0) setSelectedGame(data[0].id);
      })
      .catch(() => setError('Failed to load game definitions. Is the backend running?'));
  }, []);

  async function handleStart() {
    if (!selectedGame) return;
    setLoading(true);
    setError('');

    try {
      const res = await fetch('/api/matches', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          gameId: selectedGame,
          players: [
            { name: player1Name, id: 'p1' },
            { name: player2Name, id: 'p2' },
          ],
        }),
      });

      if (!res.ok) {
        const txt = await res.text();
        throw new Error(txt || 'Failed to create match');
      }

      const data: CreateMatchResponse = await res.json();
      onMatchCreated(data.matchId, data.players);
    } catch (err: any) {
      setError(err.message ?? 'Unknown error');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="lobby-page">
      <div className="lobby-card">
        <h1 className="lobby-title">Town Wars</h1>
        <p className="lobby-subtitle">Trading Card Game Engine</p>

        {error && <div className="error-box">{error}</div>}

        <div className="lobby-form">
          <div className="form-group">
            <label>Game</label>
            <select
              value={selectedGame}
              onChange={e => setSelectedGame(e.target.value)}
            >
              {definitions.length === 0 && (
                <option value="">Loading...</option>
              )}
              {definitions.map(d => (
                <option key={d.id} value={d.id}>
                  {d.name} v{d.version}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Player 1 Name</label>
            <input
              type="text"
              value={player1Name}
              onChange={e => setPlayer1Name(e.target.value)}
              placeholder="Player 1"
            />
          </div>

          <div className="form-group">
            <label>Player 2 Name</label>
            <input
              type="text"
              value={player2Name}
              onChange={e => setPlayer2Name(e.target.value)}
              placeholder="Player 2"
            />
          </div>

          <div className="lobby-note">
            <strong>2-Player Local:</strong> Both players share the same screen. Take turns on the same device.
          </div>

          <button
            className="start-btn"
            onClick={handleStart}
            disabled={loading || !selectedGame || !player1Name || !player2Name}
          >
            {loading ? 'Starting...' : 'Start Game'}
          </button>
        </div>
      </div>
    </div>
  );
}

import React, { useState, useCallback } from 'react';
import { GameStateDto, AvailableAction, GameState } from '../types/game';
import { useGameHub } from '../hooks/useGameHub';
import { GameBoard } from '../components/GameBoard';

interface GamePageProps {
  matchId: string;
  players: { id: string; name: string }[];
  onLeave: () => void;
}

export function GamePage({ matchId, players, onLeave }: GamePageProps) {
  const [gameState, setGameState] = useState<GameStateDto | null>(null);
  const [error, setError] = useState('');
  // For 2-player local, the "viewing player" is always the active player
  // We show both players' areas with the current active player at bottom
  const [myPlayerId, setMyPlayerId] = useState(players[0]?.id ?? '');

  const handleStateUpdate = useCallback((state: GameStateDto) => {
    setGameState(state);
    // In 2-player local mode, switch perspective to the active player
    setMyPlayerId(state.activePlayerId);
  }, []);

  const handleError = useCallback((msg: string) => {
    setError(msg);
    setTimeout(() => setError(''), 4000);
  }, []);

  const { connected, sendAction, resolveChoice, endPhase } = useGameHub({
    matchId,
    playerId: myPlayerId,
    onStateUpdate: handleStateUpdate,
    onError: handleError,
  });

  async function handleAction(action: AvailableAction, targetIds?: string[]) {
    try {
      if (action.type === 'endPhase') {
        await endPhase();
        return;
      }

      await sendAction({
        type: action.type,
        sourceObjectId: action.sourceObjectId,
        abilityId: action.abilityId,
        targetIds: targetIds ?? [],
      });
    } catch (err: any) {
      handleError(err.message ?? 'Action failed');
    }
  }

  async function handleEndPhase() {
    try {
      await endPhase();
    } catch (err: any) {
      handleError(err.message ?? 'Failed to end phase');
    }
  }

  async function handleResolveChoice(choiceId: string, selectedIds: string[]) {
    try {
      await resolveChoice(choiceId, selectedIds);
    } catch (err: any) {
      handleError(err.message ?? 'Failed to resolve choice');
    }
  }

  if (!connected) {
    return (
      <div className="connecting-screen">
        <div className="connecting-message">Connecting to game server...</div>
      </div>
    );
  }

  if (!gameState) {
    return (
      <div className="connecting-screen">
        <div className="connecting-message">Loading game state...</div>
      </div>
    );
  }

  const activePlayer = gameState.players.find(p => p.id === gameState.activePlayerId);

  return (
    <div className="game-page">
      <div className="game-header">
        <span className="match-id">Match: {matchId}</span>
        <span className="active-turn-label">
          {gameState.state === GameState.GameEnded
            ? `Game Over - ${gameState.winner} wins!`
            : `${activePlayer?.name ?? '...'}'s Turn`}
        </span>
        <button className="leave-btn" onClick={onLeave}>Leave</button>
      </div>

      {error && <div className="error-toast">{error}</div>}

      <GameBoard
        gameState={gameState}
        myPlayerId={myPlayerId}
        onAction={handleAction}
        onEndPhase={handleEndPhase}
        onResolveChoice={handleResolveChoice}
      />
    </div>
  );
}

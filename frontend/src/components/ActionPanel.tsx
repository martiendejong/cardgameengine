import React from 'react';
import { AvailableAction, GameStateDto, GameState } from '../types/game';

interface ActionPanelProps {
  gameState: GameStateDto;
  myPlayerId: string;
  onEndPhase: () => void;
  onAction: (action: AvailableAction) => void;
}

const PHASE_LABELS: Record<string, string> = {
  start: 'Start Phase',
  main: 'Main Phase',
  combat: 'Combat Phase',
  end: 'End Phase',
};

export function ActionPanel({ gameState, myPlayerId, onEndPhase, onAction }: ActionPanelProps) {
  const isMyTurn = gameState.activePlayerId === myPlayerId;
  const phase = PHASE_LABELS[gameState.currentPhaseId] || gameState.currentPhaseId;
  const activePlayer = gameState.players.find(p => p.id === gameState.activePlayerId);

  const globalActions = gameState.availableActions.filter(
    a => a.type === 'endPhase' && a.available
  );

  return (
    <div className="action-panel">
      <div className="turn-info">
        <div className="turn-number">Turn {gameState.turnNumber}</div>
        <div className="current-phase">{phase}</div>
        <div className="active-player-label">
          {isMyTurn ? 'Your turn' : `${activePlayer?.name ?? '...'}'s turn`}
        </div>
      </div>

      {isMyTurn && gameState.state !== GameState.GameEnded && (
        <div className="phase-actions">
          <button
            className="end-phase-btn"
            onClick={onEndPhase}
          >
            End {phase}
          </button>
        </div>
      )}

      {!isMyTurn && gameState.state !== GameState.GameEnded && (
        <div className="waiting-label">Waiting for opponent...</div>
      )}

      {gameState.state === GameState.GameEnded && (
        <div className="game-over-label">Game Over</div>
      )}
    </div>
  );
}

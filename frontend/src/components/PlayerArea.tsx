import React from 'react';
import { PlayerStateDto, ObjectStateDto, AvailableAction } from '../types/game';
import { CardView } from './CardView';

interface PlayerAreaProps {
  player: PlayerStateDto;
  objects: ObjectStateDto[];
  actions: AvailableAction[];
  isActivePlayer: boolean;
  isBottom: boolean;
  selectableTargets: string[];
  selectedTargets: string[];
  onAction: (action: AvailableAction) => void;
  onSelectTarget: (id: string) => void;
}

export function PlayerArea({
  player,
  objects,
  actions,
  isActivePlayer,
  isBottom,
  selectableTargets,
  selectedTargets,
  onAction,
  onSelectTarget,
}: PlayerAreaProps) {
  const battlefieldCards = objects.filter(
    o => o.controllerId === player.id && o.zoneId === 'battlefield' && !o.isDestroyed
  );
  const discardedCards = objects.filter(
    o => o.controllerId === player.id && o.isDestroyed
  );

  return (
    <div className={`player-area ${isBottom ? 'bottom-player' : 'top-player'} ${isActivePlayer ? 'active-player' : ''} ${player.isLoser ? 'loser' : ''} ${player.isWinner ? 'winner' : ''}`}>
      <div className="player-header">
        <div className="player-name-section">
          <span className="player-name">{player.name}</span>
          {isActivePlayer && <span className="active-badge">ACTIVE</span>}
          {player.isWinner && <span className="winner-badge">WINNER</span>}
          {player.isLoser && <span className="loser-badge">DEFEATED</span>}
        </div>
        <div className="player-resources">
          {Object.entries(player.resources).map(([key, val]) => (
            <div key={key} className="resource-chip">
              <span className="resource-icon">{key === 'gold' ? '💰' : '⚡'}</span>
              <span className="resource-label">{key === 'gold' ? 'Gold' : key}</span>
              <span className="resource-value">{val}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="battlefield">
        {battlefieldCards.length === 0 && (
          <div className="empty-battlefield">No cards on battlefield</div>
        )}
        {battlefieldCards.map(card => (
          <CardView
            key={card.id}
            card={card}
            actions={isBottom ? actions : []}
            isSelectableTarget={selectableTargets.includes(card.id)}
            isSelectedTarget={selectedTargets.includes(card.id)}
            onAction={onAction}
            onSelectTarget={onSelectTarget}
            flipped={!isBottom}
          />
        ))}
      </div>

      {discardedCards.length > 0 && (
        <div className="discard-area">
          <span className="discard-label">Discard ({discardedCards.length}):</span>
          {discardedCards.map(c => (
            <span key={c.id} className="discarded-card-name">{c.name}</span>
          ))}
        </div>
      )}
    </div>
  );
}

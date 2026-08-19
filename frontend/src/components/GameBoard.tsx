import { useState } from 'react';
import { GameStateDto, AvailableAction, GameState, CardDefinitionDto } from '../types/game';
import { PlayerArea } from './PlayerArea';
import { ActionPanel } from './ActionPanel';
import { ChoiceDialog } from './ChoiceDialog';
import { GameLog } from './GameLog';
import { CardDetailModal } from './CardDetailModal';

interface GameBoardProps {
  gameState: GameStateDto;
  myPlayerId: string;
  cardDefs: Record<string, CardDefinitionDto>;
  onAction: (action: AvailableAction, targetIds?: string[]) => void;
  onEndPhase: () => void;
  onResolveChoice: (choiceId: string, selectedIds: string[]) => void;
}

export function GameBoard({
  gameState,
  myPlayerId,
  cardDefs,
  onAction,
  onEndPhase,
  onResolveChoice,
}: GameBoardProps) {
  const [pendingAction, setPendingAction] = useState<AvailableAction | null>(null);
  const [selectedTargets, setSelectedTargets] = useState<string[]>([]);
  const [inspectedId, setInspectedId] = useState<string | null>(null);

  const inspectedCard = inspectedId
    ? gameState.objects.find(o => o.id === inspectedId) ?? null
    : null;

  const myPlayer = gameState.players.find(p => p.id === myPlayerId);
  const opponentPlayer = gameState.players.find(p => p.id !== myPlayerId);

  const selectableTargets = pendingAction?.validTargets ?? [];

  function handleActionClick(action: AvailableAction) {
    if (!action.available) return;

    if (action.type === 'endPhase') {
      onEndPhase();
      return;
    }

    if (action.requiresChoice && action.validTargets && action.validTargets.length > 0) {
      // Need to select targets
      setPendingAction(action);
      setSelectedTargets([]);
    } else if (action.requiresChoice && (!action.validTargets || action.validTargets.length === 0)) {
      // No valid targets, send with empty
      onAction(action, []);
    } else {
      // No choice needed
      onAction(action, []);
    }
  }

  function handleSelectTarget(id: string) {
    if (!pendingAction) return;

    const max = pendingAction.requiresChoice?.max ?? 1;
    setSelectedTargets(prev => {
      if (prev.includes(id)) return prev.filter(x => x !== id);
      if (prev.length >= max) return [...prev.slice(1), id];
      return [...prev, id];
    });
  }

  function handleConfirmTargets() {
    if (!pendingAction) return;
    const min = pendingAction.requiresChoice?.min ?? 1;
    if (selectedTargets.length < min) return;

    onAction(pendingAction, selectedTargets);
    setPendingAction(null);
    setSelectedTargets([]);
  }

  function handleCancelTargetSelect() {
    setPendingAction(null);
    setSelectedTargets([]);
  }

  const isMyTurn = gameState.activePlayerId === myPlayerId;
  const isReactionMine = gameState.state === GameState.WaitingForReaction
    && gameState.reactionPlayerId === myPlayerId;
  const passAction = gameState.availableActions.find(a => a.type === 'pass');

  return (
    <div className="game-board">
      {gameState.state === GameState.GameEnded && gameState.winner && (
        <div className="winner-overlay">
          <div className="winner-box">
            <h2>Game Over!</h2>
            <p className="winner-text">{gameState.winner} wins!</p>
            <button onClick={() => window.location.href = '/'}>Back to Lobby</button>
          </div>
        </div>
      )}

      {/* Main play area: one scroll container for both player sections */}
      <div className="board-main">
        {/* Opponent area (top) */}
        {opponentPlayer && (
          <PlayerArea
            player={opponentPlayer}
            objects={gameState.objects}
            actions={gameState.availableActions}
            isActivePlayer={gameState.activePlayerId === opponentPlayer.id}
            isBottom={false}
            selectableTargets={selectableTargets}
            selectedTargets={selectedTargets}
            onAction={handleActionClick}
            onSelectTarget={handleSelectTarget}
            onInspect={setInspectedId}
          />
        )}

        {/* Middle zone: action panel + target selection banner */}
        <div className="middle-zone">
          {gameState.state === GameState.WaitingForReaction && (
            <div className="reaction-banner">
              {isReactionMine ? (
                <>
                  <span>
                    ⚡ {gameState.reactionWindowEvent === 'spellCast'
                      ? 'The enemy is casting a spell!'
                      : 'The enemy declared an attack!'} Play a reaction or pass.
                  </span>
                  {passAction && (
                    <button className="pass-btn" onClick={() => onAction(passAction, [])}>
                      Pass — let it resolve
                    </button>
                  )}
                </>
              ) : (
                <span>⏳ Waiting for the opponent's reaction...</span>
              )}
            </div>
          )}
          {pendingAction && (
            <div className="target-select-banner">
              <span>Select target for: <strong>{pendingAction.label}</strong></span>
              <span className="target-count">({selectedTargets.length}/{pendingAction.requiresChoice?.max ?? 1} selected)</span>
              <button
                className="confirm-target-btn"
                disabled={selectedTargets.length < (pendingAction.requiresChoice?.min ?? 1)}
                onClick={handleConfirmTargets}
              >
                Confirm
              </button>
              <button className="cancel-target-btn" onClick={handleCancelTargetSelect}>
                Cancel
              </button>
            </div>
          )}
          <ActionPanel
            gameState={gameState}
            myPlayerId={myPlayerId}
            onEndPhase={onEndPhase}
            onAction={handleActionClick}
          />
        </div>

        {/* My area (bottom) */}
        {myPlayer && (
          <PlayerArea
            player={myPlayer}
            objects={gameState.objects}
            actions={isMyTurn || isReactionMine ? gameState.availableActions : []}
            isActivePlayer={isMyTurn}
            isBottom={true}
            selectableTargets={selectableTargets}
            selectedTargets={selectedTargets}
            onAction={handleActionClick}
            onSelectTarget={handleSelectTarget}
            onInspect={setInspectedId}
          />
        )}
      </div>

      {/* Game log */}
      <GameLog log={gameState.log} />

      {/* Choice dialog (for server-initiated choices) */}
      {gameState.pendingChoice && (
        <ChoiceDialog
          choice={gameState.pendingChoice}
          objects={gameState.objects}
          onResolve={onResolveChoice}
        />
      )}

      {/* Card inspector */}
      {inspectedCard && (
        <CardDetailModal
          card={inspectedCard}
          def={cardDefs[inspectedCard.definitionId]}
          attachments={gameState.objects.filter(o => o.attachedToId === inspectedCard.id && !o.isDestroyed)}
          nameOf={id => cardDefs[id]?.name ?? id}
          onClose={() => setInspectedId(null)}
          onInspect={setInspectedId}
        />
      )}
    </div>
  );
}

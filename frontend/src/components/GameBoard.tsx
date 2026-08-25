import { useState, useRef, useEffect } from 'react';
import { GameStateDto, AvailableAction, GameState, CardDefinitionDto } from '../types/game';
import { PlayerArea } from './PlayerArea';
import { ChoiceDialog } from './ChoiceDialog';
import { GameLog } from './GameLog';
import { CardDetailModal } from './CardDetailModal';
import { BASE } from '../config';

interface GameBoardProps {
  gameState: GameStateDto;
  myPlayerId: string;
  cardDefs: Record<string, CardDefinitionDto>;
  pendingAction: AvailableAction | null;
  selectedTargets: string[];
  onActionClick: (action: AvailableAction) => void;
  onSelectTarget: (id: string) => void;
  onAction: (action: AvailableAction, targetIds?: string[], chosenAmount?: number) => void;
  onResolveChoice: (choiceId: string, selectedIds: string[]) => void;
}

export function GameBoard({
  gameState,
  myPlayerId,
  cardDefs,
  pendingAction,
  selectedTargets,
  onActionClick,
  onSelectTarget,
  onAction,
  onResolveChoice,
}: GameBoardProps) {
  const [cardAnims, setCardAnims] = useState<Record<string, string>>({});
  const prevStateRef = useRef<typeof gameState | null>(null);
  const boardRef = useRef<HTMLDivElement>(null);

  function scrollBoard(to: 'top' | 'bottom') {
    const el = boardRef.current;
    if (!el) return;
    el.scrollTo({ top: to === 'top' ? 0 : el.scrollHeight, behavior: 'smooth' });
  }

  // My turn → show my side; opponent's turn → show theirs
  useEffect(() => {
    scrollBoard(gameState.activePlayerId === myPlayerId ? 'bottom' : 'top');
  }, [gameState.activePlayerId, myPlayerId]);

  // Target selection: follow where the valid targets are
  useEffect(() => {
    if (!pendingAction) {
      if (gameState.activePlayerId === myPlayerId) scrollBoard('bottom');
      return;
    }
    const targetIds = pendingAction.validTargets ?? [];
    const targetObjs = gameState.objects.filter(o => targetIds.includes(o.id));
    const anyEnemy = targetObjs.some(o => o.controllerId !== myPlayerId);
    scrollBoard(anyEnemy ? 'top' : 'bottom');
  }, [pendingAction]);

  useEffect(() => {
    const prev = prevStateRef.current;
    prevStateRef.current = gameState;
    if (!prev) return;

    const newAnims: Record<string, string> = {};
    const prevMap = new Map(prev.objects.map(o => [o.id, o]));

    for (const obj of gameState.objects) {
      if (obj.isDestroyed || obj.zoneId !== 'battlefield') continue;
      const p = prevMap.get(obj.id);

      if (!p && !obj.hasSummoningSickness) {
        // Newly appeared on battlefield (play from hand handled by summoningSickness flag absence)
        newAnims[obj.id] = 'anim-summon';
      } else if (!p) {
        newAnims[obj.id] = 'anim-summon';
      } else {
        const prevHp = p.properties['currentHp'] ?? 0;
        const currHp = obj.properties['currentHp'] ?? 0;
        if (currHp < prevHp) {
          newAnims[obj.id] = 'anim-hit';
        } else if (!p.isTapped && obj.isTapped) {
          newAnims[obj.id] = 'anim-attack';
        }
      }
    }

    if (Object.keys(newAnims).length === 0) return;

    setCardAnims(cur => ({ ...cur, ...newAnims }));
    const ids = Object.keys(newAnims);
    setTimeout(() => {
      setCardAnims(cur => {
        const next = { ...cur };
        ids.forEach(id => delete next[id]);
        return next;
      });
    }, 650);
  }, [gameState]);
  const [inspectedId, setInspectedId] = useState<string | null>(null);

  const inspectedCard = inspectedId
    ? gameState.objects.find(o => o.id === inspectedId) ?? null
    : null;

  const myPlayer = gameState.players.find(p => p.id === myPlayerId);
  const opponentPlayer = gameState.players.find(p => p.id !== myPlayerId);

  const selectableTargets = pendingAction?.validTargets ?? [];

  const isMyTurn = gameState.activePlayerId === myPlayerId;
  const isReactionMine = gameState.state === GameState.WaitingForReaction
    && gameState.reactionPlayerId === myPlayerId;
  const passAction = gameState.availableActions.find(a => a.type === 'pass');

  return (
    <div className="game-board">
      {gameState.state === GameState.GameEnded && gameState.winner && (
        <div className="winner-overlay">
          <div className="winner-box">
            {gameState.encounter ? (() => {
              const enc = gameState.encounter;
              const me = gameState.players.find(p => p.id === enc.playerId);
              const won = me?.isWinner ?? false;
              const rewardLabel = (id: string) => {
                const def = cardDefs[id];
                return def ? `${def.icon ? def.icon + ' ' : ''}${def.name}` : id;
              };
              const finish = async () => {
                if (won) {
                  try {
                    await fetch(`${BASE}api/campaign/complete`, {
                      method: 'POST',
                      headers: { 'Content-Type': 'application/json' },
                      body: JSON.stringify({ matchId: gameState.matchId }),
                    });
                  } catch { /* progress check happens again on the campaign page */ }
                }
                window.location.href = `${BASE}?campaign=1`;
              };
              return (
                <>
                  <h2>{won ? 'Victory!' : 'The town has fallen...'}</h2>
                  <p className="winner-text">
                    {won ? 'The attack is repelled!' : 'The raiders were too strong this time.'}
                  </p>
                  {won && enc.rewardCards.length > 0 && (
                    <div className="mission-reward-list">
                      <p>New cards for your collection:</p>
                      {enc.rewardCards.map((id, i) => (
                        <span key={i} className="collection-card">
                          {rewardLabel(id)}{enc.rewardCopies > 1 ? ` ×${enc.rewardCopies}` : ''}
                        </span>
                      ))}
                    </div>
                  )}
                  <button onClick={finish}>{won ? 'Continue' : 'Try Again'}</button>
                </>
              );
            })() : (
              <>
                <h2>Game Over!</h2>
                <p className="winner-text">{gameState.winner} wins!</p>
                <button onClick={() => window.location.href = '/'}>Back to Lobby</button>
              </>
            )}
          </div>
        </div>
      )}

      {/* Main play area: one scroll container for both player sections */}
      <div className="board-main" ref={boardRef}>
        {/* Opponent area (top) */}
        {opponentPlayer && (
          <PlayerArea
            player={opponentPlayer}
            objects={gameState.objects}
            actions={gameState.availableActions}
            cardDefs={cardDefs}
            isActivePlayer={gameState.activePlayerId === opponentPlayer.id}
            isBottom={false}
            selectableTargets={selectableTargets}
            selectedTargets={selectedTargets}
            cardAnims={cardAnims}
            onAction={onActionClick}
            onSelectTarget={onSelectTarget}
            onInspect={setInspectedId}
          />
        )}

        {/* Middle zone: reaction banner (target selection now renders in GamePage, above the end-turn bar) */}
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
        </div>

        {/* My area (bottom) */}
        {myPlayer && (
          <PlayerArea
            player={myPlayer}
            objects={gameState.objects}
            actions={isMyTurn || isReactionMine ? gameState.availableActions : []}
            cardDefs={cardDefs}
            isActivePlayer={isMyTurn}
            isBottom={true}
            selectableTargets={selectableTargets}
            selectedTargets={selectedTargets}
            cardAnims={cardAnims}
            onAction={onActionClick}
            onSelectTarget={onSelectTarget}
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
          actions={gameState.availableActions.filter(a => a.sourceObjectId === inspectedCard.id)}
          onAction={a => { onActionClick(a); setInspectedId(null); }}
          onClose={() => setInspectedId(null)}
          onInspect={setInspectedId}
        />
      )}
    </div>
  );
}

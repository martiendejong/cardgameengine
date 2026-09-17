import { useState, useCallback, useEffect, useRef } from 'react';
import { GameStateDto, AvailableAction, GameState, GameDefinitionFull, CardDefinitionDto } from '../types/game';
import { useGameHub } from '../hooks/useGameHub';
import { GameBoard } from '../components/GameBoard';
import { ActionPanel } from '../components/ActionPanel';
import { TargetSelectBanner } from '../components/TargetSelectBanner';
import { BASE } from '../config';

interface GamePageProps {
  matchId: string;
  seat: string; // fixed player id, or '' for hotseat (perspective follows the active player)
  onLeave: () => void;
}

type QueuedAction = { action: AvailableAction; targetIds: string[]; chosenAmount?: number };

export function GamePage({ matchId, seat, onLeave }: GamePageProps) {
  const [gameState, setGameState] = useState<GameStateDto | null>(null);
  const [error, setError] = useState('');
  const [myPlayerId, setMyPlayerId] = useState(seat);
  const [copied, setCopied] = useState(false);
  const [cardDefs, setCardDefs] = useState<Record<string, CardDefinitionDto>>({});
  const [pendingAction, setPendingAction] = useState<AvailableAction | null>(null);
  const [selectedTargets, setSelectedTargets] = useState<string[]>([]);
  const [chosenAmount, setChosenAmount] = useState<number>(1);

  // Track in-flight request and queue one follow-up action.
  // The queue holds the most recent click: clicking twice while waiting replaces the queue.
  const isRequestInFlightRef = useRef(false);
  const queuedActionRef = useRef<QueuedAction | null>(null);

  const isHotseat = seat === '';

  // Load the static card definitions once (for the card inspector)
  const gameId = gameState?.gameId;
  useEffect(() => {
    if (!gameId) return;
    fetch(`${BASE}api/definitions/${gameId}?t=${Date.now()}`, { cache: 'no-store' })
      .then(r => r.json())
      .then((def: GameDefinitionFull) => {
        const map: Record<string, CardDefinitionDto> = {};
        for (const card of def.cards) map[card.id] = card;
        setCardDefs(map);
      })
      .catch(() => { /* inspector will show runtime info only */ });
  }, [gameId]);

  const handleStateUpdate = useCallback((state: GameStateDto) => {
    hasGameStateRef.current = true;
    setGameState(state);
    if (isHotseat) setMyPlayerId(state.activePlayerId);
  }, [isHotseat]);

  const [joinDenied, setJoinDenied] = useState<string | null>(null);
  const hasGameStateRef = useRef(false);

  const handleError = useCallback((msg: string) => {
    setError(msg);
    setTimeout(() => setError(''), 4000);
    // No game state yet means this error is the response to our own JoinMatch call
    // (the server rejected it — wrong account for this seat) rather than a transient
    // action failure mid-game; show a dedicated screen instead of spinning forever.
    if (!hasGameStateRef.current) setJoinDenied(msg);
  }, []);

  const { connected, sendAction, resolveChoice, endPhase } = useGameHub({
    matchId,
    playerId: seat,
    onStateUpdate: handleStateUpdate,
    onError: handleError,
  });

  // In hotseat mode we act as whoever is active; in seat mode always as our seat
  const actAs = isHotseat ? myPlayerId : seat;

  // actAsRef keeps dispatchAction's closure from capturing a stale actAs.
  const actAsRef = useRef(actAs);
  actAsRef.current = actAs;

  const dispatchAction = useCallback(async (action: AvailableAction, targetIds: string[] = [], amount?: number) => {
    if (isRequestInFlightRef.current) {
      // Store latest queued intent; previous queued click is discarded.
      queuedActionRef.current = { action, targetIds, chosenAmount: amount };
      return;
    }

    isRequestInFlightRef.current = true;
    try {
      if (action.type === 'endPhase') {
        await endPhase(actAsRef.current);
      } else {
        await sendAction(actAsRef.current, {
          type: action.type,
          sourceObjectId: action.sourceObjectId,
          abilityId: action.abilityId,
          targetIds,
          chosenAmount: amount,
        });
      }
    } catch (err: any) {
      handleError(err.message ?? 'Action failed');
    } finally {
      isRequestInFlightRef.current = false;
      const queued = queuedActionRef.current;
      if (queued) {
        queuedActionRef.current = null;
        void dispatchAction(queued.action, queued.targetIds, queued.chosenAmount);
      }
    }
  }, [sendAction, endPhase, handleError]);

  function handleActionClick(action: AvailableAction) {
    if (!action.available) return;

    if (action.type === 'endPhase') {
      void dispatchAction(action, []);
      return;
    }

    if (action.requiresChoice && action.validTargets && action.validTargets.length > 0) {
      // Need to select targets (and optionally an amount)
      setPendingAction(action);
      setSelectedTargets([]);
      setChosenAmount(action.requiresChoice.amountMin ?? 1);
    } else if (action.requiresChoice && (!action.validTargets || action.validTargets.length === 0)) {
      void dispatchAction(action, []);
    } else {
      void dispatchAction(action, []);
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

    const amount = pendingAction.requiresChoice?.chooseAmount ? chosenAmount : undefined;
    void dispatchAction(pendingAction, selectedTargets, amount);
    setPendingAction(null);
    setSelectedTargets([]);
    setChosenAmount(1);
  }

  function handleCancelTargetSelect() {
    setPendingAction(null);
    setSelectedTargets([]);
  }

  async function handleEndPhase() {
    try {
      await endPhase(actAs);
    } catch (err: any) {
      handleError(err.message ?? 'Failed to end phase');
    }
  }

  async function handleResolveChoice(choiceId: string, selectedIds: string[]) {
    try {
      await resolveChoice(actAs, choiceId, selectedIds);
    } catch (err: any) {
      handleError(err.message ?? 'Failed to resolve choice');
    }
  }

  function opponentInviteUrl(): string | null {
    if (isHotseat || !gameState) return null;
    const opponent = gameState.players.find(p => p.id !== seat);
    if (!opponent || opponent.isBot) return null;
    const url = new URL(window.location.origin + window.location.pathname);
    url.searchParams.set('match', matchId);
    url.searchParams.set('player', opponent.id);
    return url.toString();
  }

  async function copyInvite() {
    const url = opponentInviteUrl();
    if (!url) return;
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // clipboard unavailable; the input below is selectable
    }
  }

  if (joinDenied) {
    return (
      <div className="connecting-screen">
        <div className="connecting-message">{joinDenied}</div>
        <button className="back-btn" onClick={onLeave} style={{ marginTop: 16 }}>
          ⚔️ Back to Lobby
        </button>
      </div>
    );
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
  const mySeatPlayer = gameState.players.find(p => p.id === actAs);
  const inviteUrl = opponentInviteUrl();
  const opponentJoined = gameState.players.some(p => p.id !== seat);

  return (
    <div className="game-page">
      <div className="game-header">
        <span className="match-id">
          Match: {matchId}
          {!isHotseat && mySeatPlayer && <span className="seat-label"> — playing as {mySeatPlayer.name}</span>}
        </span>
      </div>

      {inviteUrl && opponentJoined && gameState.turnNumber <= 1 && (
        <div className="invite-banner">
          <span>Opponent link:</span>
          <input className="invite-url" readOnly value={inviteUrl} onFocus={e => e.target.select()} />
          <button className="copy-invite-btn" onClick={copyInvite}>
            {copied ? 'Copied!' : 'Copy'}
          </button>
        </div>
      )}

      {error && <div className="error-toast">{error}</div>}

      <GameBoard
        gameState={gameState}
        myPlayerId={actAs}
        cardDefs={cardDefs}
        pendingAction={pendingAction}
        selectedTargets={selectedTargets}
        isPaused={false}
        onActionClick={handleActionClick}
        onSelectTarget={handleSelectTarget}
        onAction={(action, targetIds, amount) => void dispatchAction(action, targetIds, amount)}
        onResolveChoice={handleResolveChoice}
      />

      {pendingAction && (
        <TargetSelectBanner
          pendingAction={pendingAction}
          selectedTargets={selectedTargets}
          chosenAmount={chosenAmount}
          onAmountChange={setChosenAmount}
          onConfirm={handleConfirmTargets}
          onCancel={handleCancelTargetSelect}
        />
      )}

      <ActionPanel
        gameState={gameState}
        myPlayerId={actAs}
        isPaused={false}
        onEndPhase={handleEndPhase}
        onLeave={onLeave}
      />
    </div>
  );
}

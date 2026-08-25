import { useState, useCallback, useEffect } from 'react';
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

export function GamePage({ matchId, seat, onLeave }: GamePageProps) {
  const [gameState, setGameState] = useState<GameStateDto | null>(null);
  const [error, setError] = useState('');
  const [myPlayerId, setMyPlayerId] = useState(seat);
  const [copied, setCopied] = useState(false);
  const [cardDefs, setCardDefs] = useState<Record<string, CardDefinitionDto>>({});
  const [pendingAction, setPendingAction] = useState<AvailableAction | null>(null);
  const [selectedTargets, setSelectedTargets] = useState<string[]>([]);
  const [chosenAmount, setChosenAmount] = useState<number>(1);

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
    setGameState(state);
    if (isHotseat) setMyPlayerId(state.activePlayerId);
  }, [isHotseat]);

  const handleError = useCallback((msg: string) => {
    setError(msg);
    setTimeout(() => setError(''), 4000);
  }, []);

  const { connected, sendAction, resolveChoice, endPhase } = useGameHub({
    matchId,
    playerId: seat,
    onStateUpdate: handleStateUpdate,
    onError: handleError,
  });

  // In hotseat mode we act as whoever is active; in seat mode always as our seat
  const actAs = isHotseat ? myPlayerId : seat;

  async function handleAction(action: AvailableAction, targetIds?: string[], chosenAmount?: number) {
    try {
      if (action.type === 'endPhase') {
        await endPhase(actAs);
        return;
      }
      await sendAction(actAs, {
        type: action.type,
        sourceObjectId: action.sourceObjectId,
        abilityId: action.abilityId,
        targetIds: targetIds ?? [],
        chosenAmount,
      });
    } catch (err: any) {
      handleError(err.message ?? 'Action failed');
    }
  }

  function handleActionClick(action: AvailableAction) {
    if (!action.available) return;

    if (action.type === 'endPhase') {
      handleAction(action, []);
      return;
    }

    if (action.requiresChoice && action.validTargets && action.validTargets.length > 0) {
      // Need to select targets (and optionally an amount)
      setPendingAction(action);
      setSelectedTargets([]);
      setChosenAmount(action.requiresChoice.amountMin ?? 1);
    } else if (action.requiresChoice && (!action.validTargets || action.validTargets.length === 0)) {
      // No valid targets, send with empty
      handleAction(action, []);
    } else {
      // No choice needed
      handleAction(action, []);
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
    handleAction(pendingAction, selectedTargets, amount);
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
        onActionClick={handleActionClick}
        onSelectTarget={handleSelectTarget}
        onAction={handleAction}
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
        onEndPhase={handleEndPhase}
        onLeave={onLeave}
      />
    </div>
  );
}

import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { GameStateDto, ActionRequest } from '../types/game';
import { BASE } from '../config';

interface UseGameHubOptions {
  matchId: string;
  playerId: string; // seat to join as ('' = omniscient hotseat)
  onStateUpdate: (state: GameStateDto) => void;
  onError: (message: string) => void;
}

export function useGameHub({ matchId, playerId, onStateUpdate, onError }: UseGameHubOptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}gamehub`, { withCredentials: true }) // send the auth cookie — the hub is [Authorize]-gated
      .withAutomaticReconnect()
      .build();

    connection.on('GameStateUpdate', (state: GameStateDto) => {
      onStateUpdate(state);
    });

    connection.on('ActionError', (message: string) => {
      onError(message);
    });

    connection.onreconnected(() => {
      connection.invoke('JoinMatch', matchId, playerId).catch(() => {});
    });

    connection.start()
      .then(() => {
        if (cancelled) return;
        setConnected(true);
        return connection.invoke('JoinMatch', matchId, playerId);
      })
      .catch(err => {
        if (cancelled) return;
        console.error('SignalR connection failed:', err);
        onError('Failed to connect to game server');
      });

    connectionRef.current = connection;

    return () => {
      cancelled = true;
      connection.stop().catch(() => {});
    };
  }, [matchId, playerId]);

  const sendAction = useCallback(async (asPlayerId: string, action: ActionRequest) => {
    if (!connectionRef.current) return;
    await connectionRef.current.invoke('SendAction', matchId, asPlayerId, action);
  }, [matchId]);

  const resolveChoice = useCallback(async (asPlayerId: string, choiceId: string, selectedIds: string[]) => {
    if (!connectionRef.current) return;
    await connectionRef.current.invoke('ResolveChoice', matchId, asPlayerId, choiceId, selectedIds);
  }, [matchId]);

  const endPhase = useCallback(async (asPlayerId: string) => {
    if (!connectionRef.current) return;
    await connectionRef.current.invoke('EndPhase', matchId, asPlayerId);
  }, [matchId]);

  return { connected, sendAction, resolveChoice, endPhase };
}

import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { GameStateDto, ActionRequest } from '../types/game';

interface UseGameHubOptions {
  matchId: string;
  playerId: string;
  onStateUpdate: (state: GameStateDto) => void;
  onError: (message: string) => void;
}

export function useGameHub({ matchId, playerId, onStateUpdate, onError }: UseGameHubOptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/gamehub', {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('GameStateUpdate', (state: GameStateDto) => {
      onStateUpdate(state);
    });

    connection.on('ActionError', (message: string) => {
      onError(message);
    });

    connection.start()
      .then(() => {
        if (cancelled) return;
        setConnected(true);
        return connection.invoke('JoinMatch', matchId, playerId);
      })
      .catch(err => {
        if (cancelled) return; // suppress error when component unmounts mid-connect
        console.error('SignalR connection failed:', err);
        onError('Failed to connect to game server');
      });

    connectionRef.current = connection;

    return () => {
      cancelled = true;
      connection.stop().catch(() => {});
    };
  }, [matchId, playerId]);

  const sendAction = useCallback(async (action: ActionRequest) => {
    if (!connectionRef.current) return;
    await connectionRef.current.invoke('SendAction', matchId, playerId, action);
  }, [matchId, playerId]);

  const resolveChoice = useCallback(async (choiceId: string, selectedIds: string[]) => {
    if (!connectionRef.current) return;
    await connectionRef.current.invoke('ResolveChoice', matchId, playerId, choiceId, selectedIds);
  }, [matchId, playerId]);

  const endPhase = useCallback(async () => {
    if (!connectionRef.current) return;
    await connectionRef.current.invoke('EndPhase', matchId, playerId);
  }, [matchId, playerId]);

  return { connected, sendAction, resolveChoice, endPhase };
}

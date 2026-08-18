import React, { useEffect, useRef } from 'react';

interface GameLogProps {
  log: string[];
}

export function GameLog({ log }: GameLogProps) {
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [log]);

  return (
    <div className="game-log">
      <div className="game-log-header">Game Log</div>
      <div className="game-log-entries">
        {log.map((entry, i) => (
          <div key={i} className={`log-entry ${getLogClass(entry)}`}>
            {entry}
          </div>
        ))}
        <div ref={bottomRef} />
      </div>
    </div>
  );
}

function getLogClass(entry: string): string {
  if (entry.startsWith('===')) return 'log-turn';
  if (entry.startsWith('---')) return 'log-phase';
  if (entry.includes('destroyed')) return 'log-destroy';
  if (entry.includes('wins')) return 'log-win';
  if (entry.includes('loses') || entry.includes('Defeated')) return 'log-lose';
  if (entry.includes('attacks')) return 'log-combat';
  if (entry.includes('summons')) return 'log-summon';
  if (entry.includes('gains') || entry.includes('heals')) return 'log-gain';
  return 'log-normal';
}

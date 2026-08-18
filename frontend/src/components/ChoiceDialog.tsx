import React, { useState } from 'react';
import { PendingChoice, ObjectStateDto } from '../types/game';

interface ChoiceDialogProps {
  choice: PendingChoice;
  objects: ObjectStateDto[];
  onResolve: (choiceId: string, selectedIds: string[]) => void;
}

export function ChoiceDialog({ choice, objects, onResolve }: ChoiceDialogProps) {
  const [selected, setSelected] = useState<string[]>([]);

  const validObjects = objects.filter(o => choice.validOptions.includes(o.id));

  function toggleSelect(id: string) {
    setSelected(prev => {
      if (prev.includes(id)) {
        return prev.filter(x => x !== id);
      }
      if (prev.length >= choice.definition.max) {
        // Replace oldest if at max
        return [...prev.slice(1), id];
      }
      return [...prev, id];
    });
  }

  function canConfirm() {
    return selected.length >= choice.definition.min && selected.length <= choice.definition.max;
  }

  return (
    <div className="choice-dialog-overlay">
      <div className="choice-dialog">
        <h3>Choose Target{choice.definition.max > 1 ? 's' : ''}</h3>
        <p className="choice-hint">
          Select {choice.definition.min === choice.definition.max
            ? choice.definition.min
            : `${choice.definition.min}-${choice.definition.max}`} target{choice.definition.max > 1 ? 's' : ''}
          {choice.definition.objectType ? ` (${choice.definition.objectType})` : ''}
        </p>

        <div className="choice-options">
          {validObjects.length === 0 && (
            <div className="no-targets">No valid targets available</div>
          )}
          {validObjects.map(obj => (
            <div
              key={obj.id}
              className={`choice-option ${selected.includes(obj.id) ? 'selected' : ''}`}
              onClick={() => toggleSelect(obj.id)}
            >
              <span className="choice-obj-name">{obj.name}</span>
              <span className="choice-obj-type">{obj.objectType}</span>
              {obj.properties['currentHp'] !== undefined && (
                <span className="choice-obj-hp">
                  {obj.properties['currentHp']}/{obj.properties['maxHp']} HP
                </span>
              )}
            </div>
          ))}
        </div>

        <div className="choice-buttons">
          {validObjects.length === 0 ? (
            <button
              className="confirm-btn"
              onClick={() => onResolve(choice.id, [])}
            >
              Cancel (No Targets)
            </button>
          ) : (
            <button
              className="confirm-btn"
              disabled={!canConfirm()}
              onClick={() => onResolve(choice.id, selected)}
            >
              Confirm ({selected.length}/{choice.definition.max})
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

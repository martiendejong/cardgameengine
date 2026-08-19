import React from 'react';
import { ObjectStateDto, AvailableAction } from '../types/game';

interface CardViewProps {
  card: ObjectStateDto;
  actions: AvailableAction[];
  attachments?: ObjectStateDto[];
  isSelectableTarget: boolean;
  isSelectedTarget: boolean;
  onAction: (action: AvailableAction, targetIds?: string[]) => void;
  onSelectTarget: (id: string) => void;
}

export function CardView({
  card,
  actions,
  attachments = [],
  isSelectableTarget,
  isSelectedTarget,
  onAction,
  onSelectTarget,
}: CardViewProps) {
  const hp = card.properties['currentHp'] ?? 0;
  const maxHp = card.properties['maxHp'] ?? 0;
  const attack = card.properties['attack'] ?? 0;
  const armor = card.properties['armor'] ?? 0;
  const ap = card.resources['ap'];

  const isCharacter = ['character', 'hero', 'unit'].some(t =>
    card.objectType === t || card.objectType.includes(t)
  );
  const isHero = card.objectType === 'hero';
  const isBuilding = card.objectType === 'building' || card.objectType === 'headquarters';

  const hpPct = maxHp > 0 ? (hp / maxHp) * 100 : 0;
  const hpColor = hpPct > 60 ? '#4caf50' : hpPct > 30 ? '#ff9800' : '#f44336';

  const cardActions = actions.filter(a => a.sourceObjectId === card.id);

  function getTypeIcon() {
    if (card.objectType === 'hero') return '★';
    if (card.objectType === 'headquarters') return '🏛';
    if (card.objectType === 'unit') return '⚔';
    if (card.objectType === 'building') return '🏠';
    if (card.objectType === 'spell') return '✨';
    if (card.objectType === 'module') return '⚙';
    return '□';
  }

  // Deterministic art gradient per card definition
  function artGradient(): string {
    let hash = 0;
    for (let i = 0; i < card.definitionId.length; i++) {
      hash = (hash * 31 + card.definitionId.charCodeAt(i)) >>> 0;
    }
    const hue1 = hash % 360;
    const hue2 = (hue1 + 45) % 360;
    return `linear-gradient(135deg, hsl(${hue1}, 45%, 22%), hsl(${hue2}, 55%, 14%))`;
  }

  function getTypeLabel() {
    switch (card.objectType) {
      case 'hero': return 'Hero';
      case 'headquarters': return 'HQ';
      case 'unit': return 'Unit';
      case 'building': return 'Building';
      case 'spell': return 'Spell';
      default: return card.objectType;
    }
  }

  return (
    <div
      className={[
        'card',
        card.isTapped ? 'tapped' : '',
        card.isDestroyed ? 'destroyed' : '',
        isSelectableTarget ? 'selectable-target' : '',
        isSelectedTarget ? 'selected-target' : '',
        `card-type-${card.objectType}`,
      ].filter(Boolean).join(' ')}
      onClick={() => {
        if (isSelectableTarget) onSelectTarget(card.id);
      }}
    >
      <div className="card-header">
        <span className="card-type-icon">{getTypeIcon()}</span>
        <span className="card-name">{card.name}</span>
        <span className="card-type-label">{getTypeLabel()}</span>
      </div>

      <div className="card-art" style={{ background: artGradient() }}>
        <span className="card-art-icon">{card.icon || getTypeIcon()}</span>
      </div>

      {card.isTapped && <div className="tapped-indicator">TAPPED</div>}
      {card.hasSummoningSickness && card.zoneId === 'battlefield' && (
        <div className="status-indicator new-indicator">NEW</div>
      )}
      {card.hasMovedThisTurn && !card.hasSummoningSickness && (
        <div className="status-indicator moved-indicator">MOVED</div>
      )}

      <div className="card-stats">
        {(isCharacter || isBuilding) && (
          <div className="hp-bar-container">
            <div
              className="hp-bar"
              style={{ width: `${hpPct}%`, backgroundColor: hpColor }}
            />
            <span className="hp-text">{hp}/{maxHp} HP</span>
          </div>
        )}

        <div className="stat-row">
          {attack > 0 && <span className="stat atk" title="Attack">⚔ {attack}</span>}
          {armor > 0 && <span className="stat arm" title="Armor">🛡 {armor}</span>}
          {ap !== undefined && (
            <span className="stat ap" title="Action Points">AP: {ap}</span>
          )}
          {Object.entries(card.resources)
            .filter(([key, val]) => key !== 'ap' && val > 0)
            .map(([key, val]) => (
              <span key={key} className="stat tokens" title={key}>
                ◈ {val}
              </span>
            ))}
        </div>

        {card.tags.length > 0 && (
          <div className="tags">
            {card.tags.map(tag => (
              <span key={tag} className="tag">{tag}</span>
            ))}
          </div>
        )}
      </div>

      {attachments.length > 0 && (
        <div className="attachments">
          {attachments.map(mod => {
            const modActions = actions.filter(a => a.sourceObjectId === mod.id);
            return (
              <div key={mod.id} className={`module-chip ${mod.isTapped ? 'module-tapped' : ''}`}>
                <span className="module-name" title={mod.slot ?? ''}>
                  ⚙ {mod.name}
                </span>
                {modActions.map(action => (
                  <button
                    key={`${action.sourceObjectId}-${action.abilityId || action.type}`}
                    className={`action-btn module-btn ${action.available ? 'available' : 'unavailable'}`}
                    disabled={!action.available}
                    title={action.unavailableReason || action.label}
                    onClick={e => {
                      e.stopPropagation();
                      if (action.available) onAction(action);
                    }}
                  >
                    {action.label.split(': ')[1] || action.label}
                  </button>
                ))}
              </div>
            );
          })}
        </div>
      )}

      {cardActions.length > 0 && (
        <div className="card-actions">
          {cardActions.map(action => (
            <button
              key={`${action.sourceObjectId}-${action.abilityId || action.type}`}
              className={`action-btn ${action.available ? 'available' : 'unavailable'}`}
              disabled={!action.available}
              title={action.unavailableReason || action.label}
              onClick={e => {
                e.stopPropagation();
                if (action.available) onAction(action);
              }}
            >
              {action.type === 'attack' ? '⚔ Attack' : action.label.split(': ')[1] || action.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

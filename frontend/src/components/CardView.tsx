import React from 'react';
import { ObjectStateDto, AvailableAction } from '../types/game';
import { STAT_TIPS, TAG_TIPS, STATUS_TIPS, HOUSING_COST_TIP, HOUSING_PROVIDED_TIP } from '../utils/cardText';
import cardBg from '../assets/card.png';
import imgTownChief from '../assets/cards/town-chief.png';
import imgPeasant from '../assets/cards/peasant.png';
import imgBagOfGold from '../assets/cards/bag-of-gold.png';
import imgGoldMine from '../assets/cards/gold-mine.png';
import imgMilitia from '../assets/cards/militia.png';
import imgTownHall from '../assets/cards/town-hall.png';
import imgArcher from '../assets/cards/archer.png';
import imgArcheryRange from '../assets/cards/archery-range.png';
import imgBarracks from '../assets/cards/barracks.png';
import imgPaladin from '../assets/cards/paladin.png';
import imgSoldier from '../assets/cards/soldier.png';
import imgArchmage from '../assets/cards/archmage.png';
import imgHealingSalve from '../assets/cards/healing-salve.png';
import imgWoodenPalisade from '../assets/cards/wooden-palisade.png';
import imgPyromancer from '../assets/cards/pyromancer.png';
import imgScavenger from '../assets/cards/scavenger.png';
import imgWarchief from '../assets/cards/warchief.png';

const CARD_ART: Record<string, string> = {
  'town-chief': imgTownChief,
  'peasant': imgPeasant,
  'bag-of-gold': imgBagOfGold,
  'gold-mine': imgGoldMine,
  'militia': imgMilitia,
  'town-hall': imgTownHall,
  'archer': imgArcher,
  'archery-range': imgArcheryRange,
  'barracks': imgBarracks,
  'paladin': imgPaladin,
  'soldier': imgSoldier,
  'archmage': imgArchmage,
  'healing-salve': imgHealingSalve,
  'wooden-palisade': imgWoodenPalisade,
  'pyromancer': imgPyromancer,
  'scavenger': imgScavenger,
  'warchief': imgWarchief,
};

interface CardViewProps {
  card: ObjectStateDto;
  actions: AvailableAction[];
  attachments?: ObjectStateDto[];
  isSelectableTarget: boolean;
  isSelectedTarget: boolean;
  playCost?: number | null;
  playCostResource?: string;
  onAction: (action: AvailableAction, targetIds?: string[]) => void;
  onSelectTarget: (id: string) => void;
  onInspect?: (objectId: string) => void;
}

export function CardView({
  card,
  actions,
  attachments = [],
  isSelectableTarget,
  isSelectedTarget,
  playCost,
  playCostResource,
  onAction,
  onSelectTarget,
  onInspect,
}: CardViewProps) {
  const hp = card.properties['currentHp'] ?? 0;
  const maxHp = card.properties['maxHp'] ?? 0;
  const attack = card.properties['attack'] ?? 0;
  const armor = card.properties['armor'] ?? 0;

  const isCharacter = ['character', 'hero', 'unit'].some(t =>
    card.objectType === t || card.objectType.includes(t)
  );
  const isBuilding = card.objectType === 'building' || card.objectType === 'headquarters';

  const hpPct = maxHp > 0 ? (hp / maxHp) * 100 : 0;
  const hpColor = hpPct > 60 ? '#4caf50' : hpPct > 30 ? '#ff9800' : '#f44336';

  // Only show available actions on the small card
  const availableActions = actions.filter(a => a.sourceObjectId === card.id && a.available);

  function getTypeIcon() {
    if (card.objectType === 'hero') return '★';
    if (card.objectType === 'headquarters') return '🏛';
    if (card.objectType === 'unit') return '⚔';
    if (card.objectType === 'building') return '🏠';
    if (card.objectType === 'spell') return '✨';
    if (card.objectType === 'module') return '⚙';
    return '□';
  }

  function artGradient(): string {
    let hash = 0;
    for (let i = 0; i < card.definitionId.length; i++) {
      hash = (hash * 31 + card.definitionId.charCodeAt(i)) >>> 0;
    }
    const hue1 = hash % 360;
    const hue2 = (hue1 + 45) % 360;
    return `linear-gradient(135deg, hsl(${hue1}, 45%, 22%), hsl(${hue2}, 55%, 14%))`;
  }

  const costLabel = playCost != null
    ? `${playCost}${playCostResource === 'energy' ? '⚡' : '💰'}`
    : null;

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
      style={{ backgroundImage: `url(${cardBg})`, backgroundSize: '100% 100%', backgroundRepeat: 'no-repeat' }}
      onClick={() => {
        if (isSelectableTarget) onSelectTarget(card.id);
        else if (onInspect && card.definitionId !== 'hidden') onInspect(card.id);
      }}
    >
      {costLabel && <span className="card-cost">{costLabel}</span>}

      <div className="card-header">
        <span className="card-name">{card.name}</span>
      </div>

      {CARD_ART[card.definitionId] ? (
        <div
          className="card-art"
          style={{
            backgroundImage: `url(${CARD_ART[card.definitionId]})`,
          }}
        />
      ) : (
        <div className="card-art" style={{ background: artGradient() }}>
          <span className="card-art-icon">{card.icon || getTypeIcon()}</span>
        </div>
      )}

      {card.isTapped && <div className="tapped-indicator">TAPPED</div>}
      {card.underConstruction && (
        <div
          className="status-indicator construction-indicator tip"
          data-tip="Under construction — provides no abilities until finished."
        >
          🔨 {card.constructionProgress}/{card.constructionRequirement ?? '?'}
        </div>
      )}
      {card.hasSummoningSickness && card.zoneId === 'battlefield' && (
        <div className="status-indicator new-indicator tip" data-tip={STATUS_TIPS.new}>NEW</div>
      )}
      {card.hasMovedThisTurn && !card.hasSummoningSickness && (
        <div className="status-indicator moved-indicator tip" data-tip={STATUS_TIPS.moved}>MOVED</div>
      )}

      <div className="card-stats">
        {(isCharacter || isBuilding) && maxHp > 0 && (
          <div className="hp-bar-container">
            <div className="hp-bar" style={{ width: `${hpPct}%`, backgroundColor: hpColor }} />
            <span className="hp-text">{hp}/{maxHp} HP</span>
          </div>
        )}

        <div className="stat-row">
          {attack > 0 && <span className="stat atk tip" data-tip={STAT_TIPS.attack}>⚔ {attack}</span>}
          {armor > 0 && <span className="stat arm tip" data-tip={STAT_TIPS.armor}>🛡 {armor}</span>}
          {card.lifetime != null && (
            <span className="stat lifetime tip" data-tip="Duration — expires at 0.">
              ⏳ {card.lifetime}
            </span>
          )}
          {card.housingProvided != null && (
            <span className="stat housing tip" data-tip={HOUSING_PROVIDED_TIP(card.housingProvided)}>
              🏠 +{card.housingProvided}
            </span>
          )}
        </div>
      </div>

      {attachments.length > 0 && (
        <div className="attachments">
          {attachments.map(mod => {
            const modActions = actions.filter(a => a.sourceObjectId === mod.id && a.available);
            return (
              <div key={mod.id} className={`module-chip ${mod.isTapped ? 'module-tapped' : ''}`}>
                <span
                  className="module-name"
                  title={mod.slot ?? ''}
                  onClick={e => {
                    e.stopPropagation();
                    if (onInspect) onInspect(mod.id);
                  }}
                >
                  {mod.icon ?? '⚙'} {mod.name}
                </span>
                {modActions.map(action => (
                  <button
                    key={`${action.sourceObjectId}-${action.abilityId || action.type}`}
                    className="action-btn module-btn available"
                    title={action.label}
                    onClick={e => {
                      e.stopPropagation();
                      onAction(action);
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

      {availableActions.length > 0 && (
        <div className="card-actions">
          {availableActions.map(action => (
            <button
              key={`${action.sourceObjectId}-${action.abilityId || action.type}`}
              className="action-btn available"
              title={action.label}
              onClick={e => {
                e.stopPropagation();
                onAction(action);
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

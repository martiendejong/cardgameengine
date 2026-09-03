import { useEffect } from 'react';
import { ObjectStateDto, CardDefinitionDto, AvailableAction } from '../types/game';
import { DeckControl } from './CardView';
import {
  explainCost, explainCondition, explainChoice, explainEffect, explainTrigger,
  STAT_TIPS, TAG_TIPS, TYPE_TIPS, STATUS_TIPS, playCostTip, slotTip,
  HOUSING_COST_TIP, HOUSING_PROVIDED_TIP,
} from '../utils/cardText';
import { CARD_ART_LARGE as CARD_ART, cardFrameLarge as cardBg } from '../assets/cardArt';
import icoAttack from '../assets/icons/icon-attack.png';
import icoHitPoints from '../assets/icons/icon-hit-points.png';
import icoArmor from '../assets/icons/icon-armor.png';
import icoGold from '../assets/icons/icon-gold.png';
import icoHousing from '../assets/icons/icon-housing.png';
import icoAP from '../assets/icons/icon-action-points.png';
import icoMagic from '../assets/icons/icon-magic-points.png';
import icoTrigger from '../assets/icons/icon-trigger.png';

const STAT_ICONS: Record<string, string> = {
  attack: icoAttack,
  hp: icoHitPoints,
  armor: icoArmor,
  gold: icoGold,
  housing: icoHousing,
  ap: icoAP,
  magic: icoMagic,
  trigger: icoTrigger,
};

interface CardDetailModalProps {
  card: ObjectStateDto;
  def?: CardDefinitionDto;
  attachments: ObjectStateDto[];
  nameOf: (cardId: string) => string;
  actions?: AvailableAction[];
  onAction?: (action: AvailableAction) => void;
  onClose: () => void;
  onInspect: (objectId: string) => void;
  /** When set, renders a small add/remove icon for use in deck-builder card pools/lists. */
  deckControl?: DeckControl;
}

function typeLabel(objectType: string): string {
  const labels: Record<string, string> = {
    hero: 'Hero', headquarters: 'Headquarters', unit: 'Unit', building: 'Building',
    spell: 'Spell', item: 'Item', module: 'Module', treasure: 'Treasure',
  };
  return labels[objectType] ?? objectType;
}

function artGradient(definitionId: string): string {
  let hash = 0;
  for (let i = 0; i < definitionId.length; i++) {
    hash = (hash * 31 + definitionId.charCodeAt(i)) >>> 0;
  }
  const hue1 = hash % 360;
  const hue2 = (hue1 + 45) % 360;
  return `linear-gradient(135deg, hsl(${hue1}, 45%, 24%), hsl(${hue2}, 55%, 14%))`;
}

export function CardDetailModal({ card, def, attachments, nameOf, actions, onAction, onClose, onInspect, deckControl }: CardDetailModalProps) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  const hp = card.properties['currentHp'];
  const maxHp = card.properties['maxHp'];
  const attack = card.properties['attack'] ?? 0;
  const armor = card.properties['armor'] ?? 0;
  const ap = card.resources['ap'];
  const hasHp = maxHp !== undefined && maxHp > 0 &&
    ['character', 'hero', 'unit', 'building', 'headquarters', 'treasure'].some(
      t => card.objectType === t) || (maxHp ?? 0) > 1;

  const abilities = def?.abilities ?? [];
  const triggers = def?.triggers ?? [];

  const hpPct = (maxHp ?? 0) > 0 ? ((hp ?? 0) / (maxHp as number)) * 100 : 0;
  const hpColor = hpPct > 60 ? '#4caf50' : hpPct > 30 ? '#ff9800' : '#f44336';

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="card-detail"
        style={{ backgroundImage: `url(${cardBg})`, backgroundSize: '100% 100%', backgroundRepeat: 'no-repeat' }}
        onClick={e => e.stopPropagation()}
      >
        <button className="modal-close" onClick={onClose}>✕</button>

        {deckControl && (
          <button
            type="button"
            className="deck-toggle-icon detail-deck-toggle deck-toggle-add"
            disabled={!deckControl.canAdd}
            title={deckControl.canAdd ? 'Add to deck' : 'Maximum copies reached'}
            onClick={e => { e.stopPropagation(); deckControl.onAdd(); }}
          >
            +
          </button>
        )}
        {deckControl && deckControl.count > 0 && (
          <button
            type="button"
            className="deck-toggle-icon detail-deck-toggle detail-deck-toggle-remove deck-toggle-remove"
            title={`In deck (${deckControl.count}) — click to remove one`}
            onClick={e => { e.stopPropagation(); deckControl.onRemove(); }}
          >
            −
          </button>
        )}
        {deckControl && deckControl.count > 0 && (
          <span className="deck-toggle-count detail-deck-toggle-count">×{deckControl.count}</span>
        )}

        {/* Title */}
        <div className="detail-header">
          <h2 className="detail-name">{card.name}</h2>
          <div className="detail-header-meta">
            {def?.playCost !== null && def?.playCost !== undefined && (
              <span className="tip detail-cost" data-tip={playCostTip(def.playCost, def.playCostResource ?? 'gold')}>
                {def.playCost} <img src={def.playCostResource === 'energy' ? STAT_ICONS.ap : STAT_ICONS.gold} className="detail-stat-icon" alt="" />
              </span>
            )}
            {def?.slot && (
              <span className="tip detail-slot" data-tip={slotTip(def.slot)}>Slot: {def.slot}</span>
            )}
          </div>
        </div>

        {/* Art */}
        {CARD_ART[card.definitionId] ? (
          <div className="detail-art-banner" style={{ backgroundImage: `url(${CARD_ART[card.definitionId]})` }} />
        ) : (
          <div className="detail-art-banner" style={{ background: artGradient(card.definitionId) }}>
            <span className="detail-art-icon">{card.icon || '□'}</span>
          </div>
        )}

        {/* HP bar directly below art */}
        {hasHp && (
          <div className="detail-hp-container">
            <div className="detail-hp-bar" style={{ width: `${hpPct}%`, backgroundColor: hpColor }} />
            <span className="detail-hp-text"><img src={STAT_ICONS.hp} className="stat-icon stat-icon-hp-detail" alt="" />{hp}/{maxHp}</span>
          </div>
        )}

        {/* Scrollable content area */}
        <div className="card-detail-scroll">

        {/* Action buttons — top of scroll so always reachable */}
        {actions && actions.length > 0 && (
          <div className="detail-live-actions">
            {actions.map(action => (
              <button
                key={`${action.sourceObjectId}-${action.abilityId || action.type}`}
                className={`action-btn detail-action-btn ${action.available ? 'available' : ''}`}
                disabled={!action.available}
                onClick={() => { if (onAction) { onAction(action); onClose(); } }}
                title={action.available ? action.label : 'Not available right now'}
              >
                {action.type === 'attack' ? '⚔ Attack' : action.label.split(': ')[1] || action.label}
              </button>
            ))}
          </div>
        )}

        <div className="detail-stats">
          {attack > 0 && (
            <span className="tip detail-stat atk" data-tip={STAT_TIPS.attack}><img src={STAT_ICONS.attack} className="detail-stat-icon" alt="" />{attack} Attack</span>
          )}
          {armor > 0 && (
            <span className="tip detail-stat arm" data-tip={STAT_TIPS.armor}><img src={STAT_ICONS.armor} className="detail-stat-icon" alt="" />{armor} Armor</span>
          )}
          {ap !== undefined && (
            <span className="tip detail-stat ap" data-tip={STAT_TIPS.ap}><img src={STAT_ICONS.ap} className="detail-stat-icon" alt="" />{ap} AP</span>
          )}
          {card.resources['mana'] !== undefined && (
            <span className="tip detail-stat mana" data-tip="Mana — spent on caster abilities and spells."><img src={STAT_ICONS.magic} className="detail-stat-icon" alt="" />{card.resources['mana']} Mana</span>
          )}
          {card.resources['loot'] !== undefined && card.resources['loot'] > 0 && (
            <span className="tip detail-stat loot" data-tip={STAT_TIPS.loot}>◈ {card.resources['loot']} Tokens</span>
          )}
          {Object.entries(card.resources)
            .filter(([k]) => !['ap', 'mana', 'loot'].includes(k))
            .filter(([, v]) => (v as number) > 0)
            .map(([k, v]) => (
              <span key={k} className="tip detail-stat res-other" data-tip={k}>
                {k} {v as number}
              </span>
            ))
          }
          {def?.bonusAttackVsBuildings && (
            <span className="tip detail-stat bonus" data-tip="This bonus is added to Attack when the target is a building.">
              +{def.bonusAttackVsBuildings} vs Buildings
            </span>
          )}
          {def?.housingCost != null && (
            <span className="tip detail-stat housing" data-tip={HOUSING_COST_TIP(def.housingCost)}>
              <img src={STAT_ICONS.housing} className="detail-stat-icon" alt="" />Needs {def.housingCost}
            </span>
          )}
          {def?.housingProvided != null && (
            <span className="tip detail-stat housing" data-tip={HOUSING_PROVIDED_TIP(def.housingProvided)}>
              <img src={STAT_ICONS.housing} className="detail-stat-icon" alt="" />+{def.housingProvided} Housing
            </span>
          )}
          <span className="tip detail-type" data-tip={TYPE_TIPS[card.objectType] ?? ''}>
            {typeLabel(card.objectType)}
          </span>
        </div>

        {(card.tags.length > 0 || card.isTapped || card.hasSummoningSickness || card.hasMovedThisTurn) && (
          <div className="detail-tags">
            {card.tags.map(tag => (
              <span key={tag} className="tip detail-tag" data-tip={TAG_TIPS[tag] ?? tag}>{tag}</span>
            ))}
            {card.isTapped && (
              <span className="tip detail-status tapped" data-tip={STATUS_TIPS.tapped}>TAPPED</span>
            )}
            {card.hasSummoningSickness && card.zoneId === 'battlefield' && (
              <span className="tip detail-status new" data-tip={STATUS_TIPS.new}>NEW</span>
            )}
            {card.hasMovedThisTurn && (
              <span className="tip detail-status moved" data-tip={STATUS_TIPS.moved}>MOVED</span>
            )}
          </div>
        )}

        <div className="detail-body">
          {def?.onPlay && (() => {
            const tip = [
              def.onPlay.choice ? explainChoice(def.onPlay.choice) : '',
              ...def.onPlay.effects.map(e => explainEffect(e, nameOf)),
            ].filter(Boolean).join(' · ');
            return (
              <div className="detail-ability tip" data-tip={tip || undefined}>
                <span className="ability-name">✨ On Play{def.onPlay.name ? ` — ${def.onPlay.name}` : ''}</span>
              </div>
            );
          })()}

          {abilities.map(ability => {
            const tip = [
              ability.costs.length > 0 ? `Cost: ${ability.costs.map(explainCost).join(' + ')}` : '',
              ...ability.conditions.map(explainCondition),
              ability.choice ? explainChoice(ability.choice) : '',
              ...ability.effects.map(e => explainEffect(e, nameOf)),
            ].filter(Boolean).join(' · ');
            const costLabel = ability.costs.length > 0 ? ability.costs.map(explainCost).join(' + ') : '';
            return (
              <div key={ability.id} className="detail-ability tip" data-tip={tip || undefined}>
                <span className="ability-name">◆ {ability.name}</span>
                {costLabel && <span className="ability-cost-badge">{costLabel}</span>}
              </div>
            );
          })}

          {triggers.map((t, i) => (
            <div key={i} className="detail-ability tip" data-tip={explainTrigger(t, nameOf)}>
              <span className="ability-name">⚡ Trigger</span>
            </div>
          ))}

          {def?.attachModifiers && def.attachModifiers.length > 0 && (() => {
            const tip = def.attachModifiers.map(m =>
              `+${m.amount} ${m.propertyId === 'attack' ? 'Attack' : m.propertyId === 'armor' ? 'Armor' : m.propertyId} to your hero`
            ).join(' · ');
            return (
              <div className="detail-ability tip" data-tip={tip}>
                <span className="ability-name">🔩 While installed</span>
              </div>
            );
          })()}

          {def?.equipmentSlots && (
            <div className="detail-ability tip" data-tip={Object.entries(def.equipmentSlots).map(([slot, cap]) => `${slot} ×${cap}`).join(' · ')}>
              <span className="ability-name">🔧 Equipment slots</span>
            </div>
          )}

          {attachments.length > 0 && (
            <div className="detail-ability">
              <span className="ability-name">⚙ Installed modules</span>
              {attachments.map(mod => (
                <div key={mod.id} className="ability-line effect module-link" onClick={() => onInspect(mod.id)}>
                  {mod.icon ?? '⚙'} {mod.name}{mod.isTapped ? ' — tapped' : ''} — click to inspect
                </div>
              ))}
            </div>
          )}

          {abilities.length === 0 && !def?.onPlay && triggers.length === 0 &&
            (!def?.attachModifiers || def.attachModifiers.length === 0) && (
            <div className="detail-ability">
              <span className="ability-line effect">No special abilities — it fights with its stats.</span>
            </div>
          )}

        </div>

        {def?.artworkDescription && (
          <div className="detail-flavor">{def.artworkDescription}</div>
        )}

        </div>
      </div>
    </div>
  );
}

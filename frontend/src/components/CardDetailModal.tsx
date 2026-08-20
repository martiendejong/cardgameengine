import { useEffect } from 'react';
import { ObjectStateDto, CardDefinitionDto } from '../types/game';
import {
  explainCost, explainCondition, explainChoice, explainEffect, explainTrigger,
  STAT_TIPS, TAG_TIPS, TYPE_TIPS, STATUS_TIPS, playCostTip, slotTip,
  HOUSING_COST_TIP, HOUSING_PROVIDED_TIP,
} from '../utils/cardText';
import cardBg from '../assets/card.png';
import imgTownChief from '../assets/cards/town-chief.png';
import imgPeasant from '../assets/cards/peasant.png';
import imgBagOfGold from '../assets/cards/bag-of-gold.png';
import imgGoldMine from '../assets/cards/gold-mine.png';
import imgMilitia from '../assets/cards/militia.png';
import imgTownHall from '../assets/cards/town-hall.png';

const CARD_ART: Record<string, string> = {
  'town-chief': imgTownChief,
  'peasant': imgPeasant,
  'bag-of-gold': imgBagOfGold,
  'gold-mine': imgGoldMine,
  'militia': imgMilitia,
  'town-hall': imgTownHall,
};

interface CardDetailModalProps {
  card: ObjectStateDto;
  def?: CardDefinitionDto;
  attachments: ObjectStateDto[];
  nameOf: (cardId: string) => string;
  onClose: () => void;
  onInspect: (objectId: string) => void;
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

export function CardDetailModal({ card, def, attachments, nameOf, onClose, onInspect }: CardDetailModalProps) {
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

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="card-detail"
        style={{ backgroundImage: `url(${cardBg})`, backgroundSize: '100% 100%', backgroundRepeat: 'no-repeat' }}
        onClick={e => e.stopPropagation()}
      >
        <button className="modal-close" onClick={onClose}>✕</button>

        <div className="detail-header">
          {CARD_ART[card.definitionId] ? (
            <div
              className="detail-art"
              style={{
                backgroundImage: `url(${CARD_ART[card.definitionId]})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center',
              }}
            />
          ) : (
            <div className="detail-art" style={{ background: artGradient(card.definitionId) }}>
              <span className="detail-art-icon">{card.icon || '□'}</span>
            </div>
          )}
          <div className="detail-title">
            <h2>{card.name}</h2>
            <span className="tip detail-type" data-tip={TYPE_TIPS[card.objectType] ?? ''}>
              {typeLabel(card.objectType)}
            </span>
            {def?.playCost !== null && def?.playCost !== undefined && (
              <span
                className="tip detail-cost"
                data-tip={playCostTip(def.playCost, def.playCostResource ?? 'gold')}
              >
                Cost: {def.playCost} {def.playCostResource === 'energy' ? '⚡' : '💰'}
              </span>
            )}
            {def?.slot && (
              <span className="tip detail-slot" data-tip={slotTip(def.slot)}>
                Slot: {def.slot}
              </span>
            )}
          </div>
        </div>

        <div className="detail-stats">
          {attack > 0 && (
            <span className="tip detail-stat atk" data-tip={STAT_TIPS.attack}>⚔ {attack} Attack</span>
          )}
          {hasHp && (
            <span className="tip detail-stat hp" data-tip={STAT_TIPS.hp}>❤ {hp}/{maxHp} HP</span>
          )}
          {armor > 0 && (
            <span className="tip detail-stat arm" data-tip={STAT_TIPS.armor}>🛡 {armor} Armor</span>
          )}
          {ap !== undefined && (
            <span className="tip detail-stat ap" data-tip={STAT_TIPS.ap}>⚡ {ap} AP</span>
          )}
          {card.resources['loot'] !== undefined && card.resources['loot'] > 0 && (
            <span className="tip detail-stat loot" data-tip={STAT_TIPS.loot}>◈ {card.resources['loot']} Tokens</span>
          )}
          {def?.bonusAttackVsBuildings && (
            <span
              className="tip detail-stat bonus"
              data-tip="This bonus is added to Attack when the target is a building."
            >
              +{def.bonusAttackVsBuildings} vs Buildings
            </span>
          )}
          {def?.housingCost != null && (
            <span className="tip detail-stat housing" data-tip={HOUSING_COST_TIP(def.housingCost)}>
              🏠 Needs {def.housingCost} housing
            </span>
          )}
          {def?.housingProvided != null && (
            <span className="tip detail-stat housing" data-tip={HOUSING_PROVIDED_TIP(def.housingProvided)}>
              🏠 Provides {def.housingProvided} housing
            </span>
          )}
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
          {def?.onPlay && (
            <div className="detail-ability">
              <div className="ability-name">✨ On Play{def.onPlay.name ? ` — ${def.onPlay.name}` : ''}</div>
              {def.onPlay.choice && <div className="ability-line target">{explainChoice(def.onPlay.choice)}</div>}
              {def.onPlay.effects.map((e, i) => (
                <div key={i} className="ability-line effect">→ {explainEffect(e, nameOf)}</div>
              ))}
            </div>
          )}

          {abilities.map(ability => (
            <div key={ability.id} className="detail-ability">
              <div className="ability-name">◆ {ability.name}</div>
              {ability.costs.length > 0 && (
                <div className="ability-line cost">
                  Cost: {ability.costs.map(explainCost).join(' + ')}
                </div>
              )}
              {ability.conditions.map((c, i) => (
                <div key={i} className="ability-line condition">{explainCondition(c)}</div>
              ))}
              {ability.choice && <div className="ability-line target">{explainChoice(ability.choice)}</div>}
              {ability.effects.map((e, i) => (
                <div key={i} className="ability-line effect">→ {explainEffect(e, nameOf)}</div>
              ))}
            </div>
          ))}

          {triggers.map((t, i) => (
            <div key={i} className="detail-ability trigger">
              <div className="ability-name">⚡ Trigger</div>
              <div className="ability-line effect">{explainTrigger(t, nameOf)}</div>
            </div>
          ))}

          {def?.attachModifiers && def.attachModifiers.length > 0 && (
            <div className="detail-ability">
              <div className="ability-name">🔩 While installed</div>
              {def.attachModifiers.map((m, i) => (
                <div key={i} className="ability-line effect">
                  → Your hero gets +{m.amount} {m.propertyId === 'attack' ? 'Attack' : m.propertyId === 'armor' ? 'Armor' : m.propertyId}
                </div>
              ))}
            </div>
          )}

          {def?.equipmentSlots && (
            <div className="detail-ability">
              <div className="ability-name">🔧 Equipment slots</div>
              <div className="ability-line effect">
                {Object.entries(def.equipmentSlots).map(([slot, cap]) => `${slot} ×${cap}`).join(' · ')}
              </div>
            </div>
          )}

          {attachments.length > 0 && (
            <div className="detail-ability">
              <div className="ability-name">⚙ Installed modules</div>
              {attachments.map(mod => (
                <div key={mod.id} className="ability-line effect module-link" onClick={() => onInspect(mod.id)}>
                  {mod.icon ?? '⚙'} {mod.name} ({mod.slot}){mod.isTapped ? ' — tapped' : ''} — click to inspect
                </div>
              ))}
            </div>
          )}

          {abilities.length === 0 && !def?.onPlay && triggers.length === 0 &&
            (!def?.attachModifiers || def.attachModifiers.length === 0) && (
            <div className="detail-ability">
              <div className="ability-line effect">No special abilities — it fights with its stats.</div>
            </div>
          )}
        </div>

        {def?.artworkDescription && (
          <div className="detail-flavor">“{def.artworkDescription}”</div>
        )}
      </div>
    </div>
  );
}

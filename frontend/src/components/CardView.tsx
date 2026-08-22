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
import imgBoneCollector from '../assets/cards/bone-collector.png';
import imgLeatherArmor from '../assets/cards/leather-armor.png';
import imgRaiderCamp from '../assets/cards/raider-camp.png';
import imgRaider from '../assets/cards/raider.png';
import imgSkeleton from '../assets/cards/skeleton.png';
import imgZombie from '../assets/cards/zombie.png';
import imgArcaneNexus from '../assets/cards/arcane-nexus.png';
import imgGraveyard from '../assets/cards/graveyard.png';
import imgCutthroat from '../assets/cards/cutthroat.png';
import imgMercenary from '../assets/cards/mercenary.png';
import imgTownGuard from '../assets/cards/town-guard.png';
import imgThievesGuild from '../assets/cards/thieves-guild.png';
import imgSpymaster from '../assets/cards/spymaster.png';
import imgFootpad from '../assets/cards/footpad.png';
import imgAxeThrower from '../assets/cards/axe-thrower.png';
import imgKnight from '../assets/cards/knight.png';
import imgConjuror from '../assets/cards/conjuror.png';
import imgKnifeThrower from '../assets/cards/knife-thrower.png';
import imgMageApprentice from '../assets/cards/mage-apprentice.png';
import imgFireElemental from '../assets/cards/fire-elemental.png';
import imgLeylineConduit from '../assets/cards/leyline-conduit.png';
import imgArcaneSentinel from '../assets/cards/arcane-sentinel.png';
import imgShrine from '../assets/cards/shrine.png';
import imgLarva from '../assets/cards/larva.png';
import imgEgg from '../assets/cards/egg.png';
import imgTheHive from '../assets/cards/the-hive.png';
import imgBroodmother from '../assets/cards/broodmother.png';
import imgAx01 from '../assets/cards/ax-01.png';
import imgHiveGuardian from '../assets/cards/hive-guardian.png';
import imgLandingPad from '../assets/cards/landing-pad.png';
import imgMuster from '../assets/cards/muster.png';
import imgTavern from '../assets/cards/tavern.png';
import imgSiegeRam from '../assets/cards/siege-ram.png';
import imgPlateArmor from '../assets/cards/plate-armor.png';
import imgLibrary from '../assets/cards/library.png';
import imgEmergencyRepairs from '../assets/cards/emergency-repairs.png';
import imgPillager from '../assets/cards/pillager.png';
import imgNecromancer from '../assets/cards/necromancer.png';
import icoAttack from '../assets/icons/icon-attack.png';
import icoHitPoints from '../assets/icons/icon-hit-points.png';
import icoArmor from '../assets/icons/icon-armor.png';
import icoGold from '../assets/icons/icon-gold.png';
import icoHousing from '../assets/icons/icon-housing.png';
import icoAP from '../assets/icons/icon-action-points.png';
import icoMagic from '../assets/icons/icon-magic-points.png';
import icoTrigger from '../assets/icons/icon-trigger.png';

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
  'bone-collector': imgBoneCollector,
  'leather-armor': imgLeatherArmor,
  'raider-camp': imgRaiderCamp,
  'raider': imgRaider,
  'raider-brigand': imgRaider,
  'skeleton': imgSkeleton,
  'zombie': imgZombie,
  'arcane-nexus': imgArcaneNexus,
  'graveyard': imgGraveyard,
  'cutthroat': imgCutthroat,
  'mercenary': imgMercenary,
  'town-guard': imgTownGuard,
  'thieves-guild': imgThievesGuild,
  'spymaster': imgSpymaster,
  'footpad': imgFootpad,
  'axe-thrower': imgAxeThrower,
  'knight': imgKnight,
  'conjuror': imgConjuror,
  'conjurer': imgConjuror,
  'knife-thrower': imgKnifeThrower,
  'mage-apprentice': imgMageApprentice,
  'apprentice-mage': imgMageApprentice,
  'fire-elemental': imgFireElemental,
  'leyline-conduit': imgLeylineConduit,
  'arcane-sentinel': imgArcaneSentinel,
  'shrine': imgShrine,
  'larva': imgLarva,
  'egg': imgEgg,
  'the-hive': imgTheHive,
  'broodmother': imgBroodmother,
  'ax-01': imgAx01,
  'hive-guardian': imgHiveGuardian,
  'hive-warden': imgHiveGuardian,
  'landing-pad': imgLandingPad,
  'muster': imgMuster,
  'tavern': imgTavern,
  'siege-ram': imgSiegeRam,
  'plate-armor': imgPlateArmor,
  'library': imgLibrary,
  'emergency-repairs': imgEmergencyRepairs,
  'pillager': imgPillager,
  'necromancer': imgNecromancer,
};

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

interface CardViewProps {
  card: ObjectStateDto;
  actions: AvailableAction[];
  attachments?: ObjectStateDto[];
  isSelectableTarget: boolean;
  isSelectedTarget: boolean;
  playCost?: number | null;
  playCostResource?: string;
  animClass?: string;
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
  animClass,
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
        animClass ?? '',
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
        >
          {card.hasSummoningSickness && card.zoneId === 'battlefield' && (
            <div className="status-indicator new-indicator tip" data-tip={STATUS_TIPS.new}>NEW</div>
          )}
        </div>
      ) : (
        <div className="card-art" style={{ background: artGradient() }}>
          <span className="card-art-icon">{card.icon || getTypeIcon()}</span>
          {card.hasSummoningSickness && card.zoneId === 'battlefield' && (
            <div className="status-indicator new-indicator tip" data-tip={STATUS_TIPS.new}>NEW</div>
          )}
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
      {card.hasMovedThisTurn && !card.hasSummoningSickness && (
        <div className="status-indicator moved-indicator tip" data-tip={STATUS_TIPS.moved}>MOVED</div>
      )}

      <div className="card-stats">
        {(isCharacter || isBuilding) && maxHp > 0 && (
          <div className="hp-bar-container">
            <div className="hp-bar" style={{ width: `${hpPct}%`, backgroundColor: hpColor }} />
            <span className="hp-text"><img src={STAT_ICONS.hp} className="stat-icon stat-icon-hp" alt="" />{hp}/{maxHp}</span>
          </div>
        )}

        <div className="stat-row">
          {attack > 0 && <span className="stat atk tip" data-tip={STAT_TIPS.attack}><img src={STAT_ICONS.attack} className="stat-icon" alt="" />{attack}</span>}
          {armor > 0 && <span className="stat arm tip" data-tip={STAT_TIPS.armor}><img src={STAT_ICONS.armor} className="stat-icon" alt="" />{armor}</span>}
          {card.lifetime != null && (
            <span className="stat lifetime tip" data-tip="Duration — expires at 0.">
              ⏳ {card.lifetime}
            </span>
          )}
          {card.housingProvided != null && (
            <span className="stat housing tip" data-tip={HOUSING_PROVIDED_TIP(card.housingProvided)}>
              <img src={STAT_ICONS.housing} className="stat-icon" alt="" />+{card.housingProvided}
            </span>
          )}
          {card.resources['ap'] !== undefined && (
            <span className="stat res-ap tip" data-tip={STAT_TIPS.ap}>
              <img src={STAT_ICONS.ap} className="stat-icon" alt="" />{card.resources['ap']}
            </span>
          )}
          {card.resources['mana'] !== undefined && (
            <span className="stat res-mana tip" data-tip="Mana — spent on caster abilities and spells.">
              <img src={STAT_ICONS.magic} className="stat-icon" alt="" />{card.resources['mana']}
            </span>
          )}
          {Object.entries(card.resources)
            .filter(([k]) => !['ap', 'mana', 'loot'].includes(k))
            .filter(([, v]) => (v as number) > 0)
            .map(([k, v]) => (
              <span key={k} className="stat res-other tip" data-tip={k}>
                {k.substring(0, 3)} {v as number}
              </span>
            ))
          }
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

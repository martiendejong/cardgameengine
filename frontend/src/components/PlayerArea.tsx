import React from 'react';
import { PlayerStateDto, ObjectStateDto, AvailableAction, CardDefinitionDto } from '../types/game';
import { CardView } from './CardView';
import { LINE_TIP_FRONT, LINE_TIP_BACK, HOUSING_TIP } from '../utils/cardText';
import bgHuman from '../assets/bg-human.png';
import bgRaiders from '../assets/bg-raiders.png';
import bgArcane from '../assets/bg-arcane.png';
import bgBrood from '../assets/bg-brood.png';
import bgShadows from '../assets/bg-shadows.png';
import bgAx01 from '../assets/bg-ax01.png';
import cardBack from '../assets/card-back.png';

const HQ_BACKGROUNDS: Record<string, string> = {
  'town-hall': bgHuman,
  'settlement': bgHuman,
  'raider-camp': bgRaiders,
  'arcane-nexus': bgArcane,
  'the-hive': bgBrood,
  'thieves-guild': bgShadows,
  'landing-pad': bgAx01,
};

interface PlayerAreaProps {
  player: PlayerStateDto;
  objects: ObjectStateDto[];
  actions: AvailableAction[];
  cardDefs: Record<string, CardDefinitionDto>;
  isActivePlayer: boolean;
  isBottom: boolean;
  selectableTargets: string[];
  selectedTargets: string[];
  cardAnims: Record<string, string>;
  onAction: (action: AvailableAction) => void;
  onSelectTarget: (id: string) => void;
  onInspect: (objectId: string) => void;
}

export function PlayerArea({
  player,
  objects,
  actions,
  cardDefs,
  isActivePlayer,
  isBottom,
  selectableTargets,
  selectedTargets,
  cardAnims,
  onAction,
  onSelectTarget,
  onInspect,
}: PlayerAreaProps) {
  // HQ objectType varies per faction ('headquarters', 'nexus', 'hive-hq', ...) so match on known HQ ids
  const hq = objects.find(o => o.ownerId === player.id && HQ_BACKGROUNDS[o.definitionId] !== undefined);
  const bg = hq ? HQ_BACKGROUNDS[hq.definitionId] : undefined;

  const battlefieldCards = objects.filter(
    o => o.controllerId === player.id && o.zoneId === 'battlefield' && !o.isDestroyed && !o.attachedToId
  );
  const attachmentsFor = (cardId: string) =>
    objects.filter(o => o.attachedToId === cardId && !o.isDestroyed);
  const discardedCards = objects.filter(
    o => o.controllerId === player.id && (o.zoneId === 'discard' || o.isDestroyed)
  );
  const handCards = objects.filter(
    o => o.ownerId === player.id && o.zoneId === 'hand'
  );
  const deckCount = objects.filter(
    o => o.ownerId === player.id && o.zoneId === 'deck'
  ).length;

  const handArea = (
    <div className="hand-area">
      <span className="hand-label">
        Hand ({handCards.length}) · Deck ({deckCount})
      </span>
      {isBottom ? (
        <div className="hand-cards">
          {handCards.length === 0 && <span className="empty-hand">No cards in hand</span>}
          {handCards.map(card => (
            <CardView
              key={card.id}
              card={card}
              actions={actions}
              isSelectableTarget={false}
              isSelectedTarget={false}
              playCost={cardDefs[card.definitionId]?.playCost}
              playCostResource={cardDefs[card.definitionId]?.playCostResource}
              animClass={cardAnims[card.id]}
              onAction={onAction}
              onSelectTarget={onSelectTarget}
              onInspect={onInspect}
            />
          ))}
        </div>
      ) : (
        <div className="hand-cards">
          {handCards.map(card => (
            <img key={card.id} className="card-back" src={cardBack} title="Opponent's card" alt="" />
          ))}
        </div>
      )}
    </div>
  );

  return (
    <div
      className={`player-area ${isBottom ? 'bottom-player' : 'top-player'} ${isActivePlayer ? 'active-player' : ''} ${player.isLoser ? 'loser' : ''} ${player.isWinner ? 'winner' : ''}`}
      style={bg ? { backgroundImage: `url(${bg})`, backgroundSize: 'cover', backgroundPosition: 'center' } : undefined}
    >
      {!isBottom && handArea}
      <div className="player-header">
        <div className="player-name-section">
          <span className="player-name">{player.name}</span>
          {isActivePlayer && <span className="active-badge">ACTIVE</span>}
          {player.isWinner && <span className="winner-badge">WINNER</span>}
          {player.isLoser && <span className="loser-badge">DEFEATED</span>}
        </div>
        <div className="player-resources">
          {player.usesHousing && (
            <div
              className={`resource-chip tip ${player.housingUsed >= player.housingCapacity ? 'housing-full' : ''}`}
              data-tip={HOUSING_TIP}
            >
              <span className="resource-icon">🏠</span>
              <span className="resource-label">Housing</span>
              <span className="resource-value">{player.housingUsed}/{player.housingCapacity}</span>
            </div>
          )}
          {Object.entries(player.resources)
            .filter(([key]) =>
              !player.relevantResources || player.relevantResources.length === 0
                ? true
                : player.relevantResources.includes(key))
            .map(([key, val]) => (
              <div key={key} className="resource-chip">
                <span className="resource-icon">{key === 'gold' ? '💰' : '⚡'}</span>
                <span className="resource-label">{key === 'gold' ? 'Gold' : key}</span>
                <span className="resource-value">{val}</span>
              </div>
            ))}
        </div>
      </div>

      <div className="battlefield-lines">
        {(isBottom ? ['front', 'back'] : ['back', 'front']).map(line => {
          const lineCards = battlefieldCards.filter(c => (c.line || 'back') === line);
          return (
            <div key={line} className={`battle-line line-${line}`}>
              <span
                className="line-label tip"
                data-tip={line === 'front' ? LINE_TIP_FRONT : LINE_TIP_BACK}
              >
                {line === 'front' ? 'Front Line' : 'Back Line'}
              </span>
              <div className="line-cards">
                {lineCards.length === 0 && <span className="empty-line">—</span>}
                {lineCards.map(card => (
                  <CardView
                    key={card.id}
                    card={card}
                    attachments={attachmentsFor(card.id)}
                    actions={isBottom ? actions : []}
                    isSelectableTarget={selectableTargets.includes(card.id)}
                    isSelectedTarget={selectedTargets.includes(card.id)}
                    playCost={cardDefs[card.definitionId]?.playCost}
                    playCostResource={cardDefs[card.definitionId]?.playCostResource}
                    animClass={cardAnims[card.id]}
                    onAction={onAction}
                    onSelectTarget={onSelectTarget}
                    onInspect={onInspect}
                  />
                ))}
              </div>
            </div>
          );
        })}
      </div>

      {isBottom && handArea}

      {discardedCards.length > 0 && (
        <div className="discard-area">
          <span className="discard-label">Discard ({discardedCards.length}):</span>
          {discardedCards.map(c => (
            <span key={c.id} className="discarded-card-name">{c.name}</span>
          ))}
        </div>
      )}
    </div>
  );
}

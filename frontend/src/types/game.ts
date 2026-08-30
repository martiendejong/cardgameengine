// Matches C# GameState enum
export enum GameState {
  WaitingForAction = 0,
  WaitingForChoice = 1,
  WaitingForReaction = 2,
  ResolvingStack = 3,
  GameEnded = 4,
}

export interface PlayerStateDto {
  id: string;
  name: string;
  resources: Record<string, number>;
  relevantResources: string[];
  isWinner: boolean;
  isLoser: boolean;
  isBot: boolean;
  usesHousing: boolean;
  housingUsed: number;
  housingCapacity: number;
}

export interface ObjectStateDto {
  id: string;
  definitionId: string;
  name: string;
  objectType: string;
  ownerId: string;
  controllerId: string;
  zoneId: string;
  properties: Record<string, number>;
  resources: Record<string, number>;
  tags: string[];
  isTapped: boolean;
  isDestroyed: boolean;
  line: string;
  hasMovedThisTurn: boolean;
  hasSummoningSickness: boolean;
  attachedToId?: string | null;
  slot?: string | null;
  icon?: string | null;
  housingCost?: number | null;
  housingProvided?: number | null;
  underConstruction: boolean;
  constructionProgress: number;
  constructionRequirement?: number | null;
  lifetime?: number | null;
  faceDown?: boolean;
}

export interface ChoiceDefinition {
  type: string;
  controller: string;
  objectType?: string;
  tag?: string;
  min: number;
  max: number;
  chooseAmount?: boolean;
  amountMin?: number;
  amountMaxResource?: string;
}

export interface AvailableAction {
  type: string; // "activateAbility" | "attack" | "endPhase"
  sourceObjectId?: string;
  abilityId?: string;
  label: string;
  available: boolean;
  unavailableReason?: string;
  requiresChoice?: ChoiceDefinition;
  validTargets?: string[];
  amountMax?: number;
}

export interface PendingChoice {
  id: string;
  playerId: string;
  stackItemId: string;
  definition: ChoiceDefinition;
  validOptions: string[];
}

export interface GameStateDto {
  matchId: string;
  gameId: string;
  currentPhaseId: string;
  activePlayerId: string;
  state: GameState;
  players: PlayerStateDto[];
  objects: ObjectStateDto[];
  availableActions: AvailableAction[];
  pendingChoice?: PendingChoice;
  log: string[];
  winner?: string;
  turnNumber: number;
  reactionPlayerId?: string | null;
  reactionWindowEvent?: string | null;
  encounter?: EncounterDto | null;
}

export interface EncounterDto {
  missionId: string;
  playerId: string; // the human seat in a campaign match
  rewardCards: string[];
  rewardCopies: number; // playset size granted per reward card
  pendingSpawnCount: number;
}

export interface CampaignMission {
  id: string;
  campaign: string;
  name: string;
  description: string;
  rewards: string[];
  completed: boolean;
  unlocked: boolean;
}

export interface CampaignOverview {
  profile: {
    name: string;
    completedMissions: string[];
    collection: Record<string, number>;
  };
  collectionCount: number;
  requiredCards: number;
  multiplayerUnlocked: boolean;
  missions: CampaignMission[];
}

export interface ActionRequest {
  type: string;
  sourceObjectId?: string;
  abilityId?: string;
  targetIds: string[];
  chosenAmount?: number;
}

export interface GameDefinitionSummary {
  id: string;
  name: string;
  version: string;
}

export interface PlayerSetup {
  name: string;
  id?: string;
  deck?: Record<string, number>;
  isAdmin?: boolean;
}

export interface CostDto {
  type: string;
  resourceId?: string | null;
  amount?: number | null;
  scope?: string | null;
  tag?: string | null;
}

export interface ConditionDto {
  type: string;
  resourceId?: string | null;
  amount?: number | null;
  tag?: string | null;
  phase?: string | null;
  scope?: string | null;
}

export interface EffectDto {
  type: string;
  scope?: string | null;
  resourceId?: string | null;
  propertyId?: string | null;
  amount?: number | null;
  cardId?: string | null;
  tag?: string | null;
  ignoreHousing?: boolean | null;
}

export interface AbilityDto {
  id: string;
  name: string;
  costs: CostDto[];
  conditions: ConditionDto[];
  choice?: ChoiceDefinition | null;
  effects: EffectDto[];
}

export interface TriggerDto {
  event: string;
  oncePerTurn: boolean;
  effects: EffectDto[];
}

export interface AttachModifierDto {
  propertyId: string;
  amount: number;
}

export interface CardDefinitionDto {
  id: string;
  name: string;
  objectType: string;
  icon?: string | null;
  playCost?: number | null;
  playCostResource?: string;
  playCosts?: Record<string, number> | null;
  deckLimit?: string | null;
  constructionRequirement?: number | null;
  slots?: string[] | null;
  attachTags?: string[];
  properties: Record<string, number>;
  tags: string[];
  abilities: AbilityDto[];
  onPlay?: AbilityDto | null;
  triggers?: TriggerDto[];
  slot?: string | null;
  attachModifiers?: AttachModifierDto[];
  equipmentSlots?: Record<string, number> | null;
  bonusAttackVsBuildings?: number | null;
  artworkDescription?: string | null;
  housingCost?: number | null;
  housingProvided?: number | null;
}

export interface DeckRulesDto {
  maxCopies: number;
  maxDeckSize: number;
  minDeckSize: number;
  startingHandSize: number;
  drawPerTurn: number;
  defaultDeck: Record<string, number>;
}

export interface PreconDeckDto {
  id: string;
  name: string;
  description: string;
  hq: string;
  hero: string;
  hqOptions: string[];
  heroOptions: string[];
  cards: Record<string, number>;
}

export interface ObjectTypeDto {
  id: string;
  name: string;
  parentType?: string | null;
}

export interface GameDefinitionFull {
  id: string;
  name: string;
  version: string;
  cards: CardDefinitionDto[];
  deckRules?: DeckRulesDto | null;
  decks: PreconDeckDto[];
  objectTypes?: ObjectTypeDto[];
}

// A player-built, named deck saved server-side under a profile name (My Decks page).
export interface SavedDeck {
  id: string;
  gameId: string;
  name: string;
  hqId?: string | null;
  heroId?: string | null;
  cards: Record<string, number>;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMatchRequest {
  gameId: string;
  players: PlayerSetup[];
}

export interface CreateMatchResponse {
  matchId: string;
  players: { id: string; name: string }[];
}

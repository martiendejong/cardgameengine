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
  isWinner: boolean;
  isLoser: boolean;
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
}

export interface ChoiceDefinition {
  type: string;
  controller: string;
  objectType?: string;
  tag?: string;
  min: number;
  max: number;
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
}

export interface ActionRequest {
  type: string;
  sourceObjectId?: string;
  abilityId?: string;
  targetIds: string[];
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

export interface AbilitySummary {
  id: string;
  name: string;
}

export interface CardDefinitionDto {
  id: string;
  name: string;
  objectType: string;
  playCost?: number | null;
  properties: Record<string, number>;
  tags: string[];
  abilities: AbilitySummary[];
  onPlay?: AbilitySummary | null;
  artworkDescription?: string;
}

export interface DeckRulesDto {
  maxCopies: number;
  maxDeckSize: number;
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
  cards: Record<string, number>;
}

export interface GameDefinitionFull {
  id: string;
  name: string;
  version: string;
  cards: CardDefinitionDto[];
  deckRules?: DeckRulesDto | null;
  decks: PreconDeckDto[];
}

export interface CreateMatchRequest {
  gameId: string;
  players: PlayerSetup[];
}

export interface CreateMatchResponse {
  matchId: string;
  players: { id: string; name: string }[];
}

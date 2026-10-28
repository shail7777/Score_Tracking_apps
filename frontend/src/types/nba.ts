export interface Team {
  id: number;
  name: string;
  city: string;
  conference: string;
  division: string;
}

export interface Player {
  id: number;
  firstName: string;
  lastName: string;
  heightInches: number | null;
  weightLbs: number | null;
  birthDate: string | null;
  currentTeamId: number | null;
  currentTeamName: string | null;
}

export interface TeamPlayer {
  id: number;
  teamId: number;
  teamName: string;
  playerId: number;
  playerName: string;
  startDate: string;
  endDate: string | null;
  isActive: boolean;
}

export type GameStatus = 'Scheduled' | 'InProgress' | 'Final';

export interface Game {
  id: number;
  season: string;
  gameDate: string;
  arena: string | null;
  status: GameStatus;
  homeTeamId: number;
  homeTeamName: string;
  awayTeamId: number;
  awayTeamName: string;
  homeScore: number | null;
  awayScore: number | null;
  winnerTeamId: number | null;
}

// A game from one team's point of view (team schedule).
export interface TeamGame {
  id: number;
  season: string;
  gameDate: string;
  arena: string | null;
  status: GameStatus;
  isHome: boolean;
  opponentId: number;
  opponentName: string;
  teamScore: number | null;
  opponentScore: number | null;
  result: 'W' | 'L' | null;
}

export interface ScorecardPlayer {
  playerId: number;
  name: string;
  points: number;
  twoPointers: number;
  threePointers: number;
}

export interface ScorecardTeam {
  teamId: number;
  city: string;
  name: string;
  totalPoints: number;
  players: ScorecardPlayer[];
}

export interface ScorecardEvent {
  id: number;
  quarter: number;
  clockSeconds: number | null;
  teamId: number;
  teamName: string;
  playerId: number;
  playerName: string;
  points: number;
  awayScore: number;
  homeScore: number;
  createdAt: string;
}

export interface Scorecard {
  gameId: number;
  season: string;
  gameDate: string;
  arena: string | null;
  status: GameStatus;
  eventCount: number;
  isLocked: boolean;
  quarter: number;
  clockSeconds: number;
  awayTeam: ScorecardTeam;
  homeTeam: ScorecardTeam;
  events: ScorecardEvent[];
}

export type TeamInput = Omit<Team, 'id'>;
export type PlayerInput = Pick<
  Player,
  'firstName' | 'lastName' | 'heightInches' | 'weightLbs' | 'birthDate'
>;
export type TeamPlayerInput = Pick<
  TeamPlayer,
  'teamId' | 'playerId' | 'startDate' | 'endDate'
>;
export type GameInput = Pick<
  Game,
  'season' | 'gameDate' | 'arena' | 'status' | 'homeTeamId' | 'awayTeamId' | 'homeScore' | 'awayScore'
>;

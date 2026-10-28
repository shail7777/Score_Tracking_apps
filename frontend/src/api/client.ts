import type {
  Game,
  GameInput,
  GameStatus,
  Player,
  PlayerInput,
  Scorecard,
  Team,
  TeamGame,
  TeamInput,
  TeamPlayer,
  TeamPlayerInput,
} from '../types/nba';

const BASE_URL = import.meta.env.VITE_API_URL ?? '';

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });
  if (!res.ok) {
    let detail = '';
    try {
      detail = ((await res.json()) as { title?: string }).title ?? '';
    } catch {
      /* response had no JSON body */
    }
    throw new ApiError(
      detail || `${init?.method ?? 'GET'} ${path} failed: ${res.status} ${res.statusText}`,
      res.status,
    );
  }
  return res.status === 204 ? (undefined as T) : ((await res.json()) as T);
}

const json = (method: string, body: unknown): RequestInit => ({ method, body: JSON.stringify(body) });

const qs = (params: Record<string, string | number | boolean | undefined>) => {
  const sp = new URLSearchParams();
  Object.entries(params).forEach(([k, v]) => {
    if (v !== undefined && v !== '') sp.set(k, String(v));
  });
  const s = sp.toString();
  return s ? `?${s}` : '';
};

export const api = {
  teams: {
    list: () => request<Team[]>('/api/teams'),
    get: (id: number) => request<Team>(`/api/teams/${id}`),
    roster: (id: number, activeOnly = true) =>
      request<TeamPlayer[]>(`/api/teams/${id}/roster${qs({ activeOnly })}`),
    games: (id: number, status?: GameStatus) =>
      request<TeamGame[]>(`/api/teams/${id}/games${qs({ status })}`),
    create: (t: TeamInput) => request<Team>('/api/teams', json('POST', t)),
    update: (id: number, t: TeamInput) => request<void>(`/api/teams/${id}`, json('PUT', t)),
    remove: (id: number) => request<void>(`/api/teams/${id}`, { method: 'DELETE' }),
  },
  players: {
    list: (params: { teamId?: number; search?: string } = {}) =>
      request<Player[]>(`/api/players${qs(params)}`),
    get: (id: number) => request<Player>(`/api/players/${id}`),
    history: (id: number) => request<TeamPlayer[]>(`/api/players/${id}/history`),
    create: (p: PlayerInput) => request<Player>('/api/players', json('POST', p)),
    update: (id: number, p: PlayerInput) => request<void>(`/api/players/${id}`, json('PUT', p)),
    remove: (id: number) => request<void>(`/api/players/${id}`, { method: 'DELETE' }),
  },
  teamPlayers: {
    list: (params: { teamId?: number; playerId?: number; activeOnly?: boolean } = {}) =>
      request<TeamPlayer[]>(`/api/teamplayers${qs(params)}`),
    create: (tp: TeamPlayerInput) => request<TeamPlayer>('/api/teamplayers', json('POST', tp)),
    update: (id: number, tp: TeamPlayerInput) => request<void>(`/api/teamplayers/${id}`, json('PUT', tp)),
    remove: (id: number) => request<void>(`/api/teamplayers/${id}`, { method: 'DELETE' }),
  },
  games: {
    list: (params: { teamId?: number; status?: GameStatus; season?: string } = {}) =>
      request<Game[]>(`/api/games${qs(params)}`),
    get: (id: number) => request<Game>(`/api/games/${id}`),
    scorecard: (id: number) => request<Scorecard>(`/api/games/${id}/scorecard`),
    recordEvent: (gameId: number, playerId: number, points: 2 | 3, at?: { quarter: number; clockSeconds: number }) =>
      request<Scorecard>(`/api/games/${gameId}/events`, json('POST', { playerId, points, ...at })),
    setClock: (gameId: number, quarter: number, clockSeconds: number) =>
      request<Scorecard>(`/api/games/${gameId}/clock`, json('PUT', { quarter, clockSeconds })),
    undoLast: (gameId: number) =>
      request<Scorecard>(`/api/games/${gameId}/events/last`, { method: 'DELETE' }),
    finish: (gameId: number) => request<Scorecard>(`/api/games/${gameId}/finish`, { method: 'POST' }),
    create: (g: GameInput) => request<Game>('/api/games', json('POST', g)),
    update: (id: number, g: GameInput) => request<void>(`/api/games/${id}`, json('PUT', g)),
    remove: (id: number) => request<void>(`/api/games/${id}`, { method: 'DELETE' }),
  },
};

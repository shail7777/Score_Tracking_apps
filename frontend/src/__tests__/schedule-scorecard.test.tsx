import { cleanup, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from '../api/client';
import SchedulePanel from '../components/SchedulePanel';
import ScorecardPage from '../pages/ScorecardPage';
import type { Scorecard, TeamGame } from '../types/nba';

afterEach(cleanup);

const teamGame = (over: Partial<TeamGame>): TeamGame => ({
  id: 1, season: '2025-26', gameDate: '2026-04-01T19:30:00', arena: null, status: 'Final',
  isHome: true, opponentId: 2, opponentName: 'Celtics', teamScore: null, opponentScore: null, result: null,
  ...over,
});

describe('SchedulePanel', () => {
  it('lists games with home/away labels, outcome and a scorecard link', async () => {
    vi.spyOn(api.teams, 'games').mockResolvedValue([
      teamGame({ id: 11, isHome: true, opponentName: 'Celtics', teamScore: 112, opponentScore: 105, result: 'W' }),
      teamGame({ id: 12, isHome: false, opponentName: 'Heat', teamScore: 90, opponentScore: 99, result: 'L' }),
      teamGame({ id: 13, isHome: true, opponentName: 'Bulls' }),
      teamGame({ id: 14, isHome: false, opponentName: 'Nets', status: 'Scheduled' }),
      teamGame({ id: 15, isHome: true, opponentName: 'Suns', status: 'InProgress' }),
    ]);

    render(<MemoryRouter><SchedulePanel teamId={1} /></MemoryRouter>);

    const rows = await screen.findAllByRole('row');
    const text = rows.slice(1).map((r) => r.textContent ?? '');
    expect(text[0]).toContain('vs Celtics');
    expect(text[0]).toContain('W 112–105');
    expect(text[1]).toContain('@ Heat');
    expect(text[1]).toContain('L 90–99');
    expect(text[2]).toContain('Final');
    expect(text[3]).toContain('Upcoming');
    expect(text[4]).toContain('In progress');

    const links = screen.getAllByRole('link');
    expect(links.map((a) => a.getAttribute('href'))).toEqual([
      '/games/11/scorecard', '/games/12/scorecard', '/games/13/scorecard', '/games/14/scorecard', '/games/15/scorecard',
    ]);
  });

  it('shows an empty state', async () => {
    vi.spyOn(api.teams, 'games').mockResolvedValue([]);
    render(<MemoryRouter><SchedulePanel teamId={1} /></MemoryRouter>);
    expect(await screen.findByText('No games scheduled.')).toBeTruthy();
  });
});

const scorecard = (over: Partial<Scorecard> = {}): Scorecard => ({
  gameId: 7, season: '2025-26', gameDate: '2026-04-02T02:00:00', arena: 'TD Garden', status: 'InProgress', eventCount: 3, isLocked: false, quarter: 1, clockSeconds: 720, events: [],
  awayTeam: {
    teamId: 1, city: 'Los Angeles', name: 'Lakers', totalPoints: 5,
    players: [
      { playerId: 10, name: 'LeBron James', points: 5, twoPointers: 1, threePointers: 1 },
      { playerId: 11, name: 'Austin Reaves', points: 0, twoPointers: 0, threePointers: 0 },
    ],
  },
  homeTeam: {
    teamId: 2, city: 'Boston', name: 'Celtics', totalPoints: 3,
    players: [{ playerId: 20, name: 'Jayson Tatum', points: 3, twoPointers: 0, threePointers: 1 }],
  },
  ...over,
});

describe('ScorecardPage', () => {
  const renderAt = (path: string) =>
    render(
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/" element={<p>home</p>} />
          <Route path="/games/:gameId/scorecard" element={<ScorecardPage />} />
        </Routes>
      </MemoryRouter>,
    );

  it('shows the score header and a table of players for each team', async () => {
    const spy = vi.spyOn(api.games, 'scorecard').mockResolvedValue(scorecard());
    renderAt('/games/7/scorecard');

    expect(await screen.findByLabelText('Lakers score')).toBeTruthy();
    expect(screen.getByLabelText('Lakers score').textContent).toBe('5');
    expect(screen.getByLabelText('Celtics score').textContent).toBe('3');
    expect(screen.getByText('In progress')).toBeTruthy();
    expect(spy).toHaveBeenCalledWith(7);

    const away = within(screen.getByRole('region', { name: /Away team/ }));
    expect(away.getByRole('columnheader', { name: 'PTS' })).toBeTruthy();
    const lebron = away.getByRole('row', { name: /LeBron James/ });
    expect(within(lebron).getAllByRole('cell').slice(0, 3).map((c) => c.textContent)).toEqual(['5', '1', '1']);
    expect(away.getByText('Austin Reaves')).toBeTruthy();
    expect(away.getByRole('row', { name: /Team total/ }).textContent).toContain('5');

    const home = within(screen.getByRole('region', { name: /Home team/ }));
    expect(home.getByText('Jayson Tatum')).toBeTruthy();
  });

  it('explains when a finished game has no play-by-play', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(scorecard({ status: 'Final', eventCount: 0 }));
    renderAt('/games/7/scorecard');
    expect(await screen.findByText(/No play-by-play was recorded for this game/)).toBeTruthy();
  });

  it('shows a not-found state for an unknown game', async () => {
    vi.spyOn(api.games, 'scorecard').mockRejectedValue(new ApiError('Not Found', 404));
    renderAt('/games/999/scorecard');
    await waitFor(() => expect(screen.getByText('Game not found')).toBeTruthy());
  });
});

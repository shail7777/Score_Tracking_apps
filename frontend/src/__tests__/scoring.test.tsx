import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from '../api/client';
import ScorecardPage from '../pages/ScorecardPage';
import type { Scorecard } from '../types/nba';

afterEach(cleanup);

const base = (over: Partial<Scorecard> = {}): Scorecard => ({
  gameId: 7, season: '2025-26', gameDate: '2026-04-02T02:00:00', arena: null,
  status: 'Scheduled', eventCount: 0, isLocked: false, quarter: 1, clockSeconds: 720, events: [],
  awayTeam: {
    teamId: 1, city: 'Los Angeles', name: 'Lakers', totalPoints: 0,
    players: [{ playerId: 10, name: 'LeBron James', points: 0, twoPointers: 0, threePointers: 0 }],
  },
  homeTeam: {
    teamId: 2, city: 'Boston', name: 'Celtics', totalPoints: 0,
    players: [{ playerId: 20, name: 'Jayson Tatum', points: 0, twoPointers: 0, threePointers: 0 }],
  },
  ...over,
});

// A scorecard where LeBron has scored `points` via `twos`/`threes` baskets.
const scored = (points: number, twos: number, threes: number, over: Partial<Scorecard> = {}): Scorecard =>
  base({
    status: 'InProgress', eventCount: twos + threes,
    awayTeam: {
      teamId: 1, city: 'Los Angeles', name: 'Lakers', totalPoints: points,
      players: [{ playerId: 10, name: 'LeBron James', points, twoPointers: twos, threePointers: threes }],
    },
    ...over,
  });

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={['/games/7/scorecard']}>
      <Routes>
        <Route path="/games/:gameId/scorecard" element={<ScorecardPage />} />
      </Routes>
    </MemoryRouter>,
  );

const btn = (name: string) => screen.getByRole('button', { name }) as HTMLButtonElement;

describe('ScorecardPage scoring', () => {
  it('+3 records a three for that player and shows the returned totals', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(base());
    const record = vi.spyOn(api.games, 'recordEvent').mockResolvedValue(scored(3, 0, 1));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add 3 points for LeBron James' }));

    await waitFor(() => expect(screen.getByLabelText('Lakers score').textContent).toBe('3'));
    expect(record).toHaveBeenCalledWith(7, 10, 3, { quarter: 1, clockSeconds: 720 });
    const row = screen.getByRole('row', { name: /LeBron James/ });
    expect(within(row).getAllByRole('cell').slice(0, 3).map((c) => c.textContent)).toEqual(['3', '0', '1']);
    expect(screen.getByText('In progress')).toBeTruthy();
  });

  it('+2 records a two for a home-team player', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(base());
    const record = vi.spyOn(api.games, 'recordEvent').mockResolvedValue(base({ status: 'InProgress', eventCount: 1 }));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add 2 points for Jayson Tatum' }));

    await waitFor(() => expect(record).toHaveBeenCalledWith(7, 20, 2, { quarter: 1, clockSeconds: 720 }));
  });

  it('disables the buttons while a request is in flight', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(base());
    let finish!: (s: Scorecard) => void;
    vi.spyOn(api.games, 'recordEvent').mockReturnValue(new Promise<Scorecard>((r) => (finish = r)));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add 2 points for LeBron James' }));

    await waitFor(() => expect(btn('Add 3 points for LeBron James').disabled).toBe(true));
    finish(scored(2, 1, 0));
    await waitFor(() => expect(btn('Add 3 points for LeBron James').disabled).toBe(false));
  });

  it('shows the server error when recording fails', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(base());
    vi.spyOn(api.games, 'recordEvent').mockRejectedValue(new ApiError('Player is not on either team\'s roster for this game.', 400));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add 2 points for LeBron James' }));

    expect((await screen.findByRole('alert')).textContent).toContain('not on either team');
    expect(screen.getByLabelText('Lakers score').textContent).toBe('0');
  });

  it('reloads the scorecard after a 409 conflict', async () => {
    const load = vi.spyOn(api.games, 'scorecard').mockResolvedValue(base());
    vi.spyOn(api.games, 'recordEvent').mockRejectedValue(new ApiError('The game was updated by someone else.', 409));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add 2 points for LeBron James' }));

    await waitFor(() => expect(load).toHaveBeenCalledTimes(2));
    expect((await screen.findByRole('alert')).textContent).toContain('updated by someone else');
  });

  it('undo calls the API and shows the reverted score', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(scored(3, 0, 1));
    const undo = vi.spyOn(api.games, 'undoLast').mockResolvedValue(base());
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Undo last' }));

    await waitFor(() => expect(screen.getByLabelText('Lakers score').textContent).toBe('0'));
    expect(undo).toHaveBeenCalledWith(7);
  });

  it('undo and finish are disabled until something has been scored', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(base());
    renderPage();
    await screen.findByLabelText('Lakers score');

    expect(btn('Undo last').disabled).toBe(true);
    expect(btn('Finish game').disabled).toBe(true);
  });

  it('finishing asks for confirmation and locks the page', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(scored(3, 0, 1));
    const finish = vi.spyOn(api.games, 'finish').mockResolvedValue(scored(3, 0, 1, { status: 'Final', isLocked: true }));
    const confirm = vi.spyOn(window, 'confirm');
    renderPage();
    const finishBtn = await screen.findByRole('button', { name: 'Finish game' });

    confirm.mockReturnValueOnce(false);
    fireEvent.click(finishBtn);
    expect(finish).not.toHaveBeenCalled();

    confirm.mockReturnValueOnce(true);
    fireEvent.click(finishBtn);

    await waitFor(() => expect(finish).toHaveBeenCalledWith(7));
    expect(await screen.findByText('This game is final; scoring is locked.')).toBeTruthy();
    expect(btn('Add 2 points for LeBron James').disabled).toBe(true);
    expect(btn('Add 3 points for Jayson Tatum').disabled).toBe(true);
    expect(btn('Undo last').disabled).toBe(true);
    expect(btn('Finish game').disabled).toBe(true);
  });

  it('a past game with no events can still be scored', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(base({ status: 'Final', eventCount: 0, isLocked: false }));
    renderPage();

    const add = await screen.findByRole('button', { name: 'Add 2 points for LeBron James' });
    expect((add as HTMLButtonElement).disabled).toBe(false);
    expect(screen.getByText(/No play-by-play was recorded/)).toBeTruthy();
  });
});

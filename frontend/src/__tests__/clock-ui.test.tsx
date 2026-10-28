import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import ScoringHistory from '../components/ScoringHistory';
import ScorecardPage from '../pages/ScorecardPage';
import type { Scorecard, ScorecardEvent } from '../types/nba';

afterEach(cleanup);

const play = (id: number, over: Partial<ScorecardEvent> = {}): ScorecardEvent => ({
  id, quarter: 1, clockSeconds: 600, teamId: 1, teamName: 'Lakers', playerId: 10, playerName: 'LeBron James',
  points: 3, awayScore: 3, homeScore: 0, createdAt: '2026-04-01T12:00:00', ...over,
});

const card = (over: Partial<Scorecard> = {}): Scorecard => ({
  gameId: 7, season: '2025-26', gameDate: '2026-04-02T02:00:00', arena: null,
  status: 'Scheduled', eventCount: 0, isLocked: false, quarter: 1, clockSeconds: 720, events: [],
  awayTeam: { teamId: 1, city: 'Los Angeles', name: 'Lakers', totalPoints: 0, players: [{ playerId: 10, name: 'LeBron James', points: 0, twoPointers: 0, threePointers: 0 }] },
  homeTeam: { teamId: 2, city: 'Boston', name: 'Celtics', totalPoints: 0, players: [{ playerId: 20, name: 'Jayson Tatum', points: 0, twoPointers: 0, threePointers: 0 }] },
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

const btn = (name: string | RegExp) => screen.getByRole('button', { name }) as HTMLButtonElement;

describe('ScoringHistory', () => {
  it('shows an empty state', () => {
    render(<ScoringHistory events={[]} awayName="Lakers" homeName="Celtics" />);
    expect(screen.getByText('No scoring yet.')).toBeTruthy();
  });

  it('lists plays in the order given with quarter, clock, play and running score', () => {
    render(
      <ScoringHistory
        awayName="Lakers"
        homeName="Celtics"
        events={[
          play(2, { quarter: 2, clockSeconds: 521, playerName: 'Jayson Tatum', teamName: 'Celtics', points: 2, awayScore: 3, homeScore: 2 }),
          play(1),
        ]}
      />,
    );

    const rows = screen.getAllByRole('row').slice(1).map((r) => (r.textContent ?? '').replace(/\s+/g, ' '));
    expect(rows[0]).toContain('Q2 8:41');
    expect(rows[0]).toContain('Jayson Tatum +2 (2-pointer)');
    expect(rows[0]).toContain('3 – 2');
    expect(rows[1]).toContain('Q1 10:00');
    expect(rows[1]).toContain('LeBron James +3 (3-pointer)');
    expect(screen.getByRole('columnheader', { name: /Lakers – Celtics/ })).toBeTruthy();
  });

  it('shows only the last 10 plays until "Show all" is clicked', () => {
    const events = Array.from({ length: 14 }, (_, i) => play(14 - i, { awayScore: 14 - i }));
    render(<ScoringHistory events={events} awayName="Lakers" homeName="Celtics" />);

    expect(screen.getAllByRole('row')).toHaveLength(11); // header + 10
    fireEvent.click(screen.getByRole('button', { name: 'Show all 14 plays' }));
    expect(screen.getAllByRole('row')).toHaveLength(15);
    fireEvent.click(screen.getByRole('button', { name: 'Show last 10 only' }));
    expect(screen.getAllByRole('row')).toHaveLength(11);
  });
});

describe('ScorecardPage clock', () => {
  it('starts from the saved quarter and clock and shows scoring history at the bottom', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(
      card({ quarter: 3, clockSeconds: 245, status: 'InProgress', eventCount: 1, events: [play(1)] }),
    );
    renderPage();

    expect((await screen.findByRole('timer', { name: 'Game clock time' })).textContent).toBe('4:05');
    expect((screen.getByRole('combobox', { name: 'Quarter' }) as HTMLSelectElement).value).toBe('3');
    expect(screen.getByRole('timer', { name: 'Shot clock time' }).textContent).toBe('24');

    const history = screen.getByRole('region', { name: 'Scoring history' });
    expect(within(history).getByRole('row', { name: /LeBron James \+3/ })).toBeTruthy();
    // history comes after the player tables on the page
    const tables = screen.getByRole('region', { name: /Home team/ });
    expect(tables.compareDocumentPosition(history) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it('Start toggles to Pause', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card());
    renderPage();
    await screen.findByRole('timer', { name: 'Game clock time' });

    fireEvent.click(btn('Start'));
    expect(btn('Pause')).toBeTruthy();
    expect((screen.getByRole('combobox', { name: 'Quarter' }) as HTMLSelectElement).disabled).toBe(true);

    fireEvent.click(btn('Pause'));
    expect(btn('Start')).toBeTruthy();
  });

  it('pausing saves the clock to the server', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card());
    const save = vi.spyOn(api.games, 'setClock').mockResolvedValue(card());
    renderPage();
    await screen.findByRole('timer', { name: 'Game clock time' });

    fireEvent.click(btn('Start'));
    fireEvent.click(btn('Pause'));

    await waitFor(() => expect(save).toHaveBeenCalledTimes(1));
    expect(save.mock.calls[0].slice(0, 2)).toEqual([7, 1]);
  });

  it('changing the quarter resets the clock to a full period and saves it', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card({ clockSeconds: 100 }));
    const save = vi.spyOn(api.games, 'setClock').mockResolvedValue(card());
    renderPage();
    expect((await screen.findByRole('timer', { name: 'Game clock time' })).textContent).toBe('1:40');

    fireEvent.change(screen.getByRole('combobox', { name: 'Quarter' }), { target: { value: '5' } });

    expect(screen.getByRole('timer', { name: 'Game clock time' }).textContent).toBe('5:00');
    expect(save).toHaveBeenCalledWith(7, 5, 300);

    fireEvent.change(screen.getByRole('combobox', { name: 'Quarter' }), { target: { value: '2' } });
    expect(screen.getByRole('timer', { name: 'Game clock time' }).textContent).toBe('12:00');
    expect(save).toHaveBeenLastCalledWith(7, 2, 720);
  });

  it('Next quarter advances the quarter', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card({ quarter: 4, clockSeconds: 0 }));
    vi.spyOn(api.games, 'setClock').mockResolvedValue(card());
    renderPage();
    await screen.findByRole('timer', { name: 'Game clock time' });

    fireEvent.click(btn('Next quarter'));

    expect((screen.getByRole('combobox', { name: 'Quarter' }) as HTMLSelectElement).value).toBe('5');
    expect(screen.getByRole('timer', { name: 'Game clock time' }).textContent).toBe('5:00');
  });

  it('a basket is recorded with the current quarter and clock', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card({ quarter: 2, clockSeconds: 521 }));
    const record = vi.spyOn(api.games, 'recordEvent').mockResolvedValue(card({ quarter: 2, clockSeconds: 521, status: 'InProgress', eventCount: 1 }));
    renderPage();

    fireEvent.click(await screen.findByRole('button', { name: 'Add 3 points for LeBron James' }));

    await waitFor(() => expect(record).toHaveBeenCalledWith(7, 10, 3, { quarter: 2, clockSeconds: 521 }));
  });

  it('shot clock reset buttons set 24 and 14', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card());
    renderPage();
    await screen.findByRole('timer', { name: 'Shot clock time' });

    fireEvent.click(btn('Reset 14'));
    expect(screen.getByRole('timer', { name: 'Shot clock time' }).textContent).toBe('14');
    fireEvent.click(btn('Reset 24'));
    expect(screen.getByRole('timer', { name: 'Shot clock time' }).textContent).toBe('24');
  });

  it('the clock controls are disabled once the game is locked', async () => {
    vi.spyOn(api.games, 'scorecard').mockResolvedValue(card({ status: 'Final', isLocked: true, eventCount: 1, events: [play(1)] }));
    renderPage();
    await screen.findByRole('timer', { name: 'Game clock time' });

    expect(btn('Start').disabled).toBe(true);
    expect(btn('Next quarter').disabled).toBe(true);
    expect(btn('Reset 24').disabled).toBe(true);
    expect((screen.getByRole('combobox', { name: 'Quarter' }) as HTMLSelectElement).disabled).toBe(true);
  });
});

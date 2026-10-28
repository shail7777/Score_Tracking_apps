import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from '../api/client';
import HomePage from '../pages/HomePage';
import TeamDetailPage from '../pages/TeamDetailPage';
import type { Team, TeamPlayer } from '../types/nba';

const team = (id: number, name: string, city = 'City'): Team => ({
  id, name, city, conference: '', division: '',
});

const rosterEntry = (id: number, playerName: string): TeamPlayer => ({
  id, teamId: 1, teamName: 'Lakers', playerId: id, playerName,
  startDate: '2026-04-01', endDate: null, isActive: true,
});

afterEach(cleanup);

describe('HomePage', () => {
  it('renders one link tile per team pointing at its details page', async () => {
    vi.spyOn(api.teams, 'list').mockResolvedValue([team(1, 'Lakers', 'LA'), team(2, 'Celtics', '')]);

    render(<MemoryRouter><HomePage /></MemoryRouter>);

    const lakers = await screen.findByRole('link', { name: 'LA Lakers' });
    expect(lakers.getAttribute('href')).toBe('/teams/1');
    expect(screen.getByRole('link', { name: 'Celtics' }).getAttribute('href')).toBe('/teams/2');
  });

  describe('search', () => {
    const setup = async () => {
      vi.spyOn(api.teams, 'list').mockResolvedValue([
        team(1, 'Lakers', 'Los Angeles'),
        team(2, 'Celtics', 'Boston'),
        team(3, 'Clippers', 'LA'),
      ]);
      render(<MemoryRouter><HomePage /></MemoryRouter>);
      await screen.findByRole('link', { name: 'Boston Celtics' });
      return screen.getByRole('searchbox', { name: 'Search teams' });
    };
    const search = (box: HTMLElement, value: string) => fireEvent.change(box, { target: { value } });
    const tileNames = () => screen.queryAllByRole('link').map((a) => a.getAttribute('aria-label'));

    it('filters tiles by team name, case-insensitively', async () => {
      const box = await setup();
      search(box, 'CELT');
      expect(tileNames()).toEqual(['Boston Celtics']);
      expect(screen.getByRole('status').textContent).toBe('1 of 3 teams');
    });

    it('matches on city and across city + name words', async () => {
      const box = await setup();
      search(box, 'los lak');
      expect(tileNames()).toEqual(['Los Angeles Lakers']);
      search(box, 'boston');
      expect(tileNames()).toEqual(['Boston Celtics']);
    });

    it('shows a message when nothing matches and restores all tiles when cleared', async () => {
      const box = await setup();
      search(box, 'zzz');
      expect(screen.getByText('No teams match "zzz".')).toBeTruthy();
      expect(tileNames()).toEqual([]);
      search(box, '');
      expect(tileNames()).toHaveLength(3);
    });
  });

  it('shows an error message when the API fails', async () => {
    vi.spyOn(api.teams, 'list').mockRejectedValue(new Error('boom'));
    render(<MemoryRouter><HomePage /></MemoryRouter>);
    expect(await screen.findByText('boom')).toBeTruthy();
  });
});

describe('TeamDetailPage', () => {
  const renderAt = (path: string) =>
    render(
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/" element={<p>home</p>} />
          <Route path="/teams/:id" element={<TeamDetailPage />} />
        </Routes>
      </MemoryRouter>,
    );

  it('lists the roster players in the left column', async () => {
    vi.spyOn(api.teams, 'get').mockResolvedValue(team(1, 'Lakers', 'Los Angeles'));
    vi.spyOn(api.teams, 'roster').mockResolvedValue([rosterEntry(1, 'LeBron James'), rosterEntry(2, 'Austin Reaves')]);
    vi.spyOn(api.teams, 'games').mockResolvedValue([]);

    renderAt('/teams/1');

    expect(await screen.findByText('LeBron James')).toBeTruthy();
    expect(screen.getByText('Austin Reaves')).toBeTruthy();
    expect(screen.queryByText('Pos.')).toBeNull();
    expect(document.querySelector('.col-left')?.textContent).toContain('LeBron James');
  });

  it('shows an empty state when the team has no players', async () => {
    vi.spyOn(api.teams, 'get').mockResolvedValue(team(1, 'Lakers'));
    vi.spyOn(api.teams, 'roster').mockResolvedValue([]);
    vi.spyOn(api.teams, 'games').mockResolvedValue([]);

    renderAt('/teams/1');

    expect(await screen.findByText('No players on roster.')).toBeTruthy();
  });

  it('shows a not-found state for an unknown team', async () => {
    vi.spyOn(api.teams, 'get').mockRejectedValue(new ApiError('Not Found', 404));
    vi.spyOn(api.teams, 'roster').mockRejectedValue(new ApiError('Not Found', 404));
    vi.spyOn(api.teams, 'games').mockRejectedValue(new ApiError('Not Found', 404));

    renderAt('/teams/9999');

    await waitFor(() => expect(screen.getByText('Team not found')).toBeTruthy());
    expect(screen.getByRole('link', { name: /All teams/ }).getAttribute('href')).toBe('/');
  });
});

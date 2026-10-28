import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import TeamBanner from '../components/TeamBanner';
import PlayersPage from '../pages/PlayersPage';
import TeamDetailPage from '../pages/TeamDetailPage';
import type { Player, Team } from '../types/nba';

afterEach(cleanup);

const team: Team = { id: 6, name: 'Celtics', city: 'Boston', conference: '', division: '' };

describe('TeamBanner', () => {
  it('shows the city above the team name as the page heading', () => {
    render(<TeamBanner team={team} />);
    expect(screen.getByText('Boston').className).toContain('team-banner-city');
    expect(screen.getByRole('heading', { level: 2, name: 'Celtics' })).toBeTruthy();
  });

  it('omits the city line when the team has no city', () => {
    render(<TeamBanner team={{ ...team, city: '' }} />);
    expect(document.querySelector('.team-banner-city')).toBeNull();
    expect(screen.getByRole('heading', { name: 'Celtics' })).toBeTruthy();
  });

  it('appears above the players on the team page', async () => {
    vi.spyOn(api.teams, 'get').mockResolvedValue(team);
    vi.spyOn(api.teams, 'roster').mockResolvedValue([]);
    vi.spyOn(api.teams, 'games').mockResolvedValue([]);
    render(
      <MemoryRouter initialEntries={['/teams/6']}>
        <Routes><Route path="/teams/:id" element={<TeamDetailPage />} /></Routes>
      </MemoryRouter>,
    );

    const banner = (await screen.findByRole('heading', { level: 2, name: 'Celtics' })).closest('.team-banner')!;
    const players = screen.getByRole('region', { name: 'Players' });
    expect(banner.compareDocumentPosition(players) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });
});

describe('PlayersPage', () => {
  it('lists only name and team columns (no height or weight)', async () => {
    const player: Player = {
      id: 1, firstName: 'Jayson', lastName: 'Tatum', heightInches: 80, weightLbs: 210, birthDate: null,
      currentTeamId: 6, currentTeamName: 'Celtics',
    };
    vi.spyOn(api.teams, 'list').mockResolvedValue([team]);
    vi.spyOn(api.players, 'list').mockResolvedValue([player]);
    render(<MemoryRouter><PlayersPage /></MemoryRouter>);

    expect(await screen.findByText('Jayson Tatum')).toBeTruthy();
    expect(screen.getAllByRole('columnheader').map((h) => h.textContent)).toEqual(['Name', 'Team']);
    expect(screen.queryByText(/lbs/)).toBeNull();
    expect(screen.queryByText(`6'8"`)).toBeNull();
  });
});

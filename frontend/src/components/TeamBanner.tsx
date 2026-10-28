import type { Team } from '../types/nba';

export default function TeamBanner({ team }: { team: Team }) {
  return (
    <header className="team-banner">
      {team.city && <p className="team-banner-city">{team.city}</p>}
      <h2 className="team-banner-name">{team.name}</h2>
    </header>
  );
}

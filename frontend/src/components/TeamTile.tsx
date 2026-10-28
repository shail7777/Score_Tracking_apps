import { Link } from 'react-router-dom';
import type { Team } from '../types/nba';

export default function TeamTile({ team }: { team: Team }) {
  return (
    <Link to={`/teams/${team.id}`} className="tile" aria-label={`${team.city} ${team.name}`.trim()}>
      {team.city && <span className="tile-city">{team.city}</span>}
      <span className="tile-name">{team.name}</span>
    </Link>
  );
}

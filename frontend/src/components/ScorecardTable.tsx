import { Link } from 'react-router-dom';
import type { ScorecardTeam } from '../types/nba';

interface Props {
  team: ScorecardTeam;
  label: 'Away' | 'Home';
  // When provided, each player row gets +2 / +3 buttons.
  onScore?: (playerId: number, points: 2 | 3) => void;
  disabled?: boolean;
}

export default function ScorecardTable({ team, label, onScore, disabled }: Props) {
  const twos = team.players.reduce((n, p) => n + p.twoPointers, 0);
  const threes = team.players.reduce((n, p) => n + p.threePointers, 0);

  return (
    <section className={`scorecard-team team-${label.toLowerCase()}`} aria-label={`${label} team: ${team.city} ${team.name}`.trim()}>
      <h3>
        <Link to={`/teams/${team.teamId}`}>{team.city} {team.name}</Link>
        <span className="chip">{label}</span>
      </h3>
      {team.players.length === 0 ? (
        <p>No players on roster.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Player</th>
              <th scope="col" className="num">PTS</th>
              <th scope="col" className="num">2PT</th>
              <th scope="col" className="num">3PT</th>
              {onScore && <th scope="col" className="num">Score</th>}
            </tr>
          </thead>
          <tbody>
            {team.players.map((p) => (
              <tr key={p.playerId} className={p.points > 0 ? 'scored' : undefined}>
                <th scope="row" className="plain">{p.name}</th>
                <td className="num">{p.points}</td>
                <td className="num">{p.twoPointers}</td>
                <td className="num">{p.threePointers}</td>
                {onScore && (
                  <td className="num actions">
                    <button
                      type="button"
                      className="score-btn two"
                      disabled={disabled}
                      aria-label={`Add 2 points for ${p.name}`}
                      onClick={() => onScore(p.playerId, 2)}
                    >
                      +2
                    </button>
                    <button
                      type="button"
                      className="score-btn three"
                      disabled={disabled}
                      aria-label={`Add 3 points for ${p.name}`}
                      onClick={() => onScore(p.playerId, 3)}
                    >
                      +3
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th scope="row">Team total</th>
              <td className="num">{team.totalPoints}</td>
              <td className="num">{twos}</td>
              <td className="num">{threes}</td>
              {onScore && <td />}
            </tr>
          </tfoot>
        </table>
      )}
    </section>
  );
}

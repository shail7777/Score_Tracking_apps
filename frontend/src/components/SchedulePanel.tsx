import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../api/useFetch';
import type { TeamGame } from '../types/nba';
import Status from './Status';

const dateFmt = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

function outcome(g: TeamGame): string {
  if (g.status === 'InProgress') return 'In progress';
  if (g.status === 'Scheduled') return 'Upcoming';
  if (g.teamScore === null || g.opponentScore === null) return 'Final';
  return `${g.result} ${g.teamScore}–${g.opponentScore}`;
}

export default function SchedulePanel({ teamId }: { teamId: number }) {
  const { data: games, loading, error } = useFetch(() => api.teams.games(teamId), [teamId]);

  return (
    <section aria-labelledby="schedule-heading">
      <h3 id="schedule-heading">Schedule</h3>
      <Status loading={loading} error={error}>
        {games?.length ? (
          <table>
            <thead>
              <tr>
                <th scope="col">Date</th>
                <th scope="col">Opponent</th>
                <th scope="col">Result</th>
                <th scope="col"><span className="sr-only">Scorecard</span></th>
              </tr>
            </thead>
            <tbody>
              {games.map((g) => (
                <tr key={g.id}>
                  <td>{dateFmt.format(new Date(g.gameDate))}</td>
                  <td>{g.isHome ? 'vs' : '@'} {g.opponentName}</td>
                  <td className={g.result === 'W' ? 'winner' : undefined}>{outcome(g)}</td>
                  <td>
                    <Link to={`/games/${g.id}/scorecard`} aria-label={`Open scorecard: ${g.isHome ? 'vs' : '@'} ${g.opponentName}, ${dateFmt.format(new Date(g.gameDate))}`}>
                      Scorecard →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p>No games scheduled.</p>
        )}
      </Status>
    </section>
  );
}

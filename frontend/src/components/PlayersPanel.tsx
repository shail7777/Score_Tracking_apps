import { api } from '../api/client';
import { useFetch } from '../api/useFetch';
import Status from './Status';

export default function PlayersPanel({ teamId }: { teamId: number }) {
  const { data: roster, loading, error } = useFetch(() => api.teams.roster(teamId), [teamId]);

  return (
    <section aria-labelledby="players-heading">
      <h3 id="players-heading">Players</h3>
      <Status loading={loading} error={error}>
        {roster?.length ? (
          <table>
            <thead>
              <tr>
                <th scope="col">Player</th>
              </tr>
            </thead>
            <tbody>
              {roster.map((tp) => (
                <tr key={tp.id}>
                  <td>{tp.playerName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <p>No players on roster.</p>
        )}
      </Status>
    </section>
  );
}

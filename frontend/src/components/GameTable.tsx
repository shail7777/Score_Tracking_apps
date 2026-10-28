import type { Game } from '../types/nba';

const dateFmt = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' });

interface Props {
  games: Game[];
  // When set, shows a W/L result column from this team's perspective.
  perspectiveTeamId?: number;
  onDelete?: (game: Game) => void;
}

export default function GameTable({ games, perspectiveTeamId, onDelete }: Props) {
  if (games.length === 0) return <p>No games found.</p>;

  return (
    <table>
      <thead>
        <tr>
          <th>Date</th>
          <th>Away</th>
          <th>Home</th>
          <th>Score</th>
          {perspectiveTeamId !== undefined && <th>Result</th>}
          <th>Arena</th>
          {onDelete && <th />}
        </tr>
      </thead>
      <tbody>
        {games.map((g) => (
          <tr key={g.id}>
            <td>{dateFmt.format(new Date(g.gameDate))}</td>
            <td className={g.winnerTeamId === g.awayTeamId ? 'winner' : ''}>{g.awayTeamName}</td>
            <td className={g.winnerTeamId === g.homeTeamId ? 'winner' : ''}>{g.homeTeamName}</td>
            <td>
              {g.status !== 'Final'
                ? g.status === 'InProgress' ? 'In progress' : 'Upcoming'
                : g.awayScore === null || g.homeScore === null
                  ? 'Final'
                  : `${g.awayScore} – ${g.homeScore}`}
            </td>
            {perspectiveTeamId !== undefined && (
              <td>
                {g.winnerTeamId === null ? '—' : g.winnerTeamId === perspectiveTeamId ? 'W' : 'L'}
              </td>
            )}
            <td>{g.arena}</td>
            {onDelete && (
              <td>
                <button onClick={() => onDelete(g)}>Delete</button>
              </td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

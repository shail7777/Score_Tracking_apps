import type { TeamPlayer } from '../types/nba';

interface Props {
  items: TeamPlayer[];
  // 'roster' hides the team column; 'history' hides the player column.
  mode: 'roster' | 'history';
}

export default function TeamPlayerTable({ items, mode }: Props) {
  if (items.length === 0) return <p>No players found.</p>;

  return (
    <table>
      <thead>
        <tr>
          {mode === 'roster' ? <th>Player</th> : <th>Team</th>}
          <th>From</th>
          <th>To</th>
        </tr>
      </thead>
      <tbody>
        {items.map((tp) => (
          <tr key={tp.id}>
            <td>{mode === 'roster' ? tp.playerName : tp.teamName}</td>
            <td>{tp.startDate}</td>
            <td>{tp.endDate ?? 'Present'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

import { useState } from 'react';
import { api } from '../api/client';
import { useFetch } from '../api/useFetch';
import Status from '../components/Status';
import TeamPlayerTable from '../components/TeamPlayerTable';
import type { Player } from '../types/nba';

export default function PlayersPage() {
  const [search, setSearch] = useState('');
  const [teamId, setTeamId] = useState<number | undefined>();
  const [selected, setSelected] = useState<Player | null>(null);

  const teams = useFetch(() => api.teams.list());
  const players = useFetch(() => api.players.list({ teamId, search }), [teamId, search]);
  const history = useFetch(
    () => (selected ? api.players.history(selected.id) : Promise.resolve([])),
    [selected?.id],
  );

  return (
    <>
      <h2>Players</h2>
      <div className="filters">
        <input
          placeholder="Search by name…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          value={teamId ?? ''}
          onChange={(e) => setTeamId(e.target.value ? Number(e.target.value) : undefined)}
        >
          <option value="">All teams</option>
          {teams.data?.map((t) => (
            <option key={t.id} value={t.id}>{t.city} {t.name}</option>
          ))}
        </select>
      </div>
      <Status loading={players.loading} error={players.error}>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Team</th>
            </tr>
          </thead>
          <tbody>
            {players.data?.map((p) => (
              <tr key={p.id} className="clickable" onClick={() => setSelected(p)}>
                <td>{p.firstName} {p.lastName}</td>
                <td>{p.currentTeamName ?? 'Free agent'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Status>

      {selected && (
        <>
          <h3>Team history: {selected.firstName} {selected.lastName}</h3>
          <Status loading={history.loading} error={history.error}>
            <TeamPlayerTable items={history.data ?? []} mode="history" />
          </Status>
        </>
      )}
    </>
  );
}

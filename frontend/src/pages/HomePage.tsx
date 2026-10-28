import { useMemo, useState } from 'react';
import { api } from '../api/client';
import { useFetch } from '../api/useFetch';
import Status from '../components/Status';
import TeamTile from '../components/TeamTile';

export default function HomePage() {
  const { data: teams, loading, error } = useFetch(() => api.teams.list());
  const [query, setQuery] = useState('');

  // Every typed word must appear in "city name", so "boston cel" and "celtics boston" both match.
  const visible = useMemo(() => {
    const words = query.toLowerCase().split(/\s+/).filter(Boolean);
    if (!teams || words.length === 0) return teams ?? [];
    return teams.filter((t) => {
      const text = `${t.city} ${t.name}`.toLowerCase();
      return words.every((w) => text.includes(w));
    });
  }, [teams, query]);

  return (
    <>
      <h2>Select a team</h2>
      <div className="filters">
        <input
          type="search"
          className="team-search"
          placeholder="Search teams by city or name…"
          aria-label="Search teams"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        {teams && query.trim() && (
          <span className="hint" role="status">
            {visible.length} of {teams.length} teams
          </span>
        )}
      </div>
      <Status loading={loading} error={error}>
        {visible.length ? (
          <div className="tile-grid">
            {visible.map((t) => (
              <TeamTile key={t.id} team={t} />
            ))}
          </div>
        ) : (
          <p>{query.trim() ? `No teams match "${query.trim()}".` : 'No teams found.'}</p>
        )}
      </Status>
    </>
  );
}

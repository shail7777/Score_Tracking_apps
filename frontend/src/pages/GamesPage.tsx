import { FormEvent, useState } from 'react';
import { api } from '../api/client';
import { useFetch } from '../api/useFetch';
import GameTable from '../components/GameTable';
import Status from '../components/Status';
import type { Game, GameStatus } from '../types/nba';

type Tab = 'All' | GameStatus;

const emptyForm = {
  season: '2026-27',
  gameDate: '',
  arena: '',
  homeTeamId: '',
  awayTeamId: '',
  homeScore: '',
  awayScore: '',
};

export default function GamesPage() {
  const [tab, setTab] = useState<Tab>('All');
  const [teamId, setTeamId] = useState<number | undefined>();
  const [form, setForm] = useState(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  const teams = useFetch(() => api.teams.list());
  const games = useFetch(
    () => api.games.list({ teamId, status: tab === 'All' ? undefined : tab }),
    [teamId, tab],
  );

  const setField = (k: keyof typeof emptyForm) => (e: { target: { value: string } }) =>
    setForm((f) => ({ ...f, [k]: e.target.value }));

  // A game with scores entered is recorded as Final; otherwise it is Scheduled.
  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setFormError(null);
    const hasScores = form.homeScore !== '' || form.awayScore !== '';
    try {
      await api.games.create({
        season: form.season,
        gameDate: form.gameDate,
        arena: form.arena || null,
        status: hasScores ? 'Final' : 'Scheduled',
        homeTeamId: Number(form.homeTeamId),
        awayTeamId: Number(form.awayTeamId),
        homeScore: hasScores ? Number(form.homeScore) : null,
        awayScore: hasScores ? Number(form.awayScore) : null,
      });
      setForm(emptyForm);
      games.reload();
    } catch (err) {
      setFormError((err as Error).message);
    }
  };

  const remove = async (g: Game) => {
    if (!window.confirm(`Delete ${g.awayTeamName} @ ${g.homeTeamName}?`)) return;
    await api.games.remove(g.id);
    games.reload();
  };

  const teamOptions = teams.data?.map((t) => (
    <option key={t.id} value={t.id}>{t.city} {t.name}</option>
  ));

  return (
    <>
      <h2>Games</h2>
      <div className="filters">
        {(['All', 'Final', 'Scheduled'] as Tab[]).map((t) => (
          <button key={t} className={tab === t ? 'active' : ''} onClick={() => setTab(t)}>
            {t === 'All' ? 'All' : t === 'Final' ? 'Previous' : 'Upcoming'}
          </button>
        ))}
        <select
          value={teamId ?? ''}
          onChange={(e) => setTeamId(e.target.value ? Number(e.target.value) : undefined)}
        >
          <option value="">All teams</option>
          {teamOptions}
        </select>
      </div>

      <Status loading={games.loading} error={games.error}>
        <GameTable games={games.data ?? []} perspectiveTeamId={teamId} onDelete={remove} />
      </Status>

      <h3>Add game</h3>
      <form className="form" onSubmit={submit}>
        <input required placeholder="Season (2026-27)" value={form.season} onChange={setField('season')} />
        <input required type="datetime-local" value={form.gameDate} onChange={setField('gameDate')} />
        <select required value={form.awayTeamId} onChange={setField('awayTeamId')}>
          <option value="">Away team…</option>
          {teamOptions}
        </select>
        <select required value={form.homeTeamId} onChange={setField('homeTeamId')}>
          <option value="">Home team…</option>
          {teamOptions}
        </select>
        <input placeholder="Arena" value={form.arena} onChange={setField('arena')} />
        <input type="number" min={0} placeholder="Away score" value={form.awayScore} onChange={setField('awayScore')} />
        <input type="number" min={0} placeholder="Home score" value={form.homeScore} onChange={setField('homeScore')} />
        <button type="submit">Add</button>
      </form>
      <p className="hint">Leave scores empty for an upcoming game; enter both to record a final result.</p>
      {formError && <p className="error">{formError}</p>}
    </>
  );
}

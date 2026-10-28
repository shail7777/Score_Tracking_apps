import { Link, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../api/useFetch';
import PlayersPanel from '../components/PlayersPanel';
import SchedulePanel from '../components/SchedulePanel';
import TeamBanner from '../components/TeamBanner';
import Status from '../components/Status';

export default function TeamDetailPage() {
  const rawId = useParams().id;
  const id = Number(rawId);

  const team = useFetch(() => api.teams.get(id), [id]);

  if (team.errorStatus === 404) {
    return (
      <>
        <p><Link to="/">← All teams</Link></p>
        <h2>Team not found</h2>
        <p>There is no team with id {rawId}.</p>
      </>
    );
  }

  return (
    <Status loading={team.loading} error={team.error}>
      <p><Link to="/">← All teams</Link></p>
      {team.data && <TeamBanner team={team.data} />}

      <div className="two-col">
        <div className="col-left">
          <PlayersPanel teamId={id} />
        </div>
        <div className="col-right">
          <SchedulePanel teamId={id} />
        </div>
      </div>
    </Status>
  );
}

import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ApiError, api } from '../api/client';
import { useFetch } from '../api/useFetch';
import { useGameClock } from '../api/useGameClock';
import GameClock from '../components/GameClock';
import ScorecardTable from '../components/ScorecardTable';
import ScoringHistory from '../components/ScoringHistory';
import Status from '../components/Status';
import { SHOT_CLOCK_SECONDS, quarterSeconds } from '../lib/clock';
import type { GameStatus, Scorecard } from '../types/nba';
import '../scorecard.css';

const dateFmt = new Intl.DateTimeFormat(undefined, { dateStyle: 'full', timeStyle: 'short' });

const statusLabel: Record<GameStatus, string> = {
  Scheduled: 'Not started',
  InProgress: 'In progress',
  Final: 'Final',
};

export default function ScorecardPage() {
  const rawId = useParams().gameId;
  const gameId = Number(rawId);
  const { data, loading, error, errorStatus, reload } = useFetch(() => api.games.scorecard(gameId), [gameId]);

  // Scoring actions return the updated scorecard, which replaces the fetched one until the next fetch.
  const [updated, setUpdated] = useState<Scorecard | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  useEffect(() => setUpdated(null), [data]);

  const sc = updated ?? data;

  // The clock lives in the browser; it is loaded once from the saved game state, then saved when it stops.
  const clock = useGameClock();
  const loadedFor = useRef<number | null>(null);
  useEffect(() => {
    if (data && loadedFor.current !== gameId) {
      loadedFor.current = gameId;
      clock.dispatch({ type: 'load', quarter: data.quarter, seconds: data.clockSeconds });
    }
  }, [data, gameId]);

  const saveClock = (quarter: number, seconds: number) => {
    if (sc?.isLocked) return;
    api.games.setClock(gameId, quarter, seconds).catch(() => undefined);
  };
  const wasRunning = useRef(false);
  useEffect(() => {
    // Paused by the user, quarter ended, or shot-clock violation.
    if (wasRunning.current && !clock.running) saveClock(clock.quarter, clock.gameSeconds);
    wasRunning.current = clock.running;
  }, [clock.running]);

  const recordBasket = (playerId: number, points: 2 | 3) =>
    run(async () => {
      const result = await api.games.recordEvent(gameId, playerId, points, {
        quarter: clock.quarter,
        clockSeconds: clock.gameSeconds,
      });
      clock.dispatch({ type: 'resetShot', seconds: SHOT_CLOCK_SECONDS });
      return result;
    });

  const run = async (action: () => Promise<Scorecard>) => {
    setBusy(true);
    setActionError(null);
    try {
      setUpdated(await action());
    } catch (e) {
      setActionError((e as Error).message);
      // Someone else changed the game: show their latest state.
      if (e instanceof ApiError && e.status === 409) reload();
    } finally {
      setBusy(false);
    }
  };

  if (errorStatus === 404) {
    return (
      <>
        <p><Link to="/">← All teams</Link></p>
        <h2>Game not found</h2>
        <p>There is no game with id {rawId}.</p>
      </>
    );
  }

  const finish = () => {
    if (!sc) return;
    const msg = `Finish the game? Final score: ${sc.awayTeam.name} ${sc.awayTeam.totalPoints} – ${sc.homeTeam.name} ${sc.homeTeam.totalPoints}. Scoring will be locked.`;
    if (window.confirm(msg)) void run(() => api.games.finish(gameId));
  };

  return (
    <Status loading={loading} error={error}>
      {sc && (
        <div className="game-page">
          <Link to="/" className="back-link">← All teams</Link>

          <header className={`scoreboard${sc.awayTeam.totalPoints === sc.homeTeam.totalPoints ? ' tied' : ''}`}>
            <div className={`sb-team${sc.awayTeam.totalPoints > sc.homeTeam.totalPoints ? ' leading' : ''}`}>
              <span className="sb-label">Away</span>
              <span className="sb-name">{sc.awayTeam.name}</span>
              <strong className="sb-score" aria-label={`${sc.awayTeam.name} score`}>{sc.awayTeam.totalPoints}</strong>
            </div>
            <div className="sb-center">
              <span className={`badge badge-${sc.status}`}>{statusLabel[sc.status]}</span>
              <span>{dateFmt.format(new Date(sc.gameDate))}</span>
              {sc.arena && <span>{sc.arena}</span>}
            </div>
            <div className={`sb-team${sc.homeTeam.totalPoints > sc.awayTeam.totalPoints ? ' leading' : ''}`}>
              <span className="sb-label">Home</span>
              <span className="sb-name">{sc.homeTeam.name}</span>
              <strong className="sb-score" aria-label={`${sc.homeTeam.name} score`}>{sc.homeTeam.totalPoints}</strong>
            </div>
          </header>

          <GameClock
            quarter={clock.quarter}
            gameSeconds={clock.gameSeconds}
            shotSeconds={clock.shotSeconds}
            running={clock.running}
            disabled={sc.isLocked}
            onStart={() => clock.dispatch({ type: 'start' })}
            onPause={() => clock.dispatch({ type: 'pause' })}
            onResetShot={(seconds) => clock.dispatch({ type: 'resetShot', seconds })}
            onQuarterChange={(quarter) => {
              clock.dispatch({ type: 'setQuarter', quarter });
              saveClock(quarter, quarterSeconds(quarter));
            }}
          />

          <div className="scorecard-controls">
            <button type="button" className="btn" disabled={busy || sc.isLocked || sc.eventCount === 0} onClick={() => void run(() => api.games.undoLast(gameId))}>
              Undo last
            </button>
            <button type="button" className="btn btn-danger" disabled={busy || sc.isLocked || sc.eventCount === 0} onClick={finish}>
              Finish game
            </button>
            {sc.isLocked && <span className="hint">This game is final; scoring is locked.</span>}
            {!sc.isLocked && sc.eventCount === 0 && (
              <span className="hint">
                {sc.status === 'Final' ? 'No play-by-play was recorded for this game. ' : ''}
                Use +2 / +3 to record the first basket.
              </span>
            )}
          </div>

          {actionError && <p className="error" role="alert">{actionError}</p>}

          <div className="scorecard-grid">
            <ScorecardTable
              team={sc.awayTeam}
              label="Away"
              disabled={busy || sc.isLocked}
              onScore={(playerId, points) => void recordBasket(playerId, points)}
            />
            <ScorecardTable
              team={sc.homeTeam}
              label="Home"
              disabled={busy || sc.isLocked}
              onScore={(playerId, points) => void recordBasket(playerId, points)}
            />
          </div>

          <ScoringHistory events={sc.events} awayName={sc.awayTeam.name} homeName={sc.homeTeam.name} />
        </div>
      )}
    </Status>
  );
}

import {
  MAX_QUARTER,
  SHORT_SHOT_CLOCK_SECONDS,
  SHOT_CLOCK_SECONDS,
  formatClock,
  quarterLabel,
} from '../lib/clock';

interface Props {
  quarter: number;
  gameSeconds: number;
  shotSeconds: number;
  running: boolean;
  disabled?: boolean;
  onStart: () => void;
  onPause: () => void;
  onResetShot: (seconds: number) => void;
  onQuarterChange: (quarter: number) => void;
}

export default function GameClock({
  quarter, gameSeconds, shotSeconds, running, disabled,
  onStart, onPause, onResetShot, onQuarterChange,
}: Props) {
  const quarters = Array.from({ length: MAX_QUARTER }, (_, i) => i + 1);
  const gameOver = gameSeconds <= 0;
  const violation = shotSeconds <= 0 && !gameOver;

  return (
    <section className="game-clock" aria-label="Game clock">
      <div className="clock-block">
        <span className="clock-label">Quarter</span>
        <strong className="clock-digits quarter" aria-hidden="true">{quarterLabel(quarter)}</strong>
        <div className="clock-actions">
          <select
            aria-label="Quarter"
            value={quarter}
            disabled={disabled || running}
            onChange={(e) => onQuarterChange(Number(e.target.value))}
          >
            {quarters.map((q) => (
              <option key={q} value={q}>{quarterLabel(q)}</option>
            ))}
          </select>
          <button
            type="button"
            disabled={disabled || running || quarter >= MAX_QUARTER}
            onClick={() => onQuarterChange(quarter + 1)}
          >
            Next quarter
          </button>
        </div>
      </div>

      <div className="clock-block">
        <span className="clock-label">Game clock</span>
        <strong className={`clock-digits${running ? ' running' : ''}`} role="timer" aria-label="Game clock time">
          {formatClock(gameSeconds)}
        </strong>
        <div className="clock-actions">
          {running ? (
            <button type="button" disabled={disabled} onClick={onPause}>Pause</button>
          ) : (
            <button type="button" className="primary" disabled={disabled || gameOver} onClick={onStart}>
              {gameOver ? 'Quarter over' : 'Start'}
            </button>
          )}
        </div>
      </div>

      <div className="clock-block">
        <span className="clock-label">Shot clock</span>
        <strong
          className={`clock-digits shot${shotSeconds <= 5 ? ' low' : ''}`}
          role="timer"
          aria-label="Shot clock time"
        >
          {shotSeconds}
        </strong>
        <div className="clock-actions">
          <button type="button" disabled={disabled} onClick={() => onResetShot(SHOT_CLOCK_SECONDS)}>
            Reset {SHOT_CLOCK_SECONDS}
          </button>
          <button type="button" disabled={disabled} onClick={() => onResetShot(SHORT_SHOT_CLOCK_SECONDS)}>
            Reset {SHORT_SHOT_CLOCK_SECONDS}
          </button>
        </div>
      </div>

      {violation && <p className="error clock-note" role="status">Shot clock violation: clock stopped.</p>}
    </section>
  );
}

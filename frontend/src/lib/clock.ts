export const REGULATION_SECONDS = 720; // 12:00 quarters
export const OVERTIME_SECONDS = 300; // 5:00 overtimes
export const SHOT_CLOCK_SECONDS = 24;
export const SHORT_SHOT_CLOCK_SECONDS = 14;
export const MAX_QUARTER = 7; // Q1-Q4 plus up to three overtimes

export const quarterSeconds = (quarter: number) => (quarter <= 4 ? REGULATION_SECONDS : OVERTIME_SECONDS);

export function quarterLabel(quarter: number): string {
  if (quarter <= 4) return `Q${quarter}`;
  return quarter === 5 ? 'OT' : `${quarter - 4}OT`;
}

export function formatClock(totalSeconds: number | null): string {
  if (totalSeconds === null) return '—';
  const s = Math.max(0, Math.floor(totalSeconds));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}

export const toSeconds = (ms: number) => Math.ceil(ms / 1000);

export interface ClockState {
  quarter: number;
  gameMs: number;
  shotMs: number;
  running: boolean;
}

export type ClockAction =
  | { type: 'load'; quarter: number; seconds: number }
  | { type: 'start' }
  | { type: 'pause' }
  | { type: 'tick'; elapsed: number }
  | { type: 'resetShot'; seconds: number }
  | { type: 'setQuarter'; quarter: number };

export const initialClockState: ClockState = {
  quarter: 1,
  gameMs: REGULATION_SECONDS * 1000,
  shotMs: SHOT_CLOCK_SECONDS * 1000,
  running: false,
};

export function clockReducer(state: ClockState, action: ClockAction): ClockState {
  switch (action.type) {
    case 'load':
      return { quarter: action.quarter, gameMs: action.seconds * 1000, shotMs: SHOT_CLOCK_SECONDS * 1000, running: false };
    case 'start':
      if (state.gameMs <= 0) return state;
      // A new possession after a shot-clock violation starts from a fresh 24.
      return { ...state, running: true, shotMs: state.shotMs <= 0 ? SHOT_CLOCK_SECONDS * 1000 : state.shotMs };
    case 'pause':
      return { ...state, running: false };
    case 'tick': {
      if (!state.running) return state;
      const gameMs = Math.max(0, state.gameMs - action.elapsed);
      const shotMs = Math.max(0, state.shotMs - action.elapsed);
      // End of quarter or a shot-clock violation stops the game clock.
      return { ...state, gameMs, shotMs, running: gameMs > 0 && shotMs > 0 };
    }
    case 'resetShot':
      return { ...state, shotMs: action.seconds * 1000 };
    case 'setQuarter':
      return {
        quarter: action.quarter,
        gameMs: quarterSeconds(action.quarter) * 1000,
        shotMs: SHOT_CLOCK_SECONDS * 1000,
        running: false,
      };
  }
}

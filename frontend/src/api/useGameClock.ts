import { useEffect, useReducer } from 'react';
import { clockReducer, initialClockState, toSeconds } from '../lib/clock';

// Game clock + shot clock + quarter. Counts down while running, ticking from wall-clock time.
export function useGameClock() {
  const [state, dispatch] = useReducer(clockReducer, initialClockState);

  useEffect(() => {
    if (!state.running) return;
    let last = Date.now();
    const id = setInterval(() => {
      const now = Date.now();
      dispatch({ type: 'tick', elapsed: now - last });
      last = now;
    }, 100);
    return () => clearInterval(id);
  }, [state.running]);

  return {
    state,
    dispatch,
    quarter: state.quarter,
    running: state.running,
    gameSeconds: toSeconds(state.gameMs),
    shotSeconds: toSeconds(state.shotMs),
  };
}

import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useGameClock } from '../api/useGameClock';
import {
  clockReducer,
  formatClock,
  initialClockState,
  quarterLabel,
  quarterSeconds,
  type ClockState,
} from '../lib/clock';

describe('clock helpers', () => {
  it('formats m:ss', () => {
    expect(formatClock(720)).toBe('12:00');
    expect(formatClock(65)).toBe('1:05');
    expect(formatClock(0)).toBe('0:00');
    expect(formatClock(null)).toBe('—');
  });

  it('labels quarters and overtimes and sizes them', () => {
    expect([1, 4, 5, 6, 7].map(quarterLabel)).toEqual(['Q1', 'Q4', 'OT', '2OT', '3OT']);
    expect([quarterSeconds(1), quarterSeconds(4), quarterSeconds(5)]).toEqual([720, 720, 300]);
  });
});

describe('clockReducer', () => {
  const running: ClockState = { ...initialClockState, running: true };

  it('counts both clocks down only while running', () => {
    expect(clockReducer(initialClockState, { type: 'tick', elapsed: 1000 })).toEqual(initialClockState);
    const next = clockReducer(running, { type: 'tick', elapsed: 3000 });
    expect([next.gameMs, next.shotMs, next.running]).toEqual([717000, 21000, true]);
  });

  it('stops at a shot-clock violation, leaving the game clock', () => {
    const next = clockReducer({ ...running, shotMs: 500 }, { type: 'tick', elapsed: 800 });
    expect([next.shotMs, next.gameMs, next.running]).toEqual([0, 719200, false]);
  });

  it('stops at the end of the quarter and cannot restart', () => {
    const end = clockReducer({ ...running, gameMs: 400 }, { type: 'tick', elapsed: 1000 });
    expect([end.gameMs, end.running]).toEqual([0, false]);
    expect(clockReducer(end, { type: 'start' }).running).toBe(false);
  });

  it('starting after a violation gives a fresh 24', () => {
    const next = clockReducer({ ...initialClockState, shotMs: 0 }, { type: 'start' });
    expect([next.running, next.shotMs]).toEqual([true, 24000]);
  });

  it('resets the shot clock to 24 or 14 without touching the game clock', () => {
    const a = clockReducer({ ...running, shotMs: 3000 }, { type: 'resetShot', seconds: 14 });
    expect([a.shotMs, a.gameMs, a.running]).toEqual([14000, 720000, true]);
  });

  it('changing quarter resets the clocks, pauses, and uses 5:00 for overtime', () => {
    const q2 = clockReducer({ ...running, gameMs: 1000 }, { type: 'setQuarter', quarter: 2 });
    expect([q2.quarter, q2.gameMs, q2.shotMs, q2.running]).toEqual([2, 720000, 24000, false]);
    expect(clockReducer(q2, { type: 'setQuarter', quarter: 5 }).gameMs).toBe(300000);
  });

  it('load restores a saved quarter and time, paused', () => {
    const s = clockReducer(running, { type: 'load', quarter: 3, seconds: 245 });
    expect([s.quarter, s.gameMs, s.running]).toEqual([3, 245000, false]);
  });
});

describe('useGameClock', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('ticks down in real time while running and stops when paused', () => {
    const { result } = renderHook(() => useGameClock());

    act(() => result.current.dispatch({ type: 'start' }));
    act(() => { vi.advanceTimersByTime(5000); });
    expect([result.current.gameSeconds, result.current.shotSeconds]).toEqual([715, 19]);

    act(() => result.current.dispatch({ type: 'pause' }));
    act(() => { vi.advanceTimersByTime(5000); });
    expect([result.current.gameSeconds, result.current.shotSeconds]).toEqual([715, 19]);
  });

  it('stops itself on a shot-clock violation after 24 seconds', () => {
    const { result } = renderHook(() => useGameClock());

    act(() => result.current.dispatch({ type: 'start' }));
    act(() => { vi.advanceTimersByTime(30000); });

    expect(result.current.running).toBe(false);
    expect(result.current.shotSeconds).toBe(0);
    expect(result.current.gameSeconds).toBe(696);
  });
});

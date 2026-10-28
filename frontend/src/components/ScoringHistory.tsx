import { useState } from 'react';
import { formatClock, quarterLabel } from '../lib/clock';
import type { ScorecardEvent } from '../types/nba';

const RECENT = 10;

interface Props {
  events: ScorecardEvent[]; // newest first
  awayName: string;
  homeName: string;
}

export default function ScoringHistory({ events, awayName, homeName }: Props) {
  const [showAll, setShowAll] = useState(false);
  const visible = showAll ? events : events.slice(0, RECENT);

  return (
    <section className="scoring-history" aria-labelledby="history-heading">
      <h3 id="history-heading">Scoring history</h3>
      {events.length === 0 ? (
        <p className="hint">No scoring yet.</p>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th scope="col">When</th>
                <th scope="col">Team</th>
                <th scope="col">Play</th>
                <th scope="col" className="num">{awayName} – {homeName}</th>
              </tr>
            </thead>
            <tbody>
              {visible.map((e) => (
                <tr key={e.id}>
                  <td className="time-cell">{quarterLabel(e.quarter)} {formatClock(e.clockSeconds)}</td>
                  <td>{e.teamName}</td>
                  <td>{e.playerName} <span className={`pill pill-${e.points}`}>+{e.points}</span> ({e.points === 3 ? '3-pointer' : '2-pointer'})</td>
                  <td className="num">{e.awayScore} – {e.homeScore}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {events.length > RECENT && (
            <button type="button" className="link-btn" onClick={() => setShowAll((v) => !v)}>
              {showAll ? `Show last ${RECENT} only` : `Show all ${events.length} plays`}
            </button>
          )}
        </>
      )}
    </section>
  );
}

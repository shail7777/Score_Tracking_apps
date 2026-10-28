# Implementation Plan: Team Tiles, Team Details, Schedule and Play-by-Play Scorecard

**Status:** Features 1–7 implemented, plus an added game clock / shot clock / quarter and scoring history on the scorecard page. Feature 8 (the UI reference pass) remains.

The plan is organised as **one vertical slice per feature**. Each slice lists its **Database**, **Backend** and **Frontend** changes, and ends with its own acceptance test, so a feature is finished only when all three layers work together.

Builds on the existing app: React + Vite + TypeScript frontend, ASP.NET Core (.NET 10) API, EF Core on SQL Server (`Nba` database), sample data imported from the NBA GraphQL API (30 teams, 250 players, 144 games).

> **Open item (feature 8, UI reference):** no UI reference was attached. Layout and styling below are provisional. Put the reference in `docs/ui-reference/` and review the Frontend sections against it before starting Feature 1; every slice's frontend work is styled to it.

## Feature overview

| # | Feature | DB change | Backend change | Frontend change |
|---|---------|-----------|----------------|-----------------|
| 1 | Home page with team tiles | None | Small (sorted team list) | `HomePage`, `TeamTile`, routing |
| 2 | Tile click → team details page | None | None (existing `GET /api/teams/{id}`) | Route, `TeamDetailPage` shell |
| 3 | Players in left section | Index migration | Roster endpoint tuned | Left panel component |
| 4 | Schedule in right section | Index migration | Schedule endpoint extended | Right panel component, game links |
| 5 | Open a game → scorecard | New `ScoringEvents` table | Scorecard read endpoint, `InProgress` status | `ScorecardPage` route and data load |
| 6 | Play-by-play table (teams + players) | None | Scorecard DTO with per-player totals | Two player tables, score header |
| 7 | 2-point / 3-point buttons | `RowVersion` on `Games` | Record / undo / finish endpoints | +2 / +3 buttons, undo, finish |
| 8 | UI reference | None | None | Styling pass against the reference |

Dependencies: 1 → 2 → (3, 4 in parallel) → 5 → 6 → 7 → 8 (8 applies throughout and is verified last).

## Current state

| Area | Today |
|------|-------|
| Routes | `/teams`, `/teams/:id`, `/players`, `/games` under `Layout` |
| Pages | `TeamsPage` (table), `TeamDetailPage` (stacked roster + games), `PlayersPage`, `GamesPage` |
| API | `/api/teams` (+ `/roster`, `/games`), `/api/players`, `/api/teamplayers`, `/api/games` |
| Database | `Teams`, `Players`, `TeamPlayers`, `Games` (migration `InitialCreate`) |
| Game status | `Scheduled`, `Final` (stored as string) |

## Design decisions (apply to all slices)

1. **Scoring events are the source of truth**: one row per basket (player, 2 or 3 points, timestamp). Player, team and game totals are derived, so undo is a delete and totals cannot drift.
2. **Game scores are kept in sync** (`Games.HomeScore/AwayScore`) in the same transaction as each event, so existing endpoints and the schedule keep working.
3. **A game's roster is each team's current active `TeamPlayer` rows** (the import has no historical rosters).
4. **Points only**: no assists, fouls, free throws, quarters or game clock. The event table can later gain an event type and period without a rewrite.
5. **One EF migration per slice that needs a schema change**, applied by the existing startup `Database.Migrate()`.

---

## Feature 1: Home page with team tiles

**Goal:** `/` shows every NBA team's name as a selectable tile.

**Database:** no change. Uses `Teams`.

**Backend**
- `GET /api/teams` already returns all teams. Change its ordering to `Name` (currently `City`, which is blank for some imported teams) and keep the DTO as is.
- Verify the response for all 30 teams contains a non-empty `name` and `city`.

**Frontend**
- New `pages/HomePage.tsx`: loads `api.teams.list()` via `useFetch`, renders a search box (filters tiles by city and name as you type, with a "no match" message) above a responsive CSS grid of tiles with `Status` for loading/error and an empty state.
- New `components/TeamTile.tsx`: a keyboard-focusable link showing the team's city (location) and full name.
- Router: `index` route (`/`) renders `HomePage` (replace the redirect to `/teams`); nav gets a "Home" link; the old `/teams` table page is removed or redirected to `/`.
- Styles: tile grid, hover/focus states.

**Acceptance:** opening `/` shows 30 tiles, sorted by name, each focusable and showing the team name.

---

## Feature 2: Team tile click → team details page

**Goal:** clicking a tile navigates to `/teams/:id` showing that team.

**Database:** no change.

**Backend:** no change. `GET /api/teams/{id}` already returns the team (404 for unknown ids).

**Frontend**
- `TeamTile` links to `/teams/${team.id}` (client-side navigation, no full reload).
- `TeamDetailPage`: header with the team's city and name and a "← All teams" link to `/`; handle invalid/unknown ids with a "team not found" state (using the 404 from the API); page layout becomes a two-column container (filled by Features 3 and 4) that stacks on narrow screens.

**Acceptance:** clicking any tile opens the matching team's page; Back returns home; `/teams/9999` shows the not-found state.

---

## Feature 3: Team's players in the left section

**Goal:** the left column lists the selected team's players.

**Database**
- Migration `AddRosterIndex`: composite index on `TeamPlayers (TeamId, EndDate)` to make the "active roster" query efficient (current rows have `EndDate IS NULL`).

**Backend**
- `GET /api/teams/{id}/roster?activeOnly=true` already exists. Confirm it orders players by last name and returns player name; no contract change.
- Verify the query uses the new index (check the generated SQL/plan).

**Frontend**
- New `components/PlayersPanel.tsx` (left column): fetches `api.teams.roster(id)`; table with the player name; empty state ("No players on roster").
- Optional "include former players" toggle (existing behaviour), kept only if the UI reference includes it.

**Acceptance:** each team page shows its players on the left (e.g. a team with ~8 imported players lists exactly those); player count matches `SELECT COUNT(*) FROM TeamPlayers WHERE TeamId = @id AND EndDate IS NULL`.

---

## Feature 4: Team's schedule in the right section

**Goal:** the right column lists the team's games; each is openable.

**Database**
- Migration `AddGameTeamDateIndexes`: indexes on `Games (HomeTeamId, GameDate)` and `Games (AwayTeamId, GameDate)` so a team's schedule is read in date order without scanning.

**Backend**
- `GET /api/teams/{id}/games` already exists; extend it to:
  - sort by `GameDate` ascending by default (schedule order), with `?status=` filter retained;
  - include `opponentName` and `isHome` in the response DTO (derived for the requested team) so the UI needs no extra lookups.

**Frontend**
- New `components/SchedulePanel.tsx` (right column): fetches `api.teams.games(id)`; each row shows date, "vs"/"@" opponent, and status or score (for example `W 112–105`, `Final`, `In progress`, `Upcoming`).
- Each row is a link to `/games/:gameId/scorecard` (used by Feature 5).
- Update `types/nba.ts` and `GameTable`/`GameStatus` for the new fields and the `InProgress` value.

**Acceptance:** the schedule lists all games for the team in date order, home/away labelled correctly; clicking a row navigates to the scorecard route.

---

## Feature 5: Open a game and start a scorecard

**Goal:** opening a game shows its play-by-play scorecard screen with persisted data.

**Database**
- Migration `AddScoringEvents`: new table `ScoringEvents`
  - `Id` int PK, `GameId` int FK → `Games` (cascade), `TeamId` int FK → `Teams` (restrict), `PlayerId` int FK → `Players` (restrict), `Points` tinyint with `CHECK (Points IN (2, 3))`, `CreatedAt` datetime2 (default `SYSUTCDATETIME()`).
  - Index on `(GameId, CreatedAt)`.
- `GameStatus` gains `InProgress` (column is a string, so no data migration).

**Backend**
- New `Models/ScoringEvent.cs`; `NbaDbContext` mapping; `GameStatus.InProgress`.
- `GET /api/games/{id}/scorecard`: returns game header (id, date, status, home/away team ids and names), and for each team its active roster players with totals (zeros when there are no events). 404 for an unknown game.
- Games with no events behave as "not started"; `GamesController` validation updated so `InProgress` games may have scores.

**Frontend**
- New route `/games/:gameId/scorecard` and `pages/ScorecardPage.tsx` loading `api.games.scorecard(id)`; loading/error/not-found states; "← Back to team" link.
- Types: `Scorecard`, `ScorecardTeam`, `ScorecardPlayer`; client method `api.games.scorecard`.

**Acceptance:** clicking a game on the schedule opens the scorecard page with game header and both teams loaded from the API; the `ScoringEvents` table exists with its constraint (insert of `Points = 4` fails).

---

## Feature 6: Play-by-play screen with teams and players in tables

**Goal:** the scorecard shows each team and its players in table format with running totals.

**Database:** no change. Totals are aggregated from `ScoringEvents` with a `GROUP BY PlayerId` query.

**Backend**
- Complete the scorecard DTO: per player `playerId`, `name`, `points`, `twoPointers`, `threePointers`; per team `teamId`, `name`, `totalPoints`; header `homeScore`, `awayScore` calculated from events.
- Single aggregated query (no N+1); unit test for the aggregation.

**Frontend**
- New `components/ScorecardTable.tsx`: one table per team (away left/top, home right/bottom, matching the reference) with columns Player, `PTS`, `2PT`, `3PT` and a totals footer row.
- Score header with both team names and live totals; game status badge.
- Tables are accessible (`<th scope>`), with an empty state when a team has no players.

**Acceptance:** both teams appear as tables listing their players; with seeded events (insert a few rows manually) per-player and team totals match the database.

---

## Feature 7: 2-point and 3-point buttons to record scoring

**Goal:** the user can record a basket for a player with +2 or +3, undo, and finish the game.

**Database**
- Migration `AddGameRowVersion`: add `RowVersion` (`rowversion`/timestamp) to `Games` so two simultaneous scorers cannot overwrite each other's score totals (EF concurrency token).

**Backend**
- `POST /api/games/{id}/events` body `{ playerId, points }`:
  - validates `points` ∈ {2, 3}; the player is on the home or away team's active roster; the game is not `Final`;
  - inserts the event, recalculates `HomeScore`/`AwayScore`, sets status `InProgress` on the first event, all in one transaction;
  - on concurrency conflict returns 409; on success returns the updated scorecard.
- `DELETE /api/games/{id}/events/last`: removes the latest event and recalculates totals (reverting to `Scheduled` if none remain).
- `POST /api/games/{id}/finish`: sets `Final` (requires at least one event).
- Errors use the existing `ProblemDetails` format.
- xUnit tests: valid 2 and 3, invalid points, wrong player, finished game, undo, finish, concurrency conflict.

**Frontend**
- In `ScorecardTable`, each player row gets **+2** and **+3** buttons calling `api.games.recordEvent(gameId, playerId, points)`; state updates from the returned scorecard; buttons disabled while a request is in flight or when the game is `Final`.
- "Undo last" and "Finish game" controls (with a confirmation for finish); error message area for 400/409 responses (409 triggers a reload of the scorecard).
- Client methods: `recordEvent`, `undoLast`, `finishGame`.

**Acceptance:** pressing +3 on a player increases that player's `PTS` by 3 and `3PT` by 1, and the team and game score by 3; +2 behaves likewise; undo reverses it; reload keeps scores; the schedule row for the game shows the updated score; after finish, buttons are disabled; rows in `ScoringEvents` and `Games.HomeScore/AwayScore` agree.

---

## Feature 8: UI reference

**Database / Backend:** none.

**Frontend:** styling and layout pass across the home grid, team page columns, schedule rows and scorecard tables to match the provided reference (colours, spacing, typography, tile size, button styles, responsive behaviour). Done after Feature 7 using the real screens, with small fixes applied to earlier slices as needed.

**Acceptance:** side-by-side comparison with the reference at desktop and mobile widths.

---

## Migration sequence

| Order | Migration | Feature | Contents |
|-------|-----------|---------|----------|
| 1 | `AddRosterIndex` | 3 | Index `TeamPlayers (TeamId, EndDate)` |
| 2 | `AddGameTeamDateIndexes` | 4 | Indexes on `Games (HomeTeamId, GameDate)`, `(AwayTeamId, GameDate)` |
| 3 | `AddScoringEvents` | 5 | `ScoringEvents` table, check constraint, index |
| 4 | `AddGameRowVersion` | 7 | `Games.RowVersion` |

Create with `dotnet ef migrations add <Name> -o Data/Migrations` and apply with `dotnet ef database update` (or by starting the API). Take a backup of the `Nba` database before migration 3 and 4 if it holds real data.

## Test summary

- **Backend:** xUnit tests per slice (listed above) against a test database.
- **Frontend:** `npm run build` (type-check) per slice; Vitest + React Testing Library for tile navigation, schedule links and scorecard buttons.
- **End to end (manual):** home → tile → team page (players left, schedule right) → open game → record +2/+3 → undo → finish → check the schedule and database values.

## Risks and open questions

| Item | Impact | Handling |
|------|--------|----------|
| No UI reference provided | Rework in Feature 8 | Supply the reference before Feature 1 |
| Imported past games are `Final` with no score | Unclear whether they can be re-scored | Decided in Feature 7: a game is locked only when it is `Final` **and** has events; a past game with no events can be scored (it becomes `In progress`) |
| Rosters are current, not per-game | Traded players may appear on the wrong team in older games | Accept now; add per-game rosters later |
| Team city/conference/division are missing from the import | Tile labels and grouping are limited | Show city and name only; add a static team lookup later |
| Concurrent scorers | Lost updates | `RowVersion` (Feature 7) plus append-only events |
| No authentication | Anyone can record scores | Out of scope; address before deployment |

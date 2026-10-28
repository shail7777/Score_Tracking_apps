# NBA App – Technical Reference

For setup and running instructions see the [main README](../README.md).

Full-stack boilerplate: **React (Vite + TypeScript)** → **ASP.NET Core (.NET 10) Web API** → **SQL Server (your existing instance)**, with sample data imported from the internal NBA GraphQL API.

```
.
├── docker-compose.yml        # Optional: runs only the API container (no database)
├── backend/Nba.Api           # ASP.NET Core REST API (EF Core SQL Server, Swagger, GraphQL importer)
└── frontend                  # React SPA (Vite, React Router)
```

## Prerequisites
Docker, .NET 10 SDK, Node 18+.

## Run locally

```bash
# 1. Database: uses your existing SQL Server; set ConnectionStrings:Default in appsettings.json
#    (or via user-secrets / the ConnectionStrings__Default environment variable).

# 2. API  → http://localhost:5080/swagger  (applies EF migrations on startup)
cd backend/Nba.Api
dotnet user-secrets init
dotnet user-secrets set "NbaGraphQl:ApiKey" "<your key>"
dotnet user-secrets set "NbaGraphQl:ClientId" "<your client id>"
dotnet ef database update       # optional: create/update the tables now (needs: dotnet tool install -g dotnet-ef)
dotnet run

# 3. Frontend → http://localhost:5173
cd frontend
npm install
npm run dev
```

The API applies pending migrations on start. The Vite dev server proxies `/api` to `http://localhost:5080`.
To run only the API in Docker (`docker compose up --build`), copy `.env.example` to `.env` and set `DB_CONNECTION_STRING` (use `host.docker.internal` as the server), `NBA_API_KEY` and `NBA_CLIENT_ID`. No database container is created.

## Sample data import

On startup (after the API is listening) `Import/NbaImportService` calls the GraphQL `games` query configured under `NbaGraphQl` in `appsettings.json` (URL, `ApiKeyHeader`, `ClientIdHeader`, `LeagueId`, `StartDate`/`EndDate`, `Limit`) and saves teams, players, rosters and games.
The API key is sent in `ApiKeyHeader` and, if set, `NbaGraphQl:ClientId` in `ClientIdHeader` (defaults `client-api-key`, `client-id`).
It is skipped if `NbaGraphQl:ApiKey` is empty or `ImportOnStartup` is `false`, and a failure is only logged. Re-running is safe (idempotent):

- Teams are matched by `nickName`; players by full name; games by (date, home, away).
- The API returns no scores, conference/division, heights or weights, so those stay empty. Games in the past are stored as `Final` without a score; future ones as `Scheduled`.
- Players are put on the roster of the first team they appear under (no start dates or trades in the data).

## Backend structure

Requests flow **controller → service → database**:

- `Controllers/` – thin HTTP layer. Controllers only call a service and translate its `ServiceResult` into a status code (`ApiControllerBase`): `Ok`/`Created`/`NotFound`/`BadRequest`/`Conflict` become 200/201/404/400/409 (204 for updates and deletes). They never touch `NbaDbContext`.
- `Services/` – all data access and business rules, each behind an interface registered in `Program.cs`:
  `ITeamService`, `IPlayerService`, `ITeamPlayerService`, `IGameService` (CRUD and queries), plus `ScorecardService` (read the scorecard) and `ScoringService` (record baskets, undo, finish, clock). `Mappers` converts entities to DTOs.
- `Data/` – `NbaDbContext` and EF migrations. `Import/` – the GraphQL importer.

Backend tests (`backend/Nba.Api.Tests`) cover the services directly, plus the controller-to-HTTP mapping.

## Data model

- **Team** – city, name, conference, division.
- **Player** – name, height, weight, birth date (no team of their own).
- **TeamPlayer** – a player's stint with a team (`StartDate`, `EndDate`). `EndDate = null` is the current roster; older rows are the player's team history.
- **Game** – season, date, arena, home/away team, scores, `Status` (`Scheduled` = upcoming, `InProgress`, `Final` = previous result).
- **ScoringEvent** – one basket (game, team, player, 2 or 3 points, quarter, game-clock seconds left, timestamp); scorecard totals are derived from these rows and kept in sync with `Games.HomeScore/AwayScore`. A game is locked once it is `Final` and has events; a past imported game with no events can still be scored. `Games.RowVersion` prevents two scorers overwriting each other (409). The game also stores its `Quarter` and `ClockSecondsRemaining` so the clock survives a reload.

## API

| Method | Route | Description |
|--------|-------|-------------|
| GET/POST | `/api/teams` | List / create teams |
| GET/PUT/DELETE | `/api/teams/{id}` | Get / update / delete (409 if the team has games) |
| GET | `/api/teams/{id}/roster?activeOnly=true` | Team roster (`false` includes former players) |
| GET | `/api/teams/{id}/games?status=` | The team's schedule in date order, from the team's view (opponent, home/away, score, W/L) |
| POST | `/api/games/{id}/events` | Record a basket `{ playerId, points: 2 \| 3 }`; returns the updated scorecard (400 invalid/locked, 404 unknown game, 409 concurrent update) |
| DELETE | `/api/games/{id}/events/last` | Undo the most recent basket |
| POST | `/api/games/{id}/finish` | Mark the game `Final` and lock scoring (needs at least one basket and no tie) |
| PUT | `/api/games/{id}/clock` | Save the game clock `{ quarter, clockSeconds }` (quarters 1-4 up to 12:00, overtimes 5-7 up to 5:00) |
| GET | `/api/games/{id}/scorecard` | Play-by-play scorecard: both teams' rosters with each player's points, 2PT and 3PT counts, team totals, the saved quarter/clock, and the scoring history (newest first, with the running score) |
| GET/POST | `/api/players?teamId=&search=` | List (by current team / name) / create players |
| GET/PUT/DELETE | `/api/players/{id}` | Get / update / delete |
| GET | `/api/players/{id}/history` | Teams the player has played for |
| GET/POST | `/api/teamplayers?teamId=&playerId=&activeOnly=` | List / create roster entries (a new active entry closes the player's previous one) |
| GET/PUT/DELETE | `/api/teamplayers/{id}` | Get / update / delete |
| GET/POST | `/api/games?teamId=&status=&season=` | List / create games (previous: newest first; upcoming: soonest first) |
| GET/PUT/DELETE | `/api/games/{id}` | Get / update / delete |
| GET | `/health` | Health check (includes DB) |

## Frontend pages
`/` (home: one tile per team, sorted by name, with a search box that filters by city or team name) → `/teams/:id` (players on the left, schedule on the right; unknown ids show "Team not found") → `/games/:gameId/scorecard` (score header; game clock with Start/Pause, shot clock with Reset 24/14, and a quarter selector; a player table per team with +2 / +3 buttons; Undo last and Finish game; scoring history at the bottom showing the last 10 plays, expandable), `/players` (filter, click a row for team history), `/games` (All / Previous / Upcoming, team filter, add/delete game). `/teams` redirects to `/`.

Run the frontend tests with `npm test` (in `frontend/`) and the backend tests with `dotnet test` (in `backend/Nba.Api.Tests/`, uses in-memory SQLite).

## Notes
- Schema changes use EF migrations: after editing models run `dotnet ef migrations add <Name> -o Data/Migrations`, then `dotnet ef database update` (or just start the API).
- Rosters, games and players come from the GraphQL import (no fictional seed data).
- Keep credentials out of source control: prefer user-secrets or environment variables over `appsettings.json`.
- CORS origins are configured under `Cors:AllowedOrigins`.

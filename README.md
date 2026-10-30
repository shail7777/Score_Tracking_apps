# NBA App

A small full-stack app for browsing NBA teams, players and schedules, and keeping a simple play-by-play scorecard for a game.

- **Frontend:** React + TypeScript (Vite), runs on http://localhost:5173
- **Backend:** ASP.NET Core (.NET 10) REST API, runs on http://localhost:5080 (Swagger at `/swagger`)
- **Database:** SQL Server (tables are created automatically on first start)
- **Sample data:** imported from the NBA GraphQL API the first time the backend starts

## What you need

| Tool | Version | Check with |
|------|---------|------------|
| .NET SDK | 10.0 or newer | `dotnet --version` |
| Node.js | 18 or newer | `node --version` |
| SQL Server | any recent version, reachable from your machine | see step 1 |
| NBA API key and client ID | needed to import the sample data | ask the project owner |

Docker is only needed if you don't already have a SQL Server (step 1, option B).

## 1. Get a SQL Server

**Option A – you already have one.** Make a note of the server address, user and password. The app creates a database called `Nba` for you (the login needs permission to create databases).

**Option B – start one with Docker.** Pick your own password (at least 8 characters, with upper case, lower case and a number):

```bash
docker run -d --name nba-sql -p 1433:1433 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='YourStrong!Passw0rd' \
  mcr.microsoft.com/mssql/server:2022-latest
```

On an Apple Silicon Mac add `--platform linux/amd64` after `docker run`. Give it about 30 seconds to start.

## 2. Configure the backend

The backend needs three settings. Keep them out of the code by storing them as .NET user-secrets:

```bash
cd backend/Nba.Api
dotnet user-secrets set "ConnectionStrings:Default" "Data Source=localhost,1433;Database=Nba;User ID=sa;Password=YourStrong!Passw0rd;Trust Server Certificate=True"
dotnet user-secrets set "NbaGraphQl:ApiKey"   "<your API key>"
dotnet user-secrets set "NbaGraphQl:ClientId" "<your client ID>"
```

Change the connection string to match your SQL Server from step 1. 

You can use environment variables instead of user-secrets: `ConnectionStrings__Default`, `NbaGraphQl__ApiKey` and `NbaGraphQl__ClientId` (two underscores).

> Without the API key and client ID the app still runs, but the database stays empty, so there are no teams to show.

## 3. Start the backend

```bash
cd backend/Nba.Api
dotnet run
```

Leave it running. On the first start it creates the tables and then imports about 30 teams, 250 players and 144 games (April 2026). Look for `NBA import finished` in the console. Swagger opens at http://localhost:5080/swagger.

## 4. Start the frontend

In a second terminal:

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173.

## Using the app

1. **Home:** pick a team tile (or search by city or name).
2. **Team page:** players on the left, schedule on the right.
3. Click **Scorecard** on a game to open the play-by-play page.
4. Use **Start/Pause**, the shot clock and the quarter selector, press **+2** or **+3** next to a player to record a basket, **Undo last** to take it back, and **Finish game** to lock the score. Recent plays appear at the bottom.

## Help chat (optional)

The 💬 button in the bottom-right corner opens an FAQ assistant that runs entirely on your machine using [Ollama](https://ollama.com). To enable it:

```bash
ollama pull llama3.2:3b     # one-time, about 2 GB
ollama serve                # skip if the Ollama app is already running
```

Then restart the backend. The answers come from [backend/Nba.Api/Chat/faq.md](backend/Nba.Api/Chat/faq.md); edit that file to add or change FAQs. To use another model, change `Chat:Model` in `appsettings.json`.

## Database script (optional)

The app creates its own tables on first start, so you normally don't need this. If you'd rather set up the database yourself (or you're a DBA), run [db/schema.sql](db/schema.sql). It creates the tables, indexes and constraints, and is safe to run more than once. It contains no data.

```bash
sqlcmd -S localhost,1433 -U sa -P '<password>' -C -Q "CREATE DATABASE Nba"
sqlcmd -S localhost,1433 -U sa -P '<password>' -C -d Nba -i db/schema.sql
```

After changing the data model, regenerate it from the backend folder:

```bash
cd backend/Nba.Api
dotnet ef migrations script --idempotent --output ../../db/schema.sql
```

## Run the tests (optional)

```bash
cd backend/Nba.Api.Tests && dotnet test     # backend (uses an in-memory database)
cd frontend && npm test                      # frontend
```

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `Login failed` or can't connect to SQL Server | Check the connection string, that SQL Server is running, and that port 1433 is free (`docker ps` if you used Docker). |
| Home page says "No teams found" | The import didn't run. Make sure the API key and client ID are set, restart the backend, and look for `NBA import failed` in its console. |
| Console shows `401 Unauthenticated` | The API key or client ID is wrong or expired. |
| Frontend shows a network error | The backend isn't running on port 5080. |
| Port 5080 or 5173 already in use | Stop the other program using it. |
| Help chat says "Could not reach Ollama" | Start Ollama (`ollama serve`) and make sure the model is pulled (`ollama pull llama3.2:3b`). |

## Sharing the project

When copying or zipping the folder, leave out `node_modules`, `bin`, `obj` and any `.env` file. Never share your API key or database password.

More technical details (data model, API endpoints, migrations) are in [docs/REFERENCE.md](docs/REFERENCE.md).

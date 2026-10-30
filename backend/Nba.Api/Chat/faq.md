# NBA App FAQ

## What is this app?
A small full-stack app for browsing NBA teams, players and schedules, and keeping a simple play-by-play scorecard for a game.

## How do I find a team?
On the Home page, click a team tile, or type a city or team name into the search box to filter the tiles.

## What does the team page show?
The team's players (roster) on the left and its schedule on the right. Each game in the schedule has a Scorecard button.

## How do I see all players?
Open the Players page from the top navigation. You can search by name and filter by team.

## How do I see all games?
Open the Games page from the top navigation. You can filter games by status (scheduled, in progress, finished), by team and by season.

## How do I keep score for a game?
Click Scorecard next to a game. Press Start to run the game clock, then press +2 or +3 next to a player to record a basket. The score and the recent plays list update immediately.

## How do I fix a scoring mistake?
Press "Undo last" to remove the most recent basket. You can undo repeatedly, but not after the game is finished.

## How does the clock work?
Use Start/Pause to run or stop the game clock and the quarter selector to change the quarter. You can also set the clock manually. Baskets are stamped with the quarter and time they were recorded.

## How do I end a game?
Press "Finish game". This locks the score; no more baskets can be added or undone.

## Where does the data come from?
Teams, players and games are imported from the NBA GraphQL API the first time the backend starts (about 30 teams, 250 players and 144 games for April 2026). This needs an NBA API key and client ID.

## Why does the Home page say "No teams found"?
The import did not run. Make sure the API key and client ID are configured, restart the backend and look for "NBA import failed" in its console.

## Why do I see a network error?
The backend is not running on port 5080. Start it with `dotnet run` in backend/Nba.Api.

## What database does it use?
SQL Server. The tables are created automatically on first start.

## How do I start the app?
Start the backend with `dotnet run` in backend/Nba.Api (http://localhost:5080), then the frontend with `npm install` and `npm run dev` in the frontend folder, and open http://localhost:5173.

## Where is the API documentation?
Swagger is available at http://localhost:5080/swagger while the backend is running.

## How does this chat assistant work?
It runs locally on your machine using Ollama. Your questions never leave your computer. It only answers questions about how to use this app.

## Why does the chat assistant say it is unavailable?
Ollama is not running or the model has not been downloaded. Start Ollama, then run `ollama pull llama3.2:3b`.

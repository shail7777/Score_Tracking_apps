I need to build and NBA Application which will be a full stack 
applcaition frontend build on React and backend will be build on 
ASP.NET REST API and storing the data in my SQL docker image. Generate a 
boilarplate code for full stack applicaiton in current directotry


Create Models for teams, teamsplayes, games(previours games as well) implement the mode in backend and also in frontend.

Use this GraphAPI to call internal api and get sample data. Save the data into sql server which I will provide the connection string.


With the graph api key I will also need to pass client-id to GrapthAPI. I have configuresed the sql connection string can you migrate the model schema to sql and create tables

All the variables have been setup. Check the connection and pull sample data from graphAPI and save it into database

Create an implmentation plan for these fectures and save that file in root directory. Fetures: 1. Create a home page that displays NBA team names as selectable tiles.
2. When a user clicks a team tile, navigate to a team details page.
3. On the team details page, display the team’s players in the left-side section.
4. Display the selected team’s schedule in the right-side section.
5. Allow the user to open a game and record a simple play-by-play scorecard.
6. On the play-by-play screen, display the team and players in a table format.
7. Provide a 2-point button and a 3-point button so that the user can record scoring events for
players.
8. Reference for UI provided below


Make the plan implementation so that each feacture changes are done from frontend, backend and database side. 

Start implement feacture 1, 2 and 3 from database -> api -> frontend

Remove player # and Pos fields as there is no data from frontend -> api -> database

Remove the short name of the team eg: BUC, CAV, CEL, JAZ and only keep the full name of the team and location attrabute

At the home page make the team searchable in a search bar

Start implementating deature 4, 5, 6

start implementing feature 7

On the game detials page add a timer which we can start and shot clock, and quarter we are in. And show last histroy of scoring in the bottom


Make the team name more fancy above the players. In Players page remove heigh and weight coloums

Create a simple readme file explaining how to run this applicaiton on someone else machine

Improve the game page css and make it simple but fancy

make the dotnet api version 10

Insted of calling the database from controller make a service and call it from that service
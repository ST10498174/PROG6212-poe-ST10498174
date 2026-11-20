# RaceDay MVC front end (Part 3).
# Build from the repository root:  docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .

# ---- Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj
COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
# Address of the API inside the Docker network; override with -e RaceDayApi__BaseUrl=... when needed.
ENV RaceDayApi__BaseUrl=http://api:8080/
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]

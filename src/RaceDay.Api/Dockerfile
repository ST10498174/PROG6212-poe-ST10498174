# RaceDay Web API (Part 2 API extended in Part 3).
# Build from the repository root:  docker build -f src/RaceDay.Api/Dockerfile -t raceday-api .

# ---- Build stage: full .NET SDK, restore first so package layers are cached between builds.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/RaceDay.Api/RaceDay.Api.csproj src/RaceDay.Api/
RUN dotnet restore src/RaceDay.Api/RaceDay.Api.csproj
COPY src/RaceDay.Api/ src/RaceDay.Api/
RUN dotnet publish src/RaceDay.Api/RaceDay.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime stage: small ASP.NET Core runtime image, no SDK or source code.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
# .NET 8 images listen on port 8080 by default.
EXPOSE 8080
COPY --from=build /app/publish .
# Run as the image's built-in non-root user.
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]

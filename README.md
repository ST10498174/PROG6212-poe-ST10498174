# RaceDay — PROG6212 Programming 2B PoE

RaceDay is a full-stack event management system for South African road running, walking and cycling. **Organisers** publish events, set up age and distance categories, confirm entries and publish results. **Participants** find events, enter in a category, track their entries and keep a personal race history.

The repository contains all three parts of the PoE:

| Part | What it adds | Where |
|---|---|---|
| 1 | ERD, API endpoint plan, SQL script | `docs/` |
| 2 | ASP.NET Core Web API, EF Core, session auth, Swagger, xUnit tests | `src/RaceDay.Api`, `tests/RaceDay.Api.Tests` |
| **3** | **ASP.NET Core MVC front end, Azure Blob Storage (Azurite), Docker** | `src/RaceDay.Web`, `docker-compose.yml`, Dockerfiles |

---

## Part 3 — Quick start with Docker (one command)

Requirements: Docker Desktop (or Docker Engine with Compose v2).

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| RaceDay web app (MVC) | http://localhost:8080 |
| Web API + Swagger | http://localhost:5080/swagger |
| Azurite blob endpoint | http://localhost:10000/devstoreaccount1 |
| SQL Server | `localhost,1433` (user `sa`, password `RaceDay!Local2026` unless `SA_PASSWORD` is set) |

The first start takes a minute while SQL Server initialises; the API waits and retries automatically. Stop with `Ctrl+C`. To remove containers **and data**, run `docker compose down -v`.

To start with an **empty database** (no demo data):

```bash
docker compose down -v
SEED_DEMO_DATA=false docker compose up --build        # PowerShell: $env:SEED_DEMO_DATA="false"; docker compose up --build
```

### Running only the MVC container

```bash
docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .
docker run --rm -p 8080:8080 -e RaceDayApi__BaseUrl=http://host.docker.internal:5158/ raceday-web
```

`RaceDayApi__BaseUrl` points the container at an API running on your machine.

### Demo accounts (created by the seeder when the database is empty)

Every demo account uses the password **`RaceDay123!`**

| Role | Email | Notes |
|---|---|---|
| Organiser | `thandi.organiser@raceday.co.za` | Durban events: past results, draft results, pending entries |
| Organiser | `pieter.organiser@raceday.co.za` | Cape Town and Soweto events, one with no entries (deletable) |
| Participant | `lerato.participant@raceday.co.za` | Upcoming entries (pending and confirmed) and three published results |
| Participant | `sipho.participant@raceday.co.za` | Results plus upcoming entries |

> The Part 1 SQL script stores placeholder password hashes, so its sample users cannot log in. Register new accounts, or let the API create the database so the demo seeder adds working accounts.

## Part 3 — Running in Visual Studio (without Docker)

1. **Start Azurite** (the Azure Storage emulator), using either option:
   * `npm install -g azurite` then `azurite-blob --location ./azurite --skipApiVersionCheck`
   * or `docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck`
2. Check the SQL Server connection string in `src/RaceDay.Api/appsettings.json` (LocalDB by default). In Development the API seeds demo data into an empty database.
3. In Visual Studio, right-click the solution, choose **Configure Startup Projects…**, then **Multiple startup projects**, and set **RaceDay.Api** and **RaceDay.Web** to *Start*.
4. Press **F5**. The API runs on http://localhost:5158 (Swagger at `/swagger`) and the MVC app on http://localhost:5200.

From a terminal:

```powershell
dotnet run --project .\src\RaceDay.Api\RaceDay.Api.csproj --launch-profile http
dotnet run --project .\src\RaceDay.Web\RaceDay.Web.csproj --launch-profile http
```

## Part 3 — Features

### Organiser interface ("race control", sidebar navigation)
* **Dashboard:** events created, upcoming events, total entries, entries to confirm, events awaiting results, a next-event countdown, entries-per-event bars and results progress.
* **Events:** create, edit and delete with name, description, date, location, distance and type (Run, Walk or Cycle), plus **banner upload**. Deleting is blocked once participants have entered.
* **Categories:** add, edit and remove age groups (min/max age) or distance categories per event.
* **Entries:** every participant with category, contact details, entry date and colour-coded status. Confirm one entry, all pending entries, or set an entry back to pending.
* **Results:** capture finish time and position per entrant after race day, save as a draft or publish, and publish all drafts at once.

### Participant interface (top navigation)
* **Find events (home):** next-race countdown, personal stats, and all upcoming events with search, type, location and date filters.
* **Event page:** banner, key facts, description, categories and **Enter event** (category choice; the enrolment is recorded immediately).
* **My events:** upcoming and completed entries with **green Confirmed / amber Pending** status; withdraw before race day.
* **My results:** race history with event, date, category, finish time, pace or speed, and position, e.g. **"4th of 6 finishers"**.
* **Profile:** edit personal details and upload a profile picture.

### Cross-cutting
* **Session-based login and RBAC:** the MVC session stores user ID and role and relays the API's session cookie server-side. `[RoleAuthorize]` filters protect every area and redirect anonymous users to login and wrong-role users to "Access denied". The API enforces the same rules again.
* **Blob Storage:** files go from the MVC app to the API, then to Azure Blob Storage (Azurite). The URL is saved in the database and displayed in the MVC app. The MVC project has no Azure dependency.
* **Friendly errors:** validation messages on every form, API messages shown beside fields, styled 403/404 and "service unavailable" pages, and a redirect to login when a session expires.
* **Responsive UI:** Bootstrap 5.3 grid, RaceDay brand styling, self-hosted fonts and icons, original SVG illustrations, keyboard focus styles, and reduced-motion support.

See **`docs/Part3_Report.md`** for architecture and design decisions (with references), and **`docs/Part3_Test_Plan.md`** for test evidence.

## Part 3 — Repository structure

```text
RaceDay.sln
docker-compose.yml             # SQL Server + Azurite + API + MVC
.dockerignore
src/
  RaceDay.Api/                 # Part 2 Web API (+ Part 3 uploads, filters, dashboard data)
    Controllers/  Data/  DTOs/  Models/  Services/  Dockerfile
  RaceDay.Web/                 # Part 3 MVC front end (no database access)
    Areas/Organiser/           # race-control pages (Organiser role only)
    Areas/Participant/         # participant pages (Participant role only)
    Controllers/               # home, account, public events, profile
    Infrastructure/            # session helpers, RoleAuthorize filter, upload rules
    Services/                  # RaceDayApiClient: every call to the API
    Models/  Views/  wwwroot/  Dockerfile
tests/
  RaceDay.Api.Tests/           # xUnit tests for Parts 2 and 3
docs/
  RaceDay_ERD.png  API_ENDPOINT_PLAN.md  RaceDayDB.sql  Part1_Report.md
  Part2_Test_Plan.md  Part3_Report.md  Part3_Test_Plan.md
.github/workflows/
  part1-validation.yml  part2-build-test.yml  part3-build-test-docker.yml
```

## Part 3 — CI/CD

`.github/workflows/part3-build-test-docker.yml` runs on pushes to `main` and `part3`:

1. Restores and builds the whole solution (API + MVC + tests) in Release mode.
2. Runs all xUnit tests.
3. Validates the repository structure.
4. Validates `docker-compose.yml`, builds both Docker images and smoke-tests the MVC container.

**Part 3 CI screenshot:** `docs/part3-ci-green-build.png` *(add after the workflow passes)*

## Part 3 — Video presentation

Record an unlisted YouTube video in your **own voice** (no AI voices). Demonstrate the running application in Docker, both role interfaces, the Blob Storage flow (upload a banner, show the blob URL and the image in the MVC app), and the Docker setup, and explain the code structure.

**YouTube video link:** `ADD_UNLISTED_YOUTUBE_LINK_HERE`

## Part 3 — Submission checklist

- [x] ASP.NET Core MVC app consuming the API for all data (no DB access in MVC)
- [x] Register/login through the API; session stores user ID and role; logout clears the session
- [x] Distinct Organiser and Participant interfaces and navigation; protected pages redirect
- [x] Organiser dashboard, event CRUD with banner upload, categories, enrolments, results entry
- [x] Participant home with filters, event details with Enter Event, my events, my results, profile with picture
- [x] Azure Blob Storage (Azurite) integrated in the API; URLs saved and displayed
- [x] Dockerfiles and one-command `docker compose up --build`
- [x] Colour-coded statuses, consistent RaceDay theme, graceful error handling
- [x] CI workflow builds the MVC project, runs tests and builds the images
- [ ] **20+ meaningful Part 3 commits** from your own GitHub account on the `part3` branch
- [ ] Green Part 3 CI run and screenshot added to `docs/`
- [ ] Own-voice unlisted YouTube video link added above

## AI use disclosure

Generative AI (Anthropic's Claude) was used as a planning, coding, design and code-review aid for Part 3, including the MVC front end, API extensions, Docker configuration, tests and documentation. In line with the assignment instructions, the student must review, understand, test and be able to explain all of this code before submission, and should state here how they reviewed and changed it.

## References

Full Harvard-style references with in-text citations are in [`docs/Part3_Report.md`](docs/Part3_Report.md#references). Key sources: Microsoft Learn documentation on Azurite, session state, areas, filters, `IHttpClientFactory`, file uploads, Docker for ASP.NET Core and EF Core serialization; the Docker Compose file reference; and the Bootstrap 5.3 documentation.

Third-party assets bundled in `src/RaceDay.Web/wwwroot/lib`: Bootstrap 5.3.3 and Bootstrap Icons 1.11.3 (MIT), jQuery 3.7.1 with jQuery Validation and the unobtrusive adapter (MIT), and the Barlow and Barlow Condensed fonts (SIL Open Font License). Licence files are included beside each library.

---

# Part 2 — RESTful API (retained documentation)

RaceDay is a South African road-running, walking, and cycling event management system. The repository now contains the **Part 1 planning/database work** and the **Part 2 RESTful API implementation**.

### Part 2 Features

The API follows the endpoint plan created in Part 1 and provides:

- Registration and login for **Organiser** and **Participant** roles.
- Server-side session authentication that stores the logged-in user's `UserId` and `Role`.
- Password hashing using ASP.NET Core `PasswordHasher<TUser>`.
- User profile viewing and editing.
- Public event and category viewing.
- Organiser event CRUD, category management, event enrolment viewing, and result capture.
- Participant event enrolment, personal enrolment viewing, and personal published result viewing.
- Entity Framework Core with SQL Server using the Code-First model.
- Swagger UI for browser-based endpoint testing.
- xUnit unit tests for authentication, role enforcement, event management, enrolment, and results.
- GitHub Actions workflow that restores, builds, and runs all unit tests on every push.

### Roles

#### Organiser
An Organiser can create, update, and delete their own events, manage categories, view enrolments for their own events, and capture or update participant results.

#### Participant
A Participant can browse events, view categories, enrol in an event, view their own enrolments, view published personal results, and manage their own profile.

### Repository Structure

```text
RaceDay.sln
src/
  RaceDay.Api/
    Controllers/
    Data/
    DTOs/
    Models/
tests/
  RaceDay.Api.Tests/
docs/
  RaceDay_ERD.png
  API_ENDPOINT_PLAN.md
  RaceDayDB.sql
  Part1_Report.md
  Part2_Test_Plan.md
.github/workflows/
  part1-validation.yml
  part2-build-test.yml
```

### Requirements

- Visual Studio 2022 with the **ASP.NET and web development** workload, or .NET 8 SDK.
- SQL Server LocalDB, SQL Server Express, or another reachable SQL Server instance.
- Git for version control.

### Database Setup

#### Option A — Use the Part 1 SQL script

1. Open `docs/RaceDayDB.sql` in SQL Server Management Studio.
2. Run the full script.
3. Confirm the final verification counts are returned without errors.
4. Keep the default LocalDB connection string if your SQL Server is `(localdb)\\MSSQLLocalDB`, otherwise update `src/RaceDay.Api/appsettings.json`.

#### Option B — Let EF Core create the empty schema

If `RaceDayDB` does not exist, the API uses the Code-First model and `EnsureCreated()` to create the schema. The two required roles are seeded automatically. This is useful for a fresh Part 2 setup.

> The EF Core entity mappings intentionally match the Part 1 ERD and SQL schema. The original Part 1 documents remain in `/docs` as the implementation specification.

### Run the API in Visual Studio

1. Open `RaceDay.sln` in Visual Studio 2022.
2. Right-click **RaceDay.Api** and select **Set as Startup Project**.
3. Check `src/RaceDay.Api/appsettings.json` and confirm the SQL Server connection string is correct for your computer.
4. Press **F5** or select the HTTPS profile.
5. Swagger opens automatically at `/swagger`.

### Run from PowerShell

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project .\src\RaceDay.Api\RaceDay.Api.csproj
```

Then open the Swagger URL shown in the terminal, for example:

```text
https://localhost:7258/swagger
```

### Swagger Testing Order

Because authentication is session-based, use the endpoints in this order inside the same Swagger browser tab:

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. Test endpoints for the logged-in role.
4. `POST /api/auth/logout` when finished.

The browser keeps the RaceDay session cookie after a successful login, so protected endpoints can be tested directly through Swagger without extra tools.

#### Example Organiser registration

```json
{
  "firstName": "Jarred",
  "lastName": "Mokoena",
  "email": "jarred.organiser@raceday.co.za",
  "password": "Password123",
  "phoneNumber": "0825550111",
  "dateOfBirth": "1994-04-12",
  "role": "Organiser"
}
```

#### Example Participant registration

```json
{
  "firstName": "Lerato",
  "lastName": "Khumalo",
  "email": "lerato.participant@raceday.co.za",
  "password": "Password123",
  "phoneNumber": "0825550222",
  "dateOfBirth": "2001-09-20",
  "role": "Participant"
}
```

### Unit Tests

Run all unit tests locally before submission:

```powershell
dotnet test RaceDay.sln
```

The tests cover successful and failed registration/login, unauthenticated access, Organiser/Participant role restrictions, event ownership, category management, Participant enrolment, duplicate enrolment, and result capture. See `docs/Part2_Test_Plan.md` for the full test checklist.

### GitHub Actions / CI-CD

Part 2 uses `.github/workflows/part2-build-test.yml`. On a push, GitHub Actions:

1. Checks out the repository.
2. Installs .NET 8.
3. Restores NuGet packages.
4. Builds the full solution in Release mode.
5. Runs all xUnit tests.
6. Checks the required Part 2 repository structure.

#### CI/CD Screenshot

After the workflow passes, add a screenshot of the green Part 2 workflow here before submission:

**Part 2 CI screenshot:** `docs/part2-ci-green-build.png`

> Replace this instruction with the actual screenshot in the repository and embed it here before final submission.

### Video Presentation

Record an unlisted YouTube video using your **own voice**. Demonstrate:

- The running Swagger API.
- Registration and login for both roles.
- Session and role enforcement, including at least one `401` and one `403` example.
- Event CRUD and category management as an Organiser.
- Event enrolment as a Participant.
- Result capture and Participant result viewing.
- The project structure and important controller/EF Core code.
- The xUnit tests running locally with all tests passing.
- The successful GitHub Actions green build.

**YouTube video link:** `ADD_UNLISTED_YOUTUBE_LINK_HERE`

### Part 2 Submission Checklist

- [x] ASP.NET Core Web API project created.
- [x] SQL Server + EF Core Code-First model implemented.
- [x] Entity model matches the Part 1 ERD/SQL design.
- [x] Registration and login implemented.
- [x] Passwords hashed before storage.
- [x] Server-side session stores user ID and role.
- [x] Protected endpoints reject unauthenticated requests.
- [x] Role-specific endpoints reject the wrong role.
- [x] User profile view/update implemented.
- [x] Event endpoints implemented.
- [x] Category endpoints implemented.
- [x] Event enrolment endpoints implemented.
- [x] Result endpoints implemented.
- [x] Swagger integration included.
- [x] Unit tests included for required success/failure scenarios.
- [x] GitHub Actions build/test workflow included.
- [ ] Create **20 or more genuine meaningful Part 2 commits** using your own GitHub account.
- [ ] Push all work to the lecturer-provided GitHub repository.
- [ ] Confirm GitHub Actions has a green build.
- [ ] Add the successful Part 2 CI screenshot to the README.
- [ ] Record the required own-voice video and add the unlisted YouTube link.

### AI Use Disclosure

Generative AI was used as a planning, coding, proofreading, and code-review aid for Part 2. In accordance with the assignment instructions, the student must review, understand, test, and be able to explain the final code before submission.

### Part 1 Documentation

The original Part 1 planning files remain unchanged in the `/docs` folder and continue to act as the Part 2 specification:

- `docs/RaceDay_ERD.png`
- `docs/API_ENDPOINT_PLAN.md`
- `docs/RaceDayDB.sql`
- `docs/Part1_Report.md`

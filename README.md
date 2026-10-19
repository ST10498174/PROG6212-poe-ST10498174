# RaceDay — PROG6212 Programming 2B PoE

RaceDay is a South African road-running, walking, and cycling event management system. The repository now contains the **Part 1 planning/database work** and the **Part 2 RESTful API implementation**.

## Part 2 Features

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

## Roles

### Organiser
An Organiser can create, update, and delete their own events, manage categories, view enrolments for their own events, and capture or update participant results.

### Participant
A Participant can browse events, view categories, enrol in an event, view their own enrolments, view published personal results, and manage their own profile.

## Repository Structure

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

## Requirements

- Visual Studio 2022 with the **ASP.NET and web development** workload, or .NET 8 SDK.
- SQL Server LocalDB, SQL Server Express, or another reachable SQL Server instance.
- Git for version control.

## Database Setup

### Option A — Use the Part 1 SQL script

1. Open `docs/RaceDayDB.sql` in SQL Server Management Studio.
2. Run the full script.
3. Confirm the final verification counts are returned without errors.
4. Keep the default LocalDB connection string if your SQL Server is `(localdb)\\MSSQLLocalDB`, otherwise update `src/RaceDay.Api/appsettings.json`.

### Option B — Let EF Core create the empty schema

If `RaceDayDB` does not exist, the API uses the Code-First model and `EnsureCreated()` to create the schema. The two required roles are seeded automatically. This is useful for a fresh Part 2 setup.

> The EF Core entity mappings intentionally match the Part 1 ERD and SQL schema. The original Part 1 documents remain in `/docs` as the implementation specification.

## Run the API in Visual Studio

1. Open `RaceDay.sln` in Visual Studio 2022.
2. Right-click **RaceDay.Api** and select **Set as Startup Project**.
3. Check `src/RaceDay.Api/appsettings.json` and confirm the SQL Server connection string is correct for your computer.
4. Press **F5** or select the HTTPS profile.
5. Swagger opens automatically at `/swagger`.

## Run from PowerShell

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project .\src\RaceDay.Api\RaceDay.Api.csproj
```

Then open the Swagger URL shown in the terminal, for example:

```text
https://localhost:7258/swagger
```

## Swagger Testing Order

Because authentication is session-based, use the endpoints in this order inside the same Swagger browser tab:

1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. Test endpoints for the logged-in role.
4. `POST /api/auth/logout` when finished.

The browser keeps the RaceDay session cookie after a successful login, so protected endpoints can be tested directly through Swagger without extra tools.

### Example Organiser registration

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

### Example Participant registration

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

## Unit Tests

Run all unit tests locally before submission:

```powershell
dotnet test RaceDay.sln
```

The tests cover successful and failed registration/login, unauthenticated access, Organiser/Participant role restrictions, event ownership, category management, Participant enrolment, duplicate enrolment, and result capture. See `docs/Part2_Test_Plan.md` for the full test checklist.

## GitHub Actions / CI-CD

Part 2 uses `.github/workflows/part2-build-test.yml`. On a push, GitHub Actions:

1. Checks out the repository.
2. Installs .NET 8.
3. Restores NuGet packages.
4. Builds the full solution in Release mode.
5. Runs all xUnit tests.
6. Checks the required Part 2 repository structure.

### CI/CD Screenshot

After the workflow passes, add a screenshot of the green Part 2 workflow here before submission:

**Part 2 CI screenshot:** `docs/part2-ci-green-build.png`

> Replace this instruction with the actual screenshot in the repository and embed it here before final submission.

## Video Presentation

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

## Part 2 Submission Checklist

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

## AI Use Disclosure

Generative AI was used as a planning, coding, proofreading, and code-review aid for Part 2. In accordance with the assignment instructions, the student must review, understand, test, and be able to explain the final code before submission.

## Part 1 Documentation

The original Part 1 planning files remain unchanged in the `/docs` folder and continue to act as the Part 2 specification:

- `docs/RaceDay_ERD.png`
- `docs/API_ENDPOINT_PLAN.md`
- `docs/RaceDayDB.sql`
- `docs/Part1_Report.md`

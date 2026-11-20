# RaceDay — Part 3 Report: MVC Front End, Blob Storage and Docker

This report explains how Part 3 extends the Part 1 design and the Part 2 Web API into a complete, containerised RaceDay platform. It covers the architecture, the main implementation decisions, how authentication and role-based access work end to end, how images are stored, how the system runs in Docker, and how it was tested.

## 1. Architecture

```text
 Browser ──HTTP──▶  RaceDay.Web (ASP.NET Core MVC)      no database access, no business rules
                       │  typed HttpClient + relayed API session cookie
                       ▼
                    RaceDay.Api (ASP.NET Core Web API)  all validation, ownership and role rules
                       │                    │
                EF Core (SQL Server)   Azure.Storage.Blobs SDK
                       ▼                    ▼
                    RaceDayDB          Azurite (Azure Storage emulator)
```

* **RaceDay.Web** is a new ASP.NET Core MVC project. It has no NuGet packages and no `DbContext`; every read and write is an HTTP call to the Part 2 API through `IRaceDayApiClient`. This satisfies the brief's rule that the MVC project must not contain database access or replicated business logic.
* **RaceDay.Api** is the Part 2 API with small, documented additions (section 6). It remains the single place where validation, ownership and role checks happen.
* **Azurite** replaces a cloud storage account. Microsoft describes Azurite as an open-source emulator that gives a free local environment for testing Blob, Queue and Table Storage applications (Microsoft, n.d.a), so the same Azure SDK code runs locally, in Docker and against a real account if the connection string changes.

The MVC project is split into two **areas**, `Organiser` and `Participant`. Areas organise related functionality as a separate routing namespace and folder structure for views (Microsoft, n.d.b), which gives each role its own controllers, views and navigation. Shared pages (home, login/register, public event list and event details, profile) live outside the areas.

## 2. Authentication, session management and RBAC

### 2.1 Session relay between the MVC app and the API

The Part 2 API already authenticates with a server-side session cookie (`.RaceDay.Session`) that stores the user's ID and role. ASP.NET Core keeps session state by giving the client a cookie containing a session ID that is sent with each request (Microsoft, n.d.c). In Part 3 the "client" of the API is the MVC server, not the browser, so the MVC app relays the session:

1. `AccountController.Login` posts the credentials to `POST /api/auth/login`.
2. `RaceDayApiClient` reads the API's `Set-Cookie` header and stores the `.RaceDay.Session=…` value in the **MVC** server-side session, together with the user's ID, role and name.
3. Every later API call made for that user adds the stored cookie to the request, so the API's own role and ownership checks apply to everything the MVC app does.
4. The browser only ever holds the MVC session cookie (`.RaceDay.Web.Session`, HttpOnly). The API session cookie never leaves the server.

The typed `HttpClient` is registered through `AddHttpClient`. Microsoft notes that apps which require cookies should be careful with `IHttpClientFactory`, because pooled handlers share a cookie container (Microsoft, n.d.d). The primary handler is therefore created with `UseCookies = false`, and cookies are attached per request from the current user's session, so different users' API sessions can never be mixed up.

Logout calls `POST /api/auth/logout`, clears the MVC session and deletes its cookie. If the API session expires (the API answers 401 to a request that carried a cookie), `ApiSessionExpiredFilter` clears the MVC session and redirects to the login page with "Your session has expired" and a return URL.

### 2.2 Server-side authorisation in the MVC app

Hiding menu items is not treated as security. `RoleAuthorizeAttribute` implements `IAuthorizationFilter`. Authorisation filters run first in the filter pipeline and can short-circuit a request (Microsoft, n.d.e).

* `[RoleAuthorize]` on `ProfileController` means any logged-in user.
* `[RoleAuthorize(Roles.Organiser)]` on `OrganiserControllerBase` and `[RoleAuthorize(Roles.Participant)]` on `ParticipantControllerBase` protect whole areas.
* Anonymous users are redirected to `/Account/Login?returnUrl=…`; a user with the wrong role goes to `/Account/AccessDenied` (HTTP 403).
* Organiser pages also check event ownership in `LoadOwnedEventAsync` before showing an event's categories, entries or results, and the API checks ownership again on every write.
* Protected responses are sent with `Cache-Control: no-store`, so the browser's Back button does not show protected pages after logout.

Further hardening: anti-forgery validation is applied globally to every form POST (`AutoValidateAntiforgeryTokenAttribute`), and the login page only follows local return URLs (`Url.IsLocalUrl`) to prevent open redirects.

## 3. Organiser and Participant interfaces

| Requirement (brief, Part 3) | Where it is implemented |
|---|---|
| Organiser dashboard: events, entries per event, upcoming dates | `Areas/Organiser/Dashboard` — totals, pending entries, next-event countdown, entries-per-event bars (confirmed vs pending), completed events with publish progress |
| Event CRUD with name, description, date, location, distance, type | `Areas/Organiser/Events` — list, create, edit, delete (with a confirmation page; the API blocks deleting events that have entries) |
| Banner upload on create/edit | `Events/Form` → `UploadEventBannerAsync` → `POST /api/events/{id}/banner` |
| Category management | `Areas/Organiser/Categories` — add, edit, remove age or distance categories, with type-specific fields |
| Enrolments with category and status | `Areas/Organiser/Enrolments` — filter by status, confirm or set back to pending, "Confirm all pending" |
| Results entry and publishing | `Areas/Organiser/Results` — per-entrant finish time and position, draft/publish switch, "Publish drafts"; locked until race day |
| Participant home with filters | `Areas/Participant/Dashboard` — next-race spotlight, personal stats, search, type, location and date filters |
| Event details and categories, Enter Event | `Views/Events/Details` — facts, description, categories, and an entry panel that adapts to who is viewing |
| My events with status | `Areas/Participant/Enrolments` — upcoming and completed entries, colour-coded status, withdraw |
| My Results with time and "47th out of 312" | `Areas/Participant/Results` — position shown as a race-bib style "4th of 6 finishers", finish time, pace or speed, plus events awaiting results |
| Profile with picture upload | `Views/Profile` — edit details, upload picture (stored in Blob Storage through the API) |

## 4. Blob Storage (Azurite)

* `IBlobStorageService` / `AzureBlobStorageService` (API) use the Azure Blob Storage client library's `BlobServiceClient`, `BlobContainerClient` and `BlobClient` classes (Microsoft, n.d.f).
* Containers `event-banners` and `profile-pictures` are created on first use, so the system works with an empty Azurite instance.
* Uploads are validated on the API: size limit of 5 MB, allowed MIME types, and a check of the file's leading bytes ("magic numbers"), so a renamed text file cannot be stored as an image. Microsoft's file-upload guidance recommends validating size, type and signature and never trusting the client-supplied file name (Microsoft, n.d.g). Blobs get a GUID name.
* The resulting URL is saved in `Events.BannerImageUrl` or `Users.ProfilePictureUrl`; both columns already existed in the Part 1 ERD and SQL script, so no schema change was needed.
* **Docker networking:** inside Docker the API reaches Azurite at `http://azurite:10000`, which a browser cannot resolve. `BlobStorage:PublicBaseUrl` (set to `http://localhost:10000/devstoreaccount1` in `docker-compose.yml`) makes the stored URL browser-friendly while the SDK still talks to the container name.
* Replaced banners and pictures are deleted from storage. An event's banner is removed when the event is deleted.
* Storage outages return a 503 with a readable message instead of an unhandled exception.

The MVC app never references the Azure SDK. It streams the uploaded file to the API as `multipart/form-data`.

## 5. Docker

* Each application has a multi-stage Dockerfile: restore and publish with the .NET SDK image, then copy only the published output into the smaller ASP.NET Core runtime image. This follows Microsoft's guidance for containerising ASP.NET Core apps (Microsoft, n.d.h). Package restore is a separate layer so it is cached between builds, and the containers run as the image's non-root user.
* `docker-compose.yml` defines four services (SQL Server, Azurite, API, Web) on one network, with connection strings and URLs injected as environment variables (`ConnectionStrings__RaceDayConnection`, `BlobStorage__…`, `RaceDayApi__BaseUrl`). The Compose Specification is the recommended format for defining an application's services, networks and volumes (Docker Inc., n.d.).
* SQL Server can take a while to accept connections, so `DatabaseInitializer` retries database creation instead of crashing on its first attempt.
* Both apps use the same fixed culture (en-GB) and time zone (`TZ=Africa/Johannesburg`), so dates and numbers behave the same inside the container as on a developer's machine.

## 6. Changes to the Part 2 API (and why)

All Part 2 endpoints keep their routes and roles. The additions below were needed to deliver the Part 3 pages. They are also listed in `docs/API_ENDPOINT_PLAN.md` under "Part 3 additions".

| Change | Reason |
|---|---|
| `POST /api/events/{eventId}/banner`, `POST /api/profile/picture` | Required Blob Storage integration through the API |
| `GET /api/events/mine` | Organiser dashboard needs entries per event without N+1 calls |
| `PUT /api/enrolments/{id}/status` | Organisers manage enrolment status (Pending/Confirmed, matching the database check constraint) |
| `DELETE /api/enrolments/{id}` | Participants manage their own entries (withdraw before race day, if no result exists) |
| Optional filters on `GET /api/events` (`search`, `eventType`, `location`, `from`, `to`, `upcomingOnly`) | Participant home page filtering; with no parameters the endpoint behaves exactly as in Part 2 |
| Extra fields in existing responses (organiser name, category count, result details, `totalFinishers`) | "47th out of 312 finishers" and the results/enrolment pages |
| Create/update endpoints return flat objects instead of EF entities | **Bug fix.** The tracked entities had navigation back-references (for example `Enrolment → Event → Enrolments`), which System.Text.Json rejects as an object cycle, so these endpoints answered 500 even though the data was saved. Microsoft's EF Core documentation describes this cycle problem when serialising related data (Microsoft, n.d.i). A global `ReferenceHandler.IgnoreCycles` was also added as a safety net. |
| Swagger and HTTPS redirection controlled by configuration | Swagger can be enabled in Docker. The MVC app calls the API server-to-server over HTTP, so forced redirection is not wanted there. |
| Startup retry and optional demo seeding | Reliable Docker start-up; realistic demo data (section 7) |

## 7. Seed data and empty databases

`DemoDataSeeder` runs only when `SeedDemoData` is `true` **and** the `Users` table is empty, so it never touches real data. It creates 2 Organisers, 12 Participants, 7 South African events (past and upcoming, relative to the seeding date), 13 categories, 42 enrolments and 30 results (two left as drafts to demonstrate publishing). The application does not depend on it: with `SEED_DEMO_DATA=false` every page shows a helpful empty state ("Set up your first event", "No upcoming events yet", "No results yet"), and public endpoints return `[]` rather than errors.

Note: the Part 1 SQL script stores placeholder password hashes, so its sample users cannot log in. Register new accounts, or start from an empty database so the demo seeder can create working accounts.

## 8. User interface design

The visual identity is based on South African road racing: asphalt (`#1C252B`) for dark surfaces, a jacaranda violet (`#5B3FD0`) for actions, and road-marking yellow (`#FFCE3A`) used only as a dashed "lane line" motif. Barlow Condensed is used for headings, dates and timing numbers because it reads like a race bib; Barlow is used for body text. Both fonts are self-hosted, so the app looks identical offline and in Docker. The landing page's signature element is an animated elevation profile of a 21.1 km route; the animation is disabled for users who prefer reduced motion.

* Bootstrap 5.3 provides the grid and responsive behaviour (Bootstrap Team, n.d.). RaceDay styles are layered on top in `wwwroot/css/site.css` through CSS custom properties, so every page shares the same colours, spacing and components.
* Status colours are consistent everywhere: **green = Confirmed / Published**, **amber = Pending**, **grey = Draft / Completed**. Each badge also carries its text, so meaning never depends on colour alone.
* Each role has its own navigation: Participants get a light top bar; Organisers get a dark "race control" sidebar that collapses into a menu on small screens.
* Event types without an uploaded banner use original illustrations (coastal run, Table Mountain walk, Midlands ride) generated as SVG for this project.
* Feedback: success, info and error messages after every action; field-level validation (client and server); API messages are mapped onto the form; friendly 403, 404 and "service unavailable" pages instead of stack traces.

## 9. Testing

* **Unit tests (xUnit):** the Part 2 tests are unchanged and still run. `Part3ApiTests.cs` adds 22 tests for banner and profile uploads (success, wrong role, wrong owner, non-image, storage outage, replacement cleanup), the organiser summary, enrolment status changes, withdrawal rules, public filtering on an empty and populated database, `totalFinishers`, and the seeder's "only when empty" rule. `FakeBlobStorageService` replaces Azure so the tests need no emulator.
* **End-to-end checks (browser):** see `docs/Part3_Test_Plan.md` for the full checklist, covering RBAC redirects, CRUD, uploads, enrolment, results, session expiry, API outage and an empty database.
* **CI:** `.github/workflows/part3-build-test-docker.yml` builds the solution (API + MVC), runs all unit tests, builds both Docker images, validates `docker-compose.yml` and smoke-tests the MVC container.

## References

Bootstrap Team (n.d.) *Get started with Bootstrap* (v5.3). Available at: https://getbootstrap.com/docs/5.3/getting-started/introduction/ (Accessed: 20 November 2026).

Docker Inc. (n.d.) *Compose file reference*. Available at: https://docs.docker.com/reference/compose-file/ (Accessed: 20 November 2026).

Microsoft (n.d.a) *Use the Azurite emulator for local Azure Storage development*. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite (Accessed: 20 November 2026).

Microsoft (n.d.b) *Areas in ASP.NET Core*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/areas (Accessed: 20 November 2026).

Microsoft (n.d.c) *Session and state management in ASP.NET Core*. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state (Accessed: 20 November 2026).

Microsoft (n.d.d) *Use IHttpClientFactory to implement resilient HTTP requests*. Available at: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests (Accessed: 20 November 2026).

Microsoft (n.d.e) *Filters in ASP.NET Core*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters (Accessed: 20 November 2026).

Microsoft (n.d.f) *Quickstart: Azure Blob Storage client library for .NET*. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet (Accessed: 20 November 2026).

Microsoft (n.d.g) *Upload files in ASP.NET Core*. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads (Accessed: 20 November 2026).

Microsoft (n.d.h) *Run an ASP.NET Core app in Docker containers*. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images (Accessed: 20 November 2026).

Microsoft (n.d.i) *Related data and serialization*. Available at: https://learn.microsoft.com/en-us/ef/core/querying/related-data/serialization (Accessed: 20 November 2026).

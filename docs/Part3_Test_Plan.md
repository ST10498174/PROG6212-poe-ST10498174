# RaceDay Part 3 — Test Plan and Evidence

## 1. Unit tests (xUnit, `tests/RaceDay.Api.Tests`)

Run with `dotnet test RaceDay.sln`. GitHub Actions runs the same command on every push (`part3-build-test-docker.yml`).

The Part 2 tests (registration, login, event management, role rejection, enrolment, results) are unchanged. Part 3 adds `Part3ApiTests.cs`. It uses **Jarred** test users, in line with the assessment note, and an in-memory `FakeBlobStorageService` instead of Azurite.

| Area | Test | Expected |
|---|---|---|
| Blob upload | `UploadEventBanner_OwnerOrganiser_StoresBlobAndSavesUrl` | 200, blob stored in `event-banners`, URL saved on event |
| Blob upload | `UploadEventBanner_ReplacingBanner_DeletesPreviousBlob` | Old blob deleted after replacement |
| Blob upload / RBAC | `UploadEventBanner_Participant_ReturnsForbidden` | 403, nothing uploaded |
| Blob upload / ownership | `UploadEventBanner_DifferentOrganiser_ReturnsForbidden` | 403 |
| Blob upload / validation | `UploadEventBanner_NotAnImage_ReturnsBadRequest` | 400 (file signature check) |
| Blob upload / resilience | `UploadEventBanner_StorageUnavailable_Returns503` | 503, event unchanged |
| Profile picture | `UploadProfilePicture_LoggedInParticipant_SavesUrl` | 200, URL saved on user |
| Authentication | `UploadProfilePicture_NoSession_ReturnsUnauthorized` | 401 |
| Organiser dashboard | `GetMyEvents_Organiser_ReturnsOnlyOwnEventsWithCounts` | Only own events, correct counts |
| RBAC | `GetMyEvents_Participant_ReturnsForbidden` | 403 |
| Enrolment management | `UpdateEnrolmentStatus_OwnerOrganiser_ConfirmsEnrolment` | Status becomes Confirmed |
| Validation | `UpdateEnrolmentStatus_InvalidStatus_ReturnsBadRequest` | 400 |
| RBAC | `UpdateEnrolmentStatus_Participant_ReturnsForbidden` | 403, status unchanged |
| Participant enrolments | `WithdrawEnrolment_OwnUpcomingEnrolment_RemovesIt` | 204, enrolment removed |
| Ownership | `WithdrawEnrolment_SomeoneElsesEnrolment_ReturnsForbidden` | 403 |
| Business rule | `WithdrawEnrolment_PastEvent_ReturnsConflict` | 409 |
| Public endpoint, empty DB | `GetEvents_EmptyDatabase_ReturnsEmptyListWithoutLogin` | 200 with `[]` |
| Public endpoint, filters | `GetEvents_FilterByTypeAndUpcoming_ReturnsMatchingEventsOnly` | Only matching upcoming event |
| Public endpoint, invalid input | `GetEvents_InvalidEventType_ReturnsBadRequest` | 400 |
| Results | `GetMyResults_IncludesTotalFinishersForPosition` | Position 2 of 2 finishers |
| Seed data | `DemoDataSeeder_EmptyDatabase_SeedsBothRolesOnce` | Seeds once; second run adds nothing |
| Seed data | `DemoDataSeeder_UsersAlreadyExist_DoesNothing` | Existing data untouched |

## 2. End-to-end browser checks (MVC → API → Azurite)

These checks were run in a real browser (Chromium, automated with Playwright) against the MVC application, with Azurite storing the uploaded images. Repeat them manually for the video demonstration.

| # | Check | Result |
|---|---|---|
| 1 | anon -> organiser page redirects to login | Pass |
| 2 | anon -> participant page redirects to login | Pass |
| 3 | anon -> profile redirects to login | Pass |
| 4 | 404 page friendly | Pass |
| 5 | bad password shows API message | Pass |
| 6 | register: password mismatch caught client-side | Pass |
| 7 | register: duplicate email shows API conflict | Pass |
| 8 | register success -> participant dashboard | Pass |
| 9 | logout -> login page with message | Pass |
| 10 | after logout protected page redirects | Pass |
| 11 | organiser login -> organiser dashboard | Pass |
| 12 | organiser blocked from participant area | Pass |
| 13 | organiser blocked from another organiser's event | Pass |
| 14 | create event: required-field validation | Pass |
| 15 | create event -> categories step | Pass |
| 16 | banner URL points at Azurite blob | Pass |
| 17 | banner blob is publicly readable | Pass |
| 18 | banner renders in browser | Pass |
| 19 | category: max<min rejected | Pass |
| 20 | category: age category added | Pass |
| 21 | category: distance category added | Pass |
| 22 | category: duplicate name -> API conflict shown | Pass |
| 23 | category: edit saved | Pass |
| 24 | edit event: invalid distance rejected | Pass |
| 25 | edit event: non-image banner rejected by API | Pass |
| 26 | edit event: saved | Pass |
| 27 | participant login -> dashboard | Pass |
| 28 | participant blocked from organiser area | Pass |
| 29 | filter by type | Pass |
| 30 | filter with no matches -> empty state | Pass |
| 31 | enrol -> my events shows pending entry | Pass |
| 32 | event page now shows 'You're entered' | Pass |
| 33 | withdraw | Pass |
| 34 | re-enrol after withdraw | Pass |
| 35 | profile: invalid phone rejected | Pass |
| 36 | profile: saved | Pass |
| 37 | profile picture uploaded and shown in nav | Pass |
| 38 | organiser sees new pending entry | Pass |
| 39 | confirm entry | Pass |
| 40 | confirm all pending | Pass |
| 41 | delete blocked when entries exist | Pass |
| 42 | results locked before race day | Pass |
| 43 | result: malformed time blocked | Pass |
| 44 | result: update + publish | Pass |
| 45 | publish remaining drafts | Pass |
| 46 | delete event without entries | Pass |
| 47 | participant sees published result 'of N finishers' | Pass |
| 48 | expired API session -> login with message | Pass |
| 49 | API down: events page friendly error | Pass |
| 50 | API down: login shows friendly error | Pass |
| 51 | empty DB: home empty state | Pass |
| 52 | empty DB: events empty state | Pass |
| 53 | empty DB: organiser dashboard empty state | Pass |
| 54 | empty DB: organiser events empty | Pass |
| 55 | empty DB: /Participant/Dashboard | Pass |
| 56 | empty DB: /Participant/Enrolments | Pass |
| 57 | empty DB: /Participant/Results | Pass |
| 58 | no unexpected browser console errors | Pass |

## 3. Docker checks

1. `docker compose up --build` starts `sqlserver`, `azurite`, `api` and `web`.
2. Open http://localhost:8080. The landing page shows the demo events.
3. Log in as an Organiser, edit an event and upload a banner. The image URL starts with `http://localhost:10000/devstoreaccount1/event-banners/` and displays on the event page.
4. Open http://localhost:5080/swagger. All endpoints, including the Part 3 additions, are listed and can be tried.
5. `docker compose down -v` then `SEED_DEMO_DATA=false docker compose up --build` gives an empty database. Every page shows its empty state and nothing crashes.

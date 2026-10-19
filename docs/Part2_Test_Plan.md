# RaceDay Part 2 — Unit Test Plan

The Part 2 test project is located at `tests/RaceDay.Api.Tests` and uses xUnit with Entity Framework Core's in-memory provider. Test data uses **Jarred** as a sample user name in line with the assessment note regarding unit testing.

## Required areas covered

| Area | Test evidence |
|---|---|
| Registration success | Creates a Participant and confirms the stored password is hashed. |
| Registration failure | Duplicate email returns `409 Conflict`. |
| Login success | Correct credentials create a session containing UserId and Role. |
| Login failure | Wrong password returns `401 Unauthorized`. |
| Event management success | Organiser can create an event. |
| Authentication rejection | Unauthenticated event creation returns `401 Unauthorized`. |
| Role rejection | Participant attempting to create an event receives `403 Forbidden`. |
| Ownership rejection | A different Organiser cannot update another Organiser's event. |
| Category management | Event owner can create a valid category. |
| Event enrolment success | Participant can enrol in an event using a category from that event. |
| Enrolment duplicate failure | Duplicate event enrolment returns `409 Conflict`. |
| Enrolment role rejection | Organiser cannot use Participant enrolment functionality. |
| Result capture | Event owner can capture a result after the event date. |
| Participant result view | Participant result endpoint returns personal published result data. |

Run all tests with:

```powershell
dotnet test RaceDay.sln
```

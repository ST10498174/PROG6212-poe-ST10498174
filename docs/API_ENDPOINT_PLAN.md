# RaceDay API Endpoint Plan

## Purpose

This document plans the RESTful API that will be implemented in Part 2. No API code is implemented in Part 1.

### Role Key

- **None** — public endpoint.
- **Any** — any authenticated user.
- **Organiser** — authenticated Organiser only.
- **Participant** — authenticated Participant only.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new Organiser or Participant account. | None | `{ firstName, lastName, email, password, phoneNumber, dateOfBirth, role }` | `201 Created` with created user summary; `400 Bad Request` for invalid input; `409 Conflict` if email already exists. |
| POST | `/api/auth/login` | Authenticates a registered user and creates a session. | None | `{ email, password }` | `200 OK` with user/session summary; `400 Bad Request` for missing input; `401 Unauthorized` for invalid credentials. |
| POST | `/api/auth/logout` | Ends the logged-in user's session. | Any | None | `204 No Content`; `401 Unauthorized` if no valid session exists. |
| GET | `/api/profile` | Returns the logged-in user's own profile. | Any | None | `200 OK` with profile details; `401 Unauthorized` if not logged in. |
| PUT | `/api/profile` | Updates the logged-in user's own profile information. | Any | `{ firstName, lastName, email, phoneNumber, dateOfBirth }` | `200 OK` with updated profile; `400 Bad Request` for invalid input; `401 Unauthorized`; `409 Conflict` if email already exists. |
| GET | `/api/events` | Returns the available RaceDay events. | None | None | `200 OK` with a collection of events. |
| GET | `/api/events/{id}` | Returns the full details of one event. | None | None | `200 OK` with event details; `404 Not Found` if the event does not exist. |
| POST | `/api/events` | Creates a new event owned by the logged-in Organiser. | Organiser | `{ name, description, date, location, distanceKm, eventType }` | `201 Created` with the new event; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden` for the wrong role. |
| PUT | `/api/events/{id}` | Updates an event belonging to the logged-in Organiser. | Organiser | `{ name, description, date, location, distanceKm, eventType }` | `200 OK` with updated event; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`. |
| DELETE | `/api/events/{id}` | Deletes an event belonging to the logged-in Organiser when deletion is allowed. | Organiser | None | `204 No Content`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if related data prevents deletion. |
| GET | `/api/events/{eventId}/categories` | Returns all categories available for a specific event. | None | None | `200 OK` with category collection; `404 Not Found` if the event does not exist. |
| POST | `/api/events/{eventId}/categories` | Adds an age or distance category to an Organiser's event. | Organiser | `{ name, categoryType, minAge, maxAge, distanceKm }` | `201 Created`; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` for duplicate category. |
| PUT | `/api/categories/{id}` | Updates an existing event category. | Organiser | `{ name, categoryType, minAge, maxAge, distanceKm }` | `200 OK`; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict`. |
| DELETE | `/api/categories/{id}` | Removes a category when it has no dependent enrolments. | Organiser | None | `204 No Content`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` when dependent enrolments exist. |
| POST | `/api/enrolments` | Enrols the logged-in Participant in an event using a selected category. | Participant | `{ eventId, categoryId }` | `201 Created` with enrolment; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if already enrolled. |
| GET | `/api/enrolments/me` | Returns all enrolments belonging to the logged-in Participant. | Participant | None | `200 OK` with enrolment collection; `401 Unauthorized`; `403 Forbidden`. |
| GET | `/api/events/{eventId}/enrolments` | Returns all enrolments for an event owned by the logged-in Organiser. | Organiser | None | `200 OK` with enrolment collection; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`. |
| POST | `/api/results` | Captures a result for an enrolled Participant after an event. | Organiser | `{ enrolmentId, finishTime, finishingPosition, isPublished }` | `201 Created` with result; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`; `409 Conflict` if a result already exists. |
| PUT | `/api/results/{id}` | Updates an existing result captured for an Organiser's event. | Organiser | `{ finishTime, finishingPosition, isPublished }` | `200 OK` with updated result; `400 Bad Request`; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`. |
| GET | `/api/results/me` | Returns the logged-in Participant's published results. | Participant | None | `200 OK` with personal result history; `401 Unauthorized`; `403 Forbidden`. |
| GET | `/api/events/{eventId}/results` | Returns results captured for an Organiser's event. | Organiser | None | `200 OK` with result collection; `401 Unauthorized`; `403 Forbidden`; `404 Not Found`. |

## Planning Notes

- Event creation and management is restricted to Organisers.
- Event browsing and category viewing are public so prospective participants can view available events before logging in.
- Enrolment creation and personal enrolment views are restricted to Participants.
- Results are captured by Organisers and personal published results are viewed by Participants.
- The implementation in Part 2 should follow this plan closely. Any justified changes should be documented in the README.

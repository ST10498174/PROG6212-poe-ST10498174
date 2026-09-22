# PROG6212 Part 1 — RaceDay System Planning and Database Report

## 1\. Brief Analysis of the Part 1 Requirements

Part 1 is a planning and database-design submission. It does not require ASP.NET Core API or C# application code. The required work is an Entity Relationship Diagram (ERD), a complete API endpoint plan and a SQL Server database script, together with the required GitHub, CI/CD, README and presentation evidence.

The RaceDay system supports two user roles throughout the PoE:

* **Organiser** — manages events, categories, enrolments and results.
* **Participant** — browses events, enters events, views enrolments and tracks personal results.

The database design therefore needs to support users and roles, events, categories, participant enrolments and results.

## 2\. Summary of the Part 1 Rubric Criteria

|Criterion|Marks|Evidence Prepared|
|-|-:|-|
|Correct Submission|5|Clear repository structure, README, `/docs` folder, CI/CD screenshot placeholder and video-link placeholder.|
|ERD|25|Six entities with attributes, PKs, FKs and cardinality. The ERD matches the SQL design.|
|API Endpoint Plan|25|Complete endpoint plan using all six required columns, with success and failure response codes.|
|SQL Database Script|20|SQL Server schema with keys, constraints and realistic seed data for all entities.|
|GitHub and CI/CD|15|GitHub Actions structure-validation workflow prepared. Twenty or more meaningful commits must still be made by the student.|
|Video Presentation|10|README contains the required video section. The final own-voice video must still be recorded and linked.|

## 3\. Proposed Solution

The RaceDay data model uses six core entities:

1. `Roles`
2. `Users`
3. `Events`
4. `Categories`
5. `Enrolments`
6. `Results`

`Roles` and `Users` are separated so that the same user structure can support both Organisers and Participants. An Organiser creates Events. Each Event can have multiple Categories. A Participant enters an Event by creating an Enrolment and selecting a valid Category for that Event. After the event, a Result can be recorded against that Enrolment.

This design keeps related data in separate tables, avoids unnecessary duplication and provides clear relationships for later API implementation.

## 4\. ERD Explanation

The ERD is saved as `RaceDay\_ERD.png`.

### Relationships

* One Role can be assigned to many Users.
* One User acting as an Organiser can create many Events.
* One Event can have many Categories.
* One User acting as a Participant can have many Enrolments.
* One Event can have many Enrolments.
* One Category can be selected by many Enrolments.
* One Enrolment can have zero or one Result.

Primary and foreign keys are displayed directly in the ERD.

## 5\. API Endpoint Plan Explanation

The complete endpoint plan is saved as `API\_ENDPOINT\_PLAN.md`.

The plan covers all minimum resources required for the later API:

* Authentication
* User Profile
* Events
* Categories
* Event Enrolments
* Results

Every endpoint includes:

* HTTP Method
* Route
* Description
* Role Required
* Request Body
* Expected Response

Failure responses such as `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found` and `409 Conflict` are included where appropriate. This follows common REST API planning principles in which routes, HTTP methods and status codes communicate the intended operation and outcome \[4], while the later implementation will use ASP.NET Core Web API \[5].

## 6\. SQL Database Script Explanation

The database script is saved as `RaceDayDB.sql`.

The script:

* Creates the `RaceDayDB` database if it does not exist.
* Creates all six tables represented in the ERD.
* Defines primary keys and foreign keys.
* Defines `NOT NULL`, `UNIQUE`, `DEFAULT` and `CHECK` constraints where appropriate.
* Inserts two Organisers and two Participants.
* Inserts three Events.
* Inserts Categories for every Event.
* Inserts sample Enrolments.
* Inserts sample Results so every entity contains realistic seed data.
* Includes verification queries for checking the inserted data.

SQL Server supports primary keys, foreign keys, unique constraints, defaults and check constraints as mechanisms for enforcing data integrity \[1]–\[3].

## 7\. Testing and Expected Results

The final submission must be tested in SQL Server Management Studio before submission.

|Test|Action|Expected Result|
|-|-|-|
|Full script|Run `RaceDayDB.sql` on SQL Server|Script completes without errors.|
|Seed counts|Run final `COUNT(\*)` queries|Roles 2, Users 4, Events 3, Categories 7, Enrolments 5, Results 2.|
|Duplicate email|Attempt to insert an existing email|SQL rejects the record because Email is unique.|
|Invalid role|Attempt to add a role other than Organiser or Participant|SQL rejects the value.|
|Invalid event type|Attempt to insert an unsupported EventType|SQL rejects the value.|
|Negative distance|Insert an Event with a negative DistanceKm|SQL rejects the record.|
|Duplicate enrolment|Enrol the same Participant in the same Event twice|SQL rejects the second enrolment.|
|Wrong event/category pair|Use a Category belonging to a different Event|SQL rejects the enrolment.|
|Invalid finishing position|Insert finishing position 0 or negative|SQL rejects the Result.|
|Duplicate result|Add a second Result for the same Enrolment|SQL rejects the second Result.|
|CI/CD|Push repository to GitHub|Part 1 Repository Validation workflow passes with a green check.|

## 8\. Part 1 Requirement Checklist

* \[x] Minimum six entities included.
* \[x] Attributes identified for all entities.
* \[x] Primary keys identified.
* \[x] Foreign keys identified.
* \[x] Cardinality shown for every relationship.
* \[x] ERD and SQL schema designed to match.
* \[x] Authentication endpoints planned.
* \[x] User Profile endpoints planned.
* \[x] Event endpoints planned.
* \[x] Category endpoints planned.
* \[x] Enrolment endpoints planned.
* \[x] Result endpoints planned.
* \[x] All six endpoint-plan columns completed.
* \[x] Success and failure response codes included.
* \[x] SQL CREATE TABLE statements included.
* \[x] Primary and foreign keys included in SQL.
* \[x] NOT NULL constraints included where required.
* \[x] UNIQUE constraints included where required.
* \[x] DEFAULT constraints included where appropriate.
* \[x] CHECK constraints included for validation.
* \[x] Two Organisers seeded.
* \[x] Two Participants seeded.
* \[x] Three Events seeded.
* \[x] Categories seeded for every Event.
* \[x] Sample Enrolments seeded.
* \[x] Sample Results seeded.
* \[x] GitHub Actions workflow prepared.
* \[x] README prepared.
* \[x] AI-use disclosure included.

## 9\. Rubric Compliance Checklist

|Rubric Criterion|How It Is Addressed|
|-|-|
|Correct Submission|Repository structure, README and required `/docs` files are prepared. Final CI screenshot and YouTube link must be added before submission.|
|ERD|Complete six-entity ERD includes attributes, keys, relationship types and cardinality and is designed to match the SQL schema.|
|API Endpoint Plan|All required resources are covered and all six required columns are completed, including failure responses.|
|SQL Database Script|All entities are created with keys and constraints, with realistic seed data covering every table.|
|GitHub and CI/CD|Workflow validates repository structure. Student must complete 20+ genuine meaningful commits and capture the successful workflow screenshot.|
|Video Presentation|Student must explain ERD decisions, endpoint choices and SQL design and run the SQL script live in SSMS using their own voice.|

## 10\. AI Use Disclosure

Generative AI was used as a planning, proofreading and code-review aid while preparing this Part 1 submission. The final work must be reviewed, understood, tested and demonstrated by the student.

## 11\. References

\[1] Microsoft, “CREATE TABLE (Transact-SQL),” *Microsoft Learn*. \[Online]. Available: https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql?view=sql-server-ver17. \[Accessed: 22-Sep-2026].

\[2] Microsoft, “Primary and foreign key constraints,” *Microsoft Learn*. \[Online]. Available: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints?view=sql-server-ver17. \[Accessed: 22-Sep-2026].

\[3] Microsoft, “Unique constraints and check constraints,” *Microsoft Learn*. \[Online]. Available: https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints?view=sql-server-ver17. \[Accessed: 22-Sep-2026].

\[4] Microsoft, “API design,” *Azure Architecture Center*. \[Online]. Available: https://learn.microsoft.com/en-us/azure/architecture/microservices/design/api-design. \[Accessed: 22-Sep-2026].

\[5] Microsoft, “Create web APIs with ASP.NET Core,” *Microsoft Learn*. \[Online]. Available: https://learn.microsoft.com/en-us/aspnet/core/web-api/. \[Accessed: 22-Sep-2026].


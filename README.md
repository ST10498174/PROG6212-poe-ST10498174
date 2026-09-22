# RaceDay — PROG6212 Part 1

RaceDay is a planned web-based event management system for South African road running, walking and cycling events.

This repository currently contains **Part 1 only** of the PROG6212 Portfolio of Evidence. Part 1 focuses on system planning and database design.

## Part 1 Deliverables

The `/docs` folder contains:

* `RaceDay\\\\\\\_ERD.png` — Entity Relationship Diagram.
* `API\\\\\\\_ENDPOINT\\\\\\\_PLAN.md` — Complete API endpoint plan for the later Part 2 implementation.
* `RaceDayDB.sql` — SQL Server database creation and seed script.
* `Part1\\\\\\\_Report.md` — Part 1 analysis, rubric mapping, testing plan and references.

The repository also contains:

* `.github/workflows/part1-validation.yml` — GitHub Actions workflow used to validate the required Part 1 repository structure.

## User Roles

### Organiser

An Organiser will be able to:

* Create, update and delete events.
* Create age or distance categories for events.
* View enrolments for their events.
* Capture and publish participant results.

### Participant

A Participant will be able to:

* Register and log in.
* Browse events and available categories.
* Enter an event by selecting a category.
* View their own enrolments.
* View their own published results.
* View and update their profile.

## Database Setup in SQL Server Management Studio

1. Open SQL Server Management Studio (SSMS).
2. Connect to a SQL Server instance.
3. Open `docs/RaceDayDB.sql`.
4. Run the entire script.
5. Confirm that no SQL errors are returned.
6. Check the verification query results at the end of the script.

Expected row counts after a successful run:

|Table|Expected Rows|
|-|-:|
|Roles|2|
|Users|4|
|Events|3|
|Categories|7|
|Enrolments|5|
|Results|2|

## GitHub Actions / CI-CD

The workflow in `.github/workflows/part1-validation.yml` checks that the required Part 1 files exist in the correct repository locations.

After the workflow passes on GitHub, save a screenshot of the successful green build as:

`docs/ci-green-build.png`

## AI Use Disclosure

Generative AI was used as a planning, proofreading and code-review aid while preparing Part 1. The final database design, endpoint plan, documentation and testing evidence must be reviewed, understood and verified by the student before submission.

## References

The complete IEEE-style reference list is included in `docs/Part1\\\\\\\_Report.md`.


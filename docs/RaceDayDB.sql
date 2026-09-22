/* ============================================================
   PROG6212 - Programming 2B
   Part 1 - RaceDay Database Script

   Purpose:
   Creates and populates the RaceDay SQL Server database.

   IMPORTANT:
   This is Part 1 database work only.
   No C# API code is included in this script.
   ============================================================ */

/* ------------------------------------------------------------
   CREATE THE DATABASE
   ------------------------------------------------------------ */

-- Check whether the RaceDay database already exists.
IF DB_ID(N'RaceDayDB') IS NULL
BEGIN
    -- Create the database if it does not already exist.
    CREATE DATABASE RaceDayDB;
END;
GO

-- Select RaceDayDB so all following tables are created
-- inside the correct database.
USE RaceDayDB;
GO

/* ------------------------------------------------------------
   REMOVE EXISTING TABLES
   ------------------------------------------------------------
   Tables are removed in reverse relationship order.
   This allows the script to be run again during testing.
   ------------------------------------------------------------ */

DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Roles;
GO

/* ============================================================
   TABLE 1: ROLES
   ============================================================ */

-- Stores the two roles supported by RaceDay.
CREATE TABLE dbo.Roles
(
    -- Unique number used to identify each role.
    RoleId INT IDENTITY(1,1) NOT NULL,

    -- Stores either Organiser or Participant.
    RoleName NVARCHAR(20) NOT NULL,

    -- Defines RoleId as the primary key.
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),

    -- Prevents two roles from having the same name.
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName),

    -- Prevents invalid role names from being stored.
    CONSTRAINT CK_Roles_RoleName
        CHECK (RoleName IN (N'Organiser', N'Participant'))
);
GO

/* ============================================================
   TABLE 2: USERS
   ============================================================ */

-- Stores personal and login information for both Organisers
-- and Participants.
CREATE TABLE dbo.Users
(
    -- Unique identifier for each user.
    UserId INT IDENTITY(1,1) NOT NULL,

    -- Links the user to either Organiser or Participant.
    RoleId INT NOT NULL,

    -- User's first name.
    FirstName NVARCHAR(50) NOT NULL,

    -- User's surname.
    LastName NVARCHAR(50) NOT NULL,

    -- Email address used by the user when logging in.
    Email NVARCHAR(150) NOT NULL,

    -- Stores a password hash rather than the original password.
    PasswordHash NVARCHAR(255) NOT NULL,

    -- User's contact number.
    PhoneNumber NVARCHAR(20) NOT NULL,

    -- Date of birth can later be used when checking age categories.
    DateOfBirth DATE NOT NULL,

    -- Optional profile image URL for later parts of the PoE.
    ProfilePictureUrl NVARCHAR(500) NULL,

    -- Records when the user account was created.
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),

    -- Defines the primary key.
    CONSTRAINT PK_Users PRIMARY KEY (UserId),

    -- Email addresses must be unique.
    CONSTRAINT UQ_Users_Email UNIQUE (Email),

    -- Ensures every user refers to a valid role.
    CONSTRAINT FK_Users_Roles
        FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);
GO

/* ============================================================
   TABLE 3: EVENTS
   ============================================================ */

-- Stores road running, walking and cycling events.
CREATE TABLE dbo.Events
(
    -- Unique identifier for an event.
    EventId INT IDENTITY(1,1) NOT NULL,

    -- Identifies the Organiser who created the event.
    OrganiserUserId INT NOT NULL,

    -- Event name displayed to Participants.
    Name NVARCHAR(120) NOT NULL,

    -- Longer description explaining the event.
    Description NVARCHAR(1000) NOT NULL,

    -- Date on which the event takes place.
    EventDate DATE NOT NULL,

    -- Physical location of the event.
    Location NVARCHAR(200) NOT NULL,

    -- Main distance of the event in kilometres.
    DistanceKm DECIMAL(6,2) NOT NULL,

    -- Event must be a run, walk or cycle.
    EventType NVARCHAR(10) NOT NULL,

    -- Optional event banner URL for later parts of the PoE.
    BannerImageUrl NVARCHAR(500) NULL,

    -- Date and time that the event record was created.
    CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT SYSUTCDATETIME(),

    -- Defines the primary key.
    CONSTRAINT PK_Events PRIMARY KEY (EventId),

    -- Links the event to the user who organised it.
    CONSTRAINT FK_Events_Users
        FOREIGN KEY (OrganiserUserId) REFERENCES dbo.Users(UserId),

    -- Distance must be greater than zero.
    CONSTRAINT CK_Events_Distance CHECK (DistanceKm > 0),

    -- Only the three event types required by the assignment are allowed.
    CONSTRAINT CK_Events_EventType
        CHECK (EventType IN (N'Run', N'Walk', N'Cycle'))
);
GO

/* ============================================================
   TABLE 4: CATEGORIES
   ============================================================ */

-- Stores age or distance categories belonging to each event.
CREATE TABLE dbo.Categories
(
    -- Unique identifier for each category.
    CategoryId INT IDENTITY(1,1) NOT NULL,

    -- Identifies the event that owns the category.
    EventId INT NOT NULL,

    -- User-friendly category name.
    Name NVARCHAR(100) NOT NULL,

    -- Identifies whether the category is based on age or distance.
    CategoryType NVARCHAR(20) NOT NULL,

    -- Minimum age is used only for age categories.
    MinAge SMALLINT NULL,

    -- Maximum age is used only for age categories.
    MaxAge SMALLINT NULL,

    -- Distance is used only for distance categories.
    DistanceKm DECIMAL(6,2) NULL,

    -- Defines the primary key.
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),

    -- Links the category to its event.
    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),

    -- Prevents duplicate category names inside the same event.
    CONSTRAINT UQ_Categories_Event_Name UNIQUE (EventId, Name),

    -- Allows Enrolments to confirm that a category belongs to the event.
    CONSTRAINT UQ_Categories_Event_Category UNIQUE (EventId, CategoryId),

    -- Category type must be Age or Distance.
    CONSTRAINT CK_Categories_CategoryType
        CHECK (CategoryType IN (N'Age', N'Distance')),

    -- Ensures the correct fields are supplied for the category type.
    CONSTRAINT CK_Categories_Values
        CHECK
        (
            (
                CategoryType = N'Age'
                AND MinAge IS NOT NULL
                AND MaxAge IS NOT NULL
                AND MinAge >= 0
                AND MaxAge >= MinAge
                AND DistanceKm IS NULL
            )
            OR
            (
                CategoryType = N'Distance'
                AND DistanceKm IS NOT NULL
                AND DistanceKm > 0
                AND MinAge IS NULL
                AND MaxAge IS NULL
            )
        )
);
GO

/* ============================================================
   TABLE 5: ENROLMENTS
   ============================================================ */

-- Records a Participant entering an event and selecting a category.
CREATE TABLE dbo.Enrolments
(
    -- Unique identifier for the enrolment.
    EnrolmentId INT IDENTITY(1,1) NOT NULL,

    -- Identifies the Participant entering the event.
    ParticipantUserId INT NOT NULL,

    -- Identifies the event being entered.
    EventId INT NOT NULL,

    -- Identifies the selected category.
    CategoryId INT NOT NULL,

    -- Stores when the enrolment was created.
    EnrolmentDate DATETIME2(0) NOT NULL
        CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT SYSUTCDATETIME(),

    -- Shows the current enrolment status.
    Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Enrolments_Status DEFAULT N'Pending',

    -- Defines the primary key.
    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),

    -- Links the enrolment to a user.
    CONSTRAINT FK_Enrolments_Users
        FOREIGN KEY (ParticipantUserId) REFERENCES dbo.Users(UserId),

    -- Links the enrolment directly to its event.
    CONSTRAINT FK_Enrolments_Events
        FOREIGN KEY (EventId) REFERENCES dbo.Events(EventId),

    -- Confirms that the selected category belongs to the selected event.
    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (EventId, CategoryId)
        REFERENCES dbo.Categories(EventId, CategoryId),

    -- Prevents the same Participant entering an event twice.
    CONSTRAINT UQ_Enrolments_Participant_Event
        UNIQUE (ParticipantUserId, EventId),

    -- Restricts the permitted enrolment statuses.
    CONSTRAINT CK_Enrolments_Status
        CHECK (Status IN (N'Pending', N'Confirmed'))
);
GO

/* ============================================================
   TABLE 6: RESULTS
   ============================================================ */

-- Stores the final result recorded for an event enrolment.
CREATE TABLE dbo.Results
(
    -- Unique identifier for the result.
    ResultId INT IDENTITY(1,1) NOT NULL,

    -- Links the result to the Participant's enrolment.
    EnrolmentId INT NOT NULL,

    -- Stores how long the Participant took to finish.
    FinishTime TIME(0) NOT NULL,

    -- Stores the Participant's final finishing position.
    FinishingPosition INT NOT NULL,

    -- Allows a result to be captured before being made visible.
    IsPublished BIT NOT NULL
        CONSTRAINT DF_Results_IsPublished DEFAULT 0,

    -- Records when the result was captured.
    RecordedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_Results_RecordedAt DEFAULT SYSUTCDATETIME(),

    -- Defines the primary key.
    CONSTRAINT PK_Results PRIMARY KEY (ResultId),

    -- One enrolment may only have one final result.
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),

    -- Links the result to an existing enrolment.
    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentId) REFERENCES dbo.Enrolments(EnrolmentId),

    -- A finishing position cannot be zero or negative.
    CONSTRAINT CK_Results_FinishingPosition
        CHECK (FinishingPosition > 0)
);
GO

/* ============================================================
   SEED DATA
   ============================================================ */

-- Add the two RaceDay roles.
INSERT INTO dbo.Roles (RoleName)
VALUES
    (N'Organiser'),
    (N'Participant');
GO

-- Add two Organisers and two Participants as required.
-- The PasswordHash values are non-password seed placeholders.
-- Part 2 must generate secure password hashes when users register.
INSERT INTO dbo.Users
(
    RoleId,
    FirstName,
    LastName,
    Email,
    PasswordHash,
    PhoneNumber,
    DateOfBirth
)
VALUES
    (1, N'Naledi', N'Mokoena', N'naledi.organiser@example.com', N'SEED_HASH_ORGANISER_01', N'0825550101', '1990-05-14'),
    (1, N'Jason', N'Naidoo', N'jason.organiser@example.com', N'SEED_HASH_ORGANISER_02', N'0825550102', '1987-11-03'),
    (2, N'Lesedi', N'Khumalo', N'lesedi.participant@example.com', N'SEED_HASH_PARTICIPANT_01', N'0825550201', '2001-02-18'),
    (2, N'Michael', N'Pillay', N'michael.participant@example.com', N'SEED_HASH_PARTICIPANT_02', N'0825550202', '1983-09-26');
GO

-- Add three realistic South African road events.
INSERT INTO dbo.Events
(
    OrganiserUserId,
    Name,
    Description,
    EventDate,
    Location,
    DistanceKm,
    EventType
)
VALUES
    (1, N'Durban Heritage 10K', N'A community road race along the Durban beachfront.', '2026-08-16', N'Durban, KwaZulu-Natal', 10.00, N'Run'),
    (2, N'Johannesburg Charity Walk', N'A charity walking event supporting local community projects.', '2026-10-18', N'Johannesburg, Gauteng', 10.00, N'Walk'),
    (1, N'Cape Peninsula Cycle Challenge', N'A road cycling event with short and long-distance categories.', '2026-11-21', N'Cape Town, Western Cape', 50.00, N'Cycle');
GO

-- Add categories for Event 1: Durban Heritage 10K.
INSERT INTO dbo.Categories
    (EventId, Name, CategoryType, MinAge, MaxAge, DistanceKm)
VALUES
    (1, N'Under 20', N'Age', 0, 19, NULL),
    (1, N'Open 20-39', N'Age', 20, 39, NULL),
    (1, N'Senior 40+', N'Age', 40, 120, NULL);

-- Add categories for Event 2: Johannesburg Charity Walk.
INSERT INTO dbo.Categories
    (EventId, Name, CategoryType, MinAge, MaxAge, DistanceKm)
VALUES
    (2, N'5km Fun Walk', N'Distance', NULL, NULL, 5.00),
    (2, N'10km Challenge Walk', N'Distance', NULL, NULL, 10.00);

-- Add categories for Event 3: Cape Peninsula Cycle Challenge.
INSERT INTO dbo.Categories
    (EventId, Name, CategoryType, MinAge, MaxAge, DistanceKm)
VALUES
    (3, N'25km Cycle', N'Distance', NULL, NULL, 25.00),
    (3, N'50km Cycle', N'Distance', NULL, NULL, 50.00);
GO

-- Add realistic sample enrolments.
INSERT INTO dbo.Enrolments
(
    ParticipantUserId,
    EventId,
    CategoryId,
    Status
)
VALUES
    (3, 1, 2, N'Confirmed'),
    (4, 1, 3, N'Confirmed'),
    (3, 2, 4, N'Pending'),
    (4, 2, 5, N'Confirmed'),
    (3, 3, 7, N'Confirmed');
GO

-- Event 1 has already taken place, so sample results are included.
INSERT INTO dbo.Results
(
    EnrolmentId,
    FinishTime,
    FinishingPosition,
    IsPublished
)
VALUES
    (1, '00:48:32', 18, 1),
    (2, '01:02:15', 47, 1);
GO

/* ============================================================
   VERIFICATION QUERIES
   ============================================================ */

-- These queries confirm that the seed data was inserted.
SELECT COUNT(*) AS RoleCount FROM dbo.Roles;
SELECT COUNT(*) AS UserCount FROM dbo.Users;
SELECT COUNT(*) AS EventCount FROM dbo.Events;
SELECT COUNT(*) AS CategoryCount FROM dbo.Categories;
SELECT COUNT(*) AS EnrolmentCount FROM dbo.Enrolments;
SELECT COUNT(*) AS ResultCount FROM dbo.Results;
GO

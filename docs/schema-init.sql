USE [master];
GO

IF DB_ID(N'RaceDay') IS NOT NULL
BEGIN
    ALTER DATABASE [RaceDay] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [RaceDay];
END;
GO

CREATE DATABASE [RaceDay];
GO

USE [RaceDay];
GO

-- Step 2: Drop existing tables in reverse dependency order (children before parents)
-- to avoid foreign key constraint violations if tables already exist.
IF OBJECT_ID(N'dbo.Result', N'U')      IS NOT NULL DROP TABLE [dbo].[Result];
IF OBJECT_ID(N'dbo.Enrolment', N'U')   IS NOT NULL DROP TABLE [dbo].[Enrolment];
IF OBJECT_ID(N'dbo.Category', N'U')    IS NOT NULL DROP TABLE [dbo].[Category];
IF OBJECT_ID(N'dbo.Event', N'U')       IS NOT NULL DROP TABLE [dbo].[Event];
IF OBJECT_ID(N'dbo.Venue', N'U')       IS NOT NULL DROP TABLE [dbo].[Venue];
IF OBJECT_ID(N'dbo.Participant', N'U') IS NOT NULL DROP TABLE [dbo].[Participant];
IF OBJECT_ID(N'dbo.Organiser', N'U')   IS NOT NULL DROP TABLE [dbo].[Organiser];
GO

-- Step 3: Create Tables (parents before children)

-- Table 1: Organiser (Role 1: Admins/organisations that create and manage events)
CREATE TABLE [dbo].[Organiser]
(
    [OrganiserID]      INT            IDENTITY(1,1) NOT NULL,
    [OrganisationName] NVARCHAR(150)  NOT NULL,
    [FullName]         NVARCHAR(100)  NOT NULL,
    [Email]            NVARCHAR(150)  NOT NULL,
    [Phone]            NVARCHAR(20)   NULL,
    [PasswordHash]     NVARCHAR(255)  NOT NULL,
    [CreatedAt]        DATETIME2(0)   NOT NULL
        CONSTRAINT [DF_Organiser_CreatedAt] DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_Organiser]       PRIMARY KEY ([OrganiserID]),
    CONSTRAINT [UQ_Organiser_Email] UNIQUE ([Email])
);

-- Table 2: Participant (Role 2: Athletes entering events and viewing results)
CREATE TABLE [dbo].[Participant]
(
    [ParticipantID]         INT            IDENTITY(1,1) NOT NULL,
    [FullName]              NVARCHAR(100)  NOT NULL,
    [Email]                 NVARCHAR(150)  NOT NULL,
    [Phone]                 NVARCHAR(20)   NULL,
    [DateOfBirth]           DATE           NULL,
    [EmergencyContactName]  NVARCHAR(100)  NULL,
    [EmergencyContactPhone] NVARCHAR(20)   NULL,
    [PasswordHash]          NVARCHAR(255)  NOT NULL,
    [CreatedAt]             DATETIME2(0)   NOT NULL
        CONSTRAINT [DF_Participant_CreatedAt] DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_Participant]       PRIMARY KEY ([ParticipantID]),
    CONSTRAINT [UQ_Participant_Email] UNIQUE ([Email])
);

-- Table 3: Venue (Physical staging grounds and terrain/route descriptions)
CREATE TABLE [dbo].[Venue]
(
    [VenueID]          INT            IDENTITY(1,1) NOT NULL,
    [VenueName]        NVARCHAR(150)  NOT NULL,
    [Address]          NVARCHAR(200)  NULL,
    [City]             NVARCHAR(100)  NOT NULL,
    [Province]         NVARCHAR(50)   NOT NULL,
    [PostalCode]       NVARCHAR(10)   NULL,
    [RouteDescription] NVARCHAR(MAX)  NULL,
    CONSTRAINT [PK_Venue] PRIMARY KEY ([VenueID])
);

-- Table 4: Event (Parent entity linking Organisers and Venues)
CREATE TABLE [dbo].[Event]
(
    [EventID]     INT            IDENTITY(1,1) NOT NULL,
    [EventName]   NVARCHAR(150)  NOT NULL,
    [EventDate]   DATE           NOT NULL,
    [StartTime]   TIME(0)        NULL,
    [Description] NVARCHAR(MAX)  NULL,
    [Status]      NVARCHAR(20)   NOT NULL
        CONSTRAINT [DF_Event_Status] DEFAULT (N'Open'),
    [OrganiserID] INT            NOT NULL,
    [VenueID]     INT            NOT NULL,
    [CreatedAt]   DATETIME2(0)   NOT NULL
        CONSTRAINT [DF_Event_CreatedAt] DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_Event]          PRIMARY KEY ([EventID]),
    CONSTRAINT [FK_Event_Organiser] FOREIGN KEY ([OrganiserID])
        REFERENCES [dbo].[Organiser] ([OrganiserID]),
    CONSTRAINT [FK_Event_Venue]    FOREIGN KEY ([VenueID])
        REFERENCES [dbo].[Venue] ([VenueID]),
    CONSTRAINT [CK_Event_Status]   CHECK ([Status] IN (N'Open', N'Closed', N'Full', N'Cancelled'))
);

-- Table 5: Category (Sub-divisions and entry tiers for each event)
CREATE TABLE [dbo].[Category]
(
    [CategoryID]      INT            IDENTITY(1,1) NOT NULL,
    [EventID]         INT            NOT NULL,
    [CategoryName]    NVARCHAR(50)   NOT NULL,
    [DistanceKm]      DECIMAL(5,1)   NULL,
    [EntryFee]        DECIMAL(10,2)  NOT NULL
        CONSTRAINT [DF_Category_EntryFee] DEFAULT (0),
    [MaxParticipants] INT            NOT NULL,
    CONSTRAINT [PK_Category]          PRIMARY KEY ([CategoryID]),
    CONSTRAINT [FK_Category_Event]    FOREIGN KEY ([EventID])
        REFERENCES [dbo].[Event] ([EventID]),
    CONSTRAINT [UQ_Category_EventName] UNIQUE ([EventID], [CategoryName]),
    CONSTRAINT [CK_Category_Fee]      CHECK ([EntryFee] >= 0),
    CONSTRAINT [CK_Category_Max]      CHECK ([MaxParticipants] > 0)
);

-- Table 6: Enrolment (Junction resolving M:N relationship between Participant and Category)
CREATE TABLE [dbo].[Enrolment]
(
    [EnrolmentID]   INT            IDENTITY(1,1) NOT NULL,
    [ParticipantID] INT            NOT NULL,
    [CategoryID]    INT            NOT NULL,
    [EnrolmentDate] DATETIME2(0)   NOT NULL
        CONSTRAINT [DF_Enrolment_Date] DEFAULT (SYSDATETIME()),
    [BibNumber]     NVARCHAR(20)   NOT NULL,
    [Status]        NVARCHAR(20)   NOT NULL
        CONSTRAINT [DF_Enrolment_Status] DEFAULT (N'Confirmed'),
    CONSTRAINT [PK_Enrolment]             PRIMARY KEY ([EnrolmentID]),
    CONSTRAINT [FK_Enrolment_Participant] FOREIGN KEY ([ParticipantID])
        REFERENCES [dbo].[Participant] ([ParticipantID]),
    CONSTRAINT [FK_Enrolment_Category]    FOREIGN KEY ([CategoryID])
        REFERENCES [dbo].[Category] ([CategoryID]),
    CONSTRAINT [UQ_Enrolment_Bib]         UNIQUE ([BibNumber]),
    CONSTRAINT [UQ_Enrolment_PartCat]     UNIQUE ([ParticipantID], [CategoryID]),
    CONSTRAINT [CK_Enrolment_Status]      CHECK ([Status] IN (N'Confirmed', N'Pending', N'Withdrawn'))
);

-- Table 7: Result (1:1 with Enrolment, enforced by UNIQUE constraint on EnrolmentID)
CREATE TABLE [dbo].[Result]
(
    [ResultID]    INT           IDENTITY(1,1) NOT NULL,
    [EnrolmentID] INT           NOT NULL,
    [FinishTime]  TIME(0)       NULL,
    [Position]    INT           NULL,
    [Status]      NVARCHAR(20)  NOT NULL
        CONSTRAINT [DF_Result_Status] DEFAULT (N'Completed'),
    [CreatedAt]   DATETIME2(0)  NOT NULL
        CONSTRAINT [DF_Result_CreatedAt] DEFAULT (SYSDATETIME()),
    CONSTRAINT [PK_Result]          PRIMARY KEY ([ResultID]),
    CONSTRAINT [FK_Result_Enrolment] FOREIGN KEY ([EnrolmentID])
        REFERENCES [dbo].[Enrolment] ([EnrolmentID]),
    CONSTRAINT [UQ_Result_Enrolment] UNIQUE ([EnrolmentID]),
    CONSTRAINT [CK_Result_Status]   CHECK ([Status] IN (N'Completed', N'DNF', N'DNS', N'Disqualified')),
    CONSTRAINT [CK_Result_Position] CHECK ([Position] IS NULL OR [Position] > 0)
);
GO

-- Step 4: Seed Realistic Initial Data

-- 4.1 Organisers (Meets minimum 2 requirement)
SET IDENTITY_INSERT [dbo].[Organiser] ON;
INSERT INTO [dbo].[Organiser]
    ([OrganiserID], [OrganisationName], [FullName], [Email], [Phone], [PasswordHash])
VALUES
    (1, N'RunSA Event Management',  N'Thabo Mokoena',      N'thabo@runsa.co.za',        N'+27 82 555 0142', N'AQAAAAEAACcQAAAAEExampleHashOrganiser1!'),
    (2, N'Cape Cycling Promotions', N'Sarah van der Merwe', N'sarah@capecycling.co.za', N'+27 72 555 0198', N'AQAAAAEAACcQAAAAEExampleHashOrganiser2!');
SET IDENTITY_INSERT [dbo].[Organiser] OFF;

-- 4.2 Participants (Meets minimum 2 requirement)
SET IDENTITY_INSERT [dbo].[Participant] ON;
INSERT INTO [dbo].[Participant]
    ([ParticipantID], [FullName], [Email], [Phone], [DateOfBirth],
     [EmergencyContactName], [EmergencyContactPhone], [PasswordHash])
VALUES
    (1, N'Nomsa Dlamini', N'nomsa.dlamini@gmail.com', N'+27 83 555 0231', N'1992-04-11',
     N'Sipho Dlamini',   N'+27 83 555 0319', N'AQAAAAEAACcQAAAAEExampleHashParticipant1!'),
    (2, N'Pieter Botha',  N'pieter.botha@gmail.com',  N'+27 84 555 0667', N'1985-09-30',
     N'Anna Botha',       N'+27 84 555 0701', N'AQAAAAEAACcQAAAAEExampleHashParticipant2!');
SET IDENTITY_INSERT [dbo].[Participant] OFF;

-- 4.3 Venues (3 provinces represented)
SET IDENTITY_INSERT [dbo].[Venue] ON;
INSERT INTO [dbo].[Venue]
    ([VenueID], [VenueName], [Address], [City], [Province], [PostalCode], [RouteDescription])
VALUES
    (1, N'FNB Stadium Precinct', N'Nasrec Road',     N'Johannesburg',    N'Gauteng',       N'2001',
     N'Flat, fast city loop with one climb at 14km. Road closure supported by JMPD.'),
    (2, N'Sea Point Promenade',  N'Beach Road',      N'Cape Town',       N'Western Cape',  N'8005',
     N'Coastal route with strong south-easter winds after 09:00.'),
    (3, N'uMngeni River Park',   N'Alexandra Road',  N'Pietermaritzburg', N'KwaZulu-Natal', N'3201',
     N'Gravel trails along the river; sections can be muddy after rain.');
SET IDENTITY_INSERT [dbo].[Venue] OFF;

-- 4.4 Events (Meets minimum 3 requirement: run, cycle, walk)
SET IDENTITY_INSERT [dbo].[Event] ON;
INSERT INTO [dbo].[Event]
    ([EventID], [EventName], [EventDate], [StartTime], [Description], [Status], [OrganiserID], [VenueID])
VALUES
    (1, N'Soweto Marathon 2026',              N'2026-06-14', N'06:30:00',
     N'Flagship Gauteng road marathon through Soweto landmarks.', N'Closed', 1, 1),
    (2, N'Cape Town Cycle Tour 2027',         N'2027-03-07', N'05:45:00',
     N'World-famous 109km closed-road cycle tour around the peninsula.', N'Open', 2, 2),
    (3, N'uMngeni Community Walk & Trail Run', N'2026-10-18', N'07:00:00',
     N'Family charity walk and trail run along the uMngeni River.', N'Open', 1, 3);
SET IDENTITY_INSERT [dbo].[Event] OFF;

-- 4.5 Categories (Assigned to each event)
SET IDENTITY_INSERT [dbo].[Category] ON;
INSERT INTO [dbo].[Category]
    ([CategoryID], [EventID], [CategoryName], [DistanceKm], [EntryFee], [MaxParticipants])
VALUES
    (1, 1, N'10km Fun Run',          10.0, 180.00, 8000),
    (2, 1, N'21.1km Half Marathon',  21.1, 320.00, 12000),
    (3, 1, N'42.2km Marathon',       42.2, 450.00, 8000),
    (4, 2, N'109km Open Cycling',    109.0, 850.00, 20000),
    (5, 2, N'42km Community Ride',   42.0, 350.00, 15000),
    (6, 3, N'5km Community Walk',    5.0,  50.00, 3000),
    (7, 3, N'10km Trail Run',        10.0, 120.00, 1500);
SET IDENTITY_INSERT [dbo].[Category] OFF;

-- 4.6 Enrolments (Sample registrations across events)
SET IDENTITY_INSERT [dbo].[Enrolment] ON;
INSERT INTO [dbo].[Enrolment]
    ([EnrolmentID], [ParticipantID], [CategoryID], [EnrolmentDate], [BibNumber], [Status])
VALUES
    (1, 1, 3, N'2026-03-02 08:00:00', N'SM26-0342', N'Confirmed'),
    (2, 2, 2, N'2026-03-05 09:15:00', N'SM26-1190', N'Confirmed'),
    (3, 2, 4, N'2026-08-15 14:00:00', N'CT27-0218', N'Confirmed'),
    (4, 1, 6, N'2026-09-01 10:30:00', N'UM26-0087', N'Confirmed'),
    (5, 2, 7, N'2026-08-20 16:45:00', N'UM26-0112', N'Withdrawn');
SET IDENTITY_INSERT [dbo].[Enrolment] OFF;

-- 4.7 Results (Recorded for completed events)
SET IDENTITY_INSERT [dbo].[Result] ON;
INSERT INTO [dbo].[Result]
    ([ResultID], [EnrolmentID], [FinishTime], [Position], [Status])
VALUES
    (1, 1, N'04:12:33', 128, N'Completed'),
    (2, 2, N'02:05:47', 517, N'Completed');
SET IDENTITY_INSERT [dbo].[Result] OFF;
GO

-- Step 5: Verification Queries to demonstrate successful execution in SSMS

-- Table row counts
SELECT N'Organiser'   AS [Table], COUNT(*) AS [Rows] FROM [dbo].[Organiser]
UNION ALL SELECT N'Participant', COUNT(*) FROM [dbo].[Participant]
UNION ALL SELECT N'Venue',       COUNT(*) FROM [dbo].[Venue]
UNION ALL SELECT N'Event',       COUNT(*) FROM [dbo].[Event]
UNION ALL SELECT N'Category',    COUNT(*) FROM [dbo].[Category]
UNION ALL SELECT N'Enrolment',   COUNT(*) FROM [dbo].[Enrolment]
UNION ALL SELECT N'Result',      COUNT(*) FROM [dbo].[Result];

-- Relational query showing participants, assigned events, bib numbers, and race outcomes
SELECT 
    p.FullName AS Participant,
    ev.EventName,
    c.CategoryName,
    e.BibNumber,
    e.Status AS EnrolmentStatus,
    ISNULL(CONVERT(VARCHAR(8), r.FinishTime), 'Pending') AS FinishTime,
    ISNULL(CONVERT(VARCHAR(10), r.Position),  'Pending') AS Position
FROM [dbo].[Enrolment] e
JOIN [dbo].[Participant] p ON p.ParticipantID = e.ParticipantID
JOIN [dbo].[Category] c    ON c.CategoryID    = e.CategoryID
JOIN [dbo].[Event] ev      ON ev.EventID      = c.EventID
LEFT JOIN [dbo].[Result] r ON r.EnrolmentID   = e.EnrolmentID
ORDER BY ev.EventName, c.CategoryName;
GO
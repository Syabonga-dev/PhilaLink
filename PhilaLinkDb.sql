IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Clinics] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [ContactNumber] nvarchar(max) NOT NULL,
    [Latitude] float NOT NULL,
    [Longitude] float NOT NULL,
    [Services] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Clinics] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [Admins] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Admins] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Admins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [PerformedByUserId] uniqueidentifier NULL,
    [Details] nvarchar(max) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Users_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Nurses] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [EmployeeNumber] nvarchar(max) NOT NULL,
    [RegistrationNumber] nvarchar(max) NOT NULL,
    [Qualification] nvarchar(max) NOT NULL,
    [ClinicId] uniqueidentifier NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [AddressLine1] nvarchar(max) NOT NULL,
    [AddressLine2] nvarchar(max) NULL,
    [Suburb] nvarchar(max) NOT NULL,
    [City] nvarchar(max) NOT NULL,
    [Province] nvarchar(max) NOT NULL,
    [PostalCode] nvarchar(max) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(max) NOT NULL,
    [EmploymentDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [EmergencyContactName] nvarchar(max) NOT NULL,
    [EmergencyContactPhone] nvarchar(max) NOT NULL,
    [EmergencyContactRelationship] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Nurses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Nurses_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Nurses_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OtpVerifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [ExpiryTime] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL,
    CONSTRAINT [PK_OtpVerifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OtpVerifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Patients] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [AddressLine1] nvarchar(max) NOT NULL,
    [AddressLine2] nvarchar(max) NULL,
    [Suburb] nvarchar(max) NOT NULL,
    [City] nvarchar(max) NOT NULL,
    [Province] nvarchar(max) NOT NULL,
    [PostalCode] nvarchar(max) NOT NULL,
    [EmergencyContactName] nvarchar(max) NOT NULL,
    [EmergencyContactPhone] nvarchar(max) NOT NULL,
    [EmergencyContactRelationship] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Patients_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Proxies] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [AddressLine1] nvarchar(max) NOT NULL,
    [AddressLine2] nvarchar(max) NULL,
    [Suburb] nvarchar(max) NOT NULL,
    [City] nvarchar(max) NOT NULL,
    [Province] nvarchar(max) NOT NULL,
    [PostalCode] nvarchar(max) NOT NULL,
    [DateOfBirth] date NOT NULL,
    [Gender] nvarchar(max) NOT NULL,
    [RelationshipToPatient] nvarchar(max) NOT NULL,
    [EmergencyContactName] nvarchar(max) NOT NULL,
    [EmergencyContactPhone] nvarchar(max) NOT NULL,
    [EmergencyContactRelationship] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Proxies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Proxies_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Allergies] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [AllergyName] nvarchar(max) NOT NULL,
    [Reaction] nvarchar(max) NULL,
    [Severity] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [RecordedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Allergies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Allergies_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MedicalConditions] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [ConditionName] nvarchar(max) NOT NULL,
    [DiagnosisDate] date NULL,
    [IsChronic] bit NOT NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MedicalConditions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MedicalConditions_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Medications] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Dosage] nvarchar(max) NOT NULL,
    [Instructions] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    CONSTRAINT [PK_Medications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Medications_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SymptomAssessments] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [SymptomsJson] nvarchar(max) NOT NULL,
    [Result] nvarchar(max) NOT NULL,
    [Recommendation] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SymptomAssessments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SymptomAssessments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProxyLinks] (
    [Id] uniqueidentifier NOT NULL,
    [PatientId] uniqueidentifier NOT NULL,
    [ProxyId] uniqueidentifier NOT NULL,
    [AssignedByNurseId] uniqueidentifier NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProxyLinks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProxyLinks_Nurses_AssignedByNurseId] FOREIGN KEY ([AssignedByNurseId]) REFERENCES [Nurses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProxyLinks_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProxyLinks_Proxies_ProxyId] FOREIGN KEY ([ProxyId]) REFERENCES [Proxies] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MedicationLogs] (
    [Id] uniqueidentifier NOT NULL,
    [MedicationId] uniqueidentifier NOT NULL,
    [TakenAt] datetime2 NOT NULL,
    [Taken] bit NOT NULL,
    CONSTRAINT [PK_MedicationLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MedicationLogs_Medications_MedicationId] FOREIGN KEY ([MedicationId]) REFERENCES [Medications] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MedicationSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [MedicationId] uniqueidentifier NOT NULL,
    [TimeOfDay] time NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_MedicationSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MedicationSchedules_Medications_MedicationId] FOREIGN KEY ([MedicationId]) REFERENCES [Medications] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Admins_UserId] ON [Admins] ([UserId]);

CREATE INDEX [IX_Allergies_PatientId] ON [Allergies] ([PatientId]);

CREATE INDEX [IX_AuditLogs_PerformedByUserId] ON [AuditLogs] ([PerformedByUserId]);

CREATE INDEX [IX_MedicalConditions_PatientId] ON [MedicalConditions] ([PatientId]);

CREATE INDEX [IX_MedicationLogs_MedicationId] ON [MedicationLogs] ([MedicationId]);

CREATE INDEX [IX_Medications_PatientId] ON [Medications] ([PatientId]);

CREATE INDEX [IX_MedicationSchedules_MedicationId] ON [MedicationSchedules] ([MedicationId]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

CREATE INDEX [IX_Nurses_ClinicId] ON [Nurses] ([ClinicId]);

CREATE UNIQUE INDEX [IX_Nurses_UserId] ON [Nurses] ([UserId]);

CREATE INDEX [IX_OtpVerifications_ExpiryTime] ON [OtpVerifications] ([ExpiryTime]);

CREATE INDEX [IX_OtpVerifications_UserId] ON [OtpVerifications] ([UserId]);

CREATE UNIQUE INDEX [IX_Patients_UserId] ON [Patients] ([UserId]);

CREATE UNIQUE INDEX [IX_Proxies_UserId] ON [Proxies] ([UserId]);

CREATE INDEX [IX_ProxyLinks_AssignedByNurseId] ON [ProxyLinks] ([AssignedByNurseId]);

CREATE INDEX [IX_ProxyLinks_PatientId] ON [ProxyLinks] ([PatientId]);

CREATE INDEX [IX_ProxyLinks_ProxyId] ON [ProxyLinks] ([ProxyId]);

CREATE INDEX [IX_SymptomAssessments_PatientId] ON [SymptomAssessments] ([PatientId]);

CREATE UNIQUE INDEX [IX_Users_PhoneNumber] ON [Users] ([PhoneNumber]);

CREATE INDEX [IX_Users_Role] ON [Users] ([Role]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260902173029_InitialCreate', N'10.0.10');

COMMIT;
GO


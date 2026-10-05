# PhilaLink Backend API

PhilaLink is a healthcare coordination backend built with ASP.NET Core and Entity Framework Core.

It provides the server-side foundation for patient self-service, clinic operations, nursing workflows, medication collection management, proxy-assisted care, notifications, symptom triage, weather-aware health guidance, dynamic administrative reporting, and an AI-assisted patient chatbot.

The API is designed around strict role separation, clinic scoping, patient self-access, secure staff onboarding, auditable sensitive actions, automated regression testing, and a PostgreSQL database target suitable for Supabase.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Core Goals](#core-goals)
- [Current Backend Capabilities](#current-backend-capabilities)
- [Roles and Authorization Model](#roles-and-authorization-model)
- [Role Ownership and Privileges](#role-ownership-and-privileges)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
- [Database](#database)
- [Authentication and Account Security](#authentication-and-account-security)
- [Secure Staff Onboarding](#secure-staff-onboarding)
- [Patient Features](#patient-features)
- [Nurse Features](#nurse-features)
- [Clinic Administrator Features](#clinic-administrator-features)
- [Super Administrator Features](#super-administrator-features)
- [Clinic Administrator Assignment Lifecycle](#clinic-administrator-assignment-lifecycle)
- [Proxy Features](#proxy-features)
- [Appointments](#appointments)
- [Medication and Collection Management](#medication-and-collection-management)
- [Clinic Stock](#clinic-stock)
- [Notifications](#notifications)
- [Dynamic Reporting](#dynamic-reporting)
- [Excel Reporting](#excel-reporting)
- [Secure PDF Reporting](#secure-pdf-reporting)
- [Symptom Assessment](#symptom-assessment)
- [AI Chatbot](#ai-chatbot)
- [Weather Integration](#weather-integration)
- [Audit Logging](#audit-logging)
- [Automated Testing](#automated-testing)
- [Important Status Values](#important-status-values)
- [Configuration and Secrets](#configuration-and-secrets)
- [Local Development Setup](#local-development-setup)
- [Entity Framework Core Migrations](#entity-framework-core-migrations)
- [Swagger / OpenAPI](#swagger--openapi)
- [Security Notes](#security-notes)
- [Current Development Status](#current-development-status)
- [Remaining Production Hardening](#remaining-production-hardening)
- [Frontend Repository](#frontend-repository)
- [Useful Commands](#useful-commands)
- [Development Workflow](#development-workflow)
- [Project Direction](#project-direction)

---

## Project Overview

PhilaLink is intended to support healthcare interactions between patients, clinics, nurses, administrators, and trusted proxies.

The backend centralizes:

- patient accounts and profiles
- clinic assignment
- nurse accounts
- proxy accounts
- ClinicAdmin accounts
- SuperAdmin administration
- medication management
- medication schedules
- medication adherence logs
- medication collection workflows
- clinic medicine stock
- appointment management
- proxy-patient relationships
- patient notifications
- nurse alerts
- symptom triage
- patient preferences
- health records
- health metrics
- weather-based health guidance
- AI chatbot conversations
- administrative reporting
- Excel exports
- password-protected PDF exports
- audit logging
- account security
- role-based authorization
- automated authorization testing
- regression testing

The current backend is built as a REST API and is consumed by the separate PhilaLink frontend application.

---

## Core Goals

PhilaLink's backend is designed around the following principles:

1. **Patient self-service**

   Patients should securely access their own profile, medications, appointments, records, collections, notifications, preferences, weather guidance, symptom assessments, and chatbot conversations.

2. **Clinic-scoped staff access**

   Clinic administrators and nurses should only access data relevant to the clinic they are currently assigned to.

3. **System-wide SuperAdmin administration**

   SuperAdmin is responsible for global administration without inheriting responsibilities that belong specifically to ClinicAdmin.

4. **Least privilege**

   Each role receives only the permissions required for its responsibilities.

5. **Patient self-scoping**

   Patient `/me` endpoints resolve identity from the authenticated JWT instead of trusting arbitrary patient IDs supplied by the client.

6. **Proxy relationship scoping**

   Proxy access is restricted to patients linked through active proxy relationships.

7. **Soft deactivation where appropriate**

   Users, clinics, stock records, proxy links, medications, schedules, and other lifecycle-managed records use active/inactive state instead of destructive deletion where appropriate.

8. **Auditability**

   Sensitive administrative and clinical actions are recorded through the audit logging system.

9. **Secure account onboarding**

   Administrator-created accounts use temporary passwords, email delivery, forced password replacement, and restricted first-login authorization.

10. **Provider-neutral integrations**

    External providers such as AI and weather services are isolated behind backend services rather than being called directly by the frontend.

11. **Automated regression protection**

    Critical role boundaries, clinic scoping, authentication workflows, reporting, medication workflows, OTP behavior, and other backend features are covered by automated tests.

---

## Current Backend Capabilities

The backend currently includes support for:

- Patient registration
- Patient login
- JWT authentication
- Patient OTP verification
- Password recovery
- Forced password change for staff accounts
- Temporary staff passwords
- Staff onboarding emails
- Account invitation resend
- Temporary-password rotation
- SuperAdmin bootstrap seeding
- Clinic management
- ClinicAdmin creation
- ClinicAdmin assignment
- ClinicAdmin reassignment
- ClinicAdmin deassignment
- Nurse creation
- Proxy creation
- System-wide account directory
- Account activation
- Account deactivation
- Patient profile management
- Patient dashboard data
- Nurse self-service
- Nurse alerts
- Appointment booking
- Appointment rescheduling
- Appointment cancellation
- Medication management
- Medication scheduling
- Medication adherence logging
- Medication collection workflows
- Clinic stock management
- Proxy assignment
- Proxy lifecycle management
- Patient notification management
- Audit logs
- Health records
- Health metrics
- Allergies
- Medical conditions
- Patient preferences
- Symptom assessment
- AI chatbot history
- AI chatbot messages
- Emergency chatbot interception
- OpenWeather current conditions
- OpenWeather forecast data
- Weather-based patient health tips
- ClinicAdmin reporting
- SuperAdmin reporting
- Dynamic Excel reports
- Excel dashboard filters
- Excel KPI cards
- Excel pie charts
- Excel bar graphs
- Password-protected PDF reporting
- Authorization tests
- Backend regression tests

---

## Roles and Authorization Model

PhilaLink currently supports exactly five application roles:

| Role | Scope | Main Responsibilities |
|---|---|---|
| `SuperAdmin` | System-wide | Clinic management, ClinicAdmin lifecycle, system reporting, account visibility, global audit |
| `ClinicAdmin` | Assigned clinic | Clinic administration, Nurse and Proxy management, inventory, reports, clinic audit |
| `Nurse` | Assigned clinic | Clinical workflows, patient care, medications, collections, alerts |
| `Proxy` | Linked patients | Limited patient access through active proxy relationships |
| `Patient` | Self | Own healthcare data and patient self-service |

There is no generic application role named:

```text
Admin
```

---

### Authorization Policies

The application defines authorization policies including:

```text
SuperAdminOnly
AdminOnly
ClinicStaff
PatientOnly
ProxyOnly
PatientOrProxy
PasswordChangeAllowed
```

The normal protected API policy requires an authenticated user with:

```text
mustChangePassword = false
```

This prevents staff accounts that are still using temporary passwords from accessing ordinary protected API functionality.

`PasswordChangeAllowed` exists so temporary-password users can complete the required password-change flow.

---

## Role Ownership and Privileges

PhilaLink intentionally separates SuperAdmin and ClinicAdmin responsibilities.

---

### SuperAdmin Responsibilities

SuperAdmin owns:

```text
Clinic creation
Clinic lifecycle management
ClinicAdmin account creation
ClinicAdmin assignment
ClinicAdmin reassignment
ClinicAdmin deassignment
System-wide account directory
System-wide analytics
System-wide reporting
System-wide audit access
```

SuperAdmin should not be the normal creator of:

```text
Nurse
Proxy
```

Those responsibilities belong to ClinicAdmin.

---

### ClinicAdmin Responsibilities

ClinicAdmin owns:

```text
Nurse creation
Proxy creation
Clinic-scoped staff management
Clinic-scoped inventory
Clinic-scoped reporting
Clinic-scoped analytics
Clinic-scoped audit visibility
Proxy operations within the clinic
```

ClinicAdmin cannot create another ClinicAdmin.

ClinicAdmin cannot perform system-wide administration.

---

### Nurse Responsibilities

Nurse responsibilities include:

```text
Patient care workflows
Medication workflows
Collection workflows
Clinic patient visibility
Clinic alerts
Clinical operations
```

Nurses are restricted to their assigned clinic.

---

### Proxy Responsibilities

Proxy access is based on active patient-proxy relationships.

A Proxy does not receive unrestricted clinic or patient access.

---

### Patient Responsibilities

A Patient is restricted to their own healthcare information and self-service operations.

---

## Technology Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core 10
- PostgreSQL
- Npgsql
- JWT Bearer authentication
- BCrypt password hashing
- Swagger / OpenAPI
- ASP.NET Core rate limiting

---

### Testing

- xUnit
- Microsoft.NET.Test.Sdk
- Entity Framework Core InMemory provider
- ASP.NET Core framework reference

---

### External Services

- Supabase PostgreSQL target
- Gemini-compatible AI provider
- OpenWeather API
- SMTP email delivery

---

### Main Production NuGet Packages

```text
BCrypt.Net-Next
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.EntityFrameworkCore.Design
Microsoft.EntityFrameworkCore.Tools
Npgsql.EntityFrameworkCore.PostgreSQL
Swashbuckle.AspNetCore
```

The backend no longer uses the SQL Server EF provider.

---

## Project Structure

```text
PhilaLink/
├── Controllers/
│   ├── AccountIdentityController.cs
│   ├── AdminController.cs
│   ├── AppointmentsController.cs
│   ├── AuditController.cs
│   ├── AuthController.cs
│   ├── ChatbotController.cs
│   ├── ClinicAdminController.cs
│   ├── ClinicAdminReportsController.cs
│   ├── ClinicAdminSecureReportsController.cs
│   ├── ClinicController.cs
│   ├── ClinicStockController.cs
│   ├── CollectionsController.cs
│   ├── MedicationController.cs
│   ├── PasswordResetController.cs
│   ├── PatientController.cs
│   ├── ProxyController.cs
│   ├── SuperAdminController.cs
│   ├── SuperAdminAccountsController.cs
│   ├── SuperAdminReportsController.cs
│   └── ...
│
├── Data/
│   ├── PhilaLinkDbContext.cs
│   └── PhilaLinkDbContextFactory.cs
│
├── Migrations/
│
├── Models/
│   ├── Constants/
│   ├── DTOs/
│   ├── Entities/
│   └── ...
│
├── Services/
│   ├── AI/
│   ├── Implementations/
│   └── Interfaces/
│
├── Tests/
│   └── PersonalProject.Tests/
│       ├── Authorization/
│       ├── Regression/
│       ├── Support/
│       └── PersonalProject.Tests.csproj
│
├── Utilities/
│   ├── AccountEmailSender.cs
│   ├── ClinicAdminDynamicReportBuilder.cs
│   ├── ClinicAdminSecurePdfBuilder.cs
│   └── ...
│
├── Dockerfile
├── Program.cs
├── PersonalProject.csproj
├── PersonalProject.slnx
└── README.md
```

---

### Production Project and Test Project

The backend repository contains two separate projects:

```text
PersonalProject.csproj
Tests/PersonalProject.Tests/PersonalProject.Tests.csproj
```

The root ASP.NET project explicitly excludes:

```text
Tests/**/*.cs
```

from production compilation.

This is necessary because SDK-style .NET projects recursively include C# files by default.

The test project compiles the test source independently.

---

### Solution

The solution file is:

```text
PersonalProject.slnx
```

It includes:

```text
PersonalProject.csproj
Tests/PersonalProject.Tests/PersonalProject.Tests.csproj
```

This allows the full backend and test suite to be restored, built, and tested together.

---

## Architecture

The backend primarily follows a service-oriented layered structure:

```text
HTTP Request
    ↓
Controller
    ↓
Service Interface
    ↓
Service Implementation
    ↓
Entity Framework Core
    ↓
PostgreSQL
```

Some reporting and administration features use direct read-oriented EF Core projections where the feature is primarily concerned with assembling report data.

---

### Chatbot Flow

```text
ChatbotController
    ↓
IChatbotService
    ↓
ChatbotService
    ↓
IChatbotProvider
    ↓
Gemini Provider
```

---

### Weather Flow

```text
WeatherController
    ↓
IWeatherService
    ↓
WeatherService
    ↓
OpenWeather API
```

---

### Administrative Reporting Flow

```text
Frontend Report Builder
    ↓
ClinicAdminReportsController
or
SuperAdminReportsController
    ↓
Filtered EF Core Query
    ↓
ClinicAdminDynamicReportPreviewDto
    ↓
ClinicAdminDynamicReportBuilder
    ↓
Excel Workbook
```

For protected PDF reports:

```text
Report Filters
    ↓
Report Preview Dataset
    ↓
ClinicAdminSecurePdfBuilder
    ↓
Password-Protected PDF
```

---

## Database

The target database is PostgreSQL.

The application uses Entity Framework Core through Npgsql.

---

### Identity and Profiles

```text
Users
Patients
Nurses
Proxies
Admins
```

---

### Clinic and Stock

```text
Clinics
ClinicStocks
```

---

### Patient Health

```text
Allergies
MedicalConditions
HealthMetrics
HealthRecords
SymptomAssessments
PatientPreferences
```

---

### Medication

```text
Medications
MedicationSchedules
MedicationLogs
```

---

### Medication Collection

```text
MedicationCollections
MedicationCollectionItems
```

---

### Proxy Relationships

```text
ProxyLinks
```

---

### Communication and Security

```text
Notifications
AuditLogs
OtpVerifications
ChatConversations
ChatMessages
```

---

### Appointments

```text
Appointments
```

---

### Important Constraints

The EF Core model includes constraints such as:

- unique user ID number
- unique user phone number
- unique patient number
- unique nurse employee number
- unique nurse registration number
- one patient profile per user
- one nurse profile per user
- one proxy profile per user
- one admin profile per user
- one patient preference record per patient
- unique clinic stock row per clinic + medication name + strength + form
- unique medication collection item per collection + medication

Foreign-key delete behavior is intentionally restrictive for many healthcare, account, appointment, collection, medication, proxy, and audit relationships.

Cascade delete is used only where the child record is intentionally dependent on its parent.

---

## Authentication and Account Security

### JWT Authentication

Authentication is JWT Bearer based.

JWT validation includes:

- issuer validation
- signing-key validation
- lifetime validation
- zero clock skew
- role claims
- `mustChangePassword` claim

Normal JWT access tokens currently have a one-hour lifetime.

---

### Patient Registration

Patient registration creates the required patient-side records.

A patient account includes the user identity and associated patient profile.

Newly registered patients must complete verification before ordinary patient access is accepted.

---

### Password Policy

The backend enforces:

- minimum 12 characters
- uppercase character
- lowercase character
- digit
- special character
- new password must differ from the existing password

---

### OTP Verification

OTP handling includes:

- cryptographically generated six-digit codes
- hashed OTP storage
- expiration
- resend cooldown
- maximum failed-attempt count
- OTP purpose tracking
- invalidation of previous OTPs for the same user and purpose
- no plaintext OTP logging
- invalidation after delivery failure

A primary OTP purpose is:

```text
AccountVerification
```

---

### Password Recovery

The backend contains a password-recovery workflow using identity verification and OTP-backed password reset.

Successful password reset updates the password hash and clears temporary-password state where applicable.

---

## Secure Staff Onboarding

Administrator-created staff accounts use a temporary-password lifecycle.

Supported administrator-created account types include:

```text
ClinicAdmin
Nurse
Proxy
```

---

### Account Creation Process

When an administrator creates a supported account, the backend:

```text
1. Validates the administrator's role
2. Validates clinic scope
3. Generates a strong temporary password
4. Hashes the temporary password with BCrypt
5. Stores only the hash
6. Sets MustChangePassword = true
7. Creates the role-specific profile
8. Sends the temporary password by email
9. Returns account metadata without returning the plaintext password
10. Writes relevant audit activity
```

The plaintext temporary password exists only long enough for the backend to send the onboarding email.

It is not part of the normal API response DTO.

---

### First Login

A newly created staff account authenticates using:

```text
SA ID number
+
Temporary password
```

The account is restricted from ordinary protected functionality because:

```text
MustChangePassword = true
```

The user must replace the temporary password.

After successful password change:

```text
MustChangePassword = false
```

and a fresh JWT is returned.

---

### Invitation Resend

Administrators can resend an invitation while the target account is still in the temporary-password state.

Resending:

- creates a new temporary password
- replaces the previous password hash
- invalidates the previous temporary password
- emails the new temporary password
- records invitation delivery outcome
- respects role ownership

A completed account that no longer requires its first password change cannot use the invitation-resend workflow as though it were still a new account.

---

### Invitation Ownership

For invitation resend:

```text
SuperAdmin
    → ClinicAdmin invitations

ClinicAdmin
    → Nurse invitations
    → Proxy invitations within own clinic
```

---

## Patient Features

Patient self-service is based on the authenticated user's identity rather than trusting a client-supplied patient ID for `/me` operations.

Current patient functionality includes:

- profile access
- dashboard
- medication list
- medication schedules
- medication adherence logging
- appointments
- appointment booking
- appointment rescheduling
- appointment cancellation
- health records
- health metrics
- medication collection information
- collection history
- notifications
- patient preferences
- symptom assessment
- chatbot conversations
- weather data
- weather health tips

The patient self-service API resolves the authenticated user through the JWT identity.

---

## Nurse Features

Nurses are clinic-scoped.

Current nurse self-service includes routes such as:

```text
GET /api/nurses/me
GET /api/nurses/me/dashboard
GET /api/nurses/me/patients
GET /api/nurses/me/alerts
```

A Nurse cannot use their account to read patient data belonging to another clinic.

Clinic ownership is validated before patient-care information is returned.

---

### Nurse Alerts

Overdue collection alerts use:

```text
OVERDUE_COLLECTIONS
Severity: High
```

Low-stock alerts use:

```text
LOW_STOCK
Severity: Medium
```

Nurses may generate audit events through permitted actions.

Direct administrative audit-history reading remains restricted to administrative roles.

---

## Clinic Administrator Features

A ClinicAdmin is associated with a clinic and operates within that clinic's scope.

Current ClinicAdmin functionality includes:

- ClinicAdmin `/me`
- clinic overview
- clinic-scoped administration
- Nurse creation
- Proxy creation
- staff lifecycle operations
- clinic stock management
- proxy-related clinic operations
- clinic-scoped reports
- Excel report export
- secure PDF report export
- clinic-scoped audit visibility

Clinic administrators do not receive system-wide administration.

---

### ClinicAdmin Scope

ClinicAdmin scope is resolved from the current database state.

This is important because clinic assignment can change.

The backend does not rely solely on an old client-side clinic value to authorize ClinicAdmin operations.

---

## Super Administrator Features

SuperAdmin is the highest application administration role.

Current functionality includes:

- system-wide clinic management
- ClinicAdmin creation
- ClinicAdmin assignment
- ClinicAdmin reassignment
- ClinicAdmin deassignment
- system-wide account directory
- system-wide account visibility
- system-wide analytics
- system-wide reporting
- optional report clinic filtering
- system-wide Excel export
- secure PDF report export
- full audit visibility

A SuperAdmin can be bootstrapped from configuration when the API starts and no SuperAdmin exists.

---

### System-Wide Account Directory

The backend exposes a SuperAdmin-only account directory.

System account information includes fields such as:

```text
UserId
FullName
IdNumber
Email
PhoneNumber
Role
ClinicId
ClinicName
IsActive
IsVerified
MustChangePassword
CreatedAt
UpdatedAt
```

The directory can optionally be filtered by role.

This gives SuperAdmin visibility into the actual account state across the system without transferring Nurse/Proxy creation ownership away from ClinicAdmin.

---

## Clinic Administrator Assignment Lifecycle

ClinicAdmin clinic assignment is controlled by SuperAdmin.

SuperAdmin can:

```text
Assign
Reassign
Deassign
```

a ClinicAdmin.

---

### Assignment

An unassigned ClinicAdmin can be assigned to an active clinic.

The backend updates:

```text
Admin.ClinicId
```

and records the assignment event.

---

### Reassignment

A ClinicAdmin can be moved from one active clinic to another.

After reassignment, the previous clinic is no longer valid scope for that ClinicAdmin.

---

### Deassignment

A ClinicAdmin can be removed from their clinic assignment.

The account can remain active, but clinic-scoped administrative operations are unavailable until SuperAdmin assigns the administrator to another clinic.

---

### Assignment Notifications

The backend supports email notifications for ClinicAdmin assignment lifecycle events such as:

```text
Assigned
Reassigned
Deassigned
Confirmed
```

---

## Proxy Features

A Proxy may access only patients connected through active proxy links.

A proxy is also associated with clinic context.

Proxy access checks include active relationship validation.

---

### ProxyLink Lifecycle

`ProxyLink` lifecycle data can include:

- patient
- proxy
- assigning Nurse
- assigning administrator
- assignment timestamp
- active state
- end timestamp
- user who ended the link
- end reason

Proxy relationships are ended softly rather than deleted.

---

### Clinic Boundary

Proxy assignment validates clinic ownership.

A proxy from one clinic should not be assigned to a patient belonging to another clinic through ordinary clinic-scoped administration.

---

## Appointments

The backend supports:

- appointment booking
- appointment rescheduling
- appointment cancellation
- patient self-service
- clinic-scoped staff access
- optional Nurse assignment
- external/provider name support
- duration
- reason
- mode
- status

---

### Appointment Statuses

Common appointment statuses include:

```text
Scheduled
Confirmed
Pending
Completed
Cancelled
Missed
Rescheduled
```

---

### Appointment Modes

```text
InPerson
Telehealth
```

---

## Medication and Collection Management

Medication support includes:

- medication records
- active/inactive medication state
- medication schedules
- adherence logs
- patient medication access
- Nurse medication workflows

---

### Medication Schedules

Schedules store:

- medication
- time of day
- active state

---

### Medication Logs

Logs store information such as:

- medication
- recorded time
- taken/not-taken result
- optional notes

These records support adherence reporting.

---

### Medication Collections

Collections support:

- patient
- clinic
- optional proxy
- processing Nurse
- scheduled collection date
- actual collection date
- status
- notes
- collection items

Collection completion is transactional and can include:

```text
Collection update
+
Clinic stock deduction
+
Audit logging
```

---

### Collection Statuses

Common collection statuses include:

```text
Scheduled
Collected
Cancelled
Pending
Overdue
```

---

## Clinic Stock

Clinic stock records include:

- clinic
- medication name
- strength
- form
- unit
- quantity on hand
- reorder level
- active state

The database prevents duplicate stock rows for the same combination:

```text
Clinic
+
MedicationName
+
Strength
+
Form
```

Medication collection processing can deduct quantities from clinic stock.

Low-stock information is also used by Nurse and administrative workflows.

---

## Notifications

Notifications are associated with users.

The backend supports patient and system notifications.

Examples include weather-generated health guidance.

System-generated notifications can use duplicate suppression for matching content within the configured time window.

---

## Dynamic Reporting

PhilaLink now includes dynamic report-builder APIs for both:

```text
ClinicAdmin
SuperAdmin
```

The report system is data-driven rather than being limited to one fixed exported spreadsheet.

---

### ClinicAdmin Report Builder

Base route:

```text
/api/clinic-admin/report-builder
```

Supported report types include:

```text
Patients
Appointments
Collections
Medication Adherence
Inventory
Staff
```

ClinicAdmin reports automatically use the authenticated administrator's clinic.

The client does not decide the ClinicAdmin's effective clinic scope.

---

### SuperAdmin Report Builder

Base route:

```text
/api/super-admin/report-builder
```

Supported report types include:

```text
Patients
Appointments
Collections
Medication Adherence
Inventory
Staff
Clinics
Audit Activity
```

SuperAdmin reports can operate:

```text
System-wide
```

or optionally be limited to:

```text
One clinic
```

using a clinic filter.

---

### Dynamic Report Query

The shared report query supports fields including:

```text
ReportType
ClinicId
DateFrom
DateTo
Status
Search
Role
Provider
AppointmentType
Mode
Medication
```

For ClinicAdmin:

```text
ClinicId is ignored as an authorization scope
```

because the authenticated ClinicAdmin's database assignment determines the clinic.

For SuperAdmin:

```text
ClinicId = null
```

means all clinics.

A supplied `ClinicId` limits the system report to that clinic.

---

### Report Preview Response

Dynamic report previews contain:

```text
ReportType
Title
ClinicName
RequestedBy
GeneratedAt
DateFrom
DateTo
Columns
Rows
Summary
FilterOptions
```

This allows the frontend to construct table previews based on the selected report and filters.

---

## Excel Reporting

Both ClinicAdmin and SuperAdmin Excel exports use the shared:

```text
ClinicAdminDynamicReportBuilder
```

The workbook is dynamically assembled according to report type and returned data.

---

### Workbook Structure

Generated Excel reports contain:

```text
Dashboard
Data
Calc
Lists
```

`Calc` and `Lists` are internal helper worksheets and are hidden from the ordinary workbook interface.

---

### Dashboard

The Dashboard contains:

- PhilaLink report identity
- report title
- clinic or system scope
- requesting administrator
- generated timestamp
- reporting period
- report-specific dropdown filters
- optional date filters
- live KPI summary
- pie chart
- bar graph
- filtered table preview

---

### Dashboard Filters

Filter choices are generated from report data and report-specific options.

Depending on report type, filters can include fields such as:

```text
Clinic
Status
Medication
Strength
Form
Unit
Role
Provider
Appointment type
Mode
Patient
Collector
Action
Performed by
```

SuperAdmin reports can include clinic as an additional workbook-level filter where relevant.

---

### KPI Cards

Dashboard KPIs depend on the report type.

Examples include:

#### Inventory

```text
Items shown
Units on hand
Low stock
Inactive
```

#### Collections

```text
Collections shown
Collected
Missed
Scheduled
```

#### Appointments

```text
Appointments shown
Completed
Scheduled / Pending
Cancelled
```

#### Medication Adherence

```text
Logs shown
Taken
Missed
Adherence
```

#### Staff

```text
Staff shown
Nurses
Proxies
Active
```

---

### Charts

The Excel workbook supports:

```text
Pie chart
Bar graph
```

The chart dimensions vary according to report type.

Examples:

```text
Inventory
    → Stock status distribution
    → Units on hand by medication

Appointments
    → Appointment status
    → Appointments by type

Collections
    → Collection status
    → Collections by medication

Medication Adherence
    → Taken vs missed
    → Dose records by medication

Staff
    → Role distribution
    → Staff by clinic

Audit Activity
    → Audit action distribution
    → Audit activity by clinic/category
```

---

### Data Worksheet

The `Data` worksheet contains the full exported report dataset.

It includes ordinary Excel column filters and is intended for:

- sorting
- filtering
- manual analysis
- formulas
- totals
- averages
- further user analysis

The Dashboard is designed for summarized reporting while the Data sheet retains the detailed export.

---

## Secure PDF Reporting

PhilaLink also supports password-protected administrative PDF reports.

---

### ClinicAdmin Secure PDF

Base route:

```text
POST /api/clinic-admin/report-builder/export/pdf
```

---

### SuperAdmin Secure PDF

Base route:

```text
POST /api/super-admin/report-builder/export/pdf
```

---

### Secure PDF Request

The request includes:

```text
Filters
Password
LogoJpegBase64
```

`Filters` uses the same dynamic report filter model as report preview and Excel generation.

---

### Password Requirements

The secure PDF password currently requires:

```text
Minimum: 8 characters
Maximum: 128 characters
```

---

### Optional Report Logo

The secure PDF request can contain an optional JPEG logo encoded as Base64.

The backend validates the supplied image payload before using it.

---

## Symptom Assessment

The symptom-assessment service performs deterministic triage.

Assessment results can follow:

```text
Emergency
Urgent
Non-emergency
```

paths.

Emergency indicators include phrases relating to:

- chest pain
- inability to breathe
- severe shortness of breath
- unconsciousness
- not breathing
- severe bleeding
- seizures
- overdose
- anaphylaxis
- severe allergic reactions
- stroke-like symptoms
- suicidal intent

Symptom assessment is patient-only and patient-scoped.

---

## AI Chatbot

PhilaLink includes a patient chatbot with persisted conversation history.

Current capabilities include:

- send chatbot messages
- retrieve chatbot history
- clear chatbot history
- load patient context
- persist patient messages
- persist assistant responses
- provider abstraction
- deterministic emergency interception

The AI model is configured through backend configuration rather than being controlled by the frontend.

---

### Emergency Interception

Before an eligible patient message is sent to the external AI provider, the backend checks for emergency indicators.

If an emergency is detected:

```text
Patient message persisted
    ↓
External AI call skipped
    ↓
Deterministic emergency response generated
    ↓
Emergency response persisted
    ↓
Patient directed to immediate medical help
```

This ensures emergency behavior does not depend entirely on generative model output.

---

## Weather Integration

PhilaLink integrates with OpenWeather.

Patient weather functionality includes routes such as:

```text
GET  /api/weather/me/current
GET  /api/weather/me/forecast
POST /api/weather/me/tips
```

The backend uses clinic/location context for the patient weather workflow.

The OpenWeather API key remains server-side.

---

## Audit Logging

Sensitive actions are recorded in:

```text
AuditLogs
```

Audit information includes fields such as:

- action
- performing user
- clinic scope where relevant
- details
- timestamp

---

### Audit Visibility

```text
SuperAdmin
    → System-wide audit history

ClinicAdmin
    → Assigned-clinic audit history

Nurse
    → May generate audit events
    → Does not receive administrator audit-history access
```

---

### Example Audited Operations

Audit records can be generated for actions such as:

- ClinicAdmin creation
- Nurse creation
- Proxy creation
- invitation delivery
- invitation resend
- account activation
- account deactivation
- ClinicAdmin assignment
- ClinicAdmin reassignment
- ClinicAdmin deassignment
- medication operations
- collection processing
- reporting/export activity

---

## Automated Testing

The backend now contains a dedicated xUnit test project:

```text
Tests/PersonalProject.Tests/PersonalProject.Tests.csproj
```

Testing is therefore no longer only a future roadmap item.

---

### Test Structure

```text
Tests/
└── PersonalProject.Tests/
    ├── Authorization/
    ├── Regression/
    ├── Support/
    └── PersonalProject.Tests.csproj
```

---

### Authorization Tests

Current authorization suites include:

```text
RoleBoundaryTests.cs
ClinicScopeTests.cs
PatientScopeTests.cs
TemporaryPasswordTests.cs
```

These test important boundaries including:

- role ownership
- ClinicAdmin scope
- Nurse clinic scope
- patient self-access
- temporary-password restrictions

---

### Regression Tests

Current regression suites include:

```text
AppointmentWorkflowTests.cs
AuthWorkflowTests.cs
ClinicStockWorkflowTests.cs
MedicationCollectionWorkflowTests.cs
MedicationWorkflowTests.cs
OtpVerificationTests.cs
PasswordResetWorkflowTests.cs
ReportBuilderSmokeTests.cs
SouthAfricanIdNumberTests.cs
SymptomAssessmentTests.cs
```

---

### Test Support

The test project contains support utilities for:

- in-memory EF Core database creation
- reusable test data
- audit service test doubles
- deterministic test setup

---

### Reporting Tests

The report-builder tests inspect the generated XLSX structure.

They validate areas such as:

- required workbook parts
- worksheet XML
- Excel relationships
- chart XML
- workbook recalculation
- filter configuration
- KPI formulas
- report-preview behavior

---

### Run Tests

```powershell
dotnet test .\PersonalProject.slnx
```

---

## Important Status Values

### Appointment Statuses

```text
Scheduled
Confirmed
Pending
Completed
Cancelled
Missed
Rescheduled
```

---

### Appointment Modes

```text
InPerson
Telehealth
```

---

### Collection Statuses

```text
Scheduled
Collected
Cancelled
Pending
Overdue
```

---

## Configuration and Secrets

Sensitive values must not be committed to Git.

Use:

```text
.NET User Secrets
```

for local development and environment/platform secrets in deployed environments.

---

### Core Configuration Keys

```text
ConnectionStrings:DefaultConnection

Jwt:Key
Jwt:Issuer

AI:ApiKey
AI:Model

Weather:ApiKey

Seed:SuperAdminFullName
Seed:SuperAdminIdNumber
Seed:SuperAdminPhoneNumber
Seed:SuperAdminEmail
Seed:SuperAdminPassword
```

---

### SMTP Configuration

SMTP configuration is used for:

- patient OTP delivery
- administrator-created account invitations
- temporary-password delivery
- ClinicAdmin assignment notifications

SMTP-related configuration includes values for the configured mail server such as:

```text
Smtp:Host
Smtp:Port
Smtp:Username
Smtp:Password
```

and related sender/frontend configuration where required.

---

### Environment Variable Format

ASP.NET Core uses double underscores for nested environment variables.

Examples:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Jwt__Issuer
AI__ApiKey
AI__Model
Weather__ApiKey
Seed__SuperAdminFullName
Seed__SuperAdminIdNumber
Seed__SuperAdminPhoneNumber
Seed__SuperAdminEmail
Seed__SuperAdminPassword
```

---

### Never Commit

Never commit:

- database passwords
- production connection strings
- JWT signing keys
- SMTP passwords
- AI API keys
- OpenWeather API keys
- SuperAdmin seed passwords
- other production secrets

---

## Local Development Setup

### Prerequisites

- .NET 10 SDK
- Git
- PostgreSQL or Supabase PostgreSQL access
- EF Core CLI compatible with the project

---

### Clone

```powershell
git clone https://github.com/Syabonga-dev/PhilaLink.git
cd PhilaLink
```

---

### Restore Entire Solution

```powershell
dotnet restore .\PersonalProject.slnx
```

---

### Build Entire Solution

```powershell
dotnet build .\PersonalProject.slnx
```

---

### Run Tests

```powershell
dotnet test .\PersonalProject.slnx
```

---

### Run Backend

```powershell
dotnet run --project .\PersonalProject.csproj
```

---

### Format

```powershell
dotnet format .\PersonalProject.csproj
```

---

### Configure Local Secrets

Example:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_POSTGRES_CONNECTION_STRING" --project .\PersonalProject.csproj

dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_SIGNING_KEY" --project .\PersonalProject.csproj
dotnet user-secrets set "Jwt:Issuer" "PhilaLink" --project .\PersonalProject.csproj

dotnet user-secrets set "AI:ApiKey" "YOUR_AI_API_KEY" --project .\PersonalProject.csproj
dotnet user-secrets set "AI:Model" "YOUR_CONFIGURED_MODEL" --project .\PersonalProject.csproj

dotnet user-secrets set "Weather:ApiKey" "YOUR_OPENWEATHER_API_KEY" --project .\PersonalProject.csproj
```

---

### Optional SuperAdmin Seed

```powershell
dotnet user-secrets set "Seed:SuperAdminFullName" "YOUR_NAME" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminIdNumber" "YOUR_ID_VALUE" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminPhoneNumber" "YOUR_PHONE" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminEmail" "YOUR_EMAIL" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminPassword" "YOUR_STRONG_PASSWORD" --project .\PersonalProject.csproj
```

---

## Entity Framework Core Migrations

The backend uses PostgreSQL through Npgsql.

A design-time DbContext factory is included at:

```text
Data/PhilaLinkDbContextFactory.cs
```

This allows Entity Framework tooling to instantiate the database context without running the complete API.

---

### List Migrations

```powershell
dotnet ef migrations list --project .\PersonalProject.csproj
```

---

### Create Migration

```powershell
dotnet ef migrations add MigrationName --project .\PersonalProject.csproj
```

---

### Apply Migrations

```powershell
dotnet ef database update --project .\PersonalProject.csproj
```

---

### Generate SQL Script

```powershell
dotnet ef migrations script `
  --project .\PersonalProject.csproj `
  --output .\migration.sql
```

Always inspect generated migrations and SQL before applying them to shared or production databases.

---

## Swagger / OpenAPI

Swagger is enabled in the configured development environment.

Bearer-authenticated calls use:

```text
Authorization: Bearer <JWT>
```

---

## Security Notes

Current backend security measures include:

- BCrypt password hashing
- JWT authentication
- role-based authorization
- clinic scoping
- patient self-scoping
- proxy relationship checks
- account active-state checks
- patient verification
- forced staff password changes
- temporary-password rotation
- strong password rules
- cryptographically generated OTPs
- OTP hashing
- OTP attempt limits
- OTP expiry
- OTP resend cooldown
- invitation role restrictions
- deterministic chatbot emergency interception
- server-side external API keys
- restrictive foreign-key delete behavior
- audit logging
- soft-deactivation patterns
- frontend CORS policy
- ASP.NET Core rate limiting

---

### Rate Limiting

The backend contains ASP.NET Core rate limiting.

Rate-limited areas include sensitive and higher-cost operations such as authentication, registration/verification, external-service-backed functionality, and other protected workflows.

Rate-limit coverage should continue to be reviewed as the API grows.

---

### Current JWT Revocation Limitation

JWT authorization is currently based on claims and token expiry.

After successful password change, the backend returns a fresh token containing:

```text
mustChangePassword = false
```

However, the project does not yet contain a complete token-version or security-stamp mechanism capable of immediately invalidating every previously issued normal JWT.

With the current one-hour token lifetime, a previously valid token can remain valid until expiration unless another authorization check rejects the account.

Immediate session revocation remains part of production security hardening.

---

## Current Development Status

The backend foundation is substantially implemented.

Completed or substantially completed areas include:

- PostgreSQL backend foundation
- five-role authorization model
- strict SuperAdmin / ClinicAdmin privilege separation
- patient registration
- patient verification
- authentication
- forced staff password changes
- secure temporary-password onboarding
- account invitation emails
- invitation resend
- password reset
- patient self-service
- Nurse self-service
- ClinicAdmin self-service
- SuperAdmin administration
- ClinicAdmin assignment lifecycle
- system-wide account directory
- appointments
- medications
- medication schedules
- medication adherence
- medication collections
- clinic stock
- proxy relationships
- notifications
- audit logging
- symptom triage
- AI chatbot
- deterministic chatbot emergency interception
- weather integration
- ClinicAdmin dynamic reporting
- SuperAdmin dynamic reporting
- Excel report generation
- dashboard filters
- reporting KPIs
- reporting charts
- secure PDF reporting
- authorization tests
- workflow regression tests

Automated testing is now part of the repository and should no longer be described as a future-only phase.

---

## Remaining Production Hardening

The remaining work is increasingly focused on production readiness rather than core feature scaffolding.

---

### Continuous Integration

Remaining CI work includes:

- automated build workflow
- automated test workflow
- execution on pushes
- execution on pull requests
- optional dependency vulnerability checks
- clear build/test status reporting

---

### Security Hardening

Remaining or review areas include:

- JWT token-version/security-stamp invalidation
- session revocation
- endpoint-by-endpoint rate-limit review
- security headers
- HSTS review
- centralized exception handling
- anti-enumeration review
- brute-force protection review
- dependency vulnerability scanning
- OWASP API Top 10 review
- production secrets review
- Supabase security review
- penetration-test-style verification

---

### Database Performance

Areas for further work include:

- query-plan inspection
- composite index review
- read-query optimization
- `AsNoTracking()` consistency
- over-fetching reduction
- N+1 review
- pagination consistency
- sorting/filtering consistency
- `EXPLAIN ANALYZE`
- PostgreSQL monitoring

---

### API Performance

Potential hardening includes:

- cancellation-token propagation
- response compression
- payload limits
- connection pool tuning
- selective caching
- external-service timeout strategies
- retry strategies
- circuit-breaker behavior where appropriate

---

### Observability

Remaining observability work includes:

- deeper health checks
- dependency health checks
- structured operational logging
- OpenTelemetry
- metrics
- tracing
- slow-query monitoring
- failure-rate monitoring
- external-provider monitoring

---

### Load Testing

Planned load testing should measure:

```text
P50 latency
P95 latency
P99 latency
Requests per second
Error rate
Database latency
Connection pool utilization
CPU
Memory
External provider latency
```

Possible tools include:

```text
k6
Postman / Newman
```

---

### Backup and Recovery

Production-readiness work should include:

- backup configuration verification
- restore testing
- recovery procedure documentation
- disaster-recovery expectations
- database recovery drills

---

### Final Acceptance

Before production completion, PhilaLink should undergo full role acceptance testing for:

```text
SuperAdmin
ClinicAdmin
Nurse
Proxy
Patient
```

This should verify both permitted actions and forbidden actions.

---

## Frontend Repository

The frontend is maintained separately:

```text
Syabonga-dev/PhilaLink_Frontend
```

It is a React/Vite application that consumes this API.

The frontend contains the role-specific dashboards and administrative reporting interfaces.

---

## Useful Commands

```powershell
# Restore API + tests
dotnet restore .\PersonalProject.slnx

# Build API + tests
dotnet build .\PersonalProject.slnx

# Run complete test suite
dotnet test .\PersonalProject.slnx

# Format production backend
dotnet format .\PersonalProject.csproj

# Run API
dotnet run --project .\PersonalProject.csproj

# EF version
dotnet ef --version

# List migrations
dotnet ef migrations list --project .\PersonalProject.csproj

# Create migration
dotnet ef migrations add MigrationName --project .\PersonalProject.csproj

# Apply migration
dotnet ef database update --project .\PersonalProject.csproj

# Generate migration SQL
dotnet ef migrations script `
  --project .\PersonalProject.csproj `
  --output .\migration.sql

# Check vulnerable production packages
dotnet list .\PersonalProject.csproj package --vulnerable

# Git state
git status

# Recent commits
git log --oneline --decorate -10
```

---

## Development Workflow

Recommended backend workflow:

```text
1. Pull latest main
2. Confirm clean Git state
3. Make one focused backend change
4. Format when appropriate
5. Build the complete solution
6. Run automated tests
7. Inspect Git diff
8. Commit
9. Push
10. Verify pushed files on GitHub
```

---

### Database Change Workflow

For database changes:

```text
1. Update entities / DbContext
2. Build successfully
3. Generate migration
4. Inspect migration
5. Inspect generated SQL when appropriate
6. Commit migration
7. Apply migration
8. Verify database schema
9. Test affected API workflows
```

Avoid destructive Git commands when local changes are not fully understood.

---

### Reporting Change Workflow

For report-generator changes:

```text
1. Update report query / preview if required
2. Update report builder
3. Update report regression tests
4. Build solution
5. Run test suite
6. Generate a real XLSX/PDF
7. Open exported files in their native applications
8. Verify filters, tables, charts and formulas
9. Push
10. Verify GitHub versions
```

This is especially important because a generated `.xlsx` is an Open XML package and can be structurally valid XML while still requiring Excel compatibility validation.

---

## Project Direction

PhilaLink is being developed toward a backend that is:

```text
Correct
+
Secure
+
Auditable
+
Testable
+
Recoverable
+
Observable
+
Performant
+
Maintainable
```

Because PhilaLink handles healthcare-related information, the project treats the following as first-class concerns:

```text
Privacy
Authorization boundaries
Clinic scoping
Patient self-scoping
Secure staff onboarding
Auditability
Emergency handling
Data integrity
Operational safety
Recovery
Testing
```

The goal is not only to provide application functionality, but to ensure that healthcare workflows remain predictable, traceable, role-correct, and safe as the system moves toward production readiness.

# PhilaLink Backend API

PhilaLink is a healthcare coordination backend built with ASP.NET Core and Entity Framework Core. It provides the server-side foundation for patient self-service, clinic operations, nursing workflows, medication collection management, proxy-assisted care, notifications, symptom triage, weather-aware health guidance, and an AI-assisted patient chatbot.

The API is designed around strict role separation, clinic scoping, patient self-access, auditable sensitive actions, and a PostgreSQL database target suitable for Supabase.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Core Goals](#core-goals)
- [Current Backend Capabilities](#current-backend-capabilities)
- [Roles and Authorization Model](#roles-and-authorization-model)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Architecture](#architecture)
- [Database](#database)
- [Authentication and Account Security](#authentication-and-account-security)
- [Patient Features](#patient-features)
- [Nurse Features](#nurse-features)
- [Clinic Administrator Features](#clinic-administrator-features)
- [Super Administrator Features](#super-administrator-features)
- [Proxy Features](#proxy-features)
- [Appointments](#appointments)
- [Medication and Collection Management](#medication-and-collection-management)
- [Clinic Stock](#clinic-stock)
- [Notifications](#notifications)
- [Symptom Assessment](#symptom-assessment)
- [AI Chatbot](#ai-chatbot)
- [Weather Integration](#weather-integration)
- [Audit Logging](#audit-logging)
- [Important Status Values](#important-status-values)
- [Configuration and Secrets](#configuration-and-secrets)
- [Local Development Setup](#local-development-setup)
- [Entity Framework Core Migrations](#entity-framework-core-migrations)
- [Swagger / OpenAPI](#swagger--openapi)
- [Security Notes](#security-notes)
- [Current Development Status](#current-development-status)
- [Planned Hardening and Optimization](#planned-hardening-and-optimization)
- [Frontend Repository](#frontend-repository)
- [Useful Commands](#useful-commands)

---

## Project Overview

PhilaLink is intended to support healthcare interactions between patients, clinics, nurses, administrators, and trusted proxies.

The backend centralizes:

- patient accounts and profiles
- clinic assignment
- nurse and administrator accounts
- medication management
- medication schedules and adherence logs
- medication collection workflows
- clinic medicine stock
- appointment management
- proxy-patient relationships
- patient notifications
- symptom triage
- weather-based health guidance
- AI chatbot conversations
- audit logging
- security and role-based authorization

The current backend is built as a REST API and is intended to be consumed by the separate PhilaLink frontend application.

---

## Core Goals

PhilaLink's backend is designed around the following principles:

1. **Patient self-service** — patients should securely access their own profile, medications, appointments, records, collections, notifications, preferences, weather guidance, symptom assessments, and chatbot conversations.
2. **Clinic-scoped staff access** — clinic administrators and nurses should only access data relevant to the clinic they are assigned to unless the account is a SuperAdmin.
3. **Least privilege** — each role receives only the permissions required for its responsibilities.
4. **Soft deactivation where appropriate** — users, clinics, stock records, proxy links, medications, schedules, and other lifecycle-managed records use active/inactive state rather than destructive deletion where appropriate.
5. **Auditability** — sensitive administrative and clinical actions are recorded through the audit log system.
6. **Secure account onboarding** — patient verification, OTP handling, temporary staff passwords, forced password changes, and JWT authorization are handled in the backend.
7. **Provider-neutral external integrations** — chatbot and weather providers are isolated behind service abstractions rather than being called directly from controllers.

---

## Current Backend Capabilities

The backend currently includes support for:

- Patient registration and login
- JWT authentication
- Patient OTP verification
- Forced password change for staff accounts created with temporary passwords
- SuperAdmin bootstrap seeding
- Clinic management
- Nurse management
- Clinic administrator management
- Patient profile management
- Patient dashboard data
- Appointment booking, rescheduling, and cancellation
- Medication management and scheduling
- Medication adherence logging
- Medication collection workflows
- Clinic stock management
- Proxy assignment and lifecycle management
- Patient notification management
- Nurse alerts
- Audit logs
- Health records and health metrics
- Allergies and medical conditions
- Patient preferences
- Symptom assessment
- AI chatbot history and messages
- Emergency chatbot interception
- OpenWeather current conditions
- OpenWeather forecast data
- Weather-based patient health tips

---

## Roles and Authorization Model

PhilaLink currently supports exactly five application roles:

| Role | Scope | Main Responsibilities |
|---|---|---|
| `SuperAdmin` | System-wide | Global administration, clinic management, elevated administrative operations |
| `ClinicAdmin` | Assigned clinic | Clinic-scoped administration, staff and clinic operational management |
| `Nurse` | Assigned clinic | Clinical operations, patient work, medications, collections, stock-related workflows |
| `Proxy` | Assigned patients only | Access to patients linked through active proxy relationships |
| `Patient` | Self only | Access to the patient's own healthcare data and self-service features |

There is no generic `Admin` role.

### Authorization policies

The application defines:

- `SuperAdminOnly`
- `AdminOnly`
- `ClinicStaff`
- `PatientOnly`
- `ProxyOnly`
- `PatientOrProxy`
- `PasswordChangeAllowed`

The default authorization policy requires an authenticated user with the JWT claim:

```text
mustChangePassword = false
```

This prevents staff accounts that are still using temporary passwords from accessing normal protected API features. `PasswordChangeAllowed` is the exception used by the password-change flow.

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

### External services

- Supabase PostgreSQL target
- Gemini chatbot provider
- OpenWeather API

### Main NuGet packages

- `BCrypt.Net-Next`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`
- `Npgsql`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Swashbuckle.AspNetCore`

The previous SQL Server EF provider has been removed from the project.

---

## Project Structure

```text
PhilaLink/
├── Controllers/
├── Data/
│   ├── PhilaLinkDbContext.cs
│   └── PhilaLinkDbContextFactory.cs
├── Migrations/
│   ├── *_InitialPostgresSchema.cs
│   ├── *_InitialPostgresSchema.Designer.cs
│   └── PhilaLinkDbContextModelSnapshot.cs
├── Models/
│   ├── Constants/
│   ├── DTOs/
│   ├── Entities/
│   └── Admin.cs
├── Services/
│   ├── AI/
│   ├── Implementations/
│   └── Interfaces/
├── Program.cs
└── PersonalProject.csproj
```

### Main controllers

The backend includes controllers for authentication, users, administrators, clinics, nurses, patients, appointments, medications, medication collections, clinic stock, proxies, notifications, symptom assessments, weather, chatbot functionality, and audit logs.

---

## Architecture

The backend follows a service-oriented layered structure:

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

External providers are accessed through services rather than directly from controllers.

### Chatbot flow

```text
ChatbotController
    ↓
IChatbotService
    ↓
ChatbotService
    ↓
IChatbotProvider
    ↓
GeminiChatbotProvider
```

### Weather flow

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

## Database

The target database is PostgreSQL.

The backend currently models approximately 25 application tables, plus Entity Framework's migration history table.

### Identity and profiles

- `Users`
- `Patients`
- `Nurses`
- `Proxies`
- `Admins`

### Clinic and stock

- `Clinics`
- `ClinicStocks`

### Patient health

- `Allergies`
- `MedicalConditions`
- `HealthMetrics`
- `HealthRecords`
- `SymptomAssessments`
- `PatientPreferences`

### Medication

- `Medications`
- `MedicationSchedules`
- `MedicationLogs`

### Medication collection

- `MedicationCollections`
- `MedicationCollectionItems`

### Proxy relationships

- `ProxyLinks`

### Communication and security

- `Notifications`
- `AuditLogs`
- `OtpVerifications`
- `ChatConversations`
- `ChatMessages`

### Appointments

- `Appointments`

### Important constraints

The EF Core model includes:

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

Foreign-key delete behavior is intentionally restrictive for many account, clinic, appointment, collection, medication, proxy, and audit relationships. Cascade delete is used only where child records are intentionally dependent on their parent.

---

## Authentication and Account Security

### JWT authentication

Authentication is JWT Bearer based.

JWT validation currently includes:

- issuer validation
- signing-key validation
- lifetime validation
- zero clock skew

The configured token lifetime is currently one hour.

### Patient registration

Patient registration creates the required records transactionally. A newly registered patient receives a `User`, a `Patient`, a `PatientPreference`, and a generated patient number. Patients start unverified and must complete verification before normal patient login is accepted.

### Staff accounts

Staff accounts created administratively are marked verified and receive:

```text
MustChangePassword = true
```

They cannot use normal protected API functionality until the temporary password has been replaced.

### Password policy

The backend enforces:

- minimum 12 characters
- uppercase character
- lowercase character
- digit
- special character
- new password must differ from the current password

### OTP verification

OTP handling includes:

- cryptographically generated six-digit codes
- hashed OTP storage
- five-minute expiry
- maximum failed-attempt count
- resend cooldown
- invalidation of previous OTPs for the same user and purpose
- OTP purpose tracking
- no plaintext OTP logging
- invalidation when delivery fails

Primary current OTP purpose:

```text
AccountVerification
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
- medication collection information
- collection history
- notifications
- patient preferences
- symptom assessment
- chatbot conversations
- weather data
- weather health tips

The patient self-service API resolves the authenticated user through the JWT NameIdentifier.

---

## Nurse Features

Nurses are clinic-scoped.

Current nurse self-service includes:

```text
GET /api/nurses/me
GET /api/nurses/me/dashboard
GET /api/nurses/me/patients
GET /api/nurses/me/alerts
```

### Nurse alerts

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

Nurses may generate audit records through their permitted actions, but direct audit-history reading is restricted to administrators.

---

## Clinic Administrator Features

A ClinicAdmin is associated with a clinic and operates within that clinic's scope.

Current functionality includes:

- ClinicAdmin `/me`
- clinic overview
- clinic-scoped administration
- staff-related workflows
- clinic stock management
- proxy-related clinic operations
- clinic-scoped audit visibility

Clinic administrators do not receive system-wide access.

---

## Super Administrator Features

SuperAdmin is the highest application role and can perform system-wide administrative operations including clinic management, elevated user administration, full audit visibility, and ClinicAdmin creation.

A SuperAdmin can be bootstrapped from configuration when the API starts and no SuperAdmin exists.

---

## Proxy Features

A proxy may access only patients connected through active proxy links.

`ProxyLink` lifecycle data includes:

- patient
- proxy
- assigning nurse
- assigning administrator
- assignment timestamp
- active state
- end timestamp
- user who ended the link
- end reason

Proxy relationships are ended softly rather than deleted.

---

## Appointments

The backend supports booking, rescheduling, cancellation, clinic-scoped staff access, patient self-service access, and optional nurse assignment.

### Appointment statuses

```text
Scheduled
Confirmed
Pending
Completed
Cancelled
Missed
Rescheduled
```

### Appointment modes

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
- nurse medication workflows

### Medication schedules

Schedules store medication, time of day, and active state.

### Medication logs

Logs store medication, time recorded, taken/not taken state, and optional notes.

### Medication collections

Collections support:

- patient
- clinic
- optional proxy
- processing nurse
- scheduled collection date
- actual collection date
- status
- notes
- collection items

Collection completion is handled transactionally and can include stock deduction and audit logging.

### Collection statuses

```text
Scheduled
Collected
Cancelled
Pending
Overdue
```

---

## Clinic Stock

Clinic stock records include clinic, medication name, strength, form, unit, quantity on hand, reorder level, and active state.

The database prevents duplicate stock rows for the same:

```text
Clinic + MedicationName + Strength + Form
```

Collection processing can deduct quantities from clinic stock.

---

## Notifications

Notifications are associated with users. The backend supports patient/system notifications, including weather-generated health-tip notifications.

System-generated patient notification creation includes duplicate suppression for matching messages within the configured time window.

---

## Symptom Assessment

The symptom-assessment service performs deterministic triage.

Results can be classified into emergency, urgent, or non-emergency paths.

Emergency indicators include phrases related to chest pain, inability to breathe, severe shortness of breath, unconsciousness, not breathing, severe bleeding, seizures, overdose, anaphylaxis, severe allergic reactions, stroke-like symptoms, and suicidal intent.

The feature is patient-only and patient-scoped.

---

## AI Chatbot

PhilaLink includes a patient chatbot with persisted conversation history.

### Main capabilities

- send chatbot messages
- retrieve chatbot history
- clear chatbot history
- load patient context
- persist user and assistant messages
- provider abstraction
- deterministic emergency interception

Configured model:

```text
gemini-3.5-flash-lite
```

### Emergency interception

Before a patient message is sent to Gemini, the backend checks for emergency indicators. If detected, the message is stored, Gemini is not called, a deterministic emergency response is stored, and the patient is instructed to seek emergency medical help immediately.

---

## Weather Integration

PhilaLink integrates with OpenWeather using the coordinates of the patient's assigned clinic.

Current patient routes include:

```text
GET  /api/weather/me/current
GET  /api/weather/me/forecast
POST /api/weather/me/tips
```

The OpenWeather API key remains server-side. Patient latitude and longitude are not required in the patient schema for this feature.

---

## Audit Logging

Sensitive actions are recorded in `AuditLogs` with action, performing user, clinic scope, details, and timestamp.

- SuperAdmin can view system-wide audit history.
- ClinicAdmin can view audit history for the assigned clinic.
- Nurse may generate audit events through permitted actions but cannot directly read audit history.

---

## Important Status Values

### Appointment statuses

```text
Scheduled
Confirmed
Pending
Completed
Cancelled
Missed
Rescheduled
```

### Appointment modes

```text
InPerson
Telehealth
```

### Collection statuses

```text
Scheduled
Collected
Cancelled
Pending
Overdue
```

---

## Configuration and Secrets

Sensitive values should not be committed to Git. Use .NET User Secrets locally and environment variables / platform secrets in deployed environments.

### Configuration keys

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

### Environment-variable equivalents

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

Never commit database passwords, JWT signing keys, Gemini API keys, OpenWeather API keys, SuperAdmin seed passwords, or production connection strings.

---

## Local Development Setup

### Prerequisites

- .NET 10 SDK
- Git
- PostgreSQL / Supabase PostgreSQL access
- EF Core CLI 10.0.10

### Clone

```powershell
git clone https://github.com/Syabonga-dev/PhilaLink.git
cd PhilaLink
```

### Restore

```powershell
dotnet restore .\PersonalProject.csproj
```

### Install EF CLI

```powershell
dotnet tool install --global dotnet-ef --version 10.0.10
```

Verify:

```powershell
dotnet ef --version
```

### Configure local secrets

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_POSTGRES_CONNECTION_STRING" --project .\PersonalProject.csproj

dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_SIGNING_KEY" --project .\PersonalProject.csproj
dotnet user-secrets set "Jwt:Issuer" "PhilaLink" --project .\PersonalProject.csproj

dotnet user-secrets set "AI:ApiKey" "YOUR_GEMINI_API_KEY" --project .\PersonalProject.csproj
dotnet user-secrets set "AI:Model" "gemini-3.5-flash-lite" --project .\PersonalProject.csproj

dotnet user-secrets set "Weather:ApiKey" "YOUR_OPENWEATHER_API_KEY" --project .\PersonalProject.csproj
```

Optional SuperAdmin seed:

```powershell
dotnet user-secrets set "Seed:SuperAdminFullName" "YOUR_NAME" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminIdNumber" "YOUR_ID_VALUE" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminPhoneNumber" "YOUR_PHONE" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminEmail" "YOUR_EMAIL" --project .\PersonalProject.csproj
dotnet user-secrets set "Seed:SuperAdminPassword" "YOUR_STRONG_PASSWORD" --project .\PersonalProject.csproj
```

### Build

```powershell
dotnet build .\PersonalProject.csproj
```

### Format

```powershell
dotnet format .\PersonalProject.csproj
```

### Run

```powershell
dotnet run --project .\PersonalProject.csproj
```

---

## Entity Framework Core Migrations

The backend uses PostgreSQL through Npgsql.

A design-time factory is included at:

```text
Data/PhilaLinkDbContextFactory.cs
```

This allows EF tooling to create the DbContext without starting the full ASP.NET application.

The old SQL Server migration chain has been replaced with a clean PostgreSQL initial migration:

```text
InitialPostgresSchema
```

### List migrations

```powershell
dotnet ef migrations list --project .\PersonalProject.csproj
```

### Create a migration

```powershell
dotnet ef migrations add MigrationName --project .\PersonalProject.csproj
```

### Apply migrations

```powershell
dotnet ef database update --project .\PersonalProject.csproj
```

### Generate SQL without a direct database connection

```powershell
dotnet ef migrations script `
  --project .\PersonalProject.csproj `
  --output .\migration.sql
```

Always inspect generated migrations before applying them to a shared or production database.

---

## Swagger / OpenAPI

Swagger is enabled in development. Bearer authentication is configured in the Swagger definition.

Protected calls use:

```text
Authorization: Bearer <JWT>
```

---

## Security Notes

Current measures include:

- BCrypt password hashing
- JWT authentication
- role-based authorization
- clinic scoping
- patient self-scoping
- proxy relationship checks
- account active-state checks
- patient verification
- forced staff password changes
- strong password rules
- cryptographically generated OTPs
- OTP hashing
- OTP attempt limits
- OTP expiry and resend cooldown
- deterministic chatbot emergency interception
- server-side external API keys
- restrictive foreign-key delete rules
- audit logging
- soft-deactivation patterns

### Current token-revocation limitation

The forced-password-change mechanism is JWT-claim based. After a successful password change, the backend returns a fresh token with `mustChangePassword = false`. The project does not yet implement a token-version or security-stamp mechanism for immediately invalidating every previously issued normal JWT. With the current one-hour JWT lifetime, previously valid tokens can remain valid until expiration.

---

## Current Development Status

Completed backend foundation includes:

- role model
- authentication and authorization
- clinic scoping
- patient self-service
- nurse self-service
- ClinicAdmin self-service
- appointments
- medications
- medication collections
- clinic stock
- proxies
- notifications
- audit logs
- OTP security
- patient verification
- Gemini chatbot
- deterministic chatbot emergency interception
- symptom triage
- OpenWeather integration
- PostgreSQL provider configuration
- clean initial PostgreSQL migration

Next major backend phases:

1. apply and verify the PostgreSQL schema
2. validate tables, foreign keys, indexes, and constraints
3. verify SuperAdmin bootstrap
4. seed realistic fake development/test data
5. performance optimization
6. additional security hardening
7. automated testing
8. observability
9. load testing
10. production-readiness review

---

## Planned Hardening and Optimization

### Database performance

- inspect query plans
- refine composite indexes
- optimize appointment, collection, notification, OTP, proxy, chatbot, and audit queries
- use `AsNoTracking()` for read-only queries
- reduce over-fetching
- avoid N+1 patterns
- add pagination, filtering, and sorting
- use `EXPLAIN ANALYZE`
- use PostgreSQL monitoring such as `pg_stat_statements`

### API performance

- request cancellation
- response compression
- payload-size controls
- connection-pool tuning
- external-service timeout/retry/circuit-breaker strategies
- selective caching
- ETag support where useful

### Security hardening

- login rate limiting
- OTP rate limiting
- chatbot rate limiting
- symptom-assessment rate limiting
- weather endpoint rate limiting
- account-creation rate limiting
- anti-enumeration improvements
- brute-force protection
- production CORS restrictions
- security headers
- centralized production error handling
- secrets review
- dependency vulnerability scanning
- OWASP API Top 10 review
- backup/recovery planning
- incident-response planning
- penetration-test-style verification
- Supabase hardening

### Idempotency priorities

- medication collection completion
- appointment booking
- appointment rescheduling
- staff creation
- proxy assignment
- notification creation

### Quality and observability

- unit tests
- integration tests
- authorization tests
- PostgreSQL integration tests
- health checks
- structured logging
- OpenTelemetry
- metrics
- tracing
- slow-query monitoring
- degradation/recovery monitoring

### Load testing

Planned tools and measurements include Postman/Newman, k6, P50/P95/P99 latency, requests per second, error rate, database latency, pool utilization, CPU/memory, and external-service latency.

---

## Frontend Repository

The frontend is maintained separately:

```text
Syabonga-dev/PhilaLink_Frontend
```

It is a React/Vite application that consumes this API.

---

## Useful Commands

```powershell
# Restore
dotnet restore .\PersonalProject.csproj

# Format
dotnet format .\PersonalProject.csproj

# Build
dotnet build .\PersonalProject.csproj

# Run
dotnet run --project .\PersonalProject.csproj

# EF version
dotnet ef --version

# List migrations
dotnet ef migrations list --project .\PersonalProject.csproj

# Create migration
dotnet ef migrations add MigrationName --project .\PersonalProject.csproj

# Apply migration
dotnet ef database update --project .\PersonalProject.csproj

# Check vulnerable packages
dotnet list .\PersonalProject.csproj package --vulnerable

# Git state
git status
git log --oneline --decorate -10
```

---

## Development Workflow

Recommended workflow:

```text
1. Pull latest main
2. Confirm clean Git state
3. Make one focused backend change
4. Format
5. Build
6. Test
7. Inspect Git diff
8. Commit
9. Push
```

For database changes:

```text
1. Update entities / DbContext
2. Build successfully
3. Generate migration
4. Inspect migration
5. Commit migration
6. Apply migration
7. Verify database schema
8. Test affected API workflows
```

Avoid destructive Git commands when there are unknown local changes.

---

## Project Direction

PhilaLink is being developed toward a backend that is:

```text
Correct
+ Secure
+ Auditable
+ Recoverable
+ Observable
+ Performant
+ Maintainable
```

Because the project handles healthcare-related information, privacy, authorization boundaries, auditability, deterministic emergency handling, and safe operational behavior are treated as first-class backend concerns.

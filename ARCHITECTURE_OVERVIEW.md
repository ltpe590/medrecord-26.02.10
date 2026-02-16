# MedRecord - Medical Records Management System
## Architecture Overview

## 📋 Solution Structure

The **medrecord** solution is a comprehensive medical records management system built with C# and .NET 8.0. It follows a clean architecture pattern with three main projects:

### Projects

1. **Core** - Class library containing business logic and domain entities
2. **WebApi** - ASP.NET Core Web API for backend services
3. **WPF** - Windows Presentation Foundation desktop client application

---

## 🏗️ Core Project Architecture

### Technology Stack
- **.NET 8.0**
- **Entity Framework Core 8.0** with SQL Server
- **ASP.NET Identity** for authentication
- **FluentValidation** for validation logic
- **BCrypt.Net** for password hashing

### Domain Entities

#### **Patient** (`PatientEntity.cs`)
The central entity representing a patient with:
- Basic demographics (Name, Sex, Date of Birth)
- Contact information (Phone, Address)
- Clinical data (Blood Group, Allergies, Short Notes)
- Soft delete capability
- One-to-many relationship with Visits

Key features:
- Immutable properties with private setters
- Protected constructor for EF Core
- Update methods with validation
- Timestamp tracking (CreatedAt, UpdatedAt)

#### **Visit** (`Visit.cs`)
Represents a patient visit/encounter:
- Timestamps (StartedAt, EndedAt, PausedAt)
- Presenting symptoms and duration
- Visit state management (Active/Paused/Ended)
- Vitals (temperature, blood pressure)
- Related VisitEntries for clinical documentation

Features:
- State-based pause/resume functionality
- Support for visit lifecycle management
- Specialty-specific section entries

#### **AppUser** (`AppUser.cs`)
Extends IdentityUser with:
- Activity status tracking
- Last login timestamp
- Specialty profile association
- Fingerprint authentication support

#### Supporting Entities
- **VisitEntry** - Clinical documentation sections
- **ClinicalCatalog** - Clinical systems and sections
- **SpecialtyProfile** - Medical specialty configurations
- **ObstetricHistory** - Obstetric-specific data

---

## 🔧 Services Layer

### Core Services

#### **VisitService**
Manages visit lifecycle and operations:
```csharp
- StartOrResumeVisitAsync() - Handles active/paused visit logic
- PauseVisitAsync() - Pause ongoing visit
- ResumeVisitAsync() - Resume paused visit
- EndVisitAsync() - Complete visit
- SaveVisitAsync() - Persist visit data
- GetVisitHistoryForPatientAsync() - Retrieve patient history
- GetPausedVisitsTodayAsync() - Today's paused visits
- GetStalePausedVisitsAsync() - Old paused visits
```

Key features:
- Prevents multiple active visits per patient
- Handles visit state transitions
- Timezone-aware (Asia/Baghdad)
- Specialty-specific sections (Ob/Gyne)

#### **PatientService/PatientMappingService**
Patient management and data transformation

#### **LoginService**
JWT-based authentication

#### **ProfileService**
Specialty profile management (ObGyne, Ophthalmology, Orthopedic)

#### **UserService**
User account management

---

## 🌐 WebApi Project

### Configuration (`Program.cs`)

#### Database Setup
```csharp
UseSqlServer with connection string from appsettings
Migrations assembly: Core
```

#### Authentication & Authorization
- **ASP.NET Identity** with Entity Framework stores
- **JWT Bearer Authentication**
- Token validation with configurable issuer/audience
- Authorization policies

#### Dependency Injection
- Generic Repository pattern
- Scoped services for business logic
- Singleton specialty profiles
- HTTP client factory for external APIs

#### Middleware Pipeline
1. Exception handling middleware
2. HTTPS redirection
3. CORS (AllowAll policy)
4. Authentication
5. Authorization
6. Health checks at `/health`

#### Swagger Configuration
- JWT Bearer support in Swagger UI
- Persistent authorization
- Request duration display
- API documentation

### Features
- RESTful API endpoints
- JSON serialization with Newtonsoft.Json
- Reference loop handling
- UTC datetime handling
- User seeding on startup

---

## 🖥️ WPF Project

### Desktop Client Application

#### Main Window (`MainWindow.xaml.cs`)
Central UI hub with:
- Login section (expandable)
- Patient management panel
- Visit management interface
- Event-driven architecture

#### ViewModels
- **MainWindowViewModel** - Main application state
- **PatientViewModel** - Patient data representation
- Integration with Core services via dependency injection

#### Views
1. **RegisterPatientWindow** - Patient registration/editing
2. **SettingsWindow** - Application configuration
3. **DebugWindow** - Debugging interface

#### Features
- MVVM pattern implementation
- Async/await for responsive UI
- Error handling with user notifications
- Patient search functionality
- Real-time data binding

---

## 📊 Data Access Layer

### Repository Pattern
```csharp
IGenericRepository<T> - Base CRUD operations
IPatientRepository - Patient-specific queries
IVisitRepository - Visit-specific queries
ITestCatalogRepository - Test catalog management
```

### DbContext
**ApplicationDbContext** with:
- Entity Framework Core DbSets
- Identity integration
- Migration support
- Configured relationships

---

## 🔐 Security

### Authentication
- JWT tokens with symmetric key encryption
- Token lifetime validation
- Refresh token support
- Identity cookie authentication (if needed)

### Password Security
- BCrypt password hashing
- Identity password validators
- Secure password storage

### Authorization
- Role-based access control (via Identity)
- Specialty-based permissions
- User active status checks

---

## 📝 Validation

### FluentValidation
- **PatientCreateDtoValidator**
- Centralized validation rules
- Dependency injection integration
- Automatic validation on API endpoints

### Domain Validators
- **StringValidator** - String validation
- **DateValidator** - Date of birth validation
- Inline validation in domain entities

---

## 🌍 Localization & Timezone

- **Timezone**: Asia/Baghdad
- UTC storage with local conversion
- Clinic day calculations
- Date range utilities

---

## 📦 NuGet Dependencies

### Core Project
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (8.0.0)
- FluentValidation (8.6.3)
- BCrypt.Net-Next (4.0.3)
- Microsoft.Extensions.* (Configuration, Logging, Http)

### WebApi Project
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.OpenApi (Swagger)
- Newtonsoft.Json

---

## 🎯 Specialty Support

### Currently Implemented
1. **Obstetrics & Gynecology (ObGyne)**
   - GPA (Gravida, Para, Abortion) tracking
   - Obstetric history
   - Specialty-specific sections

2. **Ophthalmology**
   - Vision-specific profiles

3. **Orthopedics**
   - Musculoskeletal profiles

### Extensibility
- **ISpecialtyProfile** interface for new specialties
- **ClinicalCatalog** for system aggregation
- Dynamic section initialization

---

## 🔄 Clinical Workflow

### Visit Lifecycle
```
1. Start Visit → Active State
2. Document clinical findings
3. Option to Pause → Paused State
4. Resume from Pause → Active State
5. End Visit → Completed State
```

### Visit States
- **Active** - Currently in progress (EndedAt = null, PausedAt = null)
- **Paused** - Temporarily suspended (EndedAt = null, PausedAt != null)
- **Completed** - Finished (EndedAt != null)

### Data Entry
- Specialty-specific sections
- Only filled sections saved to database
- Support for vitals (temperature, BP)
- Free-text notes and diagnosis

---

## 🗂️ File Organization

### Source Code Snapshots
The `FullSourceCode 26.01` directory contains timestamped snapshots:
- Core module snapshots
- DTOs snapshots
- Entities snapshots
- WebApi snapshots
- WPF snapshots

These serve as backup/history checkpoints during development.

---

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 (recommended)

### Configuration
1. Update connection string in `appsettings.json`
2. Set JWT secret key
3. Run migrations: `dotnet ef database update`
4. Start WebApi project
5. Launch WPF client

### Default Users
Admin user seeded on first run via `UserSeeder.SeedAdminUserAsync()`

---

## 🧩 Design Patterns

1. **Repository Pattern** - Data access abstraction
2. **MVVM** - WPF UI architecture
3. **Dependency Injection** - Service registration and resolution
4. **Factory Pattern** - Specialty profile creation
5. **Unit of Work** - DbContext transaction management
6. **DTO Pattern** - Data transfer objects for API
7. **Validator Pattern** - FluentValidation rules

---

## 📈 Extensibility Points

### Adding New Specialties
1. Implement `ISpecialtyProfile`
2. Register in DI container
3. Add to ClinicalCatalog
4. Define specialty-specific sections

### Adding New Features
- New entities in Core/Entities
- Repository interfaces/implementations
- Service layer methods
- API controllers
- WPF views and viewmodels

---

## 🔍 Key Observations

### Strengths
✅ Clean separation of concerns
✅ SOLID principles adherence
✅ Comprehensive validation
✅ Flexible specialty system
✅ Modern C# features (records, nullable reference types)
✅ Async/await throughout
✅ Timezone awareness

### Areas for Enhancement
⚠️ Consider adding:
- Unit tests
- Logging framework integration
- Caching layer
- Background job processing
- Audit trail
- Report generation
- Document management
- Telemedicine features

---

## 📚 Medical Domain Context

This system supports **outpatient clinic workflows** with:
- Patient registration and demographics
- Visit-based encounters
- Clinical documentation by specialty
- Historical record keeping
- Pause/resume for busy clinic environments

---

## 💡 Notes for Developers

### Coding Conventions
- Private fields with underscore prefix (`_db`)
- Async suffix for async methods
- Protected constructors for EF Core entities
- Validation before state changes
- UTC for all timestamps

### Database Strategy
- Code-first migrations
- Soft deletes for patients
- Navigation properties for relationships
- Value objects for complex types (PhoneNumber, Vitals)

---

## 📞 Support & Contribution

This appears to be an in-development medical records system. The architecture is well-structured for healthcare workflows with room for expansion into:
- Laboratory integration
- Imaging systems (PACS)
- Prescription management
- Billing and insurance
- Appointment scheduling
- Electronic health records (EHR) interoperability

---

*Document generated: February 2025*
*Solution version: .NET 8.0*

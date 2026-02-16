# Global Validators Strategy

All validation logic should be centralized in global validators under `Core/Validators/` instead of being scattered throughout the codebase.

## Available Global Validators

### 1. **StringValidator** - String validation
- `ValidateNotEmpty(value, parameterName)` - Ensures string is not null, empty, or whitespace only

### 2. **DateValidator** - Date/DateTime validation
- `ValidateDateOfBirth(dateOfBirth, parameterName)` - Validates date is not in future and represents realistic age (< 150 years)

### 3. **IntegerValidator** - Integer & ID validation
- `ValidatePositiveId(id, parameterName)` - Validates ID is positive (> 0), supports int? optional IDs
- `ValidateNonNegative(value, parameterName)` - Validates integer is ≥ 0, supports int? optional values
- `ValidateRange(value, min, max, parameterName)` - Validates integer is within inclusive range [min, max]
- `ValidateNotExceeds(value1, value2, value1Name, value2Name)` - Validates value1 ≤ value2, supports int? optional values

### 4. **NullValidator** - Null/object validation
- `ValidateNotNull(obj, parameterName)` - Ensures object reference is not null
- `ValidateNotNullOrEmpty<T>(collection, parameterName)` - Ensures collection is not null and not empty
- `ValidateAllNotNull(params (object, name)[])` - Validates multiple objects at once

### 5. **ObstetricHistoryValidator** - Domain-specific obstetric validation
- `ValidateObstetricMetrics(gravida, para, abortion, lmp)` - Validates all obstetric history metrics with business logic (Para ≤ Gravida)

## Usage Examples

### In Entity Constructors/Methods
```csharp
public class Patient
{
    public Patient(string name, Sex sex, DateOnly dateOfBirth)
    {
        StringValidator.ValidateNotEmpty(name, nameof(name));
        DateValidator.ValidateDateOfBirth(dateOfBirth, nameof(dateOfBirth));
        
        Name = name.Trim();
        DateOfBirth = dateOfBirth;
    }
}
```

### In Services
```csharp
public class PatientService
{
    public PatientService(IPatientRepository repo, ILogger logger)
    {
        NullValidator.ValidateAllNotNull(
            (repo, nameof(repo)),
            (logger, nameof(logger))
        );
        
        _repository = repo;
        _logger = logger;
    }
}
```

### In Domain Logic
```csharp
public static void ProcessVisit(int visitId, int patientId)
{
    IntegerValidator.ValidatePositiveId(visitId, nameof(visitId));
    IntegerValidator.ValidatePositiveId(patientId, nameof(patientId));
    
    // Process visit...
}
```

## Guidelines for Global Validators

✅ **DO**
- Create validators for cross-cutting validation concerns
- Use validators in entity constructors and methods
- Use validators in service methods
- Combine validators for related validations (like ObstetricHistoryValidator)
- Document validators with XML comments and usage examples

❌ **DON'T**
- Scatter validation logic throughout files
- Duplicate validation checks across multiple methods
- Create file-specific validators
- Use inline validation instead of global validators

## When to Create New Validators

Create a new global validator when:
1. The same validation logic appears in 2+ locations
2. The validation represents a domain concept (e.g., ObstetricHistoryValidator)
3. The validation is complex with multiple related checks
4. The validation is used across multiple namespaces or layers

## Future Validator Candidates

Based on current codebase:
- `VisitValidator` - Visit state and parameter validation
- `UrlValidator` - URL format and connection validation
- `DiagnosisValidator` - Diagnosis text validation
- `PercentageValidator` - Range [0-100] integer validation

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
ZiraAI is a .NET 9.0 Web API project for AI-powered plant analysis services. It follows Clean Architecture with CQRS pattern using the Ziraai framework.

## Essential Commands

### Development
```bash
# Build the solution
dotnet build

# Run the API with hot reload
dotnet watch run --project ./WebAPI/WebAPI.csproj

# Run without watch
dotnet run --project ./WebAPI/WebAPI.csproj
```

### Database Management
```bash
# Add a new migration (PostgreSQL)
dotnet ef migrations add MigrationName --project DataAccess --startup-project WebAPI --context ProjectDbContext --output-dir Migrations/Pg

# Update database
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Remove last migration
dotnet ef migrations remove --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test ./Tests/Tests.csproj
```

## Architecture

### Project Structure
- **Core**: Cross-cutting concerns, utilities, base classes, dependency injection setup
- **Entities**: Domain entities, DTOs, and data transfer objects
- **DataAccess**: Entity Framework repositories, database contexts, configurations
- **Business**: CQRS handlers (Commands/Queries), business rules, validation, services
- **WebAPI**: Controllers, API configuration, middleware, startup
- **PlantAnalysisWorkerService**: Background processing service for async analysis
- **Tests**: Unit and integration tests
- **UiPreparation**: Angular web app and Flutter mobile app

### Key Patterns & Conventions

#### CQRS Implementation
- All business operations use MediatR pattern
- Commands: `Business/Handlers/{Entity}/Commands/`
- Queries: `Business/Handlers/{Entity}/Queries/`
- Naming: `Create{Entity}Command`, `Get{Entity}Query`, `Update{Entity}Command`

#### Repository Pattern
- Interface: `DataAccess/Abstract/I{Entity}Repository.cs`
- Implementation: `DataAccess/Concrete/EntityFramework/{Entity}Repository.cs`
- All repositories inherit from `IRepository<T>`

#### Entity Configuration
- EF configurations: `DataAccess/Concrete/Configurations/{Entity}EntityConfiguration.cs`
- Database context: `DataAccess/Concrete/EntityFramework/Contexts/ProjectDbContext.cs`

#### API Controllers
- Location: `WebAPI/Controllers/{Entity}Controller.cs`
- Base class: `BaseApiController` (includes MediatR mediator)
- Standard endpoints: GET (list/detail), POST, PUT, DELETE

## Database Configuration
- Primary: PostgreSQL (connection string: `DArchPgContext`)
- Migrations folder: `DataAccess/Migrations/Pg/`
- Design-time factory: `DataAccess/Concrete/EntityFramework/Contexts/DesignTimeDbContextFactory.cs`

### PostgreSQL Timezone Compatibility
```csharp
// Required in Program.cs for PostgreSQL timestamp handling
System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
System.AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
```

## Authentication & Security
- JWT Bearer authentication with refresh tokens
- Token expiry: 60 minutes (access), 180 minutes (refresh)
- Claims-based authorization
- Role management: Admin, Farmer, Sponsor
- Operation claims for fine-grained permissions

## Service Registration
- Autofac modules:
  - `Core/DependencyResolvers/CoreModule.cs`: Core services
  - `Business/DependencyResolvers/AutofacBusinessModule.cs`: Business services
  - `Business/Startup.cs`: Business layer configuration
  - `WebAPI/Startup.cs`: API configuration

## Key Features

### Plant Analysis System
- **Synchronous Analysis**: Direct N8N webhook integration with immediate response
- **Asynchronous Analysis**: RabbitMQ-based processing with worker service
- **Image Processing**: Intelligent optimization with configurable size targets (default: 0.25MB)
- **URL-Based Processing**: Reduces OpenAI token usage by 99.6% through URL-based image handling
- **Multi-format Support**: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF

### Subscription System
- **Tiers**: Trial, S (Small), M (Medium), L (Large), XL (Extra Large)
- **Usage Tracking**: Daily and monthly request limits with automatic resets
- **Auto-renewal**: Configurable with payment gateway integration
- **Trial Support**: Automatic trial subscription on registration

### Sponsorship System
- **Purchase-Based Model**: Sponsors purchase packages and distribute codes to farmers
- **Tier-Based Features**: Messaging and profile visibility based on tier level
- **Smart Links**: XL tier exclusive feature for advanced link management
- **Analytics**: Comprehensive tracking and reporting

### Dynamic Configuration
- Database-driven configuration with real-time updates
- Memory caching with 15-minute TTL
- Type-safe value getters (decimal, int, bool, string)
- Key categories: ImageProcessing, Application settings

## Important Services

### Core Services
- **PlantAnalysisService**: Synchronous AI plant analysis
- **PlantAnalysisAsyncService**: Asynchronous analysis with RabbitMQ
- **ConfigurationService**: Dynamic database-driven configuration
- **ImageProcessingService**: Intelligent image optimization
- **AuthenticationService**: JWT token generation and validation
- **SubscriptionValidationService**: Quota management and usage tracking

### File Storage
- **Providers**: FreeImageHost, ImgBB, Local, S3
- **Configuration**: `FileStorage` section in appsettings.json
- **Local Storage**: `wwwroot/uploads/plant-images/`

### Messaging & Integration
- **RabbitMQ**: Message queue for async processing
- **N8N Webhook**: AI/ML integration endpoint
- **Redis**: Caching layer
- **Elasticsearch**: Search functionality
- **Hangfire**: Background job processing

## Adding New Features

### Creating a New Entity Flow
1. Create entity in `Entities/Concrete/{Entity}.cs`
2. Add DTOs in `Entities/Dtos/`
3. Create repository interface in `DataAccess/Abstract/`
4. Implement repository in `DataAccess/Concrete/EntityFramework/`
5. Add EF configuration in `DataAccess/Concrete/Configurations/`
6. Update `ProjectDbContext` with new DbSet
7. Create CQRS handlers in `Business/Handlers/{Entity}/`
8. Add controller in `WebAPI/Controllers/`
9. Add migration: `dotnet ef migrations add Add{Entity}`

### Handler Structure Example
```csharp
// Command Handler
public class Create{Entity}CommandHandler : IRequestHandler<Create{Entity}Command, IResult>
{
    private readonly I{Entity}Repository _repository;
    // Implementation
}

// Query Handler  
public class Get{Entity}Query : IRequest<IDataResult<{Entity}>>
{
    // Properties and implementation
}
```

## Environment Configuration
- Development: `appsettings.Development.json`
- Staging: `appsettings.Staging.json`
- Production: `appsettings.json`
- Key setting: `ASPNETCORE_ENVIRONMENT`

## API Documentation
- Swagger UI: `https://localhost:{port}/swagger`
- API versioning via header: `x-dev-arch-version`
- Postman Collection: `ZiraAI_Complete_API_Collection_v6.1.json`
  - 120+ endpoints across 20 controllers
  - Complete request/response examples
  - Automated test scripts
  - Environment variables for easy testing

## Docker Deployment
```bash
# Build image
docker build -t ziraai .

# Run container
docker run -p 80:80 -p 443:443 ziraai
```

## Common Issues & Solutions

### DateTime with PostgreSQL
- Always use `DateTime.Now` instead of `DateTime.UtcNow` for PostgreSQL compatibility
- Global configuration switches are set in Program.cs

### Image Processing
- Maximum size enforced at 0.25MB for AI processing
- Automatic format conversion (PNG→JPEG) for better compression
- Progressive quality reduction to meet size targets

### Subscription Validation
- All plant analysis endpoints require active subscriptions
- Trial subscription automatically created on registration
- Usage logs track all API calls for billing

## Development Guidelines

### Code Conventions
- Follow existing patterns in the codebase
- Use CQRS for all business operations
- Implement validation using FluentValidation
- Add appropriate logging for debugging
- Handle errors gracefully with user-friendly messages

### Testing
- Write unit tests for business logic
- Add integration tests for API endpoints
- Use Postman collection for manual testing
- Verify PostgreSQL compatibility for all DateTime operations

### Security
- Never commit secrets or API keys
- Use environment variables for sensitive configuration
- Implement proper authorization on all endpoints
- Validate all user inputs
- Log security events for auditing

## Useful Resources
- PostgreSQL Connection: `Host=localhost;Port=5432;Database=ziraai_dev;Username=ziraai;Password=devpass`
- RabbitMQ Management: `http://localhost:15672`
- Redis Commander: `http://localhost:8081`
- API Base URL: `https://localhost:5001` (Development)
- N8N Webhook: `http://localhost:5678/webhook/api/plant-analysis`
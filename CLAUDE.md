# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
ZiraAI is a .NET 9.0 Web API project for plant analysis using AI/ML services. It follows Clean Architecture with CQRS pattern using the DevArchitecture framework.

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
- Supports: SQL Server, MySQL, Oracle, MongoDB
- Migrations folder: `DataAccess/Migrations/Pg/`
- Design-time factory: `DataAccess/Concrete/EntityFramework/Contexts/DesignTimeDbContextFactory.cs`

## Authentication & Security
- JWT Bearer authentication with refresh tokens
- Token expiry: 60 minutes (access), 180 minutes (refresh)
- Claims-based authorization
- Role management (Admin, User)
- Operation claims for fine-grained permissions

## Service Registration
- Autofac modules:
  - `Core/DependencyResolvers/CoreModule.cs`: Core services
  - `Business/DependencyResolvers/AutofacBusinessModule.cs`: Business services
  - `Business/Startup.cs`: Business layer configuration
  - `WebAPI/Startup.cs`: API configuration

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

## External Integrations
- **N8N Webhook**: Plant analysis AI integration (configured in appsettings)
- **Redis**: Caching layer
- **RabbitMQ**: Message queue support
- **Elasticsearch**: Search functionality
- **Hangfire**: Background job processing

## Environment Configuration
- Development: `appsettings.Development.json` (may use in-memory DB)
- Staging: `appsettings.Staging.json`
- Production: `appsettings.json`
- Key setting: `ASPNETCORE_ENVIRONMENT`

## Important Services
- **PlantAnalysisService**: `Business/Services/PlantAnalysis/` - Handles AI plant analysis and image file storage
- **AuthenticationService**: JWT token generation and validation
- **CacheService**: Redis and in-memory caching

## Image Storage
- Plant images are stored as physical files in `wwwroot/uploads/plant-images/`
- Database only stores the relative file path for performance
- Images can be accessed via API endpoint: `GET /api/plantanalyses/{id}/image`
- File naming: `plant_analysis_{id}_{timestamp}.{ext}` (extension auto-detected)

### Supported Image Formats
- **JPEG/JPG**: `data:image/jpeg;base64,` → `.jpg`
- **PNG**: `data:image/png;base64,` → `.png`
- **GIF**: `data:image/gif;base64,` → `.gif`
- **WebP**: `data:image/webp;base64,` → `.webp`
- **BMP**: `data:image/bmp;base64,` → `.bmp`
- **SVG**: `data:image/svg+xml;base64,` → `.svg`
- **TIFF**: `data:image/tiff;base64,` → `.tiff`

### Image API Usage
```bash
# Upload with auto-format detection
POST /api/plantanalyses/analyze
{
  "image": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
}

# Get image with correct MIME type
GET /api/plantanalyses/123/image
Content-Type: image/png
```

## Docker Deployment
```bash
# Build image
docker build -t ziraai .

# Run container
docker run -p 80:80 -p 443:443 ziraai
```

## API Documentation
- Swagger UI: `https://localhost:{port}/swagger`
- API versioning via header: `x-dev-arch-version`
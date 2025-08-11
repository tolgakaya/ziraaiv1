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

## Recent Major Features (2025)

### Dynamic Configuration System ✅
- **Implementation Date**: January 2025
- **Purpose**: Database-driven configuration management with real-time updates
- **Key Components**:
  - Configuration entity with CQRS handlers
  - Memory caching (15-min TTL)
  - Type-safe value getters (decimal, int, bool, string)
  - Seed data with default values
- **Benefits**: Runtime configuration changes without deployment

### Intelligent Image Processing ✅
- **Implementation Date**: January 2025
- **Purpose**: AI-optimized image processing with guaranteed file size targets
- **Key Features**:
  - Target file size guarantee (default: 0.25MB)
  - Iterative optimization with up to 10 attempts
  - Automatic PNG→JPEG conversion
  - Progressive quality reduction (85→70→50→30)
  - Dimension scaling (progressive 80% reduction)
  - Mobile photo optimization (5-10MB → 0.25MB)
- **AI Integration**: Prevents token limit issues in N8N workflow
- **Error Handling**: User-friendly messages for processing failures

### Plant Analysis with File Storage ✅
- **Implementation Date**: January 2025
- **Purpose**: Complete plant analysis workflow with image management
- **Features**:
  - Multi-format image support (JPEG, PNG, GIF, WebP, BMP, SVG, TIFF)
  - Physical file storage with database path references
  - N8N webhook integration for AI processing
  - Comprehensive metadata capture (GPS, weather, crop info)
  - Image retrieval API with correct MIME types

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
- **PlantAnalysisService**: `Business/Services/PlantAnalysis/` - Handles AI plant analysis and image file storage with intelligent processing
- **ConfigurationService**: `Business/Services/Configuration/` - Dynamic database-driven configuration with memory caching (15-min TTL)
- **ImageProcessingService**: `Business/Services/ImageProcessing/` - Intelligent image optimization with target file size guarantee
- **AuthenticationService**: JWT token generation and validation
- **CacheService**: Redis and in-memory caching

## Dynamic Configuration System

### Configuration Entity
- Database-driven configuration with real-time updates
- Memory caching with 15-minute TTL for performance
- Type-safe getters: decimal, int, bool, string values
- CQRS handlers for full configuration management

### Key Configuration Categories
- **ImageProcessing**: Image size limits, auto-resize settings, quality parameters
- **Application**: N8N webhook settings, timeout configurations

### Configuration Keys (Constants)
```csharp
// Image Processing
IMAGE_MAX_SIZE_MB = "IMAGE_MAX_SIZE_MB" // Default: 0.25MB (AI optimized)
IMAGE_ENABLE_AUTO_RESIZE = "IMAGE_ENABLE_AUTO_RESIZE" // Default: true
IMAGE_MAX_WIDTH = "IMAGE_MAX_WIDTH" // Default: 1920px
IMAGE_MAX_HEIGHT = "IMAGE_MAX_HEIGHT" // Default: 1080px
IMAGE_RESIZE_QUALITY = "IMAGE_RESIZE_QUALITY" // Default: 85
```

### Usage Example
```csharp
// Service injection
var maxSize = await _configurationService.GetDecimalValueAsync(
    ConfigurationKeys.ImageProcessing.MaxImageSizeMB, 0.25m);
    
// CQRS pattern
var result = await _mediator.Send(new GetConfigurationQuery { Key = "IMAGE_MAX_SIZE_MB" });
```

## Intelligent Image Processing

### Smart File Size Management
- **Target Size Guarantee**: Ensures images meet exact file size requirements
- **Iterative Optimization**: Up to 10 attempts with progressive quality/dimension reduction
- **Format Conversion**: Automatic PNG→JPEG for better compression
- **Mobile-Ready**: Optimizes large mobile photos (5-10MB → 0.25MB)

### Optimization Strategy
1. **Quality Reduction**: 85 → 70 → 50 → 30
2. **Dimension Scaling**: Progressive 80% reduction
3. **Format Conversion**: PNG to JPEG when beneficial
4. **Fallback**: Returns best attempt if target unreachable

### Image Processing Flow
```
Mobile Photo (8MB) →
Attempt 1: PNG→JPEG, Q85 → 3MB
Attempt 2: Q75 → 1.5MB  
Attempt 3: Q65 → 0.8MB
Attempt 4: Q55 → 0.4MB ✅ (target: 0.25MB)
```

### AI Agent Token Optimization
- **Maximum Size**: 0.25MB (base64: ~0.33MB) to prevent token limit issues
- **Quality Balance**: Maintains acceptable quality while ensuring AI compatibility
- **Error Prevention**: Eliminates "context length exceeded" errors in AI processing

## Image Storage & Processing

### File Storage
- Physical files: `wwwroot/uploads/plant-images/`
- Database: Relative file path only (performance optimization)
- API access: `GET /api/plantanalyses/{id}/image`
- Naming: `plant_analysis_{id}_{timestamp}.{ext}`

### Processing Pipeline
1. **Validation**: ValidImageAttribute with 500MB limit (extreme cases)
2. **Intelligent Processing**: Service-layer size management
3. **Target Optimization**: Resize to configured limit (default: 0.25MB)
4. **File Storage**: Save optimized image to wwwroot
5. **AI Integration**: Send processed image to N8N webhook

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
# Upload with intelligent processing
POST /api/plantanalyses/analyze
{
  "image": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
  # Large images automatically resized to 0.25MB
  # PNG converted to JPEG if beneficial
  # Quality optimized for AI processing
}

# Get processed image
GET /api/plantanalyses/123/image
Content-Type: image/jpeg # May differ from original

# Error responses for oversized images
{
  "success": false,
  "message": "Image too large even after auto-resize. Original: 0.93MB, Resized: 0.93MB, Maximum: 0.25MB"
}
```

### Configuration API
```bash
# Get configuration value
GET /api/configurations?key=IMAGE_MAX_SIZE_MB

# Update configuration  
PUT /api/configurations/1
{
  "key": "IMAGE_MAX_SIZE_MB",
  "value": "0.25",
  "description": "Maximum image size for AI processing"
}
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

## Asynchronous Plant Analysis Microservice Architecture

### Overview
Complete microservice implementation for asynchronous plant analysis processing with RabbitMQ messaging and Hangfire job processing. This architecture provides scalable, reliable, and production-ready async processing capabilities.

### Architecture Components

#### 1. WebAPI Service (Publisher)
- **Role**: API Gateway + RabbitMQ Publisher
- **Endpoints**: 
  - `POST /api/plantanalyses/analyze-async` - Queue analysis request
  - `GET /api/test/rabbitmq-health` - Health check
  - `POST /api/test/mock-n8n-response` - Testing endpoint
- **Features**: Image processing, validation, message publishing

#### 2. PlantAnalysisWorkerService (Consumer + Jobs)
- **Role**: Background message processing with Hangfire jobs
- **Components**:
  - RabbitMQ Consumer (persistent connection)
  - Hangfire job processing
  - Database operations
  - Notification system
- **Dashboard**: `/hangfire-worker` with authentication

### Message Flow
```
Client Request → WebAPI → RabbitMQ Queue → Worker Service → Hangfire Jobs → Database + Notifications
```

### RabbitMQ Configuration (appsettings.json)
```json
{
  "RabbitMQ": {
    "ConnectionString": "amqp://guest:guest@localhost:5672/",
    "Queues": {
      "PlantAnalysisRequest": "plant-analysis-requests",
      "PlantAnalysisResult": "plant-analysis-results",
      "Notification": "notifications"
    },
    "RetrySettings": {
      "MaxRetryAttempts": 3,
      "RetryDelayMilliseconds": 1000
    },
    "ConnectionSettings": {
      "RequestedHeartbeat": 60,
      "NetworkRecoveryInterval": 10
    }
  }
}
```

### Hangfire Job Services
```csharp
// Main processing job with retry
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
public async Task ProcessPlantAnalysisResultAsync(
    PlantAnalysisAsyncResponseDto result, string correlationId)

// Notification job  
[AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 10, 30 })]
public async Task SendNotificationAsync(PlantAnalysisAsyncResponseDto result)
```

### Key Features

#### 🏗️ Microservice Benefits
- **Independent Scaling**: Scale worker and API separately
- **Fault Tolerance**: API failure doesn't affect processing
- **Resource Optimization**: Dedicated resources per service
- **Deployment Flexibility**: Independent deployment cycles

#### 🔧 Technical Excellence
- **ServiceTool Issue Resolved**: Lazy initialization for aspects
- **Message Durability**: Persistent queues with acknowledgment
- **Error Handling**: Comprehensive retry mechanisms
- **Monitoring**: Hangfire dashboard with job tracking

#### 📊 Production Features
- **Health Checks**: RabbitMQ connection monitoring
- **Environment Config**: Development/Staging/Production settings
- **Testing Infrastructure**: Mock services and comprehensive test guides
- **Logging**: Detailed logging throughout the pipeline

### Deployment

#### Development Setup
```bash
# Start RabbitMQ (Docker)
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Start WebAPI
cd WebAPI && dotnet run

# Start Worker Service  
cd PlantAnalysisWorkerService && dotnet run
```

#### Production Considerations
- Use dedicated RabbitMQ cluster with credentials
- Configure Hangfire with connection pooling
- Set up monitoring for queue lengths and job failures
- Implement circuit breakers for external dependencies

### Testing
- **Test Setup Guide**: `TEST_SETUP.md`
- **Test Results**: `TEST_RESULTS.md` 
- **PowerShell Scripts**: `test_async_api.ps1`
- **Mock Services**: TestController with N8N response simulation

This microservice architecture provides enterprise-grade asynchronous processing with proper separation of concerns, reliability, and monitoring capabilities.
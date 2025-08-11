# DevArchitecture

DevArchitecture Open Source Rapid Application Framework for .Net 7

For full documentation support [DevArchitecture](https://www.devarchitecture.net)

[DevArchitecture Visual Studio Extensions](https://marketplace.visualstudio.com/search?term=devarchitecture&target=VS&category=All%20categories&vsVersion=&sortBy=Relevance)

![](https://www.devarchitecture.net/assets/images/image1-ce8537e256c57d119ad5559b6217d4c9.png)

# Support the DevArchitecture 

If you liked DevArchitecture Open Source Rapid Application Framework for .Net 7? 

## Please give a star to this repository ⭐

# Build Project with Any Terminal

``> dotnet build``

``
Build succeeded.
0 Warning(s)
0 Error(s)
``

# Run Api Project with Any Terminal
``> dotnet dev-certs https --trust``
``> dotnet watch run --project ./WebAPI/WebAPI.csproj``

# ZiraAI - Plant Analysis System

This project extends DevArchitecture with a comprehensive plant analysis system featuring:

## 🌱 Core Features

### Plant Analysis
- **AI-Powered Analysis**: Integration with N8N webhooks for advanced plant disease detection
- **Image Processing**: Automatic image resizing, format conversion, and validation
- **Multi-format Support**: JPEG, PNG, GIF, WebP, BMP, SVG, TIFF
- **Base64 Processing**: Secure image upload via data URI scheme

### Configuration System
- **Dynamic Settings**: Runtime-configurable application settings
- **Image Processing Controls**: Configurable size limits, quality settings, and auto-resize
- **Category-based Organization**: Grouped settings for easy management
- **Memory Caching**: High-performance configuration access with 15-minute TTL

### Advanced Image Processing
- **Smart Resizing**: Automatic image resizing based on configured limits
- **Quality Control**: Configurable JPEG compression quality
- **Dimension Validation**: Min/max width and height constraints
- **Format Detection**: Automatic image format identification
- **File Size Limits**: Configurable minimum and maximum file sizes

## 🏗️ Architecture

Built on Clean Architecture principles with:

- **CQRS Pattern**: Command/Query separation using MediatR
- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic encapsulation  
- **Dependency Injection**: Autofac container management
- **Entity Framework Core**: PostgreSQL integration with migrations

## 📊 Database Schema

### Plant Analysis
- Comprehensive plant analysis data with 30+ fields
- N8N response integration with detailed analysis results
- Image path storage with metadata
- User association and timestamps

### Configuration System  
- Dynamic key-value configuration storage
- Category-based organization
- Type-safe value handling (int, decimal, bool, string)
- Audit trail with created/updated tracking

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL database
- Optional: N8N instance for AI analysis

### Setup
1. **Clone and Build**
   ```bash
   git clone <repository>
   dotnet build
   ```

2. **Database Migration**
   ```bash
   dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext
   ```

3. **Configuration**
   - Update `appsettings.json` with your database connection
   - Set N8N webhook URL in configuration table
   - Adjust image processing settings as needed

4. **Run Application**
   ```bash
   dotnet watch run --project ./WebAPI/WebAPI.csproj
   ```

## 🔧 Configuration

### Image Processing Settings
- `IMAGE_MAX_SIZE_MB`: Maximum upload size (default: 50.0MB, supports decimal like 0.5MB)
- `IMAGE_MAX_WIDTH/HEIGHT`: Maximum dimensions (default: 1920x1080)
- `IMAGE_ENABLE_AUTO_RESIZE`: Enable automatic resizing (default: true)
- `IMAGE_RESIZE_QUALITY`: JPEG quality 1-100 (default: 85)

### Application Settings  
- `N8N_WEBHOOK_URL`: AI analysis endpoint
- `N8N_TIMEOUT_SECONDS`: Request timeout (default: 300)

## 📚 API Documentation

### Plant Analysis
```http
POST /api/plantanalyses/analyze
Content-Type: application/json

{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  "farmerId": "FARM001",
  "location": "Greenhouse A",
  "cropType": "Tomato"
}
```

### Configuration Management
```http
GET /api/configurations
GET /api/configurations?category=ImageProcessing  
POST /api/configurations
PUT /api/configurations/{id}
```

## 📖 Documentation

- **[Configuration System Documentation](./CONFIGURATION_SYSTEM.md)** - Comprehensive guide to configuration and image processing features
- **[API Reference](./WebAPI/Controllers/)** - Controller documentation and endpoints
- **[Architecture Overview](./Business/)** - Business logic and service patterns

## 🔒 Security Features

- **JWT Authentication**: Secure API access
- **Role-based Authorization**: Admin-only configuration management
- **Image Validation**: Magic byte checking and format verification
- **Input Sanitization**: Comprehensive validation attributes
- **Audit Trails**: Complete change tracking

## 🎯 Performance Optimizations

- **Memory Caching**: Configuration values cached for 15 minutes
- **Lazy Processing**: Images resized only when necessary  
- **Database Indexing**: Optimized queries with proper indexes
- **Resource Management**: Proper disposal patterns throughout

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is built on DevArchitecture and follows the same licensing terms.

## 🆘 Support

For issues related to:
- **DevArchitecture Framework**: Visit [DevArchitecture](https://www.devarchitecture.net)
- **Plant Analysis Features**: Check the [Configuration System Documentation](./CONFIGURATION_SYSTEM.md)
- **Bug Reports**: Open an issue in this repository
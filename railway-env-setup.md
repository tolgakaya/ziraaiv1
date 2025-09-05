# Railway Environment Variables Setup

Copy these environment variables to your Railway project dashboard:

## Database Configuration
```
ConnectionStrings__DArchPgContext=Host=postgres.railway.internal;Port=5432;Database=railway;Username=postgres;Password=YOUR_DB_PASSWORD

# Or use the full PostgreSQL URL format if Railway provides it:
DATABASE_URL=postgresql://postgres:password@postgres.railway.internal:5432/railway
```

## .NET Core Configuration
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
DOTNET_RUNNING_IN_CONTAINER=true
```

## Application Settings (optional)
```
JWT__Secret=your-jwt-secret-key-here
JWT__Issuer=ZiraAI
JWT__Audience=ZiraAI-Users
```

## Redis Configuration (if using Redis)
```
Redis__ConnectionString=redis://redis.railway.internal:6379
```

## RabbitMQ Configuration (if using RabbitMQ)
```
RabbitMQ__ConnectionString=amqp://guest:guest@rabbitmq.railway.internal:5672
```

## File Storage Configuration
```
FileStorage__Provider=FreeImageHost
```

## Notes:
1. Replace YOUR_DB_PASSWORD with actual password from Railway PostgreSQL service
2. Use railway.internal domain for internal service communication
3. Railway automatically provides PORT environment variable (defaults to 8080)
4. Double underscore (__) notation is used for nested configuration sections in .NET
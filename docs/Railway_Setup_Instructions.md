# Railway Setup Instructions for ZiraAI Production Deployment

## Overview
This guide walks you through setting up Railway for ZiraAI production deployment with your existing PostgreSQL database.

## Prerequisites ✅
- [x] Railway account created
- [x] PostgreSQL database created in Railway: `postgresql://postgres:rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ@yamabiko.proxy.rlwy.net:41760/railway`
- [x] GitHub repository with Railway deployment configuration files
- [x] Build errors resolved

## Step 1: Create Railway Project

1. **Login to Railway**: Go to [railway.app](https://railway.app) and log in
2. **Create New Project**: Click "New Project" → "Deploy from GitHub repo"
3. **Select Repository**: Choose your ZiraAI repository
4. **Configure Deployment**:
   - **Service Name**: `ziraai-api`
   - **Branch**: `master`
   - **Build Method**: Docker (automatically detected from Dockerfile)

## Step 2: Configure Environment Variables

In your Railway project dashboard, go to **Variables** tab and add these:

### Required Environment Variables
```bash
# Database Configuration
DATABASE_URL=postgresql://postgres:rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ@yamabiko.proxy.rlwy.net:41760/railway

# Railway Configuration
RAILWAY_PUBLIC_DOMAIN=${{RAILWAY_STATIC_URL}}
PORT=8080

# ASP.NET Core Configuration  
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080

# Security (Generate secure values)
JWT_SECRET_KEY=your-jwt-secret-key-here
SPONSOR_REQUEST_SECRET=your-sponsor-request-secret-here

# External Services (Add your keys)
N8N_WEBHOOK_URL=your-n8n-webhook-url
FREEIMAGEHOST_API_KEY=your-freeimagehost-api-key

# RabbitMQ (Optional for async processing)
RABBITMQ_URL=amqp://guest:guest@localhost:5672/

# Redis (Optional for caching)
REDIS_HOST=localhost
REDIS_PORT=6379
REDIS_PASSWORD=
```

### Variable Configuration Notes
- `RAILWAY_PUBLIC_DOMAIN`: Use Railway's built-in `${{RAILWAY_STATIC_URL}}` variable
- `DATABASE_URL`: Your existing PostgreSQL connection string
- Generate secure JWT and request secrets (32+ character random strings)

## Step 3: Deploy to Railway

1. **Trigger Deployment**: 
   - Push commits to master branch
   - Railway will automatically detect changes and deploy
   - Monitor deployment logs in Railway dashboard

2. **Verify Deployment**:
   - Wait for successful build (Docker build process)
   - Check deployment logs for any errors
   - Test health check endpoint: `https://your-app.up.railway.app/health`

## Step 4: GitHub Actions Integration (Optional)

The GitHub Actions workflow (`.github/workflows/railway-deploy.yml`) will:
- Build and test on every push to master
- Validate Docker build succeeds
- Ensure health check endpoint works

## Step 5: Database Migration

Once deployed successfully, run database migrations:

```bash
# Option 1: Railway CLI (Install railway CLI first)
railway run dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext

# Option 2: Connect to Railway and run locally
# Set DATABASE_URL locally and run:
dotnet ef database update --project DataAccess --startup-project WebAPI --context ProjectDbContext
```

## Expected Results

After successful deployment:
- ✅ **Health Check**: `GET /health` returns 200 OK
- ✅ **API Documentation**: `GET /swagger` accessible
- ✅ **Database Connected**: API can connect to PostgreSQL
- ✅ **Auto-Deploy**: Future commits to master trigger automatic deployment

## Troubleshooting

### Common Issues
1. **Build Failures**: Check Railway build logs for missing dependencies or build errors
2. **Database Connection**: Verify DATABASE_URL format and credentials
3. **Port Issues**: Ensure app listens on `0.0.0.0:8080` (configured in Dockerfile)
4. **Environment Variables**: Double-check all required variables are set

### Debug Endpoints
- **Health Check**: `/health` - Basic status check
- **Detailed Health**: `/health/detailed` - Extended system information including Railway deployment details
- **Swagger**: `/swagger` - API documentation

## Production Checklist

Before going live:
- [ ] Set strong JWT_SECRET_KEY and SPONSOR_REQUEST_SECRET
- [ ] Configure proper N8N_WEBHOOK_URL for AI processing
- [ ] Set up RabbitMQ and Redis for full async capabilities
- [ ] Configure monitoring and logging
- [ ] Set up custom domain (optional)
- [ ] Configure SSL certificate (Railway provides automatic HTTPS)

## Railway CLI Commands (Optional)

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login and link project
railway login
railway link

# Deploy manually
railway up

# View logs
railway logs

# Open project in browser
railway open
```

## Next Steps

1. **Complete Railway Setup**: Follow steps 1-3 above
2. **Test Deployment**: Verify health endpoint and API functionality
3. **Configure Production Settings**: Add all required environment variables
4. **Run Database Migration**: Update database schema in production
5. **Monitor Deployment**: Watch Railway logs for any issues

Your ZiraAI API will be production-ready on Railway with automatic deployments from GitHub!
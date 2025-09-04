# Railway Production Deployment Guide for ZiraAI

## 🚂 Railway Deployment Overview

This guide provides step-by-step instructions to deploy ZiraAI to Railway with GitHub integration and your existing PostgreSQL database.

### Prerequisites
- ✅ GitHub repository: https://github.com/tolgakaya/ziraaiv1
- ✅ PostgreSQL database: `postgresql://postgres:rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ@yamabiko.proxy.rlwy.net:41760/railway`
- ✅ Railway account

## 📦 **Step 1: Railway Project Setup**

### 1.1 Create New Project
1. Go to [Railway Dashboard](https://railway.com/dashboard)
2. Click **"+ New Project"**
3. Select **"Deploy from GitHub Repo"**
4. Choose `tolgakaya/ziraaiv1` repository
5. Select **master** branch

### 1.2 Service Configuration
1. Railway will detect the **Dockerfile** automatically
2. Service will be named `ziraaiv1` by default
3. Railway will start building immediately

## 🔐 **Step 2: Environment Variables Configuration**

### 2.1 Required Environment Variables

Navigate to your Railway service → **Variables** tab and add:

#### **Database Configuration**
```bash
DATABASE_URL=postgresql://postgres:rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ@yamabiko.proxy.rlwy.net:41760/railway
```

#### **Application Configuration**
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
RAILWAY_PUBLIC_DOMAIN=https://your-service-name.up.railway.app
```

#### **Required Secrets (Mark as Sealed Variables)**
```bash
JWT_SECRET=your-super-secure-jwt-secret-key-32-characters-min
SPONSOR_REQUEST_SECRET=ZiraAI-SponsorRequest-SecretKey-2025!@#
```

#### **External Integrations**
```bash
N8N_WEBHOOK_URL=https://your-n8n-instance.com/webhook/plant-analysis
FREEIMAGEHOST_API_KEY=6d207e02198a847aa98d0a2a901485a5
```

#### **Optional Services (If Needed)**
```bash
# Redis (if using caching)
REDIS_HOST=your-redis-host
REDIS_PORT=6379
REDIS_PASSWORD=your-redis-password

# RabbitMQ (if using async processing)
RABBITMQ_URL=amqp://user:password@host:port/
```

### 2.2 Variable Configuration Process

1. **Public Variables**: Add directly in Variables tab
2. **Sealed Variables** (Secrets): 
   - Click **"+ New Variable"**
   - Name: `JWT_SECRET`
   - Value: Your secret
   - ☑️ **Check "Sealed Variable"** 
   - Click **"Add"**

## 🌐 **Step 3: Domain Configuration**

### 3.1 Generate Railway Domain
1. Go to **Settings** → **Networking**
2. Click **"Generate Domain"**
3. Copy the generated domain (e.g., `ziraai-production.up.railway.app`)
4. Update `RAILWAY_PUBLIC_DOMAIN` variable with this domain

### 3.2 Custom Domain (Optional)
If you have a custom domain:
1. **Settings** → **Networking** → **Custom Domains**
2. Click **"+ Custom Domain"**
3. Enter your domain: `api.ziraai.com`
4. Add CNAME record in your DNS:
   ```
   CNAME api.ziraai.com → your-service.up.railway.app
   ```

## 📊 **Step 4: Database Migration**

### 4.1 Initial Migration
Railway'de ilk deployment sonrası database migration'ları çalıştırmak için:

```bash
# Local migration command for Railway database
dotnet ef database update \
  --project DataAccess \
  --startup-project WebAPI \
  --context ProjectDbContext \
  --connection "postgresql://postgres:rcrHmHyxJLKYacWzzJoqVRwtJadyEBDQ@yamabiko.proxy.rlwy.net:41760/railway"
```

### 4.2 Production Migration Process
Railway deployment'tan sonra:
1. Railway service logs'unu izleyin
2. Migration errors varsa Railway console'dan debug edin
3. Gerekirse Railway service'i restart edin

## ⚙️ **Step 5: GitHub Actions for Railway Deployment**

Railway GitHub integration ile otomatik deployment yapar, ancak ek kontrol için GitHub Actions ekleyebiliriz:

### 5.1 Railway GitHub Integration
1. Railway project → **Settings** → **Source**
2. **GitHub Repository**: `tolgakaya/ziraaiv1`
3. **Branch**: `master`
4. **Auto Deploy**: ✅ Enabled

### 5.2 Enhanced GitHub Actions (Optional)
Create `.github/workflows/railway-deploy.yml`:

```yaml
name: Railway Production Deployment

on:
  push:
    branches: [master]
  pull_request:
    branches: [master]

env:
  DOTNET_VERSION: '9.0.x'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Cache NuGet packages
      uses: actions/cache@v3
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --logger trx
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: TestResults

  railway-deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/master'
    steps:
    - name: Deploy to Railway
      run: |
        echo "✅ Tests passed - Railway will auto-deploy from GitHub integration"
        echo "🚂 Monitor deployment: https://railway.com/project/your-project-id"
```

## 🏥 **Step 6: Health Check Endpoint**

Railway health check için endpoint eklemek gerekiyor. WebAPI'ye ekleyelim:

### 6.1 Health Check Controller
Create minimal health endpoint in your existing controllers:

```csharp
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
```

## 📋 **Complete Deployment Checklist**

### **Railway Platform Setup**
- [ ] Create Railway project from GitHub repo
- [ ] Configure service auto-deploy from master branch
- [ ] Generate public domain
- [ ] Setup custom domain (optional)

### **Environment Variables**
- [ ] `DATABASE_URL` - PostgreSQL connection string
- [ ] `RAILWAY_PUBLIC_DOMAIN` - Your Railway domain
- [ ] `JWT_SECRET` - Sealed variable for JWT signing
- [ ] `SPONSOR_REQUEST_SECRET` - Sealed variable for HMAC
- [ ] `N8N_WEBHOOK_URL` - N8N integration URL
- [ ] `FREEIMAGEHOST_API_KEY` - Image hosting API key

### **Database Setup**
- [ ] Run initial EF migrations against Railway PostgreSQL
- [ ] Verify database schema creation
- [ ] Seed initial data (subscription tiers, etc.)

### **Testing & Verification**
- [ ] Verify application starts without errors
- [ ] Test API endpoints via Railway domain
- [ ] Verify database connectivity
- [ ] Test file upload functionality
- [ ] Validate JWT authentication

### **Monitoring Setup**
- [ ] Monitor Railway deployment logs
- [ ] Setup error notification webhooks
- [ ] Configure log retention policies
- [ ] Monitor resource usage and scaling

## 🚀 **Production Deployment Timeline**

### **Phase 1: Initial Deployment (Day 1)**
1. Create Railway project (15 min)
2. Configure environment variables (30 min)
3. First deployment and testing (45 min)
4. Database migration and verification (30 min)

### **Phase 2: Domain & SSL (Day 2)**
1. Domain configuration and DNS setup (2-24 hours for propagation)
2. SSL certificate verification
3. Production testing with real domain

### **Phase 3: Monitoring & Optimization (Day 3-5)**
1. Performance monitoring setup
2. Error tracking configuration
3. Resource usage optimization
4. Load testing and scaling validation

## ⚠️ **Important Considerations**

### **Security**
- Use **Sealed Variables** for all secrets
- Never commit sensitive data to GitHub
- Regularly rotate JWT secrets
- Monitor security logs

### **Performance**
- Railway auto-scales based on usage
- Monitor memory and CPU usage
- Consider Redis for caching if needed
- Optimize Docker image size

### **Cost Management**
- Start with Railway Hobby plan ($5/month)
- Monitor resource usage
- Scale based on actual traffic
- Consider Pro plan for production workloads

### **Backup Strategy**
- PostgreSQL: Railway provides automatic backups
- File uploads: Consider external storage for production
- Database export scheduled backups
- Environment variables backup

## 📞 **Emergency Procedures**

### **Rollback Process**
1. Railway dashboard → Service → Deployments
2. Select previous working deployment
3. Click **"Rollback"**
4. Monitor health check endpoint

### **Database Recovery**
1. Railway PostgreSQL service → **Data** tab
2. **Restore from backup**
3. Select backup timestamp
4. Confirm restoration

### **Support Channels**
- Railway Discord: [discord.gg/railway](https://discord.gg/railway)
- Railway Support: help@railway.com
- GitHub Issues: tolgakaya/ziraaiv1

## 🎯 **Success Metrics**

After deployment, monitor these KPIs:
- **Uptime**: Target 99.9%
- **Response Time**: API < 500ms
- **Error Rate**: < 0.1%
- **Build Time**: < 5 minutes
- **Deployment Time**: < 2 minutes

---

**Ready for Production**: This configuration provides enterprise-grade deployment with automatic scaling, monitoring, and security best practices.
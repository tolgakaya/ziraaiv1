# Frontend Multi-Environment Deployment Guide

Bu doküman, backend .NET uygulamasındaki deployment yapısını inceleyerek React frontend uygulaması için çoklu ortam (staging/production) deployment rehberi sunmaktadır.

---

## 1. Backend (.NET) Deployment Yapısı Analizi

### 1.1 Dockerfile Stratejisi

Backend'de 3 farklı Dockerfile kullanılıyor:

| Dosya | Ortam | Özellikler |
|-------|-------|------------|
| `Dockerfile` | Generic | Build argument ile ortam seçimi |
| `Dockerfile.staging` | Staging | Staging-specific optimizasyonlar |
| `Dockerfile.production` | Production | Security hardening, non-root user |

### 1.2 Ortam Bazlı Farklılıklar

**Staging (`Dockerfile.staging`):**
```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Staging
ENV Logging__LogLevel__Default=Information
ENV FileStorage__Provider=FreeImageHost
```

**Production (`Dockerfile.production`):**
```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Logging__LogLevel__Default=Warning
ENV FileStorage__Provider=S3
# Security: Non-root user
RUN groupadd -r ziraai && useradd --no-log-init -r -g ziraai ziraai
USER ziraai
```

### 1.3 Konfigürasyon Dosyaları

Backend'de her ortam için ayrı `appsettings.{Environment}.json` dosyası var:
- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

**Ortama Özel Değişkenler:**
- API Base URL
- Database connection strings
- External service API keys
- Logging levels
- Feature flags

---

## 2. React Frontend için Deployment Yapısı

### 2.1 Dosya Yapısı

```
frontend-app/
├── Dockerfile
├── Dockerfile.staging
├── Dockerfile.production
├── nginx.conf
├── nginx.staging.conf
├── nginx.production.conf
├── .env.development
├── .env.staging
├── .env.production
├── src/
└── public/
```

### 2.2 Environment Dosyaları

**.env.development:**
```env
REACT_APP_ENV=development
REACT_APP_API_BASE_URL=https://localhost:5001
REACT_APP_WEBSOCKET_URL=wss://localhost:5001/hubs
REACT_APP_LOG_LEVEL=debug
REACT_APP_ENABLE_DEVTOOLS=true

# Feature Flags
REACT_APP_ENABLE_ANALYTICS=false
REACT_APP_ENABLE_ERROR_REPORTING=false
```

**.env.staging:**
```env
REACT_APP_ENV=staging
REACT_APP_API_BASE_URL=https://ziraai-api-sit.up.railway.app
REACT_APP_WEBSOCKET_URL=wss://ziraai-api-sit.up.railway.app/hubs
REACT_APP_LOG_LEVEL=info
REACT_APP_ENABLE_DEVTOOLS=false

# Feature Flags
REACT_APP_ENABLE_ANALYTICS=true
REACT_APP_ENABLE_ERROR_REPORTING=true

# External Services
REACT_APP_SENTRY_DSN=https://your-staging-sentry-dsn
REACT_APP_GA_TRACKING_ID=UA-STAGING-ID
```

**.env.production:**
```env
REACT_APP_ENV=production
REACT_APP_API_BASE_URL=https://api.ziraai.com
REACT_APP_WEBSOCKET_URL=wss://api.ziraai.com/hubs
REACT_APP_LOG_LEVEL=error
REACT_APP_ENABLE_DEVTOOLS=false

# Feature Flags
REACT_APP_ENABLE_ANALYTICS=true
REACT_APP_ENABLE_ERROR_REPORTING=true

# External Services
REACT_APP_SENTRY_DSN=https://your-production-sentry-dsn
REACT_APP_GA_TRACKING_ID=UA-PRODUCTION-ID

# Performance
REACT_APP_ENABLE_SERVICE_WORKER=true
```

---

## 3. Docker Konfigürasyonu

### 3.1 Base Dockerfile (Generic)

```dockerfile
# Multi-Environment React Dockerfile
# Build arguments for environment configuration
ARG TARGET_ENVIRONMENT=staging
ARG NODE_VERSION=20

# Build stage
FROM node:${NODE_VERSION}-alpine AS build
ARG TARGET_ENVIRONMENT
WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm ci --only=production=false

# Copy source code
COPY . .

# Copy environment-specific config
COPY .env.${TARGET_ENVIRONMENT} .env

# Build for target environment
RUN npm run build

# Production stage with Nginx
FROM nginx:alpine AS final
ARG TARGET_ENVIRONMENT

# Copy built assets
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration based on environment
COPY nginx.${TARGET_ENVIRONMENT}.conf /etc/nginx/nginx.conf

# Create non-root user for security (production-like)
RUN addgroup -g 1001 -S appgroup && \
    adduser -S appuser -u 1001 -G appgroup && \
    chown -R appuser:appgroup /usr/share/nginx/html && \
    chown -R appuser:appgroup /var/cache/nginx && \
    chown -R appuser:appgroup /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R appuser:appgroup /var/run/nginx.pid

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

### 3.2 Dockerfile.staging

```dockerfile
# Railway Staging Environment Dockerfile for React
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm ci

# Copy source and staging env
COPY . .
COPY .env.staging .env

# Build with staging optimizations
RUN npm run build

# Nginx for staging
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy built assets
COPY --from=build /app/build .

# Staging nginx config
COPY nginx.staging.conf /etc/nginx/nginx.conf

# Environment info
ENV NODE_ENV=staging
ENV REACT_APP_ENV=staging

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

### 3.3 Dockerfile.production

```dockerfile
# Production Environment Dockerfile for React
# Optimized for security and performance

FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies (production only)
COPY package*.json ./
RUN npm ci --only=production=false

# Copy source and production env
COPY . .
COPY .env.production .env

# Build with production optimizations
RUN npm run build && \
    npm prune --production

# Production stage with hardened Nginx
FROM nginx:alpine AS final

# Security: Remove unnecessary packages
RUN apk del --purge apk-tools

# Copy built assets
COPY --from=build /app/build /usr/share/nginx/html

# Production nginx config with security headers
COPY nginx.production.conf /etc/nginx/nginx.conf

# Security: Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -S appuser -u 1001 -G appgroup && \
    chown -R appuser:appgroup /usr/share/nginx/html && \
    chown -R appuser:appgroup /var/cache/nginx && \
    chown -R appuser:appgroup /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R appuser:appgroup /var/run/nginx.pid

# Run as non-root user
USER appuser

# Environment
ENV NODE_ENV=production

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
```

---

## 4. Nginx Konfigürasyonları

### 4.1 nginx.staging.conf

```nginx
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for"';

    access_log /var/log/nginx/access.log main;

    sendfile on;
    keepalive_timeout 65;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_types text/plain text/css text/xml text/javascript
               application/javascript application/json application/xml;

    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;

        # Staging-specific headers
        add_header X-Environment "staging" always;
        add_header X-Robots-Tag "noindex, nofollow" always;

        # SPA routing
        location / {
            try_files $uri $uri/ /index.html;
        }

        # Cache static assets
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2)$ {
            expires 7d;
            add_header Cache-Control "public, immutable";
        }

        # API proxy (if needed)
        location /api/ {
            proxy_pass https://ziraai-api-sit.up.railway.app;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection 'upgrade';
            proxy_set_header Host $host;
            proxy_cache_bypass $http_upgrade;
        }

        # Health check
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }
}
```

### 4.2 nginx.production.conf

```nginx
worker_processes auto;
error_log /var/log/nginx/error.log error;
pid /var/run/nginx.pid;

events {
    worker_connections 2048;
    multi_accept on;
    use epoll;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Reduced logging for production
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent';

    access_log /var/log/nginx/access.log main buffer=16k;

    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 30;
    types_hash_max_size 2048;

    # Security headers
    server_tokens off;

    # Gzip compression (aggressive)
    gzip on;
    gzip_vary on;
    gzip_min_length 256;
    gzip_comp_level 6;
    gzip_proxied any;
    gzip_types text/plain text/css text/xml text/javascript
               application/javascript application/json application/xml
               application/rss+xml application/atom+xml image/svg+xml;

    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;

        # Security headers
        add_header X-Frame-Options "SAMEORIGIN" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;
        add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline' https://www.googletagmanager.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https://api.ziraai.com wss://api.ziraai.com" always;

        # SPA routing
        location / {
            try_files $uri $uri/ /index.html;
        }

        # Aggressive caching for static assets
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }

        # Service worker (no cache)
        location /service-worker.js {
            add_header Cache-Control "no-cache, no-store, must-revalidate";
        }

        # API proxy
        location /api/ {
            proxy_pass https://api.ziraai.com;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection 'upgrade';
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
        }

        # Health check
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }
}
```

---

## 5. Railway Deployment

### 5.1 railway.json Konfigürasyonu

**Staging için (`railway.staging.json`):**
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile.staging"
  },
  "deploy": {
    "numReplicas": 1,
    "healthcheckPath": "/health",
    "healthcheckTimeout": 30,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 3
  }
}
```

**Production için (`railway.production.json`):**
```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile.production"
  },
  "deploy": {
    "numReplicas": 2,
    "healthcheckPath": "/health",
    "healthcheckTimeout": 30,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 5
  }
}
```

### 5.2 Railway Environment Variables

Railway dashboard'da ayarlanması gereken değişkenler:

**Staging:**
```
NODE_ENV=staging
REACT_APP_ENV=staging
REACT_APP_API_BASE_URL=https://ziraai-api-sit.up.railway.app
```

**Production:**
```
NODE_ENV=production
REACT_APP_ENV=production
REACT_APP_API_BASE_URL=https://api.ziraai.com
```

---

## 6. Build & Deploy Komutları

### 6.1 Local Build Test

```bash
# Staging build test
docker build -f Dockerfile.staging -t frontend:staging .
docker run -p 3000:80 frontend:staging

# Production build test
docker build -f Dockerfile.production -t frontend:production .
docker run -p 3000:80 frontend:production
```

### 6.2 Railway CLI Deployment

```bash
# Staging deploy
railway link --environment staging
railway up --detach

# Production deploy
railway link --environment production
railway up --detach
```

---

## 7. Ortam Bazlı Farklılıklar Özeti

| Özellik | Development | Staging | Production |
|---------|-------------|---------|------------|
| **API URL** | localhost:5001 | ziraai-api-sit.up.railway.app | api.ziraai.com |
| **Logging** | debug | info | error |
| **DevTools** | ✅ | ❌ | ❌ |
| **Analytics** | ❌ | ✅ | ✅ |
| **Error Reporting** | ❌ | ✅ | ✅ |
| **Cache Duration** | 1 hour | 7 days | 1 year |
| **Gzip Level** | 1 | 4 | 6 |
| **Replicas** | 1 | 1 | 2+ |
| **Security Headers** | Basic | Basic | Full |
| **Non-root User** | ❌ | ❌ | ✅ |
| **Robots** | noindex | noindex | index |

---

## 8. Checklist

### Deployment Öncesi Kontrol Listesi

- [ ] Environment dosyaları oluşturuldu (.env.staging, .env.production)
- [ ] Dockerfile'lar hazırlandı (Dockerfile.staging, Dockerfile.production)
- [ ] Nginx konfigürasyonları hazırlandı
- [ ] API base URL'leri doğru ayarlandı
- [ ] Railway environment variables tanımlandı
- [ ] Health check endpoint'i çalışıyor
- [ ] Build testi yapıldı (docker build)
- [ ] Local test yapıldı (docker run)
- [ ] Security headers kontrol edildi
- [ ] Caching stratejisi belirlendi

### Post-Deployment Kontrol

- [ ] Health endpoint'i 200 dönüyor
- [ ] API bağlantısı çalışıyor
- [ ] Static assets yükleniyor
- [ ] SPA routing düzgün çalışıyor
- [ ] Console'da hata yok
- [ ] Network tab'da CORS hatası yok

---

## 9. Troubleshooting

### Yaygın Sorunlar

**1. API 404/CORS Hatası:**
- `.env` dosyasında `REACT_APP_API_BASE_URL` kontrol edin
- Nginx proxy konfigürasyonunu kontrol edin

**2. Environment Variables Yüklenmiyor:**
- Build sırasında `.env` dosyasının kopyalandığından emin olun
- React'ta sadece `REACT_APP_` prefix'li değişkenler yüklenir

**3. Routing Çalışmıyor:**
- Nginx'te `try_files $uri $uri/ /index.html` kuralı var mı kontrol edin

**4. Cache Sorunu:**
- Browser cache'i temizleyin
- Nginx cache headers'ı kontrol edin

---

## 10. Referanslar

- Backend Dockerfile: [Dockerfile](../Dockerfile)
- Backend Staging: [Dockerfile.staging](../Dockerfile.staging)
- Backend Production: [Dockerfile.production](../Dockerfile.production)
- Staging Config: [appsettings.Staging.json](../WebAPI/appsettings.Staging.json)
- Environment Variables Guide: [ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md)

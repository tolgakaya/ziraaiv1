# 📚 ZiraAI Documentation

This directory contains all project documentation for the ZiraAI platform.

## 📋 Documentation Index

### 🚀 Deployment & Operations
- **[CI/CD Deployment Guide](CI-CD-DEPLOYMENT-GUIDE.md)** - Complete deployment and configuration guide
- **[Railway Multi-Service Setup](CI-CD-DEPLOYMENT-GUIDE.md#railway-multi-service-deployment)** - Multi-service deployment configuration

### 🔧 Development
- **[Architecture Overview](../CLAUDE.md)** - Project structure and architecture
- **[API Documentation](../ZiraAI_Complete_API_Collection_v6.1.json)** - Postman collection with 120+ endpoints

### 📝 Templates
- **[Deployment Change Template](../.github/DEPLOYMENT_CHANGE_TEMPLATE.md)** - Template for documenting deployment changes

## 🚨 Important Notice

**CRITICAL**: When making deployment configuration changes, always:

1. 📝 **Document Changes**: Use the [Deployment Change Template](../.github/DEPLOYMENT_CHANGE_TEMPLATE.md)
2. 🔄 **Update Main Guide**: Modify [CI-CD-DEPLOYMENT-GUIDE.md](CI-CD-DEPLOYMENT-GUIDE.md)
3. ✅ **Test Thoroughly**: Verify in staging before production
4. 📢 **Notify Team**: Communicate changes to all team members

## 🔗 Quick Links

### Railway Services
- **[WebAPI Staging](https://ziraai-api-staging.up.railway.app)** - API staging environment
- **[Worker Staging](https://ziraai-worker-staging.up.railway.app)** - Worker staging environment
- **[WebAPI Production](https://ziraai-api-prod.up.railway.app)** - API production environment
- **[Worker Production](https://ziraai-worker-prod.up.railway.app)** - Worker production environment

### Health Checks
- **WebAPI Health**: `GET /health`
- **Worker Health**: `GET /health`

## 📞 Support

For documentation issues or questions:
1. Check existing documentation first
2. Review troubleshooting sections
3. Contact development team with specific questions

---

**Last Updated**: September 9, 2025  
**Maintainer**: Development Team
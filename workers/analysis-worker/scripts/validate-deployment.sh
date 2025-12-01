#!/bin/bash

# ============================================
# Railway Deployment Validation Script
# ============================================
# Validates analysis-worker deployment to Railway
# Usage: ./scripts/validate-deployment.sh [railway-service-url]

set -e

echo "🔍 Railway Deployment Validation"
echo "═══════════════════════════════════════════════════════════════"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Validation results
PASSED=0
FAILED=0
WARNINGS=0

# Helper functions
pass() {
    echo -e "${GREEN}✓${NC} $1"
    ((PASSED++))
}

fail() {
    echo -e "${RED}✗${NC} $1"
    ((FAILED++))
}

warn() {
    echo -e "${YELLOW}⚠${NC} $1"
    ((WARNINGS++))
}

# ============================================
# 1. Pre-Deployment Checks
# ============================================
echo ""
echo "📋 Pre-Deployment Checks"
echo "───────────────────────────────────────────────────────────────"

# Check Dockerfile exists
if [ -f "workers/analysis-worker/Dockerfile" ]; then
    pass "Dockerfile exists"
else
    fail "Dockerfile not found"
fi

# Check .dockerignore exists
if [ -f "workers/analysis-worker/.dockerignore" ]; then
    pass ".dockerignore exists"
else
    fail ".dockerignore not found"
fi

# Check railway.json exists
if [ -f "workers/analysis-worker/railway.json" ]; then
    pass "railway.json exists"
else
    fail "railway.json not found"
fi

# Check package.json exists
if [ -f "workers/analysis-worker/package.json" ]; then
    pass "package.json exists"
    
    # Validate package.json has required scripts
    if grep -q '"build"' workers/analysis-worker/package.json; then
        pass "package.json has 'build' script"
    else
        fail "package.json missing 'build' script"
    fi
else
    fail "package.json not found"
fi

# Check tsconfig.json exists
if [ -f "workers/analysis-worker/tsconfig.json" ]; then
    pass "tsconfig.json exists"
else
    fail "tsconfig.json not found"
fi

# Check src directory exists
if [ -d "workers/analysis-worker/src" ]; then
    pass "src/ directory exists"
    
    # Check index.ts exists
    if [ -f "workers/analysis-worker/src/index.ts" ]; then
        pass "src/index.ts exists"
    else
        fail "src/index.ts not found"
    fi
else
    fail "src/ directory not found"
fi

# ============================================
# 2. Build Validation
# ============================================
echo ""
echo "🔨 Build Validation"
echo "───────────────────────────────────────────────────────────────"

# Check if dist directory exists (from previous build)
if [ -d "workers/analysis-worker/dist" ]; then
    warn "dist/ directory exists (from previous build)"
    
    # Check if index.js exists
    if [ -f "workers/analysis-worker/dist/index.js" ]; then
        pass "dist/index.js exists"
    else
        warn "dist/index.js not found (may need rebuild)"
    fi
else
    warn "dist/ directory not found (will be created during Docker build)"
fi

# Validate TypeScript configuration
if [ -f "workers/analysis-worker/tsconfig.json" ]; then
    if grep -q '"outDir".*"dist"' workers/analysis-worker/tsconfig.json; then
        pass "tsconfig.json has correct outDir"
    else
        warn "tsconfig.json may have incorrect outDir"
    fi
fi

# ============================================
# 3. Environment Configuration Validation
# ============================================
echo ""
echo "⚙️  Environment Configuration"
echo "───────────────────────────────────────────────────────────────"

# Check .env.example exists
if [ -f "workers/analysis-worker/.env.example" ]; then
    pass ".env.example exists for reference"
    
    # Validate required variables are documented
    REQUIRED_VARS=("WORKER_ID" "RABBITMQ_URL" "REDIS_URL" "PROVIDER_SELECTION_STRATEGY")
    
    for var in "${REQUIRED_VARS[@]}"; do
        if grep -q "^$var=" workers/analysis-worker/.env.example || grep -q "^# $var=" workers/analysis-worker/.env.example; then
            pass "$var documented in .env.example"
        else
            warn "$var not documented in .env.example"
        fi
    done
    
    # Check for at least one provider API key
    PROVIDER_KEYS=("OPENAI_API_KEY" "GEMINI_API_KEY" "ANTHROPIC_API_KEY")
    PROVIDER_FOUND=false
    
    for key in "${PROVIDER_KEYS[@]}"; do
        if grep -q "^$key=" workers/analysis-worker/.env.example || grep -q "^# $key=" workers/analysis-worker/.env.example; then
            PROVIDER_FOUND=true
            break
        fi
    done
    
    if [ "$PROVIDER_FOUND" = true ]; then
        pass "At least one provider API key documented"
    else
        fail "No provider API keys documented"
    fi
else
    warn ".env.example not found"
fi

# ============================================
# 4. Docker Configuration Validation
# ============================================
echo ""
echo "🐳 Docker Configuration"
echo "───────────────────────────────────────────────────────────────"

# Validate Dockerfile
if [ -f "workers/analysis-worker/Dockerfile" ]; then
    # Check for multi-stage build
    if grep -q "FROM.*AS builder" workers/analysis-worker/Dockerfile; then
        pass "Dockerfile uses multi-stage build"
    else
        warn "Dockerfile may not use multi-stage build"
    fi
    
    # Check for TypeScript compilation
    if grep -q "npm run build" workers/analysis-worker/Dockerfile; then
        pass "Dockerfile compiles TypeScript"
    else
        fail "Dockerfile missing TypeScript compilation"
    fi
    
    # Check for production dependencies
    if grep -q "npm ci --omit=dev" workers/analysis-worker/Dockerfile || grep -q "npm ci --only=production" workers/analysis-worker/Dockerfile; then
        pass "Dockerfile installs production dependencies only"
    else
        warn "Dockerfile may install dev dependencies in production"
    fi
    
    # Check for non-root user
    if grep -q "USER nodejs" workers/analysis-worker/Dockerfile || grep -q "USER node" workers/analysis-worker/Dockerfile; then
        pass "Dockerfile uses non-root user"
    else
        warn "Dockerfile may run as root user"
    fi
    
    # Check CMD
    if grep -q 'CMD.*"node".*"dist/index.js"' workers/analysis-worker/Dockerfile; then
        pass "Dockerfile has correct CMD"
    else
        fail "Dockerfile missing or incorrect CMD"
    fi
fi

# Validate railway.json
if [ -f "workers/analysis-worker/railway.json" ]; then
    # Check for Dockerfile builder
    if grep -q '"builder".*"DOCKERFILE"' workers/analysis-worker/railway.json; then
        pass "railway.json uses Dockerfile builder"
    else
        fail "railway.json missing Dockerfile builder"
    fi
    
    # Check for Dockerfile path
    if grep -q '"dockerfilePath".*"workers/analysis-worker/Dockerfile"' workers/analysis-worker/railway.json; then
        pass "railway.json has correct Dockerfile path"
    else
        warn "railway.json may have incorrect Dockerfile path"
    fi
    
    # Check for restart policy
    if grep -q '"restartPolicyType".*"ON_FAILURE"' workers/analysis-worker/railway.json; then
        pass "railway.json has restart policy ON_FAILURE"
    else
        warn "railway.json may not have restart policy"
    fi
fi

# ============================================
# 5. Git Status Check
# ============================================
echo ""
echo "📦 Git Status"
echo "───────────────────────────────────────────────────────────────"

# Check if in git repository
if [ -d ".git" ]; then
    pass "In git repository"
    
    # Check if deployment files are tracked
    if git ls-files --error-unmatch workers/analysis-worker/Dockerfile >/dev/null 2>&1; then
        pass "Dockerfile is tracked by git"
    else
        warn "Dockerfile not tracked by git"
    fi
    
    if git ls-files --error-unmatch workers/analysis-worker/.dockerignore >/dev/null 2>&1; then
        pass ".dockerignore is tracked by git"
    else
        warn ".dockerignore not tracked by git"
    fi
    
    if git ls-files --error-unmatch workers/analysis-worker/railway.json >/dev/null 2>&1; then
        pass "railway.json is tracked by git"
    else
        warn "railway.json not tracked by git"
    fi
    
    # Check for uncommitted changes in deployment files
    if git diff --quiet workers/analysis-worker/Dockerfile workers/analysis-worker/.dockerignore workers/analysis-worker/railway.json; then
        pass "No uncommitted changes in deployment files"
    else
        warn "Uncommitted changes in deployment files"
    fi
else
    warn "Not in git repository"
fi

# ============================================
# 6. Dependencies Check
# ============================================
echo ""
echo "📚 Dependencies"
echo "───────────────────────────────────────────────────────────────"

# Check if node_modules exists
if [ -d "workers/analysis-worker/node_modules" ]; then
    pass "node_modules exists (dependencies installed)"
else
    warn "node_modules not found (run 'npm install' first)"
fi

# Check for package-lock.json
if [ -f "workers/analysis-worker/package-lock.json" ]; then
    pass "package-lock.json exists (dependency lock file)"
else
    warn "package-lock.json not found (should be committed)"
fi

# ============================================
# 7. Summary
# ============================================
echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "📊 Validation Summary"
echo "═══════════════════════════════════════════════════════════════"
echo ""
echo -e "${GREEN}Passed:${NC}   $PASSED"
echo -e "${YELLOW}Warnings:${NC} $WARNINGS"
echo -e "${RED}Failed:${NC}   $FAILED"
echo ""

if [ $FAILED -eq 0 ]; then
    if [ $WARNINGS -eq 0 ]; then
        echo -e "${GREEN}✅ All checks passed! Ready for Railway deployment.${NC}"
        exit 0
    else
        echo -e "${YELLOW}⚠️  Some warnings found. Review before deployment.${NC}"
        exit 0
    fi
else
    echo -e "${RED}❌ Validation failed. Fix errors before deployment.${NC}"
    exit 1
fi

#!/usr/bin/env node

/**
 * Railway Deployment Validation Script
 * Validates analysis-worker deployment configuration
 */

const fs = require('fs');
const path = require('path');

// Colors for output
const colors = {
  reset: '\x1b[0m',
  green: '\x1b[32m',
  red: '\x1b[31m',
  yellow: '\x1b[33m',
};

let passed = 0;
let failed = 0;
let warnings = 0;

function pass(message) {
  console.log(`${colors.green}✓${colors.reset} ${message}`);
  passed++;
}

function fail(message) {
  console.log(`${colors.red}✗${colors.reset} ${message}`);
  failed++;
}

function warn(message) {
  console.log(`${colors.yellow}⚠${colors.reset} ${message}`);
  warnings++;
}

function fileExists(filePath) {
  return fs.existsSync(filePath);
}

function fileContains(filePath, searchText) {
  if (!fileExists(filePath)) return false;
  const content = fs.readFileSync(filePath, 'utf8');
  return content.includes(searchText);
}

console.log('🔍 Railway Deployment Validation');
console.log('═══════════════════════════════════════════════════════════════\n');

// ============================================
// 1. Pre-Deployment Checks
// ============================================
console.log('📋 Pre-Deployment Checks');
console.log('───────────────────────────────────────────────────────────────');

const workerRoot = path.join(process.cwd(), 'workers', 'analysis-worker');

// Check Dockerfile
if (fileExists(path.join(workerRoot, 'Dockerfile'))) {
  pass('Dockerfile exists');
} else {
  fail('Dockerfile not found');
}

// Check .dockerignore
if (fileExists(path.join(workerRoot, '.dockerignore'))) {
  pass('.dockerignore exists');
} else {
  fail('.dockerignore not found');
}

// Check railway.json
if (fileExists(path.join(workerRoot, 'railway.json'))) {
  pass('railway.json exists');
} else {
  fail('railway.json not found');
}

// Check package.json
const packageJsonPath = path.join(workerRoot, 'package.json');
if (fileExists(packageJsonPath)) {
  pass('package.json exists');
  
  // Check for build script
  if (fileContains(packageJsonPath, '"build"')) {
    pass('package.json has "build" script');
  } else {
    fail('package.json missing "build" script');
  }
} else {
  fail('package.json not found');
}

// Check tsconfig.json
if (fileExists(path.join(workerRoot, 'tsconfig.json'))) {
  pass('tsconfig.json exists');
} else {
  fail('tsconfig.json not found');
}

// Check src directory
const srcDir = path.join(workerRoot, 'src');
if (fs.existsSync(srcDir) && fs.statSync(srcDir).isDirectory()) {
  pass('src/ directory exists');
  
  // Check index.ts
  if (fileExists(path.join(srcDir, 'index.ts'))) {
    pass('src/index.ts exists');
  } else {
    fail('src/index.ts not found');
  }
} else {
  fail('src/ directory not found');
}

// ============================================
// 2. Build Validation
// ============================================
console.log('\n🔨 Build Validation');
console.log('───────────────────────────────────────────────────────────────');

const distDir = path.join(workerRoot, 'dist');
if (fs.existsSync(distDir) && fs.statSync(distDir).isDirectory()) {
  warn('dist/ directory exists (from previous build)');
  
  if (fileExists(path.join(distDir, 'index.js'))) {
    pass('dist/index.js exists');
  } else {
    warn('dist/index.js not found (may need rebuild)');
  }
} else {
  warn('dist/ directory not found (will be created during Docker build)');
}

// Validate tsconfig.json
const tsconfigPath = path.join(workerRoot, 'tsconfig.json');
if (fileExists(tsconfigPath)) {
  if (fileContains(tsconfigPath, '"outDir"') && fileContains(tsconfigPath, '"dist"')) {
    pass('tsconfig.json has correct outDir');
  } else {
    warn('tsconfig.json may have incorrect outDir');
  }
}

// ============================================
// 3. Environment Configuration
// ============================================
console.log('\n⚙️  Environment Configuration');
console.log('───────────────────────────────────────────────────────────────');

const envExamplePath = path.join(workerRoot, '.env.example');
if (fileExists(envExamplePath)) {
  pass('.env.example exists for reference');
  
  // Check required variables
  const requiredVars = ['WORKER_ID', 'RABBITMQ_URL', 'REDIS_URL', 'PROVIDER_SELECTION_STRATEGY'];
  
  requiredVars.forEach(varName => {
    if (fileContains(envExamplePath, varName)) {
      pass(`${varName} documented in .env.example`);
    } else {
      warn(`${varName} not documented in .env.example`);
    }
  });
  
  // Check provider API keys
  const providerKeys = ['OPENAI_API_KEY', 'GEMINI_API_KEY', 'ANTHROPIC_API_KEY'];
  const hasProviderKey = providerKeys.some(key => fileContains(envExamplePath, key));
  
  if (hasProviderKey) {
    pass('At least one provider API key documented');
  } else {
    fail('No provider API keys documented');
  }
} else {
  warn('.env.example not found');
}

// ============================================
// 4. Docker Configuration
// ============================================
console.log('\n🐳 Docker Configuration');
console.log('───────────────────────────────────────────────────────────────');

const dockerfilePath = path.join(workerRoot, 'Dockerfile');
if (fileExists(dockerfilePath)) {
  // Check for multi-stage build
  if (fileContains(dockerfilePath, 'FROM') && fileContains(dockerfilePath, 'AS builder')) {
    pass('Dockerfile uses multi-stage build');
  } else {
    warn('Dockerfile may not use multi-stage build');
  }
  
  // Check for TypeScript compilation
  if (fileContains(dockerfilePath, 'npm run build')) {
    pass('Dockerfile compiles TypeScript');
  } else {
    fail('Dockerfile missing TypeScript compilation');
  }
  
  // Check for production dependencies
  if (fileContains(dockerfilePath, 'npm ci --omit=dev') || fileContains(dockerfilePath, 'npm ci --only=production')) {
    pass('Dockerfile installs production dependencies only');
  } else {
    warn('Dockerfile may install dev dependencies in production');
  }
  
  // Check for non-root user
  if (fileContains(dockerfilePath, 'USER nodejs') || fileContains(dockerfilePath, 'USER node')) {
    pass('Dockerfile uses non-root user');
  } else {
    warn('Dockerfile may run as root user');
  }
  
  // Check CMD
  if (fileContains(dockerfilePath, 'CMD') && fileContains(dockerfilePath, 'node') && fileContains(dockerfilePath, 'dist/index.js')) {
    pass('Dockerfile has correct CMD');
  } else {
    fail('Dockerfile missing or incorrect CMD');
  }
}

// Validate railway.json
const railwayJsonPath = path.join(workerRoot, 'railway.json');
if (fileExists(railwayJsonPath)) {
  if (fileContains(railwayJsonPath, '"builder"') && fileContains(railwayJsonPath, '"DOCKERFILE"')) {
    pass('railway.json uses Dockerfile builder');
  } else {
    fail('railway.json missing Dockerfile builder');
  }
  
  if (fileContains(railwayJsonPath, '"dockerfilePath"') && fileContains(railwayJsonPath, 'workers/analysis-worker/Dockerfile')) {
    pass('railway.json has correct Dockerfile path');
  } else {
    warn('railway.json may have incorrect Dockerfile path');
  }
  
  if (fileContains(railwayJsonPath, '"restartPolicyType"') && fileContains(railwayJsonPath, '"ON_FAILURE"')) {
    pass('railway.json has restart policy ON_FAILURE');
  } else {
    warn('railway.json may not have restart policy');
  }
}

// ============================================
// 5. Dependencies
// ============================================
console.log('\n📚 Dependencies');
console.log('───────────────────────────────────────────────────────────────');

const nodeModulesPath = path.join(workerRoot, 'node_modules');
if (fs.existsSync(nodeModulesPath) && fs.statSync(nodeModulesPath).isDirectory()) {
  pass('node_modules exists (dependencies installed)');
} else {
  warn('node_modules not found (run "npm install" first)');
}

const packageLockPath = path.join(workerRoot, 'package-lock.json');
if (fileExists(packageLockPath)) {
  pass('package-lock.json exists (dependency lock file)');
} else {
  warn('package-lock.json not found (should be committed)');
}

// ============================================
// Summary
// ============================================
console.log('\n═══════════════════════════════════════════════════════════════');
console.log('📊 Validation Summary');
console.log('═══════════════════════════════════════════════════════════════\n');

console.log(`${colors.green}Passed:${colors.reset}   ${passed}`);
console.log(`${colors.yellow}Warnings:${colors.reset} ${warnings}`);
console.log(`${colors.red}Failed:${colors.reset}   ${failed}\n`);

if (failed === 0) {
  if (warnings === 0) {
    console.log(`${colors.green}✅ All checks passed! Ready for Railway deployment.${colors.reset}`);
    process.exit(0);
  } else {
    console.log(`${colors.yellow}⚠️  Some warnings found. Review before deployment.${colors.reset}`);
    process.exit(0);
  }
} else {
  console.log(`${colors.red}❌ Validation failed. Fix errors before deployment.${colors.reset}`);
  process.exit(1);
}

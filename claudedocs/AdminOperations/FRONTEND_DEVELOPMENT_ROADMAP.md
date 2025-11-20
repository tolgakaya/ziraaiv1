# ZiraAI Admin Panel - Frontend Development Roadmap

**Created:** 2025-10-23  
**Target:** React + TypeScript Admin Panel  
**Timeline:** 6-8 hafta (MVP)  
**Team Size:** 1-2 frontend developer

---

## Executive Summary

Bu roadmap, ZiraAI Admin Operations API için modern, kullanıcı dostu bir React frontend uygulaması geliştirmek üzere tasarlanmıştır. 5 ana persona (Super Admin, Data Analyst, User Support, Sponsorship Manager, Auditor) için optimize edilmiş 28 endpoint'i kapsayan tam özellikli bir admin panel oluşturacağız.

---

## Technology Stack

### Core
- **Framework:** React 18.2+
- **Language:** TypeScript 5.0+
- **Build Tool:** Vite 5.0+
- **Package Manager:** npm / yarn / pnpm

### UI Components
- **UI Library:** Material-UI (MUI) 5.14+
  - @mui/material
  - @mui/icons-material
  - @mui/x-data-grid (advanced tables)
  - @mui/x-date-pickers

### State Management
- **Global State:** Redux Toolkit 2.0+
- **Server State:** RTK Query (built into Redux Toolkit)
- **Form State:** React Hook Form 7.48+

### Data Visualization
- **Charts:** Recharts 2.10+
- **Alternative:** ApexCharts (if more complex charts needed)

### Utilities
- **HTTP Client:** Axios 1.6+
- **Date/Time:** date-fns 2.30+
- **Validation:** Yup 1.3+
- **CSV Export:** react-csv 2.2+

### Development Tools
- **Linter:** ESLint
- **Formatter:** Prettier
- **Git Hooks:** Husky + lint-staged
- **Testing:** Vitest + React Testing Library (Phase 2)

---

## Project Structure

```
ziraai-admin-panel/
├── public/
│   ├── favicon.ico
│   └── robots.txt
│
├── src/
│   ├── api/                          # API service layer
│   │   ├── client.ts                 # Axios instance with interceptors
│   │   ├── endpoints/
│   │   │   ├── analytics.ts
│   │   │   ├── users.ts
│   │   │   ├── subscriptions.ts
│   │   │   ├── sponsorship.ts
│   │   │   ├── plantAnalysis.ts
│   │   │   └── audit.ts
│   │   └── types/                    # TypeScript interfaces
│   │       ├── analytics.types.ts
│   │       ├── user.types.ts
│   │       └── ...
│   │
│   ├── components/                   # Reusable components
│   │   ├── common/
│   │   │   ├── DataTable/
│   │   │   ├── KPICard/
│   │   │   ├── SearchBar/
│   │   │   ├── ConfirmDialog/
│   │   │   └── LoadingSpinner/
│   │   ├── charts/
│   │   │   ├── LineChart/
│   │   │   ├── BarChart/
│   │   │   └── PieChart/
│   │   └── forms/
│   │       ├── UserSearchForm/
│   │       └── SubscriptionForm/
│   │
│   ├── features/                     # Feature-based modules
│   │   ├── auth/
│   │   │   ├── Login.tsx
│   │   │   ├── authSlice.ts
│   │   │   └── ProtectedRoute.tsx
│   │   ├── dashboard/
│   │   │   ├── SuperAdminDashboard.tsx
│   │   │   ├── AnalystDashboard.tsx
│   │   │   └── components/
│   │   ├── users/
│   │   │   ├── UserList.tsx
│   │   │   ├── UserDetail.tsx
│   │   │   ├── UserSearch.tsx
│   │   │   └── userSlice.ts
│   │   ├── subscriptions/
│   │   ├── sponsorship/
│   │   ├── plantAnalysis/
│   │   └── audit/
│   │
│   ├── layouts/
│   │   ├── AdminLayout.tsx           # Main layout with sidebar
│   │   ├── Sidebar.tsx
│   │   └── Header.tsx
│   │
│   ├── routes/
│   │   └── AppRoutes.tsx             # React Router configuration
│   │
│   ├── store/                        # Redux store
│   │   ├── index.ts
│   │   └── hooks.ts                  # Typed useAppDispatch, useAppSelector
│   │
│   ├── theme/
│   │   ├── theme.ts                  # MUI theme customization
│   │   └── colors.ts                 # Color palette
│   │
│   ├── utils/
│   │   ├── formatters.ts             # Date, currency formatters
│   │   ├── validators.ts             # Validation utilities
│   │   └── constants.ts              # App constants
│   │
│   ├── App.tsx
│   ├── main.tsx
│   └── vite-env.d.ts
│
├── .env.development
├── .env.production
├── .eslintrc.cjs
├── .prettierrc
├── package.json
├── tsconfig.json
└── vite.config.ts
```

---

## Development Phases

### Phase 1: Foundation (Week 1-2)
**Goal:** Project setup + Authentication + Basic layout

#### Week 1: Setup & Configuration
- [x] Create Vite + React + TypeScript project
- [x] Install all dependencies
- [x] Configure ESLint + Prettier
- [x] Setup folder structure
- [x] Configure Material-UI theme
- [x] Setup Redux Toolkit store
- [x] Configure environment variables

**Deliverables:**
- ✅ Boilerplate project with all configs
- ✅ Theme customization (colors, typography)
- ✅ Environment setup (.env files)

#### Week 2: Authentication & Layout
- [ ] Create API client (Axios with interceptors)
- [ ] Implement Login page
- [ ] JWT token management (localStorage)
- [ ] Protected routes
- [ ] Admin layout (Sidebar + Header + Content)
- [ ] Sidebar navigation (persona-based menu)
- [ ] Logout functionality

**Deliverables:**
- ✅ Working authentication flow
- ✅ Protected admin routes
- ✅ Responsive layout
- ✅ Navigation menu

**Test Criteria:**
- User can login with valid credentials
- Invalid credentials show error
- Protected routes redirect to login
- Sidebar collapses on mobile
- Logout clears token and redirects

---

### Phase 2: Dashboard & Analytics (Week 3)
**Goal:** Super Admin dashboard with key metrics

#### Features
- [ ] Dashboard overview endpoint integration
- [ ] KPI cards (4-6 metrics)
- [ ] Activity feed (recent admin actions)
- [ ] User statistics page
- [ ] Subscription statistics page
- [ ] Sponsorship statistics page
- [ ] Date range picker component
- [ ] Charts integration (Recharts)

**Deliverables:**
- ✅ Super Admin Dashboard (main page)
- ✅ Analytics pages (users, subscriptions, sponsorship)
- ✅ Reusable KPI card component
- ✅ Reusable chart components

**Test Criteria:**
- Dashboard loads key metrics correctly
- Charts render data accurately
- Date range filter works
- Real-time data refresh works

---

### Phase 3: User Management (Week 4-5)
**Goal:** Complete user CRUD + search + actions

#### Week 4: User List & Search
- [ ] User list page (paginated)
- [ ] Server-side pagination
- [ ] User search (global search bar)
- [ ] Filter panel (status, role)
- [ ] User table with sorting
- [ ] Bulk selection

**Deliverables:**
- ✅ User list with pagination
- ✅ Global search functionality
- ✅ Advanced filtering
- ✅ Reusable DataTable component

#### Week 5: User Details & Actions
- [ ] User detail page
- [ ] User profile card
- [ ] User activity timeline
- [ ] Deactivate user modal
- [ ] Reactivate user modal
- [ ] Bulk deactivate modal
- [ ] Confirmation dialogs
- [ ] Success/error notifications

**Deliverables:**
- ✅ User detail page
- ✅ User action modals
- ✅ Bulk operations
- ✅ Notification system

**Test Criteria:**
- Pagination works correctly
- Search returns accurate results
- Filters apply correctly
- User details load completely
- Deactivate/reactivate works
- Bulk deactivate handles errors
- Notifications show appropriate messages

---

### Phase 4: Subscription Management (Week 6)
**Goal:** Subscription CRUD for user support

#### Features
- [ ] Subscription list page
- [ ] Subscription detail page
- [ ] Assign subscription modal
- [ ] Extend subscription modal
- [ ] Cancel subscription modal
- [ ] Subscription timeline visualization
- [ ] Usage chart (daily/monthly)

**Deliverables:**
- ✅ Subscription list with filters
- ✅ Subscription detail view
- ✅ Subscription action modals
- ✅ Usage visualization

**Test Criteria:**
- Subscription list filters work
- Detail page shows complete info
- Assign subscription creates new record
- Extend subscription updates end date
- Cancel subscription works
- Timeline shows history correctly

---

### Phase 5: Sponsorship Management (Week 7)
**Goal:** Sponsorship operations for managers

#### Features
- [ ] Purchase list page
- [ ] Pending approvals section
- [ ] Purchase approval workflow
- [ ] Refund modal
- [ ] Sponsor detail page
- [ ] Sponsor detailed report
- [ ] Code management table
- [ ] Bulk approve functionality

**Deliverables:**
- ✅ Purchase management dashboard
- ✅ Approval workflow
- ✅ Sponsor detail & reports
- ✅ Code management

**Test Criteria:**
- Purchase list loads with filters
- Approval workflow works end-to-end
- Refund modal validates correctly
- Sponsor report shows accurate data
- Code management table functional

---

### Phase 6: Audit & Compliance (Week 8)
**Goal:** Activity logs for auditors

#### Features
- [ ] Activity logs page
- [ ] Advanced filter panel
- [ ] Log detail modal
- [ ] CSV export functionality
- [ ] OBO analyses list
- [ ] Anomaly detection alerts (Phase 2)

**Deliverables:**
- ✅ Activity logs with advanced filtering
- ✅ Log detail view
- ✅ Export functionality
- ✅ OBO analyses tracking

**Test Criteria:**
- Activity logs load with pagination
- Filters apply correctly
- Log details show complete info
- CSV export works
- OBO list filters correctly

---

### Phase 7: Polish & Optimization (Week 9-10)
**Goal:** Performance optimization + UX improvements

#### Features
- [ ] Performance optimization
  - Code splitting (React.lazy)
  - Memoization (useMemo, React.memo)
  - Bundle size optimization
- [ ] UX improvements
  - Loading skeletons
  - Error boundaries
  - Empty states
  - Accessibility (ARIA labels)
- [ ] Responsive design refinement
- [ ] Cross-browser testing
- [ ] Documentation
  - User guide
  - Developer documentation

**Deliverables:**
- ✅ Optimized bundle size
- ✅ Improved loading states
- ✅ Better error handling
- ✅ Complete documentation

---

## Component Priority Matrix

### High Priority (MVP - Must Have)
1. ✅ Login page
2. ✅ Dashboard (Super Admin)
3. ✅ User list + search
4. ✅ User detail page
5. ✅ User actions (activate/deactivate)
6. ✅ Subscription list
7. ✅ Assign/extend subscription
8. ✅ Activity logs

### Medium Priority (Post-MVP - Should Have)
1. ✅ Sponsor management
2. ✅ Purchase approval workflow
3. ✅ Analytics dashboards
4. ✅ OBO plant analysis
5. ✅ Bulk operations
6. ✅ CSV export

### Low Priority (Future - Nice to Have)
1. ⏳ Notification system (real-time)
2. ⏳ Advanced reporting (PDF export)
3. ⏳ User roles & permissions management
4. ⏳ Theme switcher (dark mode)
5. ⏳ Multi-language support
6. ⏳ Keyboard shortcuts

---

## API Integration Checklist

### Analytics Endpoints (4)
- [ ] GET /api/admin/analytics/dashboard-overview
- [ ] GET /api/admin/analytics/user-statistics
- [ ] GET /api/admin/analytics/subscription-statistics
- [ ] GET /api/admin/sponsorship/statistics

### User Management (7)
- [ ] GET /api/admin/users
- [ ] GET /api/admin/users/{id}
- [ ] GET /api/admin/users/search
- [ ] POST /api/admin/users/deactivate/{id}
- [ ] POST /api/admin/users/reactivate/{id}
- [ ] POST /api/admin/users/bulk/deactivate
- [ ] POST /api/admin/users/register (OBO)

### Subscription Management (5)
- [ ] GET /api/admin/subscriptions
- [ ] GET /api/admin/subscriptions/{id}
- [ ] POST /api/admin/subscriptions/assign
- [ ] POST /api/admin/subscriptions/extend
- [ ] POST /api/admin/subscriptions/cancel

### Sponsorship Management (8)
- [ ] GET /api/admin/sponsorship/purchases
- [ ] GET /api/admin/sponsorship/purchases/{id}
- [ ] POST /api/admin/sponsorship/purchases/{id}/approve
- [ ] POST /api/admin/sponsorship/purchases/{id}/refund
- [ ] POST /api/admin/sponsorship/purchases/create-on-behalf-of
- [ ] GET /api/admin/sponsorship/codes
- [ ] GET /api/admin/sponsorship/codes/{id}
- [ ] GET /api/admin/sponsorship/sponsors/{id}/detailed-report

### Plant Analysis (2)
- [ ] POST /api/admin/plant-analysis/on-behalf-of
- [ ] GET /api/admin/plant-analysis/on-behalf-of

### Audit Logs (2)
- [ ] GET /api/admin/analytics/activity-logs
- [ ] GET /api/admin/audit/all

**Total:** 28 endpoints

---

## UI/UX Design Guidelines

### Color Palette

**Primary (Blue)**
```typescript
primary: {
  main: '#1976d2',
  light: '#42a5f5',
  dark: '#1565c0',
}
```

**Success (Green)**
```typescript
success: {
  main: '#43a047',
  light: '#66bb6a',
  dark: '#2e7d32',
}
```

**Warning (Orange)**
```typescript
warning: {
  main: '#ff9800',
  light: '#ffb74d',
  dark: '#f57c00',
}
```

**Error (Red)**
```typescript
error: {
  main: '#f44336',
  light: '#e57373',
  dark: '#d32f2f',
}
```

### Typography
```typescript
typography: {
  fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
  h1: { fontSize: '2.5rem', fontWeight: 600 },
  h2: { fontSize: '2rem', fontWeight: 600 },
  h3: { fontSize: '1.75rem', fontWeight: 500 },
  h4: { fontSize: '1.5rem', fontWeight: 500 },
  h5: { fontSize: '1.25rem', fontWeight: 500 },
  h6: { fontSize: '1rem', fontWeight: 500 },
  body1: { fontSize: '1rem' },
  body2: { fontSize: '0.875rem' },
}
```

### Spacing
- Small: 8px
- Medium: 16px
- Large: 24px
- XLarge: 32px

### Layout
- Sidebar width: 240px (collapsed: 60px)
- Header height: 64px
- Content max width: 1440px
- Card border radius: 8px
- Button border radius: 4px

---

## Performance Targets

### Load Times
- Initial page load: < 2s
- Route change: < 500ms
- API response handling: < 100ms
- Chart rendering: < 1s

### Bundle Size
- Total bundle: < 500KB (gzipped)
- Vendor chunk: < 300KB
- Main chunk: < 200KB

### Optimization Strategies
1. Code splitting per route
2. Lazy loading for charts
3. Memoization for expensive calculations
4. Virtual scrolling for large lists
5. Image optimization
6. HTTP/2 multiplexing

---

## Testing Strategy

### Unit Tests (Phase 2)
- Components: 80% coverage target
- Utilities: 100% coverage
- API services: 90% coverage

### Integration Tests (Phase 2)
- User flows (login, user management, subscription)
- API integration tests
- Redux state management tests

### E2E Tests (Phase 3)
- Critical user journeys
- Cross-browser compatibility
- Mobile responsiveness

---

## Deployment Strategy

### Development
- **Environment:** Local development
- **API:** https://ziraai-api-sit.up.railway.app
- **Hot Reload:** Vite HMR

### Staging
- **Platform:** Vercel / Netlify
- **API:** Staging API URL
- **Auto Deploy:** On push to `develop` branch

### Production
- **Platform:** Vercel / Netlify / AWS S3 + CloudFront
- **API:** Production API URL
- **Auto Deploy:** On push to `main` branch
- **CDN:** CloudFlare

---

## Team Responsibilities

### Frontend Developer 1 (Lead)
- Architecture & setup
- Core components (layout, auth, dashboard)
- User management
- Code review

### Frontend Developer 2 (if available)
- Subscription management
- Sponsorship management
- Audit logs
- Charts & data visualization

---

## Success Metrics

### Technical Metrics
- ✅ All 28 endpoints integrated
- ✅ < 2s initial load time
- ✅ < 500KB bundle size
- ✅ 80%+ test coverage (Phase 2)
- ✅ Zero critical bugs

### Business Metrics
- ✅ All 5 personas can complete their workflows
- ✅ User feedback score > 4/5
- ✅ Reduced admin operation time by 50%
- ✅ Zero data integrity issues

---

## Risks & Mitigation

### Risk 1: API Changes
**Mitigation:** 
- Use TypeScript for type safety
- Version API endpoints
- Maintain API changelog

### Risk 2: Performance Issues
**Mitigation:**
- Code splitting from day 1
- Performance monitoring (Lighthouse)
- Regular bundle size checks

### Risk 3: Scope Creep
**Mitigation:**
- Strict MVP definition
- Weekly sprint reviews
- Feature freeze 2 weeks before launch

### Risk 4: Browser Compatibility
**Mitigation:**
- Use modern browsers (Chrome, Firefox, Safari, Edge)
- Polyfills for older browsers
- Regular cross-browser testing

---

## Next Steps (Start of Next Session)

### Immediate Tasks
1. **Project Setup**
   ```bash
   npm create vite@latest ziraai-admin-panel -- --template react-ts
   cd ziraai-admin-panel
   npm install
   ```

2. **Install Dependencies**
   ```bash
   npm install @mui/material @mui/icons-material @emotion/react @emotion/styled
   npm install @reduxjs/toolkit react-redux
   npm install react-router-dom
   npm install axios
   npm install recharts
   npm install react-hook-form @hookform/resolvers yup
   npm install date-fns
   ```

3. **Create Folder Structure**
   ```bash
   mkdir -p src/{api,components,features,layouts,routes,store,theme,utils}
   ```

4. **Configure ESLint & Prettier**
   ```bash
   npm install -D eslint prettier eslint-config-prettier
   ```

5. **Setup MUI Theme**
   - Create `src/theme/theme.ts`
   - Define color palette
   - Configure typography

6. **Create API Client**
   - Create `src/api/client.ts`
   - Setup Axios instance
   - Add request/response interceptors

7. **Build Login Page**
   - Create `src/features/auth/Login.tsx`
   - Implement JWT token storage
   - Test with API

---

**Hazırlayan:** Claude Code  
**Tarih:** 2025-10-23  
**Durum:** Geliştirmeye hazır  
**Sonraki Seans:** React project setup ve ilk component'ler

---

## Appendix: Useful Commands

### Development
```bash
npm run dev          # Start dev server
npm run build        # Production build
npm run preview      # Preview production build
npm run lint         # Run ESLint
npm run format       # Run Prettier
```

### Testing (Phase 2)
```bash
npm run test         # Run unit tests
npm run test:watch   # Watch mode
npm run test:coverage # Coverage report
npm run test:e2e     # E2E tests
```

### Deployment
```bash
npm run build        # Build for production
npm run deploy       # Deploy to hosting (configure in package.json)
```
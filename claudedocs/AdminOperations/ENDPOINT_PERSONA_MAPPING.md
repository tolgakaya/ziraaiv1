# Endpoint-Persona Detaylı Mapping

**Created:** 2025-10-23  
**Purpose:** Frontend UI/UX Planning  
**Status:** Ready for Implementation

---

## Endpoint Kategorileri

### 1. Analytics & Dashboard (3 endpoints)
### 2. User Management (7 endpoints)
### 3. Subscription Management (5 endpoints)
### 4. Sponsorship Management (8 endpoints)
### 5. Plant Analysis OBO (2 endpoints)
### 6. Activity & Audit Logs (3 endpoints)

**Toplam:** 28 endpoint

---

## 1. Analytics & Dashboard Endpoints

### 1.1 Dashboard Overview

**Endpoint:** `GET /api/admin/analytics/dashboard-overview`

**Kullanılan Personalar:**
- 🎯 **Super Admin** (günde 3+ kez)
- 📊 **Data Analyst** (günde 1 kez)

**Frontend Özellikler:**
- Dashboard ana sayfası
- Real-time refresh (her 5 dakika otomatik)
- KPI cards (4-6 adet büyük sayı)
- Quick stats summary

**UI Komponenti:**
```typescript
<DashboardOverview>
  <KPIGrid>
    <KPICard title="Toplam Kullanıcı" value={137} trend="+4%" />
    <KPICard title="Aktif Abonelikler" value={89} trend="+2.3%" />
    <KPICard title="Sponsorlar" value={23} trend="+5" />
    <KPICard title="Bugünkü Aktivite" value={245} />
  </KPIGrid>
  <ActivityFeed limit={10} />
</DashboardOverview>
```

**Request/Response:**
```typescript
// Request
GET /api/admin/analytics/dashboard-overview
Authorization: Bearer {token}

// Response
{
  "success": true,
  "data": {
    "userStats": {
      "totalUsers": 137,
      "activeUsers": 137,
      "newToday": 4,
      "newThisWeek": 12
    },
    "subscriptionStats": {
      "activeSubscriptions": 89,
      "trialSubscriptions": 23,
      "renewalsThisMonth": 45
    },
    "sponsorshipStats": {
      "totalSponsors": 23,
      "activePurchases": 67,
      "codesDistributed": 1234,
      "codesUsed": 567
    },
    "recentActivity": [
      {
        "action": "CreatePurchaseOnBehalfOf",
        "timestamp": "2025-10-23T19:01:47",
        "adminUser": "Admin Name"
      }
    ]
  }
}
```

---

### 1.2 User Statistics

**Endpoint:** `GET /api/admin/analytics/user-statistics`

**Kullanılan Personalar:**
- 📊 **Data Analyst** (günde 5+ kez)
- 🎯 **Super Admin** (haftada 2-3 kez)

**Query Parameters:**
```typescript
interface UserStatisticsParams {
  startDate?: string;  // ISO format: "2025-10-01"
  endDate?: string;    // ISO format: "2025-10-31"
}
```

**Frontend Özellikler:**
- Date range picker (preset: "Today", "This Week", "This Month", "Last Month")
- Comparison mode (week-over-week, month-over-month)
- Breakdown charts (pie chart: role distribution, line chart: registration trend)
- Export button (CSV)

**UI Komponenti:**
```typescript
<UserStatistics>
  <DateRangePicker 
    presets={["Today", "This Week", "This Month", "Last Month"]}
    onChange={handleDateChange}
  />
  
  <StatsGrid>
    <StatCard label="Toplam Kullanıcı" value={137} />
    <StatCard label="Aktif" value={137} />
    <StatCard label="Pasif" value={0} />
    <StatCard label="Farmer" value={0} />
    <StatCard label="Sponsor" value={0} />
    <StatCard label="Admin" value={1} />
  </StatsGrid>

  <ChartRow>
    <PieChart 
      title="Rol Dağılımı" 
      data={[
        { name: 'Admin', value: 1 },
        { name: 'Farmer', value: 0 },
        { name: 'Sponsor', value: 0 }
      ]}
    />
    <LineChart 
      title="Kayıt Trendi" 
      data={registrationTrend}
      xAxis="date"
      yAxis="count"
    />
  </ChartRow>

  <ExportButton format="csv" filename="user-statistics" />
</UserStatistics>
```

**Response:**
```typescript
{
  "success": true,
  "data": {
    "totalUsers": 137,
    "activeUsers": 137,
    "inactiveUsers": 0,
    "farmerUsers": 0,
    "sponsorUsers": 0,
    "adminUsers": 1,
    "usersRegisteredToday": 4,
    "usersRegisteredThisWeek": 4,
    "usersRegisteredThisMonth": 53,
    "startDate": "2025-10-01",
    "endDate": "2025-10-31",
    "generatedAt": "2025-10-23T20:01:49.9176957+00:00"
  }
}
```

---

### 1.3 Subscription Statistics

**Endpoint:** `GET /api/admin/analytics/subscription-statistics`

**Kullanılan Personalar:**
- 📊 **Data Analyst** (günde 5+ kez)
- 🎯 **Super Admin** (haftada 2-3 kez)

**Query Parameters:**
```typescript
interface SubscriptionStatisticsParams {
  startDate?: string;
  endDate?: string;
}
```

**Frontend Özellikler:**
- Subscription tier breakdown (Trial, S, M, L, XL)
- Active vs expired comparison
- Renewal rate chart
- Churn analysis

**UI Komponenti:**
```typescript
<SubscriptionStatistics>
  <DateRangePicker onChange={handleDateChange} />
  
  <TierBreakdownChart 
    data={[
      { tier: 'Trial', count: 23, color: '#e3f2fd' },
      { tier: 'S', count: 15, color: '#90caf9' },
      { tier: 'M', count: 28, color: '#42a5f5' },
      { tier: 'L', count: 18, color: '#1e88e5' },
      { tier: 'XL', count: 5, color: '#1565c0' }
    ]}
  />

  <MetricsRow>
    <MetricCard label="Active" value={89} percentage={67.4} color="success" />
    <MetricCard label="Expired" value={23} percentage={17.4} color="error" />
    <MetricCard label="Renewal Rate" value="82%" color="info" />
    <MetricCard label="Churn Rate" value="18%" color="warning" />
  </MetricsRow>
</SubscriptionStatistics>
```

---

### 1.4 Sponsorship Statistics

**Endpoint:** `GET /api/admin/sponsorship/statistics`

**Kullanılan Personalar:**
- 💼 **Sponsorship Manager** (günde 3+ kez)
- 📊 **Data Analyst** (günde 2-3 kez)

**Query Parameters:**
```typescript
interface SponsorshipStatisticsParams {
  startDate?: string;
  endDate?: string;
}
```

**Frontend Özellikler:**
- Total purchases & revenue
- Code generation vs usage
- Farmer reach metrics
- Top sponsors highlight

**UI Komponenti:**
```typescript
<SponsorshipStatistics>
  <RevenueCard 
    totalPurchases={67}
    totalRevenue="₺123,456"
    trend="+12.3%"
  />

  <CodeMetrics>
    <ProgressCard 
      label="Kod Kullanım Oranı"
      used={567}
      total={1234}
      percentage={46}
    />
    <MetricCard label="Farmer Reach" value={234} />
  </CodeMetrics>

  <TopSponsorsTable 
    data={topSponsors}
    columns={['Sponsor', 'Purchases', 'Codes Used', 'Reach']}
  />
</SponsorshipStatistics>
```

---

## 2. User Management Endpoints

### 2.1 Get All Users

**Endpoint:** `GET /api/admin/users`

**Kullanılan Personalar:**
- 🎯 **Super Admin** (günde 2-3 kez)
- 🛟 **User Support** (günde 10+ kez)

**Query Parameters:**
```typescript
interface GetUsersParams {
  page: number;        // default: 1
  pageSize: number;    // default: 50, max: 100
  isActive?: boolean;  // filter by active status
  role?: 'Farmer' | 'Sponsor' | 'Admin';
}
```

**Frontend Özellikler:**
- Paginated table (server-side pagination)
- Search bar (triggers search endpoint)
- Filter panel (active/inactive, role)
- Quick actions (activate, deactivate, view details)
- Bulk selection + bulk actions

**UI Komponenti:**
```typescript
<UserManagement>
  <FilterPanel>
    <SearchBar 
      placeholder="Kullanıcı ara (isim, email, telefon)"
      onSearch={handleSearch}
    />
    <FilterGroup>
      <Select label="Durum" options={['Tümü', 'Aktif', 'Pasif']} />
      <Select label="Rol" options={['Tümü', 'Farmer', 'Sponsor', 'Admin']} />
    </FilterGroup>
  </FilterPanel>

  <DataTable
    columns={[
      { key: 'userId', label: 'ID', sortable: true },
      { key: 'fullName', label: 'Ad Soyad', sortable: true },
      { key: 'email', label: 'Email' },
      { key: 'mobilePhones', label: 'Telefon' },
      { key: 'isActive', label: 'Durum', render: StatusBadge },
      { key: 'createdDate', label: 'Kayıt Tarihi', sortable: true },
      { key: 'actions', label: 'İşlemler', render: ActionButtons }
    ]}
    data={users}
    page={currentPage}
    pageSize={pageSize}
    totalCount={totalCount}
    onPageChange={handlePageChange}
    selectable={true}
    onSelectionChange={handleSelectionChange}
  />

  <BulkActionsBar 
    selectedCount={selectedUsers.length}
    actions={['Deactivate', 'Send Email', 'Export']}
  />
</UserManagement>
```

**Response:**
```typescript
{
  "success": true,
  "data": [
    {
      "userId": 123,
      "fullName": "Ahmet Yılmaz",
      "email": "ahmet@example.com",
      "mobilePhones": "+905551234567",
      "isActive": true,
      "status": "Active",
      "createdDate": "2024-12-15T10:30:00",
      "lastLoginDate": "2025-01-20T14:25:00",
      "deactivatedDate": null,
      "deactivatedByAdminId": null
    }
  ],
  "message": "Users retrieved successfully",
  "totalCount": 137,
  "page": 1,
  "pageSize": 50
}
```

---

### 2.2 Get User By ID

**Endpoint:** `GET /api/admin/users/{userId}`

**Kullanılan Personalar:**
- 🛟 **User Support** (günde 20+ kez)
- 🎯 **Super Admin** (günde 5+ kez)

**Frontend Özellikler:**
- User profile card (photo, name, contact)
- Account status timeline
- Recent activity feed
- Related entities (subscriptions, analyses, sponsorships)
- Quick actions panel (activate, deactivate, send message)
- Audit log (user-specific)

**UI Komponenti:**
```typescript
<UserDetailPage userId={123}>
  <UserHeader>
    <Avatar src={user.photoUrl} size="large" />
    <UserInfo>
      <h2>{user.fullName}</h2>
      <UserMeta>
        <Badge status={user.isActive ? 'success' : 'error'}>
          {user.isActive ? 'Aktif' : 'Pasif'}
        </Badge>
        <Text>ID: {user.userId}</Text>
        <Text>Kayıt: {formatDate(user.createdDate)}</Text>
      </UserMeta>
    </UserInfo>
    <QuickActions>
      <Button icon={<MailIcon />}>Email Gönder</Button>
      <Button icon={<EditIcon />}>Düzenle</Button>
      <Dropdown
        items={[
          { label: 'Deactivate', danger: true },
          { label: 'Reset Password' },
          { label: 'View Audit Log' }
        ]}
      />
    </QuickActions>
  </UserHeader>

  <TabPanel>
    <Tab label="Genel Bilgiler" panel={<GeneralInfo user={user} />} />
    <Tab label="Abonelikler" panel={<UserSubscriptions userId={user.userId} />} />
    <Tab label="Bitki Analizleri" panel={<UserAnalyses userId={user.userId} />} />
    <Tab label="Aktiviteler" panel={<UserActivityLog userId={user.userId} />} />
  </TabPanel>
</UserDetailPage>
```

---

### 2.3 Search Users

**Endpoint:** `GET /api/admin/users/search`

**Kullanılan Personalar:**
- 🛟 **User Support** (günde 30+ kez)

**Query Parameters:**
```typescript
interface SearchUsersParams {
  searchTerm: string;  // required, min 2 chars
  page?: number;
  pageSize?: number;
}
```

**Frontend Özellikler:**
- Global search bar (header'da sabit)
- Autocomplete dropdown (ilk 5 sonuç)
- Full results page (enter tuşu)
- Highlight matched terms

**UI Komponenti:**
```typescript
<GlobalSearchBar>
  <Autocomplete
    placeholder="Kullanıcı ara..."
    minChars={2}
    onSearch={async (term) => {
      const results = await userService.search(term, 1, 5);
      return results.data;
    }}
    renderItem={(user) => (
      <SearchResultItem>
        <Avatar size="small" src={user.photoUrl} />
        <div>
          <Text strong>{highlightMatch(user.fullName, searchTerm)}</Text>
          <Text type="secondary">{user.email}</Text>
        </div>
      </SearchResultItem>
    )}
    onSelect={(user) => navigate(`/users/${user.userId}`)}
  />
</GlobalSearchBar>
```

---

### 2.4 Deactivate User

**Endpoint:** `POST /api/admin/users/deactivate/{userId}`

**Kullanılan Personalar:**
- 🎯 **Super Admin** (haftada 2-3 kez)
- 🛟 **User Support** (haftada 1-2 kez)

**Request Body:**
```typescript
interface DeactivateUserRequest {
  reason: string;  // required, min 10 chars
}
```

**Frontend Özellikler:**
- Confirmation modal (2-step confirm)
- Reason textarea (mandatory)
- Impact warning (subscriptions will be cancelled)
- Audit log entry

**UI Komponenti:**
```typescript
<ConfirmDialog
  title="Kullanıcıyı Deaktive Et"
  message="Bu kullanıcıyı deaktive etmek istediğinizden emin misiniz?"
  severity="warning"
  onConfirm={handleDeactivate}
>
  <WarningAlert>
    <strong>Dikkat:</strong> Bu işlem:
    <ul>
      <li>Kullanıcının tüm aboneliklerini iptal edecek</li>
      <li>Aktif analizlerini durduracak</li>
      <li>Giriş yapmasını engelleyecek</li>
    </ul>
  </WarningAlert>

  <Form onSubmit={handleDeactivate}>
    <Textarea
      label="Deaktif Etme Nedeni"
      placeholder="Lütfen detaylı açıklama girin (min. 10 karakter)"
      required
      minLength={10}
      value={reason}
      onChange={(e) => setReason(e.target.value)}
    />
    
    <Checkbox required>
      Tüm sonuçları anladım ve devam etmek istiyorum
    </Checkbox>

    <ButtonGroup>
      <Button variant="text" onClick={onCancel}>İptal</Button>
      <Button 
        variant="contained" 
        color="error" 
        type="submit"
        disabled={reason.length < 10}
      >
        Deaktive Et
      </Button>
    </ButtonGroup>
  </Form>
</ConfirmDialog>
```

---

### 2.5 Reactivate User

**Endpoint:** `POST /api/admin/users/reactivate/{userId}`

**Kullanılan Personalar:**
- 🎯 **Super Admin** (ayda 1-2 kez)
- 🛟 **User Support** (ayda 1-2 kez)

**Request Body:**
```typescript
interface ReactivateUserRequest {
  reason: string;  // required
}
```

**Frontend Özellikler:**
- Simple confirmation modal
- Reason textarea (optional but recommended)
- Success notification

**UI Komponenti:**
```typescript
<ConfirmDialog
  title="Kullanıcıyı Reaktive Et"
  message={`${user.fullName} kullanıcısını tekrar aktifleştirmek istediğinizden emin misiniz?`}
  severity="info"
  onConfirm={handleReactivate}
>
  <InfoAlert>
    Kullanıcı reaktive edildiğinde tekrar giriş yapabilecek.
    Abonelikleri manuel olarak yeniden atanmalıdır.
  </InfoAlert>

  <Textarea
    label="Reaktif Etme Nedeni (Opsiyonel)"
    placeholder="Neden bu kullanıcıyı tekrar aktifleştiriyorsunuz?"
    value={reason}
    onChange={(e) => setReason(e.target.value)}
  />

  <ButtonGroup>
    <Button variant="text" onClick={onCancel}>İptal</Button>
    <Button 
      variant="contained" 
      color="success" 
      onClick={() => onConfirm(reason)}
    >
      Reaktive Et
    </Button>
  </ButtonGroup>
</ConfirmDialog>
```

---

### 2.6 Bulk Deactivate Users

**Endpoint:** `POST /api/admin/users/bulk/deactivate`

**Kullanılan Personalar:**
- 🎯 **Super Admin** (ayda 1 kez)
- 🔍 **Auditor** (spam/abuse cleanup için)

**Request Body:**
```typescript
interface BulkDeactivateRequest {
  userIds: number[];  // required, array of user IDs
  reason: string;     // required
}
```

**Frontend Özellikler:**
- Bulk selection from user list
- Preview modal (affected users)
- Unified reason input
- Progress indicator
- Results summary

**UI Komponenti:**
```typescript
<BulkDeactivateModal 
  selectedUsers={selectedUsers}
  onConfirm={handleBulkDeactivate}
>
  <Alert severity="warning">
    <strong>{selectedUsers.length} kullanıcı</strong> deaktive edilecek
  </Alert>

  <UserPreviewList>
    {selectedUsers.slice(0, 5).map(user => (
      <ListItem key={user.userId}>
        <Avatar src={user.photoUrl} size="small" />
        <Text>{user.fullName}</Text>
        <Text type="secondary">{user.email}</Text>
      </ListItem>
    ))}
    {selectedUsers.length > 5 && (
      <Text type="secondary">
        ve {selectedUsers.length - 5} kullanıcı daha...
      </Text>
    )}
  </UserPreviewList>

  <Textarea
    label="Toplu Deaktif Etme Nedeni"
    placeholder="Tüm kullanıcılar için geçerli neden (örn: Spam hesapları temizleme)"
    required
    minLength={20}
    value={reason}
    onChange={(e) => setReason(e.target.value)}
  />

  <ButtonGroup>
    <Button variant="text" onClick={onCancel}>İptal</Button>
    <Button 
      variant="contained" 
      color="error" 
      disabled={reason.length < 20}
      onClick={() => onConfirm(selectedUsers.map(u => u.userId), reason)}
    >
      {selectedUsers.length} Kullanıcıyı Deaktive Et
    </Button>
  </ButtonGroup>
</BulkDeactivateModal>

{/* Progress during bulk operation */}
<ProgressDialog open={isProcessing}>
  <LinearProgress 
    value={(processedCount / selectedUsers.length) * 100} 
    variant="determinate"
  />
  <Text>
    {processedCount} / {selectedUsers.length} kullanıcı işlendi
  </Text>
</ProgressDialog>
```

---

## 3. Subscription Management Endpoints

### 3.1 Get All Subscriptions

**Endpoint:** `GET /api/admin/subscriptions`

**Kullanılan Personalar:**
- 🛟 **User Support** (günde 5+ kez)
- 📊 **Data Analyst** (haftada 2-3 kez)

**Query Parameters:**
```typescript
interface GetSubscriptionsParams {
  page: number;
  pageSize: number;
  status?: 'Active' | 'Expired' | 'Cancelled';
  tier?: 'Trial' | 'S' | 'M' | 'L' | 'XL';
  userId?: number;
}
```

**UI Komponenti:**
```typescript
<SubscriptionList>
  <FilterPanel>
    <Select label="Durum" options={['Tümü', 'Active', 'Expired', 'Cancelled']} />
    <Select label="Tier" options={['Tümü', 'Trial', 'S', 'M', 'L', 'XL']} />
    <UserAutocomplete label="Kullanıcıya Göre Filtrele" />
  </FilterPanel>

  <DataTable
    columns={[
      { key: 'subscriptionId', label: 'ID' },
      { key: 'userId', label: 'Kullanıcı', render: UserLink },
      { key: 'tier', label: 'Tier', render: TierBadge },
      { key: 'status', label: 'Durum', render: StatusBadge },
      { key: 'startDate', label: 'Başlangıç' },
      { key: 'endDate', label: 'Bitiş' },
      { key: 'dailyLimit', label: 'Günlük Limit' },
      { key: 'usedToday', label: 'Bugün Kullanılan' },
      { key: 'actions', label: 'İşlemler', render: ActionButtons }
    ]}
    data={subscriptions}
  />
</SubscriptionList>
```

---

### 3.2 Get Subscription By ID

**Endpoint:** `GET /api/admin/subscriptions/{id}`

**UI Komponenti:**
```typescript
<SubscriptionDetailPage subscriptionId={456}>
  <DetailHeader>
    <TierBadge tier={subscription.tier} size="large" />
    <h2>Subscription #{subscription.subscriptionId}</h2>
    <StatusBadge status={subscription.status} />
  </DetailHeader>

  <InfoGrid>
    <InfoCard label="Kullanıcı" value={<UserLink user={subscription.user} />} />
    <InfoCard label="Başlangıç" value={formatDate(subscription.startDate)} />
    <InfoCard label="Bitiş" value={formatDate(subscription.endDate)} />
    <InfoCard label="Günlük Limit" value={subscription.dailyLimit} />
    <InfoCard label="Aylık Limit" value={subscription.monthlyLimit} />
    <InfoCard label="Bugün Kullanılan" value={subscription.usedToday} />
    <InfoCard label="Bu Ay Kullanılan" value={subscription.usedThisMonth} />
  </InfoGrid>

  <UsageChart 
    data={subscription.usageHistory}
    title="30 Günlük Kullanım Trendi"
  />

  <ActionPanel>
    <Button onClick={() => handleExtend(subscription.subscriptionId)}>
      Süreyi Uzat
    </Button>
    <Button onClick={() => handleCancel(subscription.subscriptionId)} danger>
      İptal Et
    </Button>
  </ActionPanel>
</SubscriptionDetailPage>
```

---

### 3.3 Assign Subscription

**Endpoint:** `POST /api/admin/subscriptions/assign`

**Kullanılan Personalar:**
- 🛟 **User Support** (günde 3-5 kez)

**Request Body:**
```typescript
interface AssignSubscriptionRequest {
  userId: number;
  tier: 'Trial' | 'S' | 'M' | 'L' | 'XL';
  durationDays: number;
  reason: string;
}
```

**UI Komponenti:**
```typescript
<AssignSubscriptionModal userId={123}>
  <Form onSubmit={handleAssign}>
    <UserAutocomplete
      label="Kullanıcı Seç"
      value={selectedUser}
      onChange={setSelectedUser}
      required
    />

    <Select
      label="Tier"
      options={[
        { value: 'Trial', label: '🆓 Trial (7 gün)' },
        { value: 'S', label: '🥉 Small (15 analiz/gün)' },
        { value: 'M', label: '🥈 Medium (30 analiz/gün)' },
        { value: 'L', label: '🥇 Large (60 analiz/gün)' },
        { value: 'XL', label: '💎 XL (100 analiz/gün)' }
      ]}
      value={tier}
      onChange={setTier}
      required
    />

    <NumberInput
      label="Süre (Gün)"
      value={durationDays}
      onChange={setDurationDays}
      min={1}
      max={365}
      required
    />

    <Textarea
      label="Neden"
      placeholder="Neden bu aboneliği atıyorsunuz? (örn: Goodwill, promosyon, destek)"
      value={reason}
      onChange={(e) => setReason(e.target.value)}
      required
    />

    <InfoBox>
      <strong>Özet:</strong>
      <ul>
        <li>Kullanıcı: {selectedUser?.fullName}</li>
        <li>Tier: {tier}</li>
        <li>Süre: {durationDays} gün</li>
        <li>Başlangıç: {formatDate(new Date())}</li>
        <li>Bitiş: {formatDate(addDays(new Date(), durationDays))}</li>
      </ul>
    </InfoBox>

    <ButtonGroup>
      <Button variant="text" onClick={onCancel}>İptal</Button>
      <Button variant="contained" type="submit">
        Abonelik Ata
      </Button>
    </ButtonGroup>
  </Form>
</AssignSubscriptionModal>
```

---

## 4. Sponsorship Management Endpoints

### 4.1 Get All Purchases

**Endpoint:** `GET /api/admin/sponsorship/purchases`

**Kullanılan Personalar:**
- 💼 **Sponsorship Manager** (günde 5+ kez)
- 🎯 **Super Admin** (günde 2-3 kez)

**UI Komponenti:**
```typescript
<PurchaseManagement>
  <FilterPanel>
    <Select label="Durum" options={['Tümü', 'Pending', 'Approved', 'Rejected']} />
    <DateRangePicker label="Tarih Aralığı" />
  </FilterPanel>

  <StatusTabs>
    <Tab label="Bekleyen Onaylar" count={pendingCount} />
    <Tab label="Onaylanmış" count={approvedCount} />
    <Tab label="Reddedilmiş" count={rejectedCount} />
  </StatusTabs>

  <PurchaseGrid>
    {purchases.map(purchase => (
      <PurchaseCard key={purchase.purchaseId}>
        <SponsorInfo sponsor={purchase.sponsor} />
        <PackageDetails package={purchase.package} />
        <PaymentInfo 
          amount={purchase.totalAmount}
          status={purchase.paymentStatus}
        />
        <ActionButtons>
          {purchase.status === 'Pending' && (
            <>
              <Button color="success" onClick={() => handleApprove(purchase.purchaseId)}>
                Onayla
              </Button>
              <Button color="error" onClick={() => handleReject(purchase.purchaseId)}>
                Reddet
              </Button>
            </>
          )}
          <Button variant="text" onClick={() => navigate(`/purchases/${purchase.purchaseId}`)}>
            Detaylar
          </Button>
        </ActionButtons>
      </PurchaseCard>
    ))}
  </PurchaseGrid>
</PurchaseManagement>
```

---

### 4.2 Sponsor Detailed Report

**Endpoint:** `GET /api/admin/sponsorship/sponsors/{id}/detailed-report`

**Kullanılan Personalar:**
- 💼 **Sponsorship Manager** (haftada 5+ kez)
- 📊 **Data Analyst** (ayda 1 kez - tüm sponsorlar için)

**UI Komponenti:**
```typescript
<SponsorDetailedReport sponsorId={234}>
  <ReportHeader>
    <SponsorAvatar sponsor={sponsor} />
    <h1>{sponsor.companyName || sponsor.fullName}</h1>
    <PerformanceScore score={sponsor.performanceScore} />
  </ReportHeader>

  <KPIRow>
    <KPICard 
      label="Toplam Satın Alma"
      value={report.totalPurchases}
      trend="+5 (son 30 gün)"
    />
    <KPICard 
      label="Toplam Harcama"
      value={formatCurrency(report.totalSpent)}
      trend="+₺12,345"
    />
    <KPICard 
      label="Kod Kullanım Oranı"
      value={`${report.codeUsageRate}%`}
      trend={report.codeUsageRate > 50 ? 'success' : 'warning'}
    />
    <KPICard 
      label="Farmer Reach"
      value={report.farmerReach}
      info="Toplam kaç çiftçiye ulaştı"
    />
  </KPIRow>

  <ChartSection>
    <Chart 
      type="line" 
      title="Aylık Satın Alma Trendi"
      data={report.monthlyPurchaseTrend}
    />
    <Chart 
      type="bar" 
      title="Tier Bazlı Dağılım"
      data={report.tierBreakdown}
    />
  </ChartSection>

  <CodeManagementSection>
    <h3>Kod Yönetimi</h3>
    <StatRow>
      <Stat label="Üretilen Kod" value={report.totalCodesGenerated} />
      <Stat label="Kullanılan Kod" value={report.totalCodesUsed} />
      <Stat label="Kalan Kod" value={report.remainingCodes} />
      <Stat label="Süresi Dolan" value={report.expiredCodes} />
    </StatRow>
    <Button onClick={() => navigate(`/sponsors/${sponsorId}/codes`)}>
      Kod Listesine Git
    </Button>
  </CodeManagementSection>

  <ActionHistory>
    <h3>İşlem Geçmişi</h3>
    <Timeline>
      {report.activityHistory.map(activity => (
        <TimelineItem 
          key={activity.id}
          date={activity.timestamp}
          action={activity.action}
          details={activity.details}
        />
      ))}
    </Timeline>
  </ActionHistory>

  <ExportButton format="pdf" filename={`sponsor-report-${sponsorId}`} />
</SponsorDetailedReport>
```

---

## 5. Plant Analysis OBO Endpoints

### 5.1 Create Analysis On-Behalf-Of

**Endpoint:** `POST /api/admin/plant-analysis/on-behalf-of`

**Kullanılan Personalar:**
- 🛟 **User Support** (günde 2-3 kez)

**Request Body:**
```typescript
interface CreateOBOAnalysisRequest {
  userId: number;
  imageUrl: string;
  notes?: string;
  reason: string;
}
```

**UI Komponenti:**
```typescript
<CreateOBOAnalysisModal>
  <Form onSubmit={handleCreate}>
    <UserAutocomplete
      label="Kullanıcı Seç"
      placeholder="Analiz hangi kullanıcı için yapılacak?"
      value={selectedUser}
      onChange={setSelectedUser}
      required
    />

    <ImageUpload
      label="Bitki Fotoğrafı"
      accept="image/*"
      onUpload={handleImageUpload}
      preview={true}
      maxSize={5 * 1024 * 1024} // 5MB
      required
    />

    <Textarea
      label="Notlar (Opsiyonel)"
      placeholder="Kullanıcıya özel notlar ekleyebilirsiniz"
      value={notes}
      onChange={(e) => setNotes(e.target.value)}
    />

    <Textarea
      label="Neden (Zorunlu)"
      placeholder="Neden kullanıcı adına analiz yapıyorsunuz? (örn: Uygulama çalışmıyor, kullanıcı talebi)"
      value={reason}
      onChange={(e) => setReason(e.target.value)}
      required
      minLength={10}
    />

    <Alert severity="info">
      <strong>Bilgi:</strong> Bu analiz kullanıcı adına oluşturulacak ve 
      kullanıcının hesabında görünecek. Admin olarak yaptığınız bu işlem 
      audit log'a kaydedilecektir.
    </Alert>

    <ButtonGroup>
      <Button variant="text" onClick={onCancel}>İptal</Button>
      <Button 
        variant="contained" 
        type="submit"
        disabled={!selectedUser || !imageUrl || reason.length < 10}
      >
        Analiz Oluştur
      </Button>
    </ButtonGroup>
  </Form>
</CreateOBOAnalysisModal>
```

---

### 5.2 Get All OBO Analyses

**Endpoint:** `GET /api/admin/plant-analysis/on-behalf-of`

**Kullanılan Personalar:**
- 🔍 **Auditor** (haftada 1 kez)
- 🛟 **User Support** (kendi yaptıklarını kontrol için)

**Query Parameters:**
```typescript
interface GetOBOAnalysesParams {
  page: number;
  pageSize: number;
  adminUserId?: number;    // Hangi admin yaptı
  targetUserId?: number;   // Hangi kullanıcı için
  status?: string;         // Processing, Completed, Failed
}
```

**UI Komponenti:**
```typescript
<OBOAnalysesList>
  <FilterPanel>
    <UserAutocomplete 
      label="Admin'e Göre Filtrele"
      value={filterAdmin}
      onChange={setFilterAdmin}
    />
    <UserAutocomplete 
      label="Kullanıcıya Göre Filtrele"
      value={filterUser}
      onChange={setFilterUser}
    />
    <Select 
      label="Durum"
      options={['Tümü', 'Processing', 'Completed', 'Failed']}
    />
  </FilterPanel>

  <DataTable
    columns={[
      { key: 'analysisId', label: 'Analiz ID' },
      { key: 'createdByAdminId', label: 'Yapan Admin', render: AdminUserLink },
      { key: 'userId', label: 'Hedef Kullanıcı', render: UserLink },
      { key: 'analysisStatus', label: 'Durum', render: StatusBadge },
      { key: 'createdDate', label: 'Oluşturulma' },
      { key: 'analysisDate', label: 'Tamamlanma' },
      { key: 'reason', label: 'Neden', render: TruncatedText },
      { key: 'actions', label: 'İşlemler', render: ActionButtons }
    ]}
    data={oboAnalyses}
  />
</OBOAnalysesList>
```

---

## 6. Activity & Audit Logs

### 6.1 Activity Logs

**Endpoint:** `GET /api/admin/analytics/activity-logs`

**Kullanılan Personalar:**
- 🔍 **Auditor** (günde 10+ kez)
- 🎯 **Super Admin** (günde 2-3 kez)

**Query Parameters:**
```typescript
interface GetActivityLogsParams {
  page: number;
  pageSize: number;
  userId?: number;        // Filter by admin OR target user
  actionType?: string;    // e.g., "CreatePurchaseOnBehalfOf"
  startDate?: string;
  endDate?: string;
}
```

**UI Komponenti:**
```typescript
<ActivityLogsPage>
  <AdvancedFilterPanel>
    <DateRangePicker 
      presets={["Today", "Last 7 Days", "Last 30 Days"]}
      onChange={handleDateChange}
    />
    
    <UserAutocomplete 
      label="Kullanıcı (Admin veya Hedef)"
      onChange={setFilterUser}
    />

    <Autocomplete
      label="İşlem Tipi"
      options={actionTypes}
      freeSolo
      onChange={setFilterAction}
    />

    <Checkbox 
      label="Sadece OBO İşlemler"
      checked={onlyOBO}
      onChange={(e) => setOnlyOBO(e.target.checked)}
    />
  </AdvancedFilterPanel>

  <LogTable>
    <TableHead>
      <TableRow>
        <TableCell>Zaman</TableCell>
        <TableCell>Admin</TableCell>
        <TableCell>İşlem</TableCell>
        <TableCell>Hedef</TableCell>
        <TableCell>Detaylar</TableCell>
        <TableCell>OBO</TableCell>
        <TableCell>IP</TableCell>
      </TableRow>
    </TableHead>
    <TableBody>
      {logs.map(log => (
        <TableRow 
          key={log.id}
          hover
          onClick={() => handleViewDetails(log)}
          sx={{ cursor: 'pointer' }}
        >
          <TableCell>{formatDateTime(log.timestamp)}</TableCell>
          <TableCell>
            <UserChip userId={log.adminUserId} />
          </TableCell>
          <TableCell>
            <ActionBadge action={log.action} />
          </TableCell>
          <TableCell>
            {log.targetUserId && <UserChip userId={log.targetUserId} />}
          </TableCell>
          <TableCell>
            <TruncatedText text={log.reason} maxLength={50} />
          </TableCell>
          <TableCell>
            {log.isOnBehalfOf && <Chip label="OBO" size="small" color="warning" />}
          </TableCell>
          <TableCell>
            <Tooltip title={log.userAgent}>
              <Code>{log.ipAddress}</Code>
            </Tooltip>
          </TableCell>
        </TableRow>
      ))}
    </TableBody>
  </LogTable>

  <Pagination 
    page={currentPage}
    totalPages={Math.ceil(totalCount / pageSize)}
    onChange={handlePageChange}
  />

  <ExportButton 
    format="csv"
    filename={`activity-logs-${formatDate(new Date())}`}
    data={logs}
  />
</ActivityLogsPage>

{/* Log Detail Modal */}
<LogDetailModal open={selectedLog !== null} onClose={() => setSelectedLog(null)}>
  {selectedLog && (
    <>
      <DetailRow label="ID" value={selectedLog.id} />
      <DetailRow label="Zaman" value={formatDateTime(selectedLog.timestamp)} />
      <DetailRow label="Admin" value={<UserLink userId={selectedLog.adminUserId} />} />
      <DetailRow label="İşlem" value={selectedLog.action} />
      <DetailRow label="Entity Type" value={selectedLog.entityType} />
      <DetailRow label="Entity ID" value={selectedLog.entityId} />
      {selectedLog.targetUserId && (
        <DetailRow label="Hedef Kullanıcı" value={<UserLink userId={selectedLog.targetUserId} />} />
      )}
      <DetailRow label="OBO" value={selectedLog.isOnBehalfOf ? 'Evet' : 'Hayır'} />
      <DetailRow label="IP Address" value={selectedLog.ipAddress} />
      <DetailRow label="User Agent" value={selectedLog.userAgent} />
      <DetailRow label="Request Path" value={selectedLog.requestPath} />
      
      <Divider sx={{ my: 2 }} />
      
      <DetailRow label="Neden" value={selectedLog.reason} fullWidth />
      
      {selectedLog.beforeState && (
        <JsonViewer 
          label="Önceki Durum" 
          data={JSON.parse(selectedLog.beforeState)} 
        />
      )}
      
      {selectedLog.afterState && (
        <JsonViewer 
          label="Sonraki Durum" 
          data={JSON.parse(selectedLog.afterState)} 
        />
      )}
    </>
  )}
</LogDetailModal>
```

---

## Frontend Technology Stack Önerisi

### Core Technologies
```json
{
  "react": "^18.2.0",
  "typescript": "^5.0.0",
  "vite": "^5.0.0"
}
```

### UI Framework (Önerilen: Material-UI)
```json
{
  "@mui/material": "^5.14.0",
  "@mui/icons-material": "^5.14.0",
  "@mui/x-data-grid": "^6.18.0",
  "@mui/x-date-pickers": "^6.18.0"
}
```

**Neden Material-UI?**
- ✅ Comprehensive component library
- ✅ Excellent TypeScript support
- ✅ Built-in accessibility
- ✅ Professional admin UI patterns
- ✅ Data grid with pagination, sorting, filtering

### State Management (Önerilen: Redux Toolkit + RTK Query)
```json
{
  "@reduxjs/toolkit": "^2.0.0",
  "react-redux": "^9.0.0"
}
```

### Charts (Önerilen: Recharts)
```json
{
  "recharts": "^2.10.0"
}
```

### Form Management (Önerilen: React Hook Form)
```json
{
  "react-hook-form": "^7.48.0",
  "@hookform/resolvers": "^3.3.0",
  "yup": "^1.3.0"
}
```

### Date Handling
```json
{
  "date-fns": "^2.30.0"
}
```

### HTTP Client
```json
{
  "axios": "^1.6.0"
}
```

---

## Sonraki Adımlar

1. **Project Setup** (1 gün)
   - Create React + TypeScript + Vite project
   - Install dependencies (Material-UI, Redux Toolkit, etc.)
   - Setup folder structure
   - Configure ESLint, Prettier

2. **API Layer** (2 gün)
   - Axios client setup with interceptors
   - API service modules (analytics, users, subscriptions, etc.)
   - TypeScript interfaces for all requests/responses
   - Error handling utilities

3. **Authentication** (2 gün)
   - Login page
   - JWT token management
   - Protected routes
   - Role-based access control

4. **Layout & Navigation** (2 gün)
   - AdminLayout component (sidebar, header, footer)
   - Sidebar menu (persona-based items)
   - Breadcrumbs
   - Responsive design

5. **Dashboard** (3 gün)
   - KPI cards
   - Charts (Recharts integration)
   - Activity feed
   - Quick actions

6. **User Management** (5 gün)
   - User list with pagination
   - User search
   - User detail page
   - User actions (activate, deactivate)
   - Bulk operations

**Toplam Tahmini Süre:** ~15 gün (3 hafta)

---

**Hazırlayan:** Claude Code  
**Tarih:** 2025-10-23  
**Durum:** Frontend implementation başlamaya hazır  
**Sonraki:** React component development
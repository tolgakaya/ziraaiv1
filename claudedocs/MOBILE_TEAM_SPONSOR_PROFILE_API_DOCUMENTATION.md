# Sponsor Profile API Documentation - Mobile Team Implementation Guide

**Version**: 1.0  
**Date**: 2025-11-01  
**Base URL**: `https://ziraai-api-sit.up.railway.app` (Staging)  
**API Version Header**: `x-dev-arch-version: 1.0`

---

## Overview

Sponsor profile management API with complete CRUD operations. This update adds 10 new optional fields for social media links and business information, plus a new update endpoint.

### New Features
- ✅ 10 new optional profile fields (social media + business info)
- ✅ Update profile endpoint with partial update support
- ✅ Email and password update capability
- ✅ Enhanced profile response with all fields

---

## Authentication

All endpoints require JWT Bearer token authentication with `Sponsor` or `Admin` role.

```http
Authorization: Bearer {your_jwt_token}
x-dev-arch-version: 1.0
```

---

## Endpoints

### 1. Get Sponsor Profile

Retrieve the complete sponsor profile for the authenticated user.

**Endpoint**: `GET /api/sponsorship/profile`

**Authorization**: Required (Sponsor or Admin role)

**Headers**:
```http
GET /api/sponsorship/profile HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9...
x-dev-arch-version: 1.0
```

**Request**: No body required

**Response 200 OK**:
```json
{
  "data": {
    "id": 1,
    "sponsorId": 166,
    "companyName": "Green Tech Solutions",
    "companyDescription": "Leading agricultural technology provider in Turkey",
    "sponsorLogoUrl": "https://example.com/logos/greentech.png",
    "websiteUrl": "https://greentech.com.tr",
    "contactEmail": "contact@greentech.com.tr",
    "contactPhone": "+905551234567",
    "contactPerson": "Ahmet Yılmaz",
    
    // ✨ NEW: Social Media Links
    "linkedInUrl": "https://linkedin.com/company/greentech-solutions",
    "twitterUrl": "https://twitter.com/greentech_tr",
    "facebookUrl": "https://facebook.com/greentechsolutions",
    "instagramUrl": "https://instagram.com/greentech.tr",
    
    // ✨ NEW: Business Information
    "taxNumber": "1234567890",
    "tradeRegistryNumber": "987654/2020",
    "address": "Atatürk Caddesi No: 123 Kat: 5",
    "city": "Istanbul",
    "country": "Turkey",
    "postalCode": "34340",
    
    // Existing fields
    "companyType": "Agriculture",
    "businessModel": "B2B",
    "isVerifiedCompany": true,
    "isActive": true,
    "totalPurchases": 15,
    "totalCodesGenerated": 500,
    "totalCodesRedeemed": 342,
    "totalInvestment": 25000.00,
    "createdDate": "2024-01-15T10:30:00",
    "updatedDate": "2025-11-01T14:22:00"
  },
  "success": true,
  "message": "Sponsor profile retrieved successfully"
}
```

**Response 404 Not Found**:
```json
{
  "success": false,
  "message": "Sponsor profile not found"
}
```

**Response 401 Unauthorized**:
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

---

### 2. Create Sponsor Profile (Admin Only)

Create a new sponsor profile with user account. This endpoint is typically used by administrators.

**Endpoint**: `POST /api/sponsorship/create-profile`

**Authorization**: Required (Admin role only)

**Headers**:
```http
POST /api/sponsorship/create-profile HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer {admin_jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

**Request Body**:
```json
{
  // Required fields
  "companyName": "New Sponsor Company Ltd",
  "companyDescription": "Premium agricultural solutions provider",
  "contactEmail": "info@newsponsor.com",
  "contactPhone": "+905559876543",
  
  // Optional fields
  "sponsorLogoUrl": "https://example.com/logos/newsponsor.png",
  "websiteUrl": "https://newsponsor.com",
  "contactPerson": "Mehmet Demir",
  "companyType": "Agriculture",
  "businessModel": "B2B",
  "password": "SecurePass123!",
  
  // ✨ NEW: Social Media Links (Optional)
  "linkedInUrl": "https://linkedin.com/company/newsponsor",
  "twitterUrl": "https://twitter.com/newsponsor",
  "facebookUrl": "https://facebook.com/newsponsor",
  "instagramUrl": "https://instagram.com/newsponsor",
  
  // ✨ NEW: Business Information (Optional)
  "taxNumber": "5544332211",
  "tradeRegistryNumber": "123456/2024",
  "address": "İstiklal Caddesi No: 456",
  "city": "Ankara",
  "country": "Turkey",
  "postalCode": "06100"
}
```

**Field Validations**:
- `companyName`: Required, max 200 characters
- `companyDescription`: Required, max 1000 characters
- `contactEmail`: Required, valid email format, max 100 characters
- `contactPhone`: Required, max 20 characters
- `password`: Optional, min 6 characters (if not provided, random password generated)
- All new fields: Optional, max 200 characters

**Response 200 OK**:
```json
{
  "data": {
    "sponsorId": 167,
    "userId": 167,
    "companyName": "New Sponsor Company Ltd",
    "contactEmail": "info@newsponsor.com",
    "message": "Sponsor profile created successfully. User account created with Sponsor role."
  },
  "success": true,
  "message": "Sponsor profile created successfully"
}
```

**Response 400 Bad Request** (Validation Error):
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Company name is required",
    "Invalid email format",
    "Password must be at least 6 characters"
  ]
}
```

**Response 400 Bad Request** (Duplicate Email):
```json
{
  "success": false,
  "message": "A user with this email already exists"
}
```

---

### 3. Update Sponsor Profile ✨ NEW

Update sponsor profile information. Supports **partial updates** - only send fields you want to update.

**Endpoint**: `PUT /api/sponsorship/update-profile`

**Authorization**: Required (Sponsor or Admin role)

**Headers**:
```http
PUT /api/sponsorship/update-profile HTTP/1.1
Host: ziraai-api-sit.up.railway.app
Authorization: Bearer {sponsor_jwt_token}
x-dev-arch-version: 1.0
Content-Type: application/json
```

**Request Body** (All fields optional - partial update):
```json
{
  "companyName": "Updated Company Name",
  "companyDescription": "Updated description",
  "sponsorLogoUrl": "https://example.com/new-logo.png",
  "websiteUrl": "https://newwebsite.com",
  "contactEmail": "newemail@company.com",
  "contactPhone": "+905551112233",
  "contactPerson": "New Contact Person",
  "companyType": "AgriTech",
  "businessModel": "B2B2C",
  
  // ✨ NEW: Social Media Links
  "linkedInUrl": "https://linkedin.com/company/updated",
  "twitterUrl": "https://twitter.com/updated",
  "facebookUrl": "https://facebook.com/updated",
  "instagramUrl": "https://instagram.com/updated",
  
  // ✨ NEW: Business Information
  "taxNumber": "9988776655",
  "tradeRegistryNumber": "555666/2024",
  "address": "Yeni Adres Sokak No: 789",
  "city": "Izmir",
  "country": "Turkey",
  "postalCode": "35000",
  
  // Optional: Update password
  "password": "NewSecurePassword123!"
}
```

**Partial Update Example** (Update only social media):
```json
{
  "linkedInUrl": "https://linkedin.com/company/mycompany",
  "twitterUrl": "https://twitter.com/mycompany",
  "facebookUrl": "https://facebook.com/mycompany",
  "instagramUrl": "https://instagram.com/mycompany"
}
```

**Partial Update Example** (Update only business info):
```json
{
  "taxNumber": "1122334455",
  "address": "New Business Address",
  "city": "Istanbul",
  "postalCode": "34000"
}
```

**Field Validations** (only when field is provided):
- `companyName`: Max 200 characters
- `companyDescription`: Max 1000 characters
- `contactEmail`: Valid email format, max 100 characters, unique check
- `contactPhone`: Max 20 characters
- `sponsorLogoUrl`: Max 200 characters
- `websiteUrl`: Max 200 characters
- All URL fields: Max 200 characters
- Business fields: Max 100-200 characters
- `password`: Min 6 characters

**Response 200 OK**:
```json
{
  "success": true,
  "message": "Sponsor profile updated successfully"
}
```

**Response 400 Bad Request** (Validation Error):
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Invalid email format",
    "Password must be at least 6 characters",
    "Company name cannot exceed 200 characters"
  ]
}
```

**Response 400 Bad Request** (Duplicate Email):
```json
{
  "success": false,
  "message": "This email is already in use by another user"
}
```

**Response 404 Not Found**:
```json
{
  "success": false,
  "message": "Sponsor profile not found"
}
```

**Response 401 Unauthorized**:
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

---

## Mobile Implementation Guide

### iOS (Swift) Example

```swift
// MARK: - Models

struct SponsorProfile: Codable {
    let id: Int
    let sponsorId: Int
    let companyName: String
    let companyDescription: String?
    let sponsorLogoUrl: String?
    let websiteUrl: String?
    let contactEmail: String
    let contactPhone: String
    let contactPerson: String?
    
    // Social Media
    let linkedInUrl: String?
    let twitterUrl: String?
    let facebookUrl: String?
    let instagramUrl: String?
    
    // Business Info
    let taxNumber: String?
    let tradeRegistryNumber: String?
    let address: String?
    let city: String?
    let country: String?
    let postalCode: String?
    
    let companyType: String?
    let businessModel: String?
    let isVerifiedCompany: Bool
    let isActive: Bool
    let totalPurchases: Int
    let totalCodesGenerated: Int
    let totalCodesRedeemed: Int
    let totalInvestment: Decimal
    let createdDate: Date
    let updatedDate: Date?
}

struct UpdateSponsorProfileRequest: Codable {
    let companyName: String?
    let companyDescription: String?
    let sponsorLogoUrl: String?
    let websiteUrl: String?
    let contactEmail: String?
    let contactPhone: String?
    let contactPerson: String?
    
    // Social Media
    let linkedInUrl: String?
    let twitterUrl: String?
    let facebookUrl: String?
    let instagramUrl: String?
    
    // Business Info
    let taxNumber: String?
    let tradeRegistryNumber: String?
    let address: String?
    let city: String?
    let country: String?
    let postalCode: String?
    
    let password: String?
}

// MARK: - API Service

class SponsorProfileService {
    private let baseURL = "https://ziraai-api-sit.up.railway.app/api"
    private let apiVersion = "1.0"
    
    func getProfile() async throws -> SponsorProfile {
        var request = URLRequest(url: URL(string: "\(baseURL)/sponsorship/profile")!)
        request.setValue("Bearer \(authToken)", forHTTPHeaderField: "Authorization")
        request.setValue(apiVersion, forHTTPHeaderField: "x-dev-arch-version")
        
        let (data, _) = try await URLSession.shared.data(for: request)
        let response = try JSONDecoder().decode(APIResponse<SponsorProfile>.self, from: data)
        
        guard response.success, let profile = response.data else {
            throw APIError.requestFailed(response.message)
        }
        
        return profile
    }
    
    func updateProfile(_ updateRequest: UpdateSponsorProfileRequest) async throws {
        var request = URLRequest(url: URL(string: "\(baseURL)/sponsorship/update-profile")!)
        request.httpMethod = "PUT"
        request.setValue("Bearer \(authToken)", forHTTPHeaderField: "Authorization")
        request.setValue(apiVersion, forHTTPHeaderField: "x-dev-arch-version")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(updateRequest)
        
        let (data, _) = try await URLSession.shared.data(for: request)
        let response = try JSONDecoder().decode(APIResponse<EmptyData>.self, from: data)
        
        guard response.success else {
            throw APIError.requestFailed(response.message)
        }
    }
}

// MARK: - Usage Example

// Get profile
let service = SponsorProfileService()
let profile = try await service.getProfile()
print("Company: \(profile.companyName)")
print("LinkedIn: \(profile.linkedInUrl ?? "Not set")")

// Update social media only
let socialMediaUpdate = UpdateSponsorProfileRequest(
    companyName: nil,
    companyDescription: nil,
    sponsorLogoUrl: nil,
    websiteUrl: nil,
    contactEmail: nil,
    contactPhone: nil,
    contactPerson: nil,
    linkedInUrl: "https://linkedin.com/company/updated",
    twitterUrl: "https://twitter.com/updated",
    facebookUrl: "https://facebook.com/updated",
    instagramUrl: "https://instagram.com/updated",
    taxNumber: nil,
    tradeRegistryNumber: nil,
    address: nil,
    city: nil,
    country: nil,
    postalCode: nil,
    password: nil
)

try await service.updateProfile(socialMediaUpdate)
```

### Android (Kotlin) Example

```kotlin
// MARK: - Models

data class SponsorProfile(
    val id: Int,
    val sponsorId: Int,
    val companyName: String,
    val companyDescription: String?,
    val sponsorLogoUrl: String?,
    val websiteUrl: String?,
    val contactEmail: String,
    val contactPhone: String,
    val contactPerson: String?,
    
    // Social Media
    val linkedInUrl: String?,
    val twitterUrl: String?,
    val facebookUrl: String?,
    val instagramUrl: String?,
    
    // Business Info
    val taxNumber: String?,
    val tradeRegistryNumber: String?,
    val address: String?,
    val city: String?,
    val country: String?,
    val postalCode: String?,
    
    val companyType: String?,
    val businessModel: String?,
    val isVerifiedCompany: Boolean,
    val isActive: Boolean,
    val totalPurchases: Int,
    val totalCodesGenerated: Int,
    val totalCodesRedeemed: Int,
    val totalInvestment: Double,
    val createdDate: String,
    val updatedDate: String?
)

data class UpdateSponsorProfileRequest(
    val companyName: String? = null,
    val companyDescription: String? = null,
    val sponsorLogoUrl: String? = null,
    val websiteUrl: String? = null,
    val contactEmail: String? = null,
    val contactPhone: String? = null,
    val contactPerson: String? = null,
    
    // Social Media
    val linkedInUrl: String? = null,
    val twitterUrl: String? = null,
    val facebookUrl: String? = null,
    val instagramUrl: String? = null,
    
    // Business Info
    val taxNumber: String? = null,
    val tradeRegistryNumber: String? = null,
    val address: String? = null,
    val city: String? = null,
    val country: String? = null,
    val postalCode: String? = null,
    
    val password: String? = null
)

// MARK: - API Service (Retrofit)

interface SponsorProfileApi {
    @GET("sponsorship/profile")
    suspend fun getProfile(
        @Header("Authorization") authorization: String,
        @Header("x-dev-arch-version") apiVersion: String = "1.0"
    ): ApiResponse<SponsorProfile>
    
    @PUT("sponsorship/update-profile")
    suspend fun updateProfile(
        @Header("Authorization") authorization: String,
        @Header("x-dev-arch-version") apiVersion: String = "1.0",
        @Body request: UpdateSponsorProfileRequest
    ): ApiResponse<Unit>
}

// MARK: - Repository

class SponsorProfileRepository(
    private val api: SponsorProfileApi,
    private val tokenProvider: TokenProvider
) {
    suspend fun getProfile(): Result<SponsorProfile> {
        return try {
            val response = api.getProfile(
                authorization = "Bearer ${tokenProvider.getToken()}"
            )
            
            if (response.success && response.data != null) {
                Result.success(response.data)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    suspend fun updateProfile(request: UpdateSponsorProfileRequest): Result<Unit> {
        return try {
            val response = api.updateProfile(
                authorization = "Bearer ${tokenProvider.getToken()}",
                request = request
            )
            
            if (response.success) {
                Result.success(Unit)
            } else {
                Result.failure(Exception(response.message))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}

// MARK: - ViewModel Example

class SponsorProfileViewModel(
    private val repository: SponsorProfileRepository
) : ViewModel() {
    
    private val _profile = MutableLiveData<SponsorProfile>()
    val profile: LiveData<SponsorProfile> = _profile
    
    private val _isLoading = MutableLiveData<Boolean>()
    val isLoading: LiveData<Boolean> = _isLoading
    
    fun loadProfile() {
        viewModelScope.launch {
            _isLoading.value = true
            repository.getProfile()
                .onSuccess { _profile.value = it }
                .onFailure { /* Handle error */ }
            _isLoading.value = false
        }
    }
    
    fun updateSocialMedia(
        linkedIn: String?,
        twitter: String?,
        facebook: String?,
        instagram: String?
    ) {
        viewModelScope.launch {
            _isLoading.value = true
            
            val request = UpdateSponsorProfileRequest(
                linkedInUrl = linkedIn,
                twitterUrl = twitter,
                facebookUrl = facebook,
                instagramUrl = instagram
            )
            
            repository.updateProfile(request)
                .onSuccess { loadProfile() }
                .onFailure { /* Handle error */ }
            
            _isLoading.value = false
        }
    }
}

// MARK: - Usage in Composable

@Composable
fun SponsorProfileScreen(viewModel: SponsorProfileViewModel) {
    val profile by viewModel.profile.observeAsState()
    val isLoading by viewModel.isLoading.observeAsState(false)
    
    LaunchedEffect(Unit) {
        viewModel.loadProfile()
    }
    
    Column(modifier = Modifier.padding(16.dp)) {
        profile?.let {
            Text("Company: ${it.companyName}")
            Text("Email: ${it.contactEmail}")
            
            // Social Media Section
            Text("Social Media", style = MaterialTheme.typography.h6)
            SocialMediaLink("LinkedIn", it.linkedInUrl)
            SocialMediaLink("Twitter", it.twitterUrl)
            SocialMediaLink("Facebook", it.facebookUrl)
            SocialMediaLink("Instagram", it.instagramUrl)
            
            // Business Info Section
            Text("Business Information", style = MaterialTheme.typography.h6)
            BusinessInfoItem("Tax Number", it.taxNumber)
            BusinessInfoItem("Address", it.address)
            BusinessInfoItem("City", it.city)
            BusinessInfoItem("Postal Code", it.postalCode)
        }
    }
}
```

### Flutter (Dart) Example

```dart
// MARK: - Models

class SponsorProfile {
  final int id;
  final int sponsorId;
  final String companyName;
  final String? companyDescription;
  final String? sponsorLogoUrl;
  final String? websiteUrl;
  final String contactEmail;
  final String contactPhone;
  final String? contactPerson;
  
  // Social Media
  final String? linkedInUrl;
  final String? twitterUrl;
  final String? facebookUrl;
  final String? instagramUrl;
  
  // Business Info
  final String? taxNumber;
  final String? tradeRegistryNumber;
  final String? address;
  final String? city;
  final String? country;
  final String? postalCode;
  
  final String? companyType;
  final String? businessModel;
  final bool isVerifiedCompany;
  final bool isActive;
  final int totalPurchases;
  final int totalCodesGenerated;
  final int totalCodesRedeemed;
  final double totalInvestment;
  final DateTime createdDate;
  final DateTime? updatedDate;

  SponsorProfile({
    required this.id,
    required this.sponsorId,
    required this.companyName,
    this.companyDescription,
    this.sponsorLogoUrl,
    this.websiteUrl,
    required this.contactEmail,
    required this.contactPhone,
    this.contactPerson,
    this.linkedInUrl,
    this.twitterUrl,
    this.facebookUrl,
    this.instagramUrl,
    this.taxNumber,
    this.tradeRegistryNumber,
    this.address,
    this.city,
    this.country,
    this.postalCode,
    this.companyType,
    this.businessModel,
    required this.isVerifiedCompany,
    required this.isActive,
    required this.totalPurchases,
    required this.totalCodesGenerated,
    required this.totalCodesRedeemed,
    required this.totalInvestment,
    required this.createdDate,
    this.updatedDate,
  });

  factory SponsorProfile.fromJson(Map<String, dynamic> json) {
    return SponsorProfile(
      id: json['id'],
      sponsorId: json['sponsorId'],
      companyName: json['companyName'],
      companyDescription: json['companyDescription'],
      sponsorLogoUrl: json['sponsorLogoUrl'],
      websiteUrl: json['websiteUrl'],
      contactEmail: json['contactEmail'],
      contactPhone: json['contactPhone'],
      contactPerson: json['contactPerson'],
      linkedInUrl: json['linkedInUrl'],
      twitterUrl: json['twitterUrl'],
      facebookUrl: json['facebookUrl'],
      instagramUrl: json['instagramUrl'],
      taxNumber: json['taxNumber'],
      tradeRegistryNumber: json['tradeRegistryNumber'],
      address: json['address'],
      city: json['city'],
      country: json['country'],
      postalCode: json['postalCode'],
      companyType: json['companyType'],
      businessModel: json['businessModel'],
      isVerifiedCompany: json['isVerifiedCompany'],
      isActive: json['isActive'],
      totalPurchases: json['totalPurchases'],
      totalCodesGenerated: json['totalCodesGenerated'],
      totalCodesRedeemed: json['totalCodesRedeemed'],
      totalInvestment: (json['totalInvestment'] as num).toDouble(),
      createdDate: DateTime.parse(json['createdDate']),
      updatedDate: json['updatedDate'] != null 
          ? DateTime.parse(json['updatedDate']) 
          : null,
    );
  }
}

class UpdateSponsorProfileRequest {
  final String? companyName;
  final String? companyDescription;
  final String? sponsorLogoUrl;
  final String? websiteUrl;
  final String? contactEmail;
  final String? contactPhone;
  final String? contactPerson;
  
  // Social Media
  final String? linkedInUrl;
  final String? twitterUrl;
  final String? facebookUrl;
  final String? instagramUrl;
  
  // Business Info
  final String? taxNumber;
  final String? tradeRegistryNumber;
  final String? address;
  final String? city;
  final String? country;
  final String? postalCode;
  
  final String? password;

  UpdateSponsorProfileRequest({
    this.companyName,
    this.companyDescription,
    this.sponsorLogoUrl,
    this.websiteUrl,
    this.contactEmail,
    this.contactPhone,
    this.contactPerson,
    this.linkedInUrl,
    this.twitterUrl,
    this.facebookUrl,
    this.instagramUrl,
    this.taxNumber,
    this.tradeRegistryNumber,
    this.address,
    this.city,
    this.country,
    this.postalCode,
    this.password,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    if (companyName != null) data['companyName'] = companyName;
    if (companyDescription != null) data['companyDescription'] = companyDescription;
    if (sponsorLogoUrl != null) data['sponsorLogoUrl'] = sponsorLogoUrl;
    if (websiteUrl != null) data['websiteUrl'] = websiteUrl;
    if (contactEmail != null) data['contactEmail'] = contactEmail;
    if (contactPhone != null) data['contactPhone'] = contactPhone;
    if (contactPerson != null) data['contactPerson'] = contactPerson;
    if (linkedInUrl != null) data['linkedInUrl'] = linkedInUrl;
    if (twitterUrl != null) data['twitterUrl'] = twitterUrl;
    if (facebookUrl != null) data['facebookUrl'] = facebookUrl;
    if (instagramUrl != null) data['instagramUrl'] = instagramUrl;
    if (taxNumber != null) data['taxNumber'] = taxNumber;
    if (tradeRegistryNumber != null) data['tradeRegistryNumber'] = tradeRegistryNumber;
    if (address != null) data['address'] = address;
    if (city != null) data['city'] = city;
    if (country != null) data['country'] = country;
    if (postalCode != null) data['postalCode'] = postalCode;
    if (password != null) data['password'] = password;
    return data;
  }
}

// MARK: - API Service

class SponsorProfileService {
  static const String baseUrl = 'https://ziraai-api-sit.up.railway.app/api';
  static const String apiVersion = '1.0';
  
  final Dio _dio;
  
  SponsorProfileService(this._dio);
  
  Future<SponsorProfile> getProfile(String token) async {
    try {
      final response = await _dio.get(
        '$baseUrl/sponsorship/profile',
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
            'x-dev-arch-version': apiVersion,
          },
        ),
      );
      
      if (response.data['success'] == true && response.data['data'] != null) {
        return SponsorProfile.fromJson(response.data['data']);
      } else {
        throw Exception(response.data['message'] ?? 'Failed to load profile');
      }
    } catch (e) {
      throw Exception('Failed to load profile: $e');
    }
  }
  
  Future<void> updateProfile(
    String token, 
    UpdateSponsorProfileRequest request
  ) async {
    try {
      final response = await _dio.put(
        '$baseUrl/sponsorship/update-profile',
        data: request.toJson(),
        options: Options(
          headers: {
            'Authorization': 'Bearer $token',
            'x-dev-arch-version': apiVersion,
            'Content-Type': 'application/json',
          },
        ),
      );
      
      if (response.data['success'] != true) {
        throw Exception(response.data['message'] ?? 'Failed to update profile');
      }
    } catch (e) {
      throw Exception('Failed to update profile: $e');
    }
  }
}

// MARK: - Provider Example (using Riverpod)

final sponsorProfileProvider = FutureProvider<SponsorProfile>((ref) async {
  final service = ref.read(sponsorProfileServiceProvider);
  final token = ref.read(authTokenProvider);
  return await service.getProfile(token);
});

// MARK: - Widget Example

class SponsorProfileScreen extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileAsync = ref.watch(sponsorProfileProvider);
    
    return Scaffold(
      appBar: AppBar(title: Text('Sponsor Profile')),
      body: profileAsync.when(
        data: (profile) => SingleChildScrollView(
          padding: EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Company: ${profile.companyName}',
                  style: Theme.of(context).textTheme.headline6),
              SizedBox(height: 16),
              
              // Social Media Section
              Text('Social Media',
                  style: Theme.of(context).textTheme.subtitle1),
              _buildSocialMediaLink('LinkedIn', profile.linkedInUrl),
              _buildSocialMediaLink('Twitter', profile.twitterUrl),
              _buildSocialMediaLink('Facebook', profile.facebookUrl),
              _buildSocialMediaLink('Instagram', profile.instagramUrl),
              
              SizedBox(height: 16),
              
              // Business Info Section
              Text('Business Information',
                  style: Theme.of(context).textTheme.subtitle1),
              _buildInfoRow('Tax Number', profile.taxNumber),
              _buildInfoRow('Address', profile.address),
              _buildInfoRow('City', profile.city),
              _buildInfoRow('Postal Code', profile.postalCode),
              
              SizedBox(height: 24),
              
              // Update Button
              ElevatedButton(
                onPressed: () => _showUpdateDialog(context, ref),
                child: Text('Update Profile'),
              ),
            ],
          ),
        ),
        loading: () => Center(child: CircularProgressIndicator()),
        error: (error, stack) => Center(child: Text('Error: $error')),
      ),
    );
  }
  
  Widget _buildSocialMediaLink(String label, String? url) {
    return url != null && url.isNotEmpty
        ? InkWell(
            onTap: () => _launchUrl(url),
            child: Padding(
              padding: EdgeInsets.symmetric(vertical: 8),
              child: Row(
                children: [
                  Icon(Icons.link, size: 20),
                  SizedBox(width: 8),
                  Text(label),
                  Spacer(),
                  Text(url, style: TextStyle(color: Colors.blue)),
                ],
              ),
            ),
          )
        : SizedBox.shrink();
  }
  
  Widget _buildInfoRow(String label, String? value) {
    return value != null && value.isNotEmpty
        ? Padding(
            padding: EdgeInsets.symmetric(vertical: 4),
            child: Row(
              children: [
                Text('$label: ', style: TextStyle(fontWeight: FontWeight.bold)),
                Text(value),
              ],
            ),
          )
        : SizedBox.shrink();
  }
}
```

---

## Common Use Cases

### Use Case 1: Display Complete Profile

**Scenario**: Show all sponsor information including social media and business details

```
1. Call GET /api/sponsorship/profile
2. Display all fields in UI
3. Show social media icons with links
4. Display business information section
```

### Use Case 2: Update Social Media Only

**Scenario**: User wants to add/update social media links

```json
PUT /api/sponsorship/update-profile
{
  "linkedInUrl": "https://linkedin.com/company/mycompany",
  "twitterUrl": "https://twitter.com/mycompany",
  "facebookUrl": "https://facebook.com/mycompany",
  "instagramUrl": "https://instagram.com/mycompany"
}
```

### Use Case 3: Update Business Information Only

**Scenario**: User wants to update company address and tax info

```json
PUT /api/sponsorship/update-profile
{
  "taxNumber": "1234567890",
  "tradeRegistryNumber": "987654/2024",
  "address": "New Office Address",
  "city": "Istanbul",
  "country": "Turkey",
  "postalCode": "34000"
}
```

### Use Case 4: Update Contact Information

**Scenario**: User wants to change email and phone

```json
PUT /api/sponsorship/update-profile
{
  "contactEmail": "newemail@company.com",
  "contactPhone": "+905551234567",
  "contactPerson": "New Contact Person"
}
```

### Use Case 5: Change Password

**Scenario**: User wants to update password only

```json
PUT /api/sponsorship/update-profile
{
  "password": "NewSecurePassword123!"
}
```

---

## Error Handling

### Common Error Responses

**401 Unauthorized** - Missing or invalid token:
```json
{
  "success": false,
  "message": "Unauthorized"
}
```
**Action**: Redirect to login screen

**400 Bad Request** - Validation errors:
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    "Invalid email format",
    "Password must be at least 6 characters"
  ]
}
```
**Action**: Show validation errors to user

**404 Not Found** - Profile doesn't exist:
```json
{
  "success": false,
  "message": "Sponsor profile not found"
}
```
**Action**: Show error message, possibly redirect to create profile

**500 Internal Server Error**:
```json
{
  "success": false,
  "message": "An unexpected error occurred"
}
```
**Action**: Show generic error, retry option

---

## Testing Endpoints

### cURL Examples

**Get Profile**:
```bash
curl -X GET "https://ziraai-api-sit.up.railway.app/api/sponsorship/profile" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "x-dev-arch-version: 1.0"
```

**Update Profile** (Social Media):
```bash
curl -X PUT "https://ziraai-api-sit.up.railway.app/api/sponsorship/update-profile" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "linkedInUrl": "https://linkedin.com/company/test",
    "twitterUrl": "https://twitter.com/test",
    "facebookUrl": "https://facebook.com/test",
    "instagramUrl": "https://instagram.com/test"
  }'
```

**Update Profile** (Business Info):
```bash
curl -X PUT "https://ziraai-api-sit.up.railway.app/api/sponsorship/update-profile" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "x-dev-arch-version: 1.0" \
  -H "Content-Type: application/json" \
  -d '{
    "taxNumber": "1234567890",
    "address": "Test Address 123",
    "city": "Istanbul",
    "postalCode": "34000"
  }'
```

---

## Migration Guide

### For Existing Mobile Apps

**Step 1**: Update API models to include new fields (all optional)
- Add social media fields: linkedInUrl, twitterUrl, facebookUrl, instagramUrl
- Add business fields: taxNumber, tradeRegistryNumber, address, city, country, postalCode

**Step 2**: Update UI to display new fields (where available)
- Show social media icons/links if URLs provided
- Display business information section if data available

**Step 3**: Add update functionality
- Implement update profile screen
- Support partial updates (only changed fields)
- Add validation for each field type

**Step 4**: Test backward compatibility
- Ensure app works with profiles that don't have new fields (null values)
- Verify existing functionality unchanged

---

## Best Practices

1. **Partial Updates**: Only send fields that changed to minimize payload
2. **Validation**: Validate inputs client-side before API call
3. **Error Handling**: Always handle all possible error responses
4. **Loading States**: Show loading indicators during API calls
5. **Offline Support**: Cache profile data locally, sync when online
6. **URL Validation**: Validate URL formats before sending
7. **Password Updates**: Require confirmation before password change
8. **Success Feedback**: Show success message after update
9. **Data Refresh**: Reload profile after successful update
10. **Token Refresh**: Handle 401 errors with token refresh logic

---

## Support

For questions or issues:
- **Backend Team**: [Your contact info]
- **API Documentation**: This document
- **Postman Collection**: ZiraAI_Complete_API_Collection_v6.1.json
- **Staging URL**: https://ziraai-api-sit.up.railway.app
- **Production URL**: TBD (will be provided before production release)

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-01  
**Status**: ✅ Ready for Implementation

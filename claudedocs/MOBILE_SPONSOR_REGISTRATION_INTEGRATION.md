# Mobile Integration Guide: Sponsor Registration System

**Version**: 2.0
**Date**: 2025-10-09
**Target Audience**: Mobile Development Team (Flutter)
**Related Epic**: Dual-Role Sponsor Registration System

---

## 🚀 Quick Start Summary

### For Phone-Registered Users Becoming Sponsors:

**Before (Phone Registration Only):**
- ✅ Register with phone number
- ✅ Login with SMS OTP
- ❌ No email/password login

**After (Create Sponsor Profile):**
- ✅ Register with phone number (same as before)
- ✅ Login with SMS OTP (still works)
- ✅ **NEW:** Login with email + password
- ✅ Access to Sponsor features

**Required Fields for Profile Creation:**
- `companyName` (string, required)
- `contactEmail` (email, required) - becomes your login email
- `password` (string, min 6 chars, required) - enables email+password login
- `contactPhone`, `contactPerson` (required)
- Others optional

---

## 📋 Overview

This document provides complete integration instructions for the new **dual-role sponsor registration system**. All users now register as **Farmers first**, and can later become **Sponsors** by creating a sponsor profile.

### Key Changes from v1.0

| Aspect | Old System (v1.0) | New System (v2.0) |
|--------|-------------------|-------------------|
| Registration | Could specify `role: "Sponsor"` | Always registers as Farmer |
| Sponsor Access | Required admin assignment or special registration | Self-service: Create sponsor profile |
| User Roles | Single role (Farmer OR Sponsor) | Dual roles (Farmer AND Sponsor) |
| Profile Creation Auth | Required pre-existing Sponsor role | Any authenticated user (Farmer) |
| Email Update | No email update on profile creation | Auto-updates phone-generated email to business email |
| Password for Phone Users | No password support | Password field sets password during profile creation |
| Login Methods (Phone Reg) | SMS OTP only | Email+Password OR SMS OTP |

### Breaking Changes ⚠️

1. **Registration Endpoint**: `role` parameter is now **ignored** (no error, just ignored)
2. **Authorization**: Farmer users can now create sponsor profiles without pre-existing Sponsor role
3. **JWT Claims**: After sponsor profile creation, JWT will contain **both** Farmer and Sponsor roles
4. **Email Update**: Phone registrations with auto-generated emails (`{phone}@phone.ziraai.com`) are updated to business email on profile creation
5. **Password Requirement**: Sponsor profile creation now requires password field for phone-registered users
6. **Login Methods**: Phone-registered users can now login with email+password (in addition to SMS OTP)

---

## 🎯 User Journey

### Journey 1: Farmer Registration (No Change)

```
User Input → Phone Registration → Verify Code → Login → Farmer Features
```

**No changes needed** - existing flow continues to work.

### Journey 2: Farmer → Sponsor Upgrade (NEW)

```
Farmer User → "Become Sponsor" Button → Sponsor Profile Form (Email + Password) → API Call → Email + Password Updated + Sponsor Role Added → Token Refresh → Login with Business Email+Password + Sponsor Features Unlocked
```

**Important**: For users who registered with phone:
- **Before**: Login only with SMS OTP to phone number
- **After Profile Creation**: Login with business email + password (SMS OTP still works for phone number)
- System automatically updates email and sets password during profile creation

---

## 🔗 API Integration

### Base URL Configuration

```dart
// lib/config/api_config.dart
class ApiConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://localhost:5001'
  );

  static const String apiVersion = '1';

  static String get sponsorshipUrl =>
    '$baseUrl/api/v$apiVersion/sponsorship';
}
```

### Environment-Specific URLs

| Environment | Base URL | Example |
|-------------|----------|---------|
| Development | `http://localhost:5001` | Local development |
| Staging | `https://ziraai-api-sit.up.railway.app` | Railway staging |
| Production | `https://api.ziraai.com` | Production |

---

## 📱 Step-by-Step Implementation

### Step 1: Phone Registration (Existing - No Changes)

```dart
// lib/services/auth_service.dart

class AuthService {
  final Dio _dio;

  Future<AuthResponse> registerWithPhone({
    required String mobilePhone,
    required String fullName,
    String? referralCode,
  }) async {
    try {
      // Step 1: Request SMS code
      final smsResponse = await _dio.post(
        '${ApiConfig.baseUrl}/api/v${ApiConfig.apiVersion}/auth/register-phone',
        data: {
          'mobilePhone': mobilePhone,
          'fullName': fullName,
        },
      );

      if (!smsResponse.data['success']) {
        throw Exception(smsResponse.data['message']);
      }

      // Return success - user should verify code next
      return AuthResponse(
        success: true,
        message: 'SMS code sent successfully',
      );
    } catch (e) {
      throw Exception('Phone registration failed: $e');
    }
  }

  Future<LoginResponse> verifyPhoneRegistration({
    required String mobilePhone,
    required int code,
    required String fullName,
    String? referralCode,
  }) async {
    try {
      final response = await _dio.post(
        '${ApiConfig.baseUrl}/api/v${ApiConfig.apiVersion}/auth/verify-phone-register',
        data: {
          'mobilePhone': mobilePhone,
          'code': code,
          'fullName': fullName,
          'referralCode': referralCode,
        },
      );

      if (response.data['success']) {
        final data = response.data['data'];

        // Store tokens
        await _secureStorage.write(
          key: 'access_token',
          value: data['accessToken'],
        );
        await _secureStorage.write(
          key: 'refresh_token',
          value: data['refreshToken'],
        );

        return LoginResponse(
          success: true,
          accessToken: data['accessToken'],
          refreshToken: data['refreshToken'],
          user: User.fromJson(data['user']),
        );
      }

      throw Exception(response.data['message']);
    } catch (e) {
      throw Exception('Phone verification failed: $e');
    }
  }
}
```

**Note**: After registration, user will have **Farmer role only**.

---

### Step 2: Check User Roles (NEW)

Add role checking to determine if user is already a sponsor:

```dart
// lib/services/auth_service.dart

class AuthService {
  /// Check if current user has Sponsor role
  Future<bool> hasSponsorRole() async {
    try {
      final token = await _secureStorage.read(key: 'access_token');
      if (token == null) return false;

      // Decode JWT to check roles
      final decodedToken = JwtDecoder.decode(token);
      final roles = decodedToken['role'] as dynamic;

      if (roles is String) {
        return roles == 'Sponsor' || roles == 'Admin';
      } else if (roles is List) {
        return roles.contains('Sponsor') || roles.contains('Admin');
      }

      return false;
    } catch (e) {
      print('Error checking sponsor role: $e');
      return false;
    }
  }

  /// Get all user roles from JWT
  Future<List<String>> getUserRoles() async {
    try {
      final token = await _secureStorage.read(key: 'access_token');
      if (token == null) return [];

      final decodedToken = JwtDecoder.decode(token);
      final roles = decodedToken['role'] as dynamic;

      if (roles is String) {
        return [roles];
      } else if (roles is List) {
        return List<String>.from(roles);
      }

      return [];
    } catch (e) {
      print('Error getting user roles: $e');
      return [];
    }
  }

  /// Debug: Get all JWT claims (useful for testing)
  Future<Map<String, dynamic>> getDebugUserInfo() async {
    try {
      final response = await _dio.get(
        '${ApiConfig.sponsorshipUrl}/debug/user-info',
        options: Options(
          headers: await _getAuthHeaders(),
        ),
      );

      return response.data['data'];
    } catch (e) {
      print('Debug user info failed: $e');
      return {};
    }
  }
}
```

---

### Step 3: Create Sponsor Profile Service (NEW)

```dart
// lib/services/sponsor_service.dart

class SponsorService {
  final Dio _dio;
  final AuthService _authService;

  SponsorService(this._dio, this._authService);

  /// Create sponsor profile
  /// This will automatically:
  /// 1. Assign Sponsor role to user
  /// 2. Update user's email to contactEmail (if currently using phone-generated email)
  /// 3. Set user's password (if provided and user has no password from phone registration)
  /// This allows phone registrants to login with their business email + password
  Future<SponsorProfileResponse> createSponsorProfile({
    required String companyName,
    required String companyDescription,
    String? sponsorLogoUrl,
    String? websiteUrl,
    required String contactEmail,
    required String contactPhone,
    required String contactPerson,
    String? companyType, // Default: "Agriculture"
    String? businessModel, // Default: "B2B"
    String? password, // IMPORTANT: Required for phone-registered users to enable email+password login
  }) async {
    try {
      final response = await _dio.post(
        '${ApiConfig.sponsorshipUrl}/create-profile',
        data: {
          'companyName': companyName,
          'companyDescription': companyDescription,
          'sponsorLogoUrl': sponsorLogoUrl,
          'websiteUrl': websiteUrl,
          'contactEmail': contactEmail,
          'contactPhone': contactPhone,
          'contactPerson': contactPerson,
          'companyType': companyType ?? 'Agriculture',
          'businessModel': businessModel ?? 'B2B',
          'password': password, // Send password if provided
        },
        options: Options(
          headers: await _getAuthHeaders(),
        ),
      );

      if (response.data['success']) {
        // IMPORTANT: Token refresh required to get new Sponsor role in JWT
        await _authService.refreshToken();

        return SponsorProfileResponse(
          success: true,
          message: response.data['message'] ?? 'Sponsor profile created successfully',
        );
      }

      throw Exception(response.data['message'] ?? 'Profile creation failed');
    } on DioException catch (e) {
      if (e.response?.statusCode == 403) {
        throw Exception('Access denied. Please login again.');
      } else if (e.response?.statusCode == 400) {
        // Profile already exists
        throw Exception(e.response?.data['message'] ?? 'Profile already exists');
      }
      throw Exception('Network error: ${e.message}');
    } catch (e) {
      throw Exception('Failed to create sponsor profile: $e');
    }
  }

  /// Check if user has sponsor profile
  Future<bool> hasSponsorProfile() async {
    try {
      final response = await _dio.get(
        '${ApiConfig.sponsorshipUrl}/profile',
        options: Options(
          headers: await _getAuthHeaders(),
        ),
      );

      return response.data['success'] == true;
    } catch (e) {
      return false;
    }
  }

  /// Get sponsor profile
  Future<SponsorProfile?> getSponsorProfile() async {
    try {
      final response = await _dio.get(
        '${ApiConfig.sponsorshipUrl}/profile',
        options: Options(
          headers: await _getAuthHeaders(),
        ),
      );

      if (response.data['success']) {
        return SponsorProfile.fromJson(response.data['data']);
      }

      return null;
    } catch (e) {
      print('Error fetching sponsor profile: $e');
      return null;
    }
  }

  Future<Map<String, String>> _getAuthHeaders() async {
    final token = await _authService.getAccessToken();
    return {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
    };
  }
}
```

---

### Step 4: UI Implementation (NEW)

#### 4.1 Become Sponsor Button

Add this to user profile or settings screen:

```dart
// lib/screens/profile/profile_screen.dart

class ProfileScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return FutureBuilder<bool>(
      future: context.read<AuthService>().hasSponsorRole(),
      builder: (context, snapshot) {
        final isSponsor = snapshot.data ?? false;

        return Scaffold(
          appBar: AppBar(title: Text('Profile')),
          body: ListView(
            children: [
              // ... existing profile items

              if (!isSponsor) ...[
                Divider(),
                ListTile(
                  leading: Icon(Icons.business, color: Colors.green),
                  title: Text('Become a Sponsor'),
                  subtitle: Text('Support farmers and grow your business'),
                  trailing: Icon(Icons.arrow_forward_ios),
                  onTap: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => CreateSponsorProfileScreen(),
                      ),
                    );
                  },
                ),
              ] else ...[
                Divider(),
                ListTile(
                  leading: Icon(Icons.verified_user, color: Colors.blue),
                  title: Text('Sponsor Dashboard'),
                  subtitle: Text('Manage your sponsorships'),
                  trailing: Icon(Icons.arrow_forward_ios),
                  onTap: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => SponsorDashboardScreen(),
                      ),
                    );
                  },
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
```

#### 4.2 Create Sponsor Profile Form

```dart
// lib/screens/sponsor/create_sponsor_profile_screen.dart

class CreateSponsorProfileScreen extends StatefulWidget {
  @override
  _CreateSponsorProfileScreenState createState() => _CreateSponsorProfileScreenState();
}

class _CreateSponsorProfileScreenState extends State<CreateSponsorProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _companyNameController = TextEditingController();
  final _companyDescriptionController = TextEditingController();
  final _websiteUrlController = TextEditingController();
  final _contactEmailController = TextEditingController();
  final _contactPhoneController = TextEditingController();
  final _contactPersonController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  String _companyType = 'Agriculture';
  String _businessModel = 'B2B';
  bool _isLoading = false;
  bool _obscurePassword = true;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Become a Sponsor'),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: EdgeInsets.all(16),
          children: [
            // Header
            Card(
              color: Colors.green.shade50,
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Support Farmers',
                      style: Theme.of(context).textTheme.headlineSmall,
                    ),
                    SizedBox(height: 8),
                    Text(
                      'Create your sponsor profile to support farmers with subscriptions and grow your business visibility.',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ],
                ),
              ),
            ),
            SizedBox(height: 24),

            // Company Name (Required)
            TextFormField(
              controller: _companyNameController,
              decoration: InputDecoration(
                labelText: 'Company Name *',
                hintText: 'e.g., Ziraat Teknolojileri A.Ş.',
                border: OutlineInputBorder(),
              ),
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Company name is required';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Company Description (Required)
            TextFormField(
              controller: _companyDescriptionController,
              decoration: InputDecoration(
                labelText: 'Company Description *',
                hintText: 'Brief description of your company',
                border: OutlineInputBorder(),
              ),
              maxLines: 3,
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Company description is required';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Website URL (Optional)
            TextFormField(
              controller: _websiteUrlController,
              decoration: InputDecoration(
                labelText: 'Website URL',
                hintText: 'https://yourcompany.com',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.url,
            ),
            SizedBox(height: 16),

            // Contact Email (Required)
            // NOTE: This email will also become the user's login email
            // if they registered with phone (replacing auto-generated email)
            TextFormField(
              controller: _contactEmailController,
              decoration: InputDecoration(
                labelText: 'Contact Email *',
                hintText: 'contact@yourcompany.com',
                helperText: 'This will be your login email',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.emailAddress,
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Contact email is required';
                }
                if (!value.contains('@')) {
                  return 'Please enter a valid email';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Contact Phone (Required)
            TextFormField(
              controller: _contactPhoneController,
              decoration: InputDecoration(
                labelText: 'Contact Phone *',
                hintText: '+90 555 123 4567',
                border: OutlineInputBorder(),
              ),
              keyboardType: TextInputType.phone,
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Contact phone is required';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Contact Person (Required)
            TextFormField(
              controller: _contactPersonController,
              decoration: InputDecoration(
                labelText: 'Contact Person *',
                hintText: 'Full name of contact person',
                border: OutlineInputBorder(),
              ),
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Contact person is required';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Password (Required for phone-registered users)
            // IMPORTANT: This allows login with email+password
            TextFormField(
              controller: _passwordController,
              decoration: InputDecoration(
                labelText: 'Password *',
                hintText: 'Set your password for email login',
                helperText: 'Min 6 characters - required for email+password login',
                border: OutlineInputBorder(),
                suffixIcon: IconButton(
                  icon: Icon(_obscurePassword ? Icons.visibility_off : Icons.visibility),
                  onPressed: () {
                    setState(() {
                      _obscurePassword = !_obscurePassword;
                    });
                  },
                ),
              ),
              obscureText: _obscurePassword,
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Password is required';
                }
                if (value.length < 6) {
                  return 'Password must be at least 6 characters';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Confirm Password
            TextFormField(
              controller: _confirmPasswordController,
              decoration: InputDecoration(
                labelText: 'Confirm Password *',
                hintText: 'Re-enter your password',
                border: OutlineInputBorder(),
              ),
              obscureText: _obscurePassword,
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'Please confirm your password';
                }
                if (value != _passwordController.text) {
                  return 'Passwords do not match';
                }
                return null;
              },
            ),
            SizedBox(height: 16),

            // Company Type
            DropdownButtonFormField<String>(
              value: _companyType,
              decoration: InputDecoration(
                labelText: 'Company Type',
                border: OutlineInputBorder(),
              ),
              items: [
                DropdownMenuItem(value: 'Agriculture', child: Text('Agriculture')),
                DropdownMenuItem(value: 'Technology', child: Text('Technology')),
                DropdownMenuItem(value: 'Manufacturing', child: Text('Manufacturing')),
                DropdownMenuItem(value: 'Other', child: Text('Other')),
              ],
              onChanged: (value) {
                setState(() {
                  _companyType = value!;
                });
              },
            ),
            SizedBox(height: 16),

            // Business Model
            DropdownButtonFormField<String>(
              value: _businessModel,
              decoration: InputDecoration(
                labelText: 'Business Model',
                border: OutlineInputBorder(),
              ),
              items: [
                DropdownMenuItem(value: 'B2B', child: Text('B2B (Business to Business)')),
                DropdownMenuItem(value: 'B2C', child: Text('B2C (Business to Consumer)')),
                DropdownMenuItem(value: 'B2B2C', child: Text('B2B2C')),
              ],
              onChanged: (value) {
                setState(() {
                  _businessModel = value!;
                });
              },
            ),
            SizedBox(height: 24),

            // Submit Button
            ElevatedButton(
              onPressed: _isLoading ? null : _submitForm,
              style: ElevatedButton.styleFrom(
                padding: EdgeInsets.symmetric(vertical: 16),
                backgroundColor: Colors.green,
              ),
              child: _isLoading
                  ? CircularProgressIndicator(color: Colors.white)
                  : Text(
                      'Create Sponsor Profile',
                      style: TextStyle(fontSize: 16),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submitForm() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() {
      _isLoading = true;
    });

    try {
      final sponsorService = context.read<SponsorService>();

      await sponsorService.createSponsorProfile(
        companyName: _companyNameController.text,
        companyDescription: _companyDescriptionController.text,
        websiteUrl: _websiteUrlController.text.isEmpty
            ? null
            : _websiteUrlController.text,
        contactEmail: _contactEmailController.text,
        contactPhone: _contactPhoneController.text,
        contactPerson: _contactPersonController.text,
        companyType: _companyType,
        businessModel: _businessModel,
        password: _passwordController.text, // IMPORTANT: Send password for email+password login
      );

      // Success!
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Sponsor profile created successfully!'),
          backgroundColor: Colors.green,
        ),
      );

      // Navigate to sponsor dashboard
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (context) => SponsorDashboardScreen(),
        ),
      );
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Error: ${e.toString()}'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  @override
  void dispose() {
    _companyNameController.dispose();
    _companyDescriptionController.dispose();
    _websiteUrlController.dispose();
    _contactEmailController.dispose();
    _contactPhoneController.dispose();
    _contactPersonController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }
}
```

---

### Step 5: Token Refresh After Profile Creation (CRITICAL)

After creating sponsor profile, **you MUST refresh the token** to get the new Sponsor role in JWT claims:

```dart
// lib/services/auth_service.dart

class AuthService {
  Future<void> refreshToken() async {
    try {
      final refreshToken = await _secureStorage.read(key: 'refresh_token');

      if (refreshToken == null) {
        throw Exception('No refresh token found');
      }

      final response = await _dio.post(
        '${ApiConfig.baseUrl}/api/v${ApiConfig.apiVersion}/auth/refresh-token',
        data: {
          'refreshToken': refreshToken,
        },
      );

      if (response.data['success']) {
        final data = response.data['data'];

        // Store new tokens
        await _secureStorage.write(
          key: 'access_token',
          value: data['accessToken'],
        );
        await _secureStorage.write(
          key: 'refresh_token',
          value: data['refreshToken'],
        );

        print('Token refreshed successfully');
      }
    } catch (e) {
      print('Token refresh failed: $e');
      throw Exception('Failed to refresh token');
    }
  }
}
```

**Why is this important?**
- Sponsor profile creation adds Sponsor role to database
- For phone registrations, it also updates user's email to business email AND sets password
- But JWT token still contains old claims (only Farmer role + old email)
- Token refresh gets new JWT with **both** Farmer and Sponsor roles + updated email
- Without refresh, user won't have access to sponsor features and can't login with new email+password

---

## 🔒 Authorization Matrix

| Endpoint | Old Auth | New Auth | Who Can Access |
|----------|----------|----------|----------------|
| `POST /sponsorship/create-profile` | `Sponsor,Admin` | `Authorize` | Any authenticated user (Farmer → Sponsor) |
| `GET /sponsorship/profile` | `Sponsor,Admin` | `Sponsor,Admin` | Users with Sponsor role |
| `POST /sponsorship/purchase-package` | `Sponsor,Admin` | `Sponsor,Admin` | Users with Sponsor role |
| `POST /sponsorship/redeem` | `Farmer,Admin` | `Farmer,Admin` | Farmers redeeming codes |

**Important**: After creating sponsor profile, user will have **both Farmer AND Sponsor roles** and can access both types of endpoints.

---

## 🧪 Testing Scenarios

### Test Case 1: New User Registration → Farmer Only

```dart
test('New user registers as Farmer', () async {
  // 1. Register with phone
  final registerResponse = await authService.registerWithPhone(
    mobilePhone: '+905551234567',
    fullName: 'Test User',
  );
  expect(registerResponse.success, true);

  // 2. Verify code
  final verifyResponse = await authService.verifyPhoneRegistration(
    mobilePhone: '+905551234567',
    code: 123456,
    fullName: 'Test User',
  );
  expect(verifyResponse.success, true);

  // 3. Check roles - should be Farmer only
  final roles = await authService.getUserRoles();
  expect(roles, contains('Farmer'));
  expect(roles, isNot(contains('Sponsor')));
});
```

### Test Case 2: Create Sponsor Profile → Dual Roles + Email Update

```dart
test('Farmer creates sponsor profile and gets Sponsor role + email updated', () async {
  // Assuming user registered with phone and is logged in as Farmer
  // Current email should be: +905551234567@phone.ziraai.com

  // 1. Verify initially NOT a sponsor
  final initialCheck = await authService.hasSponsorRole();
  expect(initialCheck, false);

  // 2. Create sponsor profile with business email + password
  final profileResponse = await sponsorService.createSponsorProfile(
    companyName: 'Test Company',
    companyDescription: 'Test Description',
    contactEmail: 'business@company.com',
    contactPhone: '+905551234567',
    contactPerson: 'Test Person',
    password: 'SecurePass123', // IMPORTANT: Set password for email+password login
  );
  expect(profileResponse.success, true);

  // 3. Check roles after profile creation - should have BOTH roles
  final roles = await authService.getUserRoles();
  expect(roles, contains('Farmer'));
  expect(roles, contains('Sponsor'));

  // 4. Verify sponsor role check
  final sponsorCheck = await authService.hasSponsorRole();
  expect(sponsorCheck, true);

  // 5. IMPORTANT: Test login with new business email + new password
  final loginResponse = await authService.login(
    email: 'business@company.com',  // Business email from profile
    password: 'SecurePass123'        // Password set during profile creation
  );
  expect(loginResponse.success, true);

  // 6. Verify old phone-generated email no longer works
  final oldEmailLogin = await authService.login(
    email: '+905551234567@phone.ziraai.com',
    password: 'SecurePass123'
  );
  expect(oldEmailLogin.success, false); // Should fail - email was updated
});
```

### Test Case 3: Access Sponsor Endpoints After Profile Creation

```dart
test('User can access sponsor endpoints after profile creation', () async {
  // Assuming sponsor profile already created

  // Should now have access to sponsor-only endpoints
  final sponsorProfile = await sponsorService.getSponsorProfile();
  expect(sponsorProfile, isNotNull);
  expect(sponsorProfile!.companyName, 'Test Company');
});
```

### Test Case 4: Idempotency - Cannot Create Profile Twice

```dart
test('Creating sponsor profile twice should fail', () async {
  // 1. Create profile first time - success
  final firstAttempt = await sponsorService.createSponsorProfile(
    companyName: 'Test Company',
    companyDescription: 'Test Description',
    contactEmail: 'test@example.com',
    contactPhone: '+905551234567',
    contactPerson: 'Test Person',
  );
  expect(firstAttempt.success, true);

  // 2. Try to create again - should fail
  expect(
    () => sponsorService.createSponsorProfile(
      companyName: 'Another Company',
      companyDescription: 'Another Description',
      contactEmail: 'test2@example.com',
      contactPhone: '+905559876543',
      contactPerson: 'Another Person',
    ),
    throwsException,
  );
});
```

---

## 📊 Data Models

### SponsorProfile Model

```dart
// lib/models/sponsor_profile.dart

class SponsorProfile {
  final int id;
  final int sponsorId;
  final String companyName;
  final String companyDescription;
  final String? sponsorLogoUrl;
  final String? websiteUrl;
  final String contactEmail;
  final String contactPhone;
  final String contactPerson;
  final String companyType;
  final String businessModel;
  final bool isVerifiedCompany;
  final bool isActive;
  final int totalPurchases;
  final int totalCodesGenerated;
  final int totalCodesRedeemed;
  final double totalInvestment;
  final DateTime createdDate;

  SponsorProfile({
    required this.id,
    required this.sponsorId,
    required this.companyName,
    required this.companyDescription,
    this.sponsorLogoUrl,
    this.websiteUrl,
    required this.contactEmail,
    required this.contactPhone,
    required this.contactPerson,
    required this.companyType,
    required this.businessModel,
    required this.isVerifiedCompany,
    required this.isActive,
    required this.totalPurchases,
    required this.totalCodesGenerated,
    required this.totalCodesRedeemed,
    required this.totalInvestment,
    required this.createdDate,
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
      companyType: json['companyType'] ?? 'Agriculture',
      businessModel: json['businessModel'] ?? 'B2B',
      isVerifiedCompany: json['isVerifiedCompany'] ?? false,
      isActive: json['isActive'] ?? true,
      totalPurchases: json['totalPurchases'] ?? 0,
      totalCodesGenerated: json['totalCodesGenerated'] ?? 0,
      totalCodesRedeemed: json['totalCodesRedeemed'] ?? 0,
      totalInvestment: (json['totalInvestment'] ?? 0).toDouble(),
      createdDate: DateTime.parse(json['createdDate']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'sponsorId': sponsorId,
      'companyName': companyName,
      'companyDescription': companyDescription,
      'sponsorLogoUrl': sponsorLogoUrl,
      'websiteUrl': websiteUrl,
      'contactEmail': contactEmail,
      'contactPhone': contactPhone,
      'contactPerson': contactPerson,
      'companyType': companyType,
      'businessModel': businessModel,
      'isVerifiedCompany': isVerifiedCompany,
      'isActive': isActive,
      'totalPurchases': totalPurchases,
      'totalCodesGenerated': totalCodesGenerated,
      'totalCodesRedeemed': totalCodesRedeemed,
      'totalInvestment': totalInvestment,
      'createdDate': createdDate.toIso8601String(),
    };
  }
}
```

---

## 🚨 Error Handling

### Common Error Scenarios

#### 1. Profile Already Exists (400)

```dart
try {
  await sponsorService.createSponsorProfile(...);
} on Exception catch (e) {
  if (e.toString().contains('already exists')) {
    // User already has sponsor profile
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Profile Exists'),
        content: Text('You already have a sponsor profile. Redirecting to dashboard...'),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(builder: (context) => SponsorDashboardScreen()),
              );
            },
            child: Text('OK'),
          ),
        ],
      ),
    );
  }
}
```

#### 2. Unauthorized Access (403)

```dart
try {
  await sponsorService.createSponsorProfile(...);
} on DioException catch (e) {
  if (e.response?.statusCode == 403) {
    // Token expired or invalid
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Session Expired'),
        content: Text('Please login again to continue.'),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              authService.logout();
              Navigator.pushReplacementNamed(context, '/login');
            },
            child: Text('Login'),
          ),
        ],
      ),
    );
  }
}
```

#### 3. Network Error

```dart
try {
  await sponsorService.createSponsorProfile(...);
} on DioException catch (e) {
  if (e.type == DioExceptionType.connectionTimeout ||
      e.type == DioExceptionType.receiveTimeout) {
    showSnackBar(
      context,
      'Network timeout. Please check your internet connection.',
      Colors.orange,
    );
  } else {
    showSnackBar(
      context,
      'Network error: ${e.message}',
      Colors.red,
    );
  }
}
```

---

## 🔄 State Management Integration

### Using Provider

```dart
// lib/providers/sponsor_provider.dart

class SponsorProvider extends ChangeNotifier {
  final SponsorService _sponsorService;
  final AuthService _authService;

  SponsorProfile? _profile;
  bool _isLoading = false;
  String? _error;

  SponsorProfile? get profile => _profile;
  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get hasSponsorProfile => _profile != null;

  SponsorProvider(this._sponsorService, this._authService);

  Future<void> loadSponsorProfile() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _profile = await _sponsorService.getSponsorProfile();
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> createProfile({
    required String companyName,
    required String companyDescription,
    String? sponsorLogoUrl,
    String? websiteUrl,
    required String contactEmail,
    required String contactPhone,
    required String contactPerson,
    String? companyType,
    String? businessModel,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      await _sponsorService.createSponsorProfile(
        companyName: companyName,
        companyDescription: companyDescription,
        sponsorLogoUrl: sponsorLogoUrl,
        websiteUrl: websiteUrl,
        contactEmail: contactEmail,
        contactPhone: contactPhone,
        contactPerson: contactPerson,
        companyType: companyType,
        businessModel: businessModel,
      );

      // Reload profile after creation
      await loadSponsorProfile();

      return true;
    } catch (e) {
      _error = e.toString();
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
```

### Using Riverpod

```dart
// lib/providers/sponsor_provider.dart

final sponsorProfileProvider = StateNotifierProvider<SponsorProfileNotifier, AsyncValue<SponsorProfile?>>((ref) {
  final sponsorService = ref.watch(sponsorServiceProvider);
  return SponsorProfileNotifier(sponsorService);
});

class SponsorProfileNotifier extends StateNotifier<AsyncValue<SponsorProfile?>> {
  final SponsorService _sponsorService;

  SponsorProfileNotifier(this._sponsorService) : super(const AsyncValue.loading()) {
    loadProfile();
  }

  Future<void> loadProfile() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(() => _sponsorService.getSponsorProfile());
  }

  Future<bool> createProfile({
    required String companyName,
    required String companyDescription,
    String? sponsorLogoUrl,
    String? websiteUrl,
    required String contactEmail,
    required String contactPhone,
    required String contactPerson,
    String? companyType,
    String? businessModel,
  }) async {
    try {
      await _sponsorService.createSponsorProfile(
        companyName: companyName,
        companyDescription: companyDescription,
        sponsorLogoUrl: sponsorLogoUrl,
        websiteUrl: websiteUrl,
        contactEmail: contactEmail,
        contactPhone: contactPhone,
        contactPerson: contactPerson,
        companyType: companyType,
        businessModel: businessModel,
      );

      await loadProfile();
      return true;
    } catch (e) {
      state = AsyncValue.error(e, StackTrace.current);
      return false;
    }
  }
}
```

---

## 📱 UI/UX Recommendations

### 1. Onboarding Flow

Show benefits of becoming a sponsor during user onboarding:

```dart
// After successful registration
showDialog(
  context: context,
  builder: (context) => AlertDialog(
    title: Text('Welcome to ZiraAI!'),
    content: Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Your account has been created as a Farmer.'),
        SizedBox(height: 16),
        Text('💡 Did you know?'),
        SizedBox(height: 8),
        Text(
          'You can become a Sponsor to support farmers and promote your business!',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
      ],
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: Text('Maybe Later'),
      ),
      ElevatedButton(
        onPressed: () {
          Navigator.pop(context);
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => CreateSponsorProfileScreen(),
            ),
          );
        },
        child: Text('Become Sponsor'),
      ),
    ],
  ),
);
```

### 2. Role Badge Display

Show user's current roles in UI:

```dart
// lib/widgets/role_badge.dart

class RoleBadge extends StatelessWidget {
  final List<String> roles;

  const RoleBadge({required this.roles});

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      children: roles.map((role) {
        Color color;
        IconData icon;

        switch (role) {
          case 'Sponsor':
            color = Colors.blue;
            icon = Icons.business;
            break;
          case 'Farmer':
            color = Colors.green;
            icon = Icons.agriculture;
            break;
          case 'Admin':
            color = Colors.red;
            icon = Icons.admin_panel_settings;
            break;
          default:
            color = Colors.grey;
            icon = Icons.person;
        }

        return Chip(
          avatar: Icon(icon, color: Colors.white, size: 16),
          label: Text(role),
          backgroundColor: color,
          labelStyle: TextStyle(color: Colors.white),
        );
      }).toList(),
    );
  }
}

// Usage
FutureBuilder<List<String>>(
  future: authService.getUserRoles(),
  builder: (context, snapshot) {
    if (snapshot.hasData) {
      return RoleBadge(roles: snapshot.data!);
    }
    return SizedBox.shrink();
  },
);
```

### 3. Success Animation

Show celebration animation after sponsor profile creation:

```dart
// After successful profile creation
showDialog(
  context: context,
  barrierDismissible: false,
  builder: (context) => AlertDialog(
    content: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        // Use lottie animation or custom animation
        Icon(Icons.celebration, size: 64, color: Colors.amber),
        SizedBox(height: 16),
        Text(
          'Congratulations!',
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        SizedBox(height: 8),
        Text(
          'You are now a Sponsor!',
          textAlign: TextAlign.center,
        ),
        SizedBox(height: 8),
        Text(
          'You can now support farmers and promote your business.',
          textAlign: TextAlign.center,
          style: TextStyle(color: Colors.grey),
        ),
      ],
    ),
    actions: [
      ElevatedButton(
        onPressed: () {
          Navigator.pop(context);
          Navigator.pushReplacement(
            context,
            MaterialPageRoute(
              builder: (context) => SponsorDashboardScreen(),
            ),
          );
        },
        child: Text('Go to Sponsor Dashboard'),
      ),
    ],
  ),
);
```

---

## 🐛 Debugging

### Debug User Info Endpoint

Use this endpoint to inspect user's roles and claims:

```dart
final debugInfo = await authService.getDebugUserInfo();
print('User Debug Info:');
print('User ID: ${debugInfo['userId']}');
print('Roles: ${debugInfo['roles']}');
print('Has Sponsor Role: ${debugInfo['hasSponsorRole']}');
print('All Claims: ${debugInfo['allClaims']}');
```

**Endpoint**: `GET /api/v1/sponsorship/debug/user-info`

**Response Example**:
```json
{
  "success": true,
  "data": {
    "userId": 123,
    "roles": ["Farmer", "Sponsor"],
    "hasSponsorRole": true,
    "hasAdminRole": false,
    "allClaims": [
      {
        "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        "value": "123"
      },
      {
        "type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
        "value": "Farmer"
      },
      {
        "type": "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
        "value": "Sponsor"
      }
    ],
    "isAuthenticated": true,
    "userName": "Ahmet Yılmaz"
  }
}
```

---

## 📋 Checklist for Mobile Team

### Implementation Checklist

- [ ] Update auth service to remove/ignore `role` parameter in registration
- [ ] Add `hasSponsorRole()` and `getUserRoles()` methods
- [ ] Implement `SponsorService` with `createSponsorProfile()` method
- [ ] Add token refresh after profile creation
- [ ] Create "Become Sponsor" button in profile screen
- [ ] Implement `CreateSponsorProfileScreen` with form
- [ ] Add helper text on email field: "This will be your login email"
- [ ] Add password field with validation (min 6 characters)
- [ ] Add confirm password field with matching validation
- [ ] Add password visibility toggle icon
- [ ] Add role badge display in UI
- [ ] Implement state management for sponsor profile
- [ ] Add success/error handling for profile creation
- [ ] Add idempotency check (profile already exists)
- [ ] Test complete flow: Register → Login → Create Profile → Token Refresh
- [ ] Test email update flow: Phone Register → Create Profile → Login with Business Email
- [ ] Update environment configuration for API URLs

### Testing Checklist

- [ ] Test registration creates Farmer role only
- [ ] Test JWT contains only Farmer role after registration
- [ ] Test phone registration creates auto-generated email (`{phone}@phone.ziraai.com`)
- [ ] Test phone registration creates user with no password (empty PasswordHash)
- [ ] Test creating sponsor profile succeeds (200 OK)
- [ ] Test user email updated from phone-generated to business email
- [ ] Test user password set when provided (PasswordHash no longer empty)
- [ ] Test token refresh after profile creation
- [ ] Test JWT contains both Farmer and Sponsor roles after refresh
- [ ] Test login with old phone-generated email fails (for phone registrations)
- [ ] Test login with new business email + new password succeeds (for phone registrations)
- [ ] Test password validation (min 6 characters)
- [ ] Test password mismatch validation (password != confirmPassword)
- [ ] Test accessing sponsor endpoints with new token
- [ ] Test creating profile twice fails gracefully
- [ ] Test email update only affects phone-generated emails (not real emails)
- [ ] Test duplicate business email validation (different user already has it)
- [ ] Test network error handling
- [ ] Test session expiry handling
- [ ] Test on Development, Staging, and Production environments

---

## 🔗 Related Documentation

- [Backend API Documentation](./SPONSORSHIP_SYSTEM_COMPLETE_DOCUMENTATION.md)
- [Role Management Guide](./ROLE_MANAGEMENT_COMPLETE_GUIDE.md)
- [Environment Configuration](./ENVIRONMENT_VARIABLES_COMPLETE_REFERENCE.md)
- [Original Mobile Integration Guide](./MOBILE_SPONSORSHIP_INTEGRATION_GUIDE.md)

---

## 📞 Support

For questions or issues during integration:

1. **Backend Team**: Check API logs at `/WebAPIlogs/dev/`
2. **Database Issues**: Verify `UserGroups` and `SponsorProfiles` tables
3. **Authorization Issues**: Use debug endpoint to check JWT claims
4. **General Questions**: Create GitHub issue with `mobile-integration` label

---

## 📝 Changelog

### v2.0 (2025-10-09)
- **BREAKING**: All registrations now default to Farmer role
- **NEW**: Self-service sponsor profile creation
- **NEW**: Automatic dual-role assignment (Farmer + Sponsor)
- **NEW**: Auto-update email from phone-generated to business email on profile creation
- **NEW**: Password field for phone-registered users (enables email+password login)
- **FIXED**: 403 error on create-profile endpoint
- **FIXED**: Phone registrations can now set password and login with email+password
- **REMOVED**: `role` parameter from registration (now ignored)
- **FEATURE**: Phone registrants can login with business email+password after creating sponsor profile

### v1.0 (Previous)
- Initial sponsor system with role-based registration
- Required admin assignment for sponsor role
- Single role per user (Farmer OR Sponsor)

---

**Last Updated**: 2025-10-09
**Document Version**: 2.0
**API Version**: v1
**Status**: ✅ Ready for Implementation

# App Info (Hakkımızda / About Us) API Documentation

## Overview
API endpoints for managing application information displayed on the "About Us" page. Admin can update the information, while Farmers and Sponsors can view it.

---

## Endpoints Summary

| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| GET | `/api/v1/appinfo` | Farmer, Sponsor | Get app info (About Us page data) |
| GET | `/api/admin/appinfo` | Admin | Get app info with admin metadata |
| PUT | `/api/admin/appinfo` | Admin | Update app info |

---

## User Endpoint

### Get App Info

**URL:** `GET /api/v1/appinfo`
**Authorization:** Required (Farmer or Sponsor role)
**Header:** `x-dev-arch-version: 1.0`

#### Request

**Headers:**
```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

**cURL:**
```bash
curl -X GET \
  "https://ziraai-api-sit.up.railway.app/api/v1/appinfo" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "x-dev-arch-version: 1.0"
```

**Flutter (Dart):**
```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

class AppInfoService {
  final String baseUrl;
  final String token;

  AppInfoService({required this.baseUrl, required this.token});

  Future<AppInfo> getAppInfo() async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/v1/appinfo'),
      headers: {
        'Authorization': 'Bearer $token',
        'x-dev-arch-version': '1.0',
      },
    );

    if (response.statusCode == 200) {
      final data = json.decode(response.body);
      if (data['success']) {
        return AppInfo.fromJson(data['data']);
      }
      throw Exception(data['message']);
    }
    throw Exception('Failed to load app info');
  }
}

class AppInfo {
  final String? companyName;
  final String? companyDescription;
  final String? appVersion;
  final String? address;
  final String? email;
  final String? phone;
  final String? websiteUrl;
  final String? facebookUrl;
  final String? instagramUrl;
  final String? youTubeUrl;
  final String? twitterUrl;
  final String? linkedInUrl;
  final String? termsOfServiceUrl;
  final String? privacyPolicyUrl;
  final String? cookiePolicyUrl;
  final DateTime updatedDate;

  AppInfo({
    this.companyName,
    this.companyDescription,
    this.appVersion,
    this.address,
    this.email,
    this.phone,
    this.websiteUrl,
    this.facebookUrl,
    this.instagramUrl,
    this.youTubeUrl,
    this.twitterUrl,
    this.linkedInUrl,
    this.termsOfServiceUrl,
    this.privacyPolicyUrl,
    this.cookiePolicyUrl,
    required this.updatedDate,
  });

  factory AppInfo.fromJson(Map<String, dynamic> json) {
    return AppInfo(
      companyName: json['companyName'],
      companyDescription: json['companyDescription'],
      appVersion: json['appVersion'],
      address: json['address'],
      email: json['email'],
      phone: json['phone'],
      websiteUrl: json['websiteUrl'],
      facebookUrl: json['facebookUrl'],
      instagramUrl: json['instagramUrl'],
      youTubeUrl: json['youTubeUrl'],
      twitterUrl: json['twitterUrl'],
      linkedInUrl: json['linkedInUrl'],
      termsOfServiceUrl: json['termsOfServiceUrl'],
      privacyPolicyUrl: json['privacyPolicyUrl'],
      cookiePolicyUrl: json['cookiePolicyUrl'],
      updatedDate: DateTime.parse(json['updatedDate']),
    );
  }
}
```

#### Success Response (200 OK)

```json
{
  "success": true,
  "message": null,
  "data": {
    "companyName": "ZiraAI",
    "companyDescription": "ZiraAI, yapay zeka destekli bitki analizi hizmeti sunan yenilikçi bir tarım teknolojisi şirketidir.",
    "appVersion": "1.0.0",
    "address": "İstanbul, Türkiye",
    "email": "destek@ziraai.com",
    "phone": "+90 (212) 555 0000",
    "websiteUrl": "https://www.ziraai.com",
    "facebookUrl": "https://www.facebook.com/ziraai",
    "instagramUrl": "https://www.instagram.com/ziraai",
    "youTubeUrl": "https://www.youtube.com/@ziraai",
    "twitterUrl": "https://www.twitter.com/ziraai",
    "linkedInUrl": "https://www.linkedin.com/company/ziraai",
    "termsOfServiceUrl": "https://www.ziraai.com/terms",
    "privacyPolicyUrl": "https://www.ziraai.com/privacy",
    "cookiePolicyUrl": "https://www.ziraai.com/cookies",
    "updatedDate": "2025-11-18T10:30:00"
  }
}
```

#### Error Responses

**404 Not Found - App Info Not Exists:**
```json
{
  "success": false,
  "message": "Uygulama bilgileri bulunamadı.",
  "data": null
}
```

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

---

## Admin Endpoints

### Get App Info (Admin View)

**URL:** `GET /api/admin/appinfo`
**Authorization:** Required (Admin role)
**Header:** `x-dev-arch-version: 1.0`

#### Request

**cURL:**
```bash
curl -X GET \
  "https://ziraai-api-sit.up.railway.app/api/admin/appinfo" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "x-dev-arch-version: 1.0"
```

#### Success Response (200 OK)

```json
{
  "success": true,
  "message": null,
  "data": {
    "id": 1,
    "companyName": "ZiraAI",
    "companyDescription": "ZiraAI, yapay zeka destekli bitki analizi hizmeti sunan yenilikçi bir tarım teknolojisi şirketidir.",
    "appVersion": "1.0.0",
    "address": "İstanbul, Türkiye",
    "email": "destek@ziraai.com",
    "phone": "+90 (212) 555 0000",
    "websiteUrl": "https://www.ziraai.com",
    "facebookUrl": "https://www.facebook.com/ziraai",
    "instagramUrl": "https://www.instagram.com/ziraai",
    "youTubeUrl": "https://www.youtube.com/@ziraai",
    "twitterUrl": "https://www.twitter.com/ziraai",
    "linkedInUrl": "https://www.linkedin.com/company/ziraai",
    "termsOfServiceUrl": "https://www.ziraai.com/terms",
    "privacyPolicyUrl": "https://www.ziraai.com/privacy",
    "cookiePolicyUrl": "https://www.ziraai.com/cookies",
    "isActive": true,
    "createdDate": "2025-11-18T09:00:00",
    "updatedDate": "2025-11-18T10:30:00",
    "updatedByUserId": 1,
    "updatedByUserName": "Admin User"
  }
}
```

#### Error Responses

**404 Not Found:**
```json
{
  "success": false,
  "message": "Uygulama bilgileri bulunamadı. Lütfen önce bilgileri oluşturun.",
  "data": null
}
```

---

### Update App Info

**URL:** `PUT /api/admin/appinfo`
**Authorization:** Required (Admin role)
**Header:** `x-dev-arch-version: 1.0`

#### Request

**Body:**
```json
{
  "companyName": "ZiraAI",
  "companyDescription": "ZiraAI, yapay zeka destekli bitki analizi hizmeti sunan yenilikçi bir tarım teknolojisi şirketidir.",
  "appVersion": "1.0.0",
  "address": "İstanbul, Türkiye",
  "email": "destek@ziraai.com",
  "phone": "+90 (212) 555 0000",
  "websiteUrl": "https://www.ziraai.com",
  "facebookUrl": "https://www.facebook.com/ziraai",
  "instagramUrl": "https://www.instagram.com/ziraai",
  "youTubeUrl": "https://www.youtube.com/@ziraai",
  "twitterUrl": "https://www.twitter.com/ziraai",
  "linkedInUrl": "https://www.linkedin.com/company/ziraai",
  "termsOfServiceUrl": "https://www.ziraai.com/terms",
  "privacyPolicyUrl": "https://www.ziraai.com/privacy",
  "cookiePolicyUrl": "https://www.ziraai.com/cookies"
}
```

**cURL:**
```bash
curl -X PUT \
  "https://ziraai-api-sit.up.railway.app/api/admin/appinfo" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "companyName": "ZiraAI",
    "companyDescription": "ZiraAI, yapay zeka destekli bitki analizi hizmeti sunan yenilikçi bir tarım teknolojisi şirketidir.",
    "appVersion": "1.0.0",
    "address": "İstanbul, Türkiye",
    "email": "destek@ziraai.com",
    "phone": "+90 (212) 555 0000",
    "websiteUrl": "https://www.ziraai.com",
    "facebookUrl": "https://www.facebook.com/ziraai",
    "instagramUrl": "https://www.instagram.com/ziraai",
    "youTubeUrl": "https://www.youtube.com/@ziraai",
    "twitterUrl": "https://www.twitter.com/ziraai",
    "linkedInUrl": "https://www.linkedin.com/company/ziraai",
    "termsOfServiceUrl": "https://www.ziraai.com/terms",
    "privacyPolicyUrl": "https://www.ziraai.com/privacy",
    "cookiePolicyUrl": "https://www.ziraai.com/cookies"
  }'
```

**TypeScript (Admin Panel):**
```typescript
interface UpdateAppInfoDto {
  companyName?: string;
  companyDescription?: string;
  appVersion?: string;
  address?: string;
  email?: string;
  phone?: string;
  websiteUrl?: string;
  facebookUrl?: string;
  instagramUrl?: string;
  youTubeUrl?: string;
  twitterUrl?: string;
  linkedInUrl?: string;
  termsOfServiceUrl?: string;
  privacyPolicyUrl?: string;
  cookiePolicyUrl?: string;
}

async function updateAppInfo(data: UpdateAppInfoDto): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/admin/appinfo`, {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${getToken()}`,
      'Content-Type': 'application/json',
      'x-dev-arch-version': '1.0'
    },
    body: JSON.stringify(data)
  });

  const result = await response.json();

  if (!result.success) {
    throw new Error(result.message);
  }

  return result;
}
```

#### Success Response (200 OK)

**When creating new record:**
```json
{
  "success": true,
  "message": "Uygulama bilgileri başarıyla oluşturuldu."
}
```

**When updating existing record:**
```json
{
  "success": true,
  "message": "Uygulama bilgileri başarıyla güncellendi."
}
```

#### Error Responses

**401 Unauthorized:**
```json
{
  "message": "Geçersiz kullanıcı token'ı."
}
```

---

## Data Transfer Objects

### AppInfoDto (User View)

| Field | Type | Description |
|-------|------|-------------|
| companyName | string | Company name |
| companyDescription | string | Company description |
| appVersion | string | Application version |
| address | string | Company address |
| email | string | Contact email |
| phone | string | Contact phone |
| websiteUrl | string | Company website URL |
| facebookUrl | string | Facebook page URL |
| instagramUrl | string | Instagram profile URL |
| youTubeUrl | string | YouTube channel URL |
| twitterUrl | string | Twitter profile URL |
| linkedInUrl | string | LinkedIn company URL |
| termsOfServiceUrl | string | Terms of Service page URL |
| privacyPolicyUrl | string | Privacy Policy page URL |
| cookiePolicyUrl | string | Cookie Policy page URL |
| updatedDate | datetime | Last update timestamp |

### AdminAppInfoDto (Admin View)

Includes all fields from `AppInfoDto` plus:

| Field | Type | Description |
|-------|------|-------------|
| id | int | Record ID |
| isActive | boolean | Whether record is active |
| createdDate | datetime | Creation timestamp |
| updatedByUserId | int? | ID of admin who last updated |
| updatedByUserName | string | Name of admin who last updated |

### UpdateAppInfoDto (Admin Update)

All fields are optional strings (same as AppInfoDto without metadata fields).

---

## Business Rules

### Single Active Record Pattern
- Only one app info record can be active at a time
- When no record exists, the first PUT request creates a new record
- Subsequent PUT requests update the existing active record

### Access Control
- **Farmers and Sponsors**: Can only view app info
- **Administrators**: Can view and update app info

### Field Behavior
- All text fields are trimmed on save
- Null or empty values are preserved (fields are optional)
- UpdatedDate is automatically set on save
- UpdatedByUserId tracks the admin who made changes

---

## Flutter Widget Example

```dart
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

class AboutUsPage extends StatelessWidget {
  final AppInfo appInfo;

  const AboutUsPage({Key? key, required this.appInfo}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Hakkımızda')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Company Info Section
            _buildSection(
              'Şirket Bilgileri',
              [
                _buildInfoRow('Şirket Adı', appInfo.companyName),
                _buildInfoRow('Açıklama', appInfo.companyDescription),
                _buildInfoRow('Versiyon', appInfo.appVersion),
              ],
            ),
            SizedBox(height: 24),

            // Address Section
            _buildSection(
              'Adres',
              [_buildInfoRow('Adres', appInfo.address)],
            ),
            SizedBox(height: 24),

            // Contact Section
            _buildSection(
              'İletişim',
              [
                _buildLinkRow('E-posta', appInfo.email, 'mailto:${appInfo.email}'),
                _buildLinkRow('Telefon', appInfo.phone, 'tel:${appInfo.phone}'),
                _buildLinkRow('Web Sitesi', appInfo.websiteUrl, appInfo.websiteUrl),
              ],
            ),
            SizedBox(height: 24),

            // Social Media Section
            _buildSection(
              'Sosyal Medya',
              [
                if (appInfo.facebookUrl != null)
                  _buildSocialButton('Facebook', appInfo.facebookUrl!, Icons.facebook),
                if (appInfo.instagramUrl != null)
                  _buildSocialButton('Instagram', appInfo.instagramUrl!, Icons.camera_alt),
                if (appInfo.youTubeUrl != null)
                  _buildSocialButton('YouTube', appInfo.youTubeUrl!, Icons.play_arrow),
                if (appInfo.twitterUrl != null)
                  _buildSocialButton('Twitter', appInfo.twitterUrl!, Icons.flutter_dash),
                if (appInfo.linkedInUrl != null)
                  _buildSocialButton('LinkedIn', appInfo.linkedInUrl!, Icons.work),
              ],
            ),
            SizedBox(height: 24),

            // Legal Section
            _buildSection(
              'Yasal',
              [
                _buildLinkRow('Kullanım Koşulları', null, appInfo.termsOfServiceUrl),
                _buildLinkRow('Gizlilik Politikası', null, appInfo.privacyPolicyUrl),
                _buildLinkRow('Çerez Politikası', null, appInfo.cookiePolicyUrl),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSection(String title, List<Widget> children) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: Colors.green,
          ),
        ),
        SizedBox(height: 8),
        ...children,
      ],
    );
  }

  Widget _buildInfoRow(String label, String? value) {
    if (value == null || value.isEmpty) return SizedBox.shrink();
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(label, style: TextStyle(fontWeight: FontWeight.w500)),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }

  Widget _buildLinkRow(String label, String? displayText, String? url) {
    if (url == null || url.isEmpty) return SizedBox.shrink();
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: InkWell(
        onTap: () => _launchUrl(url),
        child: Row(
          children: [
            Icon(Icons.link, size: 16, color: Colors.blue),
            SizedBox(width: 8),
            Text(
              displayText ?? label,
              style: TextStyle(color: Colors.blue, decoration: TextDecoration.underline),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSocialButton(String name, String url, IconData icon) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: OutlinedButton.icon(
        onPressed: () => _launchUrl(url),
        icon: Icon(icon),
        label: Text(name),
      ),
    );
  }

  Future<void> _launchUrl(String url) async {
    final uri = Uri.parse(url);
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }
}
```

---

## Testing Checklist

### User Endpoint Tests
- [ ] Farmer can view app info
- [ ] Sponsor can view app info
- [ ] Returns 404 when no app info exists
- [ ] Returns 401 when not authenticated

### Admin Endpoint Tests
- [ ] Admin can view app info with metadata
- [ ] Admin can create app info (first PUT)
- [ ] Admin can update existing app info
- [ ] Returns 404 when viewing non-existent app info
- [ ] Returns 401 when not authenticated
- [ ] UpdatedByUserId and UpdatedDate are set correctly

---

## Environment Configuration

```env
# Development
API_BASE_URL=http://localhost:5001

# Staging
API_BASE_URL=https://ziraai-api-sit.up.railway.app

# Production
API_BASE_URL=https://api.ziraai.com
```

---

## Operation Claims

| Claim ID | Name | Alias | Groups |
|----------|------|-------|--------|
| 180 | GetAppInfoQuery | appinfo.get | Farmer (2), Sponsor (3) |
| 181 | GetAppInfoAsAdminQuery | appinfo.admin.get | Admin (1) |
| 182 | UpdateAppInfoCommand | appinfo.admin.update | Admin (1) |

---

**Document Version:** 1.0
**Last Updated:** 2025-11-18
**Feature Branch:** `feature/landing-page-planning`

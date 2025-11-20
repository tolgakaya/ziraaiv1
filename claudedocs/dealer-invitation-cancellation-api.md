# Dealer Invitation Cancellation API Documentation

## Overview
API endpoint for sponsors to cancel pending dealer invitations and automatically release reserved sponsorship codes.

## Endpoint Details

**URL:** `DELETE /api/v1/sponsorship/dealer/invitations/{invitationId}`  
**Authorization:** Required (Sponsor or Admin role)  
**Header:** `x-dev-arch-version: 1.0`

---

## Request

### HTTP Method
```
DELETE
```

### URL Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| invitationId | integer | Yes | The ID of the invitation to cancel |

### Headers
```http
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1.0
```

### Request Example

**cURL:**
```bash
curl -X DELETE \
  "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations/42" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "x-dev-arch-version: 1.0"
```

**JavaScript (Fetch API):**
```javascript
const invitationId = 42;

fetch(`https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations/${invitationId}`, {
  method: 'DELETE',
  headers: {
    'Authorization': `Bearer ${accessToken}`,
    'x-dev-arch-version': '1.0'
  }
})
.then(response => response.json())
.then(data => {
  if (data.success) {
    console.log('Invitation cancelled:', data.message);
  } else {
    console.error('Error:', data.message);
  }
});
```

**TypeScript (Axios):**
```typescript
import axios from 'axios';

interface CancelInvitationResponse {
  success: boolean;
  message: string;
}

async function cancelDealerInvitation(invitationId: number): Promise<CancelInvitationResponse> {
  try {
    const response = await axios.delete<CancelInvitationResponse>(
      `https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations/${invitationId}`,
      {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'x-dev-arch-version': '1.0'
        }
      }
    );
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error)) {
      throw new Error(error.response?.data?.message || 'Invitation cancellation failed');
    }
    throw error;
  }
}

// Usage
cancelDealerInvitation(42)
  .then(result => alert(result.message))
  .catch(error => alert(error.message));
```

**Flutter (Dart):**
```dart
import 'package:http/http.dart' as http;
import 'dart:convert';

Future<Map<String, dynamic>> cancelDealerInvitation(int invitationId, String token) async {
  final url = Uri.parse(
    'https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/dealer/invitations/$invitationId'
  );
  
  final response = await http.delete(
    url,
    headers: {
      'Authorization': 'Bearer $token',
      'x-dev-arch-version': '1.0',
    },
  );
  
  if (response.statusCode == 200 || response.statusCode == 400) {
    return json.decode(response.body);
  } else if (response.statusCode == 401) {
    throw Exception('Unauthorized - Please login again');
  } else if (response.statusCode == 403) {
    throw Exception('Forbidden - You cannot cancel this invitation');
  } else {
    throw Exception('Failed to cancel invitation: ${response.statusCode}');
  }
}

// Usage
try {
  final result = await cancelDealerInvitation(42, userToken);
  if (result['success']) {
    showSuccessSnackbar(result['message']);
  } else {
    showErrorSnackbar(result['message']);
  }
} catch (e) {
  showErrorSnackbar(e.toString());
}
```

---

## Response

### Success Response (200 OK)

```json
{
  "success": true,
  "message": "Davetiye iptal edildi. 5 kod serbest bırakıldı"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| success | boolean | Always `true` for successful cancellation |
| message | string | Success message including count of released codes |

### Error Responses

#### 400 Bad Request - Invitation Not Found
```json
{
  "success": false,
  "message": "Davetiye bulunamadı"
}
```

#### 400 Bad Request - Not Pending Status
```json
{
  "success": false,
  "message": "Sadece bekleyen davetiyeler iptal edilebilir. Mevcut durum: Accepted"
}
```

#### 400 Bad Request - Already Expired
```json
{
  "success": false,
  "message": "Davetiye zaten süresi dolmuş"
}
```

#### 400 Bad Request - Not Authorized
```json
{
  "success": false,
  "message": "Bu davetiyeyi iptal etme yetkiniz yok"
}
```

#### 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized"
}
```

#### 403 Forbidden
```json
{
  "success": false,
  "message": "Access denied"
}
```

#### 500 Internal Server Error
```json
{
  "success": false,
  "message": "Davetiye iptal edilirken hata oluştu"
}
```

---

## Business Rules

### Who Can Cancel
- Only the **sponsor who created the invitation** can cancel it
- **Admin users** can cancel any invitation

### What Can Be Cancelled
- Only invitations with status **"Pending"** can be cancelled
- Already accepted, expired, or cancelled invitations cannot be cancelled

### What Happens When Cancelled
1. Invitation status changes to **"Cancelled"**
2. `CancelledDate` is set to current timestamp
3. `CancelledByUserId` is set to the sponsor's user ID
4. All **reserved sponsorship codes** are released (become available again)
5. Released codes have `ReservedForInvitationId` set to `null`

---

## UI Implementation Examples

### Angular Component Example

**Component TypeScript:**
```typescript
import { Component } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '@env/environment';

interface DealerInvitation {
  id: number;
  dealerPhone: string;
  codeCount: number;
  status: string;
  createdDate: string;
  expiryDate: string;
}

@Component({
  selector: 'app-dealer-invitations',
  templateUrl: './dealer-invitations.component.html'
})
export class DealerInvitationsComponent {
  invitations: DealerInvitation[] = [];
  
  constructor(private http: HttpClient) {}
  
  cancelInvitation(invitation: DealerInvitation): void {
    if (!confirm(`${invitation.dealerPhone} numaralı davetiyeyi iptal etmek istediğinizden emin misiniz?`)) {
      return;
    }
    
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
      'x-dev-arch-version': '1.0'
    });
    
    this.http.delete<{success: boolean, message: string}>(
      `${environment.apiUrl}/sponsorship/dealer/invitations/${invitation.id}`,
      { headers }
    ).subscribe({
      next: (response) => {
        if (response.success) {
          // Remove from list
          this.invitations = this.invitations.filter(inv => inv.id !== invitation.id);
          // Show success message
          this.showNotification('success', response.message);
        } else {
          this.showNotification('error', response.message);
        }
      },
      error: (error) => {
        const message = error.error?.message || 'Davetiye iptal edilemedi';
        this.showNotification('error', message);
      }
    });
  }
  
  private showNotification(type: 'success' | 'error', message: string): void {
    // Your notification implementation
    console.log(`[${type}] ${message}`);
  }
}
```

**Component HTML:**
```html
<div class="invitations-list">
  <div *ngFor="let invitation of invitations" class="invitation-card">
    <div class="invitation-info">
      <span class="phone">{{ invitation.dealerPhone }}</span>
      <span class="code-count">{{ invitation.codeCount }} kod</span>
      <span class="status" [class]="invitation.status.toLowerCase()">
        {{ invitation.status }}
      </span>
    </div>
    
    <button 
      *ngIf="invitation.status === 'Pending'"
      class="btn-cancel"
      (click)="cancelInvitation(invitation)"
      [attr.aria-label]="'İptal et: ' + invitation.dealerPhone">
      İptal Et
    </button>
  </div>
</div>
```

### React Component Example

```tsx
import React, { useState } from 'react';
import axios from 'axios';

interface DealerInvitation {
  id: number;
  dealerPhone: string;
  codeCount: number;
  status: string;
  createdDate: string;
  expiryDate: string;
}

const DealerInvitationsList: React.FC = () => {
  const [invitations, setInvitations] = useState<DealerInvitation[]>([]);
  const [loading, setLoading] = useState<number | null>(null);

  const cancelInvitation = async (invitation: DealerInvitation) => {
    if (!window.confirm(`${invitation.dealerPhone} numaralı davetiyeyi iptal etmek istediğinizden emin misiniz?`)) {
      return;
    }

    setLoading(invitation.id);

    try {
      const response = await axios.delete(
        `${process.env.REACT_APP_API_URL}/sponsorship/dealer/invitations/${invitation.id}`,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
            'x-dev-arch-version': '1.0'
          }
        }
      );

      if (response.data.success) {
        // Remove from list
        setInvitations(prev => prev.filter(inv => inv.id !== invitation.id));
        // Show success notification
        alert(response.data.message);
      }
    } catch (error: any) {
      const message = error.response?.data?.message || 'Davetiye iptal edilemedi';
      alert(message);
    } finally {
      setLoading(null);
    }
  };

  return (
    <div className="invitations-list">
      {invitations.map(invitation => (
        <div key={invitation.id} className="invitation-card">
          <div className="invitation-info">
            <span className="phone">{invitation.dealerPhone}</span>
            <span className="code-count">{invitation.codeCount} kod</span>
            <span className={`status ${invitation.status.toLowerCase()}`}>
              {invitation.status}
            </span>
          </div>
          
          {invitation.status === 'Pending' && (
            <button
              className="btn-cancel"
              onClick={() => cancelInvitation(invitation)}
              disabled={loading === invitation.id}
            >
              {loading === invitation.id ? 'İptal ediliyor...' : 'İptal Et'}
            </button>
          )}
        </div>
      ))}
    </div>
  );
};

export default DealerInvitationsList;
```

### Flutter Widget Example

```dart
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';

class DealerInvitationsList extends StatefulWidget {
  @override
  _DealerInvitationsListState createState() => _DealerInvitationsListState();
}

class _DealerInvitationsListState extends State<DealerInvitationsList> {
  List<DealerInvitation> invitations = [];
  int? cancellingId;

  Future<void> cancelInvitation(DealerInvitation invitation) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('Davetiyeyi İptal Et'),
        content: Text('${invitation.dealerPhone} numaralı davetiyeyi iptal etmek istediğinizden emin misiniz?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text('Vazgeç'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text('İptal Et'),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() => cancellingId = invitation.id);

    try {
      final token = await getAccessToken(); // Your token retrieval method
      final url = Uri.parse(
        '${Environment.apiBaseUrl}/api/v1/sponsorship/dealer/invitations/${invitation.id}'
      );

      final response = await http.delete(
        url,
        headers: {
          'Authorization': 'Bearer $token',
          'x-dev-arch-version': '1.0',
        },
      );

      final data = json.decode(response.body);

      if (data['success']) {
        setState(() {
          invitations.removeWhere((inv) => inv.id == invitation.id);
        });
        
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(data['message']),
            backgroundColor: Colors.green,
          ),
        );
      } else {
        throw Exception(data['message']);
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(e.toString()),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      setState(() => cancellingId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      itemCount: invitations.length,
      itemBuilder: (context, index) {
        final invitation = invitations[index];
        final isCancelling = cancellingId == invitation.id;

        return Card(
          margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: ListTile(
            title: Text(invitation.dealerPhone),
            subtitle: Text('${invitation.codeCount} kod - ${invitation.status}'),
            trailing: invitation.status == 'Pending'
                ? ElevatedButton(
                    onPressed: isCancelling ? null : () => cancelInvitation(invitation),
                    child: isCancelling
                        ? SizedBox(
                            width: 16,
                            height: 16,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text('İptal Et'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.red,
                      foregroundColor: Colors.white,
                    ),
                  )
                : null,
          ),
        );
      },
    );
  }
}

class DealerInvitation {
  final int id;
  final String dealerPhone;
  final int codeCount;
  final String status;

  DealerInvitation({
    required this.id,
    required this.dealerPhone,
    required this.codeCount,
    required this.status,
  });
}
```

---

## Testing Checklist

### ✅ Manual Testing Steps

1. **Happy Path - Cancel Pending Invitation**
   - Create a dealer invitation
   - Verify invitation appears with "Pending" status
   - Click "Cancel" button
   - Confirm cancellation dialog
   - Verify success message shows released code count
   - Verify invitation disappears from list or status changes to "Cancelled"

2. **Error Case - Try to Cancel Already Cancelled**
   - Attempt to cancel the same invitation again
   - Verify error message: "Sadece bekleyen davetiyeler iptal edilebilir"

3. **Error Case - Unauthorized Cancellation**
   - Login as different sponsor
   - Try to cancel another sponsor's invitation
   - Verify error message: "Bu davetiyeyi iptal etme yetkiniz yok"

4. **Error Case - Invalid Invitation ID**
   - Try to cancel with non-existent invitation ID
   - Verify error message: "Davetiye bulunamadı"

5. **Verify Code Release**
   - Note available code count before cancellation
   - Cancel invitation with N codes
   - Verify available code count increased by N

---

## API Integration Notes

### Environment Variables
```env
# Development
REACT_APP_API_URL=http://localhost:5001/api/v1

# Staging
REACT_APP_API_URL=https://ziraai-api-sit.up.railway.app/api/v1

# Production
REACT_APP_API_URL=https://api.ziraai.com/api/v1
```

### Error Handling Best Practices

```typescript
// Recommended error handling pattern
async function cancelInvitation(id: number): Promise<void> {
  try {
    const response = await api.delete(`/sponsorship/dealer/invitations/${id}`);
    
    if (response.data.success) {
      showSuccessToast(response.data.message);
      refreshInvitationList();
    } else {
      showErrorToast(response.data.message);
    }
  } catch (error) {
    if (axios.isAxiosError(error)) {
      switch (error.response?.status) {
        case 401:
          redirectToLogin();
          break;
        case 403:
          showErrorToast('Bu işlem için yetkiniz yok');
          break;
        case 400:
          showErrorToast(error.response.data.message);
          break;
        default:
          showErrorToast('Bir hata oluştu, lütfen tekrar deneyin');
      }
    } else {
      showErrorToast('Bağlantı hatası');
    }
  }
}
```

---

## Related Endpoints

- `POST /api/v1/sponsorship/dealer/invite-via-sms` - Create dealer invitation
- `GET /api/v1/sponsorship/dealer/invitations` - List dealer invitations
- `GET /api/v1/sponsorship/dealer/invitations/{id}/details` - Get invitation details
- `POST /api/v1/sponsorship/dealer/accept-invitation` - Accept invitation (dealer side)

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-04  
**Feature Branch:** `feature/sponsor-analytics-cache`  
**Commit:** `9ff64c3`

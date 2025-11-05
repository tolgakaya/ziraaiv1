#!/bin/bash

# Test Script: Cancel Dealer Invitation Flow
# Prerequisites:
# 1. API must be running (localhost:5001 or staging server)
# 2. Valid sponsor token with pending invitations

BASE_URL="${API_BASE_URL:-https://ziraai-api-sit.up.railway.app}"

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "======================================"
echo "  Dealer Invitation Cancellation Test"
echo "======================================"
echo ""

# Test with Sponsor token (User 1114, ID: 159)
SPONSOR_TOKEN="eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjE1OSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJVc2VyIDExMTQiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiRmFybWVyIiwiU3BvbnNvciJdLCJuYmYiOjE3NjE0OTk0OTEsImV4cCI6MTc2MTUwMzA5MSwiaXNzIjoiWmlyYUFJX1N0YWdpbmciLCJhdWQiOiJaaXJhQUlfU3RhZ2luZ19Vc2VycyJ9.SlMXorY-By-dm4VF6dSvl-oRj38MYIXxkwgcngbK8-s"

echo -e "${YELLOW}Step 1: Check existing pending invitations${NC}"
INVITATIONS=$(curl -s -X GET \
  "$BASE_URL/api/v1/sponsorship/dealer/invitations?status=Pending&page=1&pageSize=10" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

echo "$INVITATIONS" | python3 -m json.tool 2>/dev/null || echo "$INVITATIONS"

# Extract first pending invitation ID
INVITATION_ID=$(echo "$INVITATIONS" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data['data'][0]['id'] if data.get('data') and len(data['data']) > 0 else 'NONE')" 2>/dev/null)

if [ "$INVITATION_ID" == "NONE" ] || [ -z "$INVITATION_ID" ]; then
    echo -e "${YELLOW}No pending invitations found. Creating one first...${NC}"
    echo ""
    
    # Create invitation first
    CREATE_RESPONSE=$(curl -s -X POST \
      "$BASE_URL/api/v1/sponsorship/dealer/invite-via-sms" \
      -H "Authorization: Bearer $SPONSOR_TOKEN" \
      -H "Content-Type: application/json" \
      -H "x-dev-arch-version: 1.0" \
      -d '{
        "phone": "+905551234567",
        "codeCount": 5
      }')
    
    echo "$CREATE_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$CREATE_RESPONSE"
    
    # Extract invitation ID from create response
    INVITATION_ID=$(echo "$CREATE_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data['data']['invitationId'])" 2>/dev/null)
    echo ""
fi

echo -e "${YELLOW}Step 2: Cancel invitation ID: $INVITATION_ID${NC}"
CANCEL_RESPONSE=$(curl -s -X DELETE \
  "$BASE_URL/api/v1/sponsorship/dealer/invitations/$INVITATION_ID" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

echo "$CANCEL_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$CANCEL_RESPONSE"

# Check success
if echo "$CANCEL_RESPONSE" | grep -q '"success":true'; then
    echo -e "${GREEN}✅ Invitation cancelled successfully${NC}"
else
    echo -e "${RED}❌ Cancellation failed${NC}"
fi

echo ""
echo -e "${YELLOW}Step 3: Verify invitation status changed to Cancelled${NC}"
VERIFY_RESPONSE=$(curl -s -X GET \
  "$BASE_URL/api/v1/sponsorship/dealer/invitations/$INVITATION_ID/details" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

echo "$VERIFY_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$VERIFY_RESPONSE"

# Check status
if echo "$VERIFY_RESPONSE" | grep -q '"status":"Cancelled"'; then
    echo -e "${GREEN}✅ Status confirmed as Cancelled${NC}"
else
    echo -e "${RED}❌ Status not updated correctly${NC}"
fi

echo ""
echo -e "${YELLOW}Step 4: Verify codes were released (check available codes)${NC}"
CODES_RESPONSE=$(curl -s -X GET \
  "$BASE_URL/api/v1/sponsorship/codes?page=1&pageSize=10&onlyUnsent=true" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

echo "$CODES_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$CODES_RESPONSE"

echo ""
echo -e "${YELLOW}Step 5: Test negative case - Try to cancel already cancelled invitation${NC}"
FAIL_RESPONSE=$(curl -s -X DELETE \
  "$BASE_URL/api/v1/sponsorship/dealer/invitations/$INVITATION_ID" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

echo "$FAIL_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$FAIL_RESPONSE"

if echo "$FAIL_RESPONSE" | grep -q '"success":false'; then
    echo -e "${GREEN}✅ Correctly rejected re-cancellation${NC}"
else
    echo -e "${RED}❌ Should have rejected re-cancellation${NC}"
fi

echo ""
echo "======================================"
echo "  Test Complete"
echo "======================================"

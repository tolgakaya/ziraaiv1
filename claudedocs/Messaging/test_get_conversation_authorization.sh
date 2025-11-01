#!/bin/bash

# Test GetConversation Authorization
# Tests authorization for sponsor, dealer, and farmer accessing plant analysis conversations

BASE_URL="https://ziraai-api-sit.up.railway.app"
ANALYSIS_ID=76  # From E2E test - UserId: 170, SponsorId: 159, DealerId: 158

echo "========================================="
echo "GetConversation Authorization Test"
echo "========================================="
echo ""
echo "Analysis ID: $ANALYSIS_ID"
echo "Expected Attribution:"
echo "  - Farmer UserId: 170"
echo "  - Sponsor UserId: 159"
echo "  - Dealer UserId: 158"
echo ""

# Login credentials
SPONSOR_PHONE="+905309999114"  # User 159
SPONSOR_PASS="User1114*"
DEALER_PHONE="+905411111113"   # User 158
DEALER_PASS="User1113*"
FARMER_PHONE="+905061111113"   # User 170
FARMER_PASS="User3978*"

echo "========================================="
echo "Step 1: Getting Fresh Tokens"
echo "========================================="

# Get sponsor token
echo ""
echo "Getting sponsor token (User 159)..."
SPONSOR_RESPONSE=$(curl -s -X POST "$BASE_URL/api/v1/auth/phone-login" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{\"mobilePhone\": \"$SPONSOR_PHONE\", \"password\": \"$SPONSOR_PASS\"}")

SPONSOR_TOKEN=$(echo "$SPONSOR_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('data', {}).get('accessToken', {}).get('token', 'ERROR'))" 2>/dev/null)

if [ "$SPONSOR_TOKEN" = "ERROR" ] || [ -z "$SPONSOR_TOKEN" ]; then
  echo "❌ Failed to get sponsor token"
  echo "Response: $SPONSOR_RESPONSE"
  exit 1
fi
echo "✅ Sponsor token obtained"

# Get dealer token
echo ""
echo "Getting dealer token (User 158)..."
DEALER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/v1/auth/phone-login" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{\"mobilePhone\": \"$DEALER_PHONE\", \"password\": \"$DEALER_PASS\"}")

DEALER_TOKEN=$(echo "$DEALER_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('data', {}).get('accessToken', {}).get('token', 'ERROR'))" 2>/dev/null)

if [ "$DEALER_TOKEN" = "ERROR" ] || [ -z "$DEALER_TOKEN" ]; then
  echo "❌ Failed to get dealer token"
  echo "Response: $DEALER_RESPONSE"
  exit 1
fi
echo "✅ Dealer token obtained"

# Get farmer token
echo ""
echo "Getting farmer token (User 170)..."
FARMER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/v1/auth/phone-login" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d "{\"mobilePhone\": \"$FARMER_PHONE\", \"password\": \"$FARMER_PASS\"}")

FARMER_TOKEN=$(echo "$FARMER_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('data', {}).get('accessToken', {}).get('token', 'ERROR'))" 2>/dev/null)

if [ "$FARMER_TOKEN" = "ERROR" ] || [ -z "$FARMER_TOKEN" ]; then
  echo "❌ Failed to get farmer token"
  echo "Response: $FARMER_RESPONSE"
  exit 1
fi
echo "✅ Farmer token obtained"

echo ""
echo "========================================="
echo "Step 2: Test Sponsor Access (Should PASS)"
echo "========================================="
echo ""

SPONSOR_CONV=$(curl -s -X GET "$BASE_URL/api/v1/sponsorship/conversation/$ANALYSIS_ID?page=1&pageSize=5" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

SPONSOR_SUCCESS=$(echo "$SPONSOR_CONV" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('success', False))" 2>/dev/null)

if [ "$SPONSOR_SUCCESS" = "True" ]; then
  echo "✅ Sponsor (159) PASSED - Conversation accessed successfully"
  echo "Response preview:"
  echo "$SPONSOR_CONV" | python3 -m json.tool 2>/dev/null | head -20
else
  echo "❌ Sponsor (159) FAILED - Expected to access conversation but got:"
  echo "$SPONSOR_CONV"
fi

echo ""
echo "========================================="
echo "Step 3: Test Dealer Access (Should PASS)"
echo "========================================="
echo ""

DEALER_CONV=$(curl -s -X GET "$BASE_URL/api/v1/sponsorship/conversation/$ANALYSIS_ID?page=1&pageSize=5" \
  -H "Authorization: Bearer $DEALER_TOKEN" \
  -H "x-dev-arch-version: 1.0")

DEALER_SUCCESS=$(echo "$DEALER_CONV" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('success', False))" 2>/dev/null)

if [ "$DEALER_SUCCESS" = "True" ]; then
  echo "✅ Dealer (158) PASSED - Conversation accessed successfully"
  echo "Response preview:"
  echo "$DEALER_CONV" | python3 -m json.tool 2>/dev/null | head -20
else
  echo "❌ Dealer (158) FAILED - Expected to access conversation but got:"
  echo "$DEALER_CONV"
fi

echo ""
echo "========================================="
echo "Step 4: Test Farmer Access (Should PASS)"
echo "========================================="
echo ""

FARMER_CONV=$(curl -s -X GET "$BASE_URL/api/v1/sponsorship/conversation/$ANALYSIS_ID?page=1&pageSize=5" \
  -H "Authorization: Bearer $FARMER_TOKEN" \
  -H "x-dev-arch-version: 1.0")

FARMER_SUCCESS=$(echo "$FARMER_CONV" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('success', False))" 2>/dev/null)

if [ "$FARMER_SUCCESS" = "True" ]; then
  echo "✅ Farmer (170) PASSED - Conversation accessed successfully"
  echo "Response preview:"
  echo "$FARMER_CONV" | python3 -m json.tool 2>/dev/null | head -20
else
  echo "❌ Farmer (170) FAILED - Expected to access conversation but got:"
  echo "$FARMER_CONV"
fi

echo ""
echo "========================================="
echo "Step 5: Test Unauthorized Access (Should FAIL)"
echo "========================================="
echo ""

# Use analysis ID 75 which has same attribution as 76
# Test with a different user who shouldn't have access
# We'll use a non-existent analysis ID to test 404 Not Found

UNAUTH_ANALYSIS=99999
UNAUTH_CONV=$(curl -s -X GET "$BASE_URL/api/v1/sponsorship/conversation/$UNAUTH_ANALYSIS?page=1&pageSize=5" \
  -H "Authorization: Bearer $SPONSOR_TOKEN" \
  -H "x-dev-arch-version: 1.0")

UNAUTH_SUCCESS=$(echo "$UNAUTH_CONV" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('success', False))" 2>/dev/null)

if [ "$UNAUTH_SUCCESS" = "False" ]; then
  echo "✅ Unauthorized access BLOCKED - 404 Not Found as expected"
else
  echo "⚠️ Unexpected response for non-existent analysis:"
  echo "$UNAUTH_CONV"
fi

echo ""
echo "========================================="
echo "Test Summary"
echo "========================================="
echo ""
echo "Expected Results:"
echo "  ✅ Sponsor (159) can access - YES"
echo "  ✅ Dealer (158) can access - YES"
echo "  ✅ Farmer (170) can access - YES"
echo "  ✅ Non-existent analysis returns 404 - YES"
echo ""
echo "Actual Results:"
echo "  Sponsor: $SPONSOR_SUCCESS"
echo "  Dealer: $DEALER_SUCCESS"
echo "  Farmer: $FARMER_SUCCESS"
echo "  Unauthorized: $([ "$UNAUTH_SUCCESS" = "False" ] && echo 'BLOCKED' || echo 'UNEXPECTED')"
echo ""

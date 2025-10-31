#!/bin/bash

# Test Sponsor Profile Creation with Email/Password Update
# User: 174 (phone: 05536866386)

echo "=== Test: Create Sponsor Profile with Email and Password ==="
echo ""

# Get JWT token for user 174
echo "Step 1: Login with phone..."
LOGIN_RESPONSE=$(curl -s -X POST "https://ziraai-api-sit.up.railway.app/api/v1/auth/login-phone" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "mobilePhone": "05536866386",
    "password": "",
    "otpCode": "956201"
  }')

echo "$LOGIN_RESPONSE" | jq '.'

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.data.accessToken.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ Failed to get token. Please check login credentials."
  exit 1
fi

echo "✅ Token obtained: ${TOKEN:0:50}..."
echo ""

# Create sponsor profile with email and password
echo "Step 2: Create Sponsor Profile..."
PROFILE_RESPONSE=$(curl -s -X POST "https://ziraai-api-sit.up.railway.app/api/v1/sponsorship/create-profile" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "x-dev-arch-version: 1.0" \
  -d '{
    "companyName": "Test Dealer Company",
    "companyDescription": "Testing email and password update",
    "contactEmail": "testdealer@example.com",
    "contactPhone": "05536866386",
    "contactPerson": "Test Person",
    "companyType": "Agriculture",
    "businessModel": "B2B",
    "password": "TestPassword123"
  }')

echo "$PROFILE_RESPONSE" | jq '.'

if [ "$(echo "$PROFILE_RESPONSE" | jq -r '.success')" == "true" ]; then
  echo ""
  echo "✅ Sponsor profile created successfully!"
  echo ""
  echo "Step 3: Now try to login with NEW email and password..."
  echo ""

  # Try login with new credentials
  NEW_LOGIN=$(curl -s -X POST "https://ziraai-api-sit.up.railway.app/api/v1/auth/login-email" \
    -H "Content-Type: application/json" \
    -H "x-dev-arch-version: 1.0" \
    -d '{
      "email": "testdealer@example.com",
      "password": "TestPassword123"
    }')

  echo "$NEW_LOGIN" | jq '.'

  if [ "$(echo "$NEW_LOGIN" | jq -r '.success')" == "true" ]; then
    echo ""
    echo "✅✅ SUCCESS! Email and password were updated correctly!"
    echo "User can now login with: testdealer@example.com / TestPassword123"
  else
    echo ""
    echo "❌ FAILED! Login with new credentials failed."
    echo "This means email/password were NOT updated in Users table."
  fi
else
  echo ""
  echo "❌ Failed to create sponsor profile"
  echo "Response: $PROFILE_RESPONSE"
fi

echo ""
echo "=== Test Complete ==="

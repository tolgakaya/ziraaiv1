#!/bin/bash

# Simple test for duplicate registration
API_URL="https://localhost:7065/api/auth/register"

echo "🧪 Testing duplicate email registration..."

# Test data
TEST_DATA='{
  "email": "test@example.com",
  "password": "TestPassword123!",
  "fullName": "Test User",
  "userRole": "Farmer"
}'

echo "📧 Testing with email: test@example.com"

echo ""
echo "📝 First registration attempt..."
curl -k -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d "$TEST_DATA" \
  -w "\nHTTP Status: %{http_code}\n" \
  -s

echo ""
echo "🔄 Second registration attempt (duplicate)..."
curl -k -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d "$TEST_DATA" \
  -w "\nHTTP Status: %{http_code}\n" \
  -s

echo ""
echo "🏁 Test completed!"
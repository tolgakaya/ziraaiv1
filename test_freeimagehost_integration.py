#!/usr/bin/env python3
"""
FreeImage.host Integration Test Script
Tests the new FreeImageHostStorageService integration with the file storage system
"""

import requests
import json
import base64
from datetime import datetime
import uuid

def test_freeimagehost_direct_api():
    """Test FreeImage.host API directly to verify functionality"""
    print("=== Testing FreeImage.host API Directly ===")
    
    # Create a small test image (1x1 pixel red PNG)
    test_image_b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="
    
    # API parameters
    api_key = "YOUR_FREEIMAGEHOST_API_KEY_HERE"  # Replace with actual API key
    api_url = "http://freeimage.host/api/1/upload/"
    
    # Prepare form data
    form_data = {
        'key': api_key,
        'action': 'upload',
        'source': test_image_b64,
        'format': 'json'
    }
    
    try:
        response = requests.post(api_url, data=form_data, timeout=30)
        print(f"Status Code: {response.status_code}")
        print(f"Response: {response.text[:500]}...")
        
        if response.status_code == 200:
            result = response.json()
            if result.get('success'):
                print("✅ FreeImage.host API working correctly")
                print(f"Image URL: {result['image']['url']}")
                return result['image']['url']
            else:
                print("❌ FreeImage.host API returned error")
                print(f"Error: {result.get('error', {}).get('message', 'Unknown error')}")
        else:
            print(f"❌ HTTP Error: {response.status_code}")
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Request failed: {e}")
    
    return None

def test_ziraai_async_api_with_freeimagehost():
    """Test ZiraAI async plant analysis API with FreeImageHost storage"""
    print("\n=== Testing ZiraAI Async API with FreeImageHost ===")
    
    # API endpoint
    api_url = "https://localhost:5001/api/plantanalyses/analyze-async"
    
    # Test image (small optimized for AI processing)
    test_image_data_uri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="
    
    # Test payload
    payload = {
        "image": test_image_data_uri,
        "userId": "test_user_freeimagehost",
        "farmerId": "farmer_001",
        "sponsorId": "sponsor_001",
        "fieldId": "field_001",
        "cropType": "tomato",
        "location": "Test Farm - FreeImageHost Integration",
        "gpsCoordinates": {
            "lat": 41.0082,
            "lng": 28.9784
        },
        "altitude": 100,
        "temperature": 25.5,
        "humidity": 65.0,
        "weatherConditions": "sunny",
        "soilType": "loamy",
        "urgencyLevel": "medium",
        "notes": "Testing FreeImageHost integration - automatic URL generation",
        "contactInfo": {
            "phone": "+90555123456",
            "email": "test@freeimagehost.test"
        },
        "additionalInfo": {
            "irrigationMethod": "drip",
            "greenhouse": False,
            "organicCertified": True
        }
    }
    
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json"
    }
    
    try:
        response = requests.post(api_url, json=payload, headers=headers, timeout=30, verify=False)
        print(f"Status Code: {response.status_code}")
        print(f"Response: {response.text}")
        
        if response.status_code == 200:
            result = response.json()
            analysis_id = result.get('data', result.get('analysisId'))
            print(f"✅ Analysis queued successfully!")
            print(f"Analysis ID: {analysis_id}")
            print(f"Expected: Image uploaded to FreeImage.host with URL method")
            print(f"Expected: 99.6% token reduction vs base64 method")
            return analysis_id
        else:
            print(f"❌ API Error: {response.status_code}")
            print(f"Response: {response.text}")
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Request failed: {e}")
    
    return None

def print_storage_configuration_guide():
    """Print configuration guide for different environments"""
    print("\n=== FreeImage.host Configuration Guide ===")
    
    print("\n📋 Configuration Examples:")
    
    print("\n🔧 Development (appsettings.Development.json):")
    print('''
{
  "FileStorage": {
    "Provider": "FreeImageHost",
    "FreeImageHost": {
      "ApiKey": "YOUR_FREEIMAGEHOST_API_KEY_HERE"
    }
  }
}
''')
    
    print("\n🚀 Production (appsettings.json):")
    print('''
{
  "FileStorage": {
    "Provider": "S3",  // Use S3 for production
    "FreeImageHost": {
      "ApiKey": "PRODUCTION_FREEIMAGEHOST_API_KEY_HERE"
    },
    "S3": {
      "BucketName": "ziraai-production-images",
      "Region": "us-east-1",
      "UseCloudFront": true,
      "CloudFrontDomain": "cdn.ziraai.com"
    }
  }
}
''')
    
    print("\n📊 Storage Provider Comparison:")
    print("┌─────────────────┬──────────────┬─────────────┬──────────────┬─────────────┐")
    print("│ Provider        │ File Size    │ Free Tier   │ Performance  │ Use Case    │")
    print("├─────────────────┼──────────────┼─────────────┼──────────────┼─────────────┤")
    print("│ Local           │ Unlimited    │ Yes         │ Fast         │ Development │")
    print("│ FreeImage.host  │ 64 MB        │ Yes         │ Good         │ Testing     │")
    print("│ ImgBB           │ 32 MB        │ Yes         │ Good         │ Development │")
    print("│ AWS S3          │ Unlimited    │ 5GB Free    │ Excellent    │ Production  │")
    print("└─────────────────┴──────────────┴─────────────┴──────────────┴─────────────┘")
    
    print("\n🔑 FreeImage.host Advantages:")
    print("  • 64 MB file size limit (vs 32 MB for ImgBB)")
    print("  • Fast upload speeds")
    print("  • Reliable CDN delivery")
    print("  • Simple API integration")
    print("  • Good for AI-optimized images (100KB target)")
    
    print("\n⚠️  API Key Setup:")
    print("  1. Go to https://freeimage.host/page/api")
    print("  2. Register for a free account")
    print("  3. Get your API key")
    print("  4. Update appsettings with your key")

def main():
    """Main test execution"""
    print("🚀 FreeImage.host Integration Test Suite")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 60)
    
    # Test 1: Direct API test
    image_url = test_freeimagehost_direct_api()
    
    # Test 2: ZiraAI integration test
    analysis_id = test_ziraai_async_api_with_freeimagehost()
    
    # Print configuration guide
    print_storage_configuration_guide()
    
    print("\n" + "=" * 60)
    print("📝 Test Results Summary:")
    print(f"  FreeImage.host Direct API: {'✅ Success' if image_url else '❌ Failed'}")
    print(f"  ZiraAI Async Integration:   {'✅ Success' if analysis_id else '❌ Failed'}")
    
    if image_url:
        print(f"\n🔗 Test Image URL: {image_url}")
    
    if analysis_id:
        print(f"📊 Analysis ID: {analysis_id}")
        print("📋 Check Hangfire dashboard for job processing status")
        print("🔍 Expected: Image stored at FreeImage.host with public URL")
    
    print("\n🎯 Next Steps:")
    print("  1. Add your FreeImage.host API key to appsettings")
    print("  2. Change Provider to 'FreeImageHost' in development")
    print("  3. Test with real plant images")
    print("  4. Monitor token usage reduction (99.6% expected)")

if __name__ == "__main__":
    main()
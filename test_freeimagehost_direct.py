#!/usr/bin/env python3
"""
Direct test of FreeImage.host API to debug the 404 error
"""
import requests
import base64

def test_freeimagehost_api():
    api_key = "6d207e02198a847aa98d0a2a901485a5"
    api_url = "https://freeimage.host/api/1/upload"
    
    # Small test image (10x10 red square in base64)
    test_image_b64 = "iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAASElEQVQYV2P8//8/AzYwirkI6f4f7W5i6YEJMqKDBRfIyCIogJGhsIiCHYwMZGQoLKJgByMDGRkKiyjYwchARobCIgp2MDKQkSEBAFp8pRIw+1YfAAAAAElFTkSuQmCC"
    
    print(f"Testing FreeImage.host API...")
    print(f"API URL: {api_url}")
    print(f"API Key: {api_key}")
    print()
    
    # Prepare form data exactly like in C# code
    form_data = {
        'key': api_key,
        'action': 'upload',
        'source': test_image_b64,
        'format': 'json'
    }
    
    try:
        print("Making API request...")
        response = requests.post(api_url, data=form_data, timeout=30)
        
        print(f"Response Status: {response.status_code}")
        print(f"Response Headers: {dict(response.headers)}")
        print(f"Response Content: {response.text}")
        
        if response.status_code == 200:
            try:
                json_response = response.json()
                if json_response.get('status_code') == 200:
                    image_url = json_response.get('image', {}).get('url')
                    print(f"✅ SUCCESS! Image uploaded to: {image_url}")
                else:
                    print(f"❌ API returned error: {json_response}")
            except Exception as e:
                print(f"❌ Failed to parse JSON: {e}")
        else:
            print(f"❌ HTTP Error {response.status_code}")
            
    except requests.exceptions.RequestException as e:
        print(f"❌ Request failed: {e}")

if __name__ == "__main__":
    test_freeimagehost_api()
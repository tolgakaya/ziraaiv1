import requests
import json
import time
import base64
from datetime import datetime
import psycopg2
import urllib3

# Disable SSL warnings for local testing
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# API endpoints
BASE_URL = "https://localhost:5001"
ASYNC_ANALYZE_URL = f"{BASE_URL}/api/plantanalyses/analyze-async"
GET_ANALYSIS_URL = f"{BASE_URL}/api/plantanalyses"

# Database connection
DB_CONFIG = {
    "host": "localhost",
    "database": "devarchitecture",
    "user": "postgres",
    "password": "Admin01!"
}

# Test image - create a small 1x1 red pixel PNG
test_image_base64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg=="
test_image_data_uri = f"data:image/png;base64,{test_image_base64}"

def test_async_flow():
    print("=" * 60)
    print("ASYNC PLANT ANALYSIS FLOW TEST")
    print("=" * 60)
    
    # Test data
    test_request = {
        "image": test_image_data_uri,
        "userId": 1,
        "farmerId": 101,
        "sponsorId": 201,
        "location": "Test Field A",
        "gpsCoordinates": {
            "lat": 41.0082,
            "lng": 28.9784
        },
        "cropType": "Tomato",
        "fieldId": "FIELD-001",
        "urgencyLevel": "High",
        "notes": "Test async flow with complete data",
        "altitude": 120.5,
        "plantingDate": "2024-10-15T00:00:00Z",
        "expectedHarvestDate": "2025-03-15T00:00:00Z",
        "lastFertilization": "2024-12-01T00:00:00Z",
        "lastIrrigation": "2024-12-20T00:00:00Z",
        "previousTreatments": ["Fertilizer-NPK", "Pesticide-Organic"],
        "weatherConditions": "Sunny",
        "temperature": 25.5,
        "humidity": 65.0,
        "soilType": "Loamy",
        "contactInfo": {
            "phone": "+905551234567",
            "email": "farmer@test.com"
        },
        "additionalInfo": {
            "plot_size": "2 hectares",
            "irrigation_type": "Drip"
        }
    }
    
    try:
        # Step 1: Submit async analysis request
        print("\n1. Submitting async analysis request...")
        response = requests.post(
            ASYNC_ANALYZE_URL,
            json=test_request,
            verify=False
        )
        
        if response.status_code == 200:
            result = response.json()
            if result["success"]:
                analysis_id = result["data"]
                print(f"   ✓ Analysis queued successfully")
                print(f"   Analysis ID: {analysis_id}")
            else:
                print(f"   ✗ Failed: {result.get('message', 'Unknown error')}")
                return
        else:
            print(f"   ✗ HTTP {response.status_code}: {response.text}")
            return
            
        # Step 2: Check database for initial record
        print("\n2. Checking database for initial record...")
        conn = psycopg2.connect(**DB_CONFIG)
        cursor = conn.cursor()
        
        query = """
            SELECT 
                "Id", "AnalysisId", "UserId", "FarmerId", "SponsorId",
                "Location", "CropType", "FieldId", "UrgencyLevel", "Notes",
                "Latitude", "Longitude", "Altitude", "Temperature", "Humidity",
                "WeatherConditions", "SoilType", "PlantingDate", "ContactPhone",
                "ContactEmail", "AnalysisStatus", "ImagePath", "CreatedDate"
            FROM "PlantAnalyses"
            WHERE "AnalysisId" = %s
        """
        
        cursor.execute(query, (analysis_id,))
        row = cursor.fetchone()
        
        if row:
            print("   ✓ Initial record found in database")
            print(f"   Database ID: {row[0]}")
            print(f"   Status: {row[20]}")
            print(f"   UserId: {row[2]}")
            print(f"   FarmerId: {row[3]}")
            print(f"   Location: {row[5]}")
            print(f"   CropType: {row[6]}")
            print(f"   Temperature: {row[13]}")
            print(f"   ImagePath: {row[21]}")
            print(f"   CreatedDate: {row[22]}")
            
            # Check for all important fields
            missing_fields = []
            field_names = ["UserId", "FarmerId", "SponsorId", "Location", "CropType", 
                          "FieldId", "UrgencyLevel", "Latitude", "Longitude", "ContactPhone"]
            field_indices = [2, 3, 4, 5, 6, 7, 8, 9, 10, 18]
            
            for name, idx in zip(field_names, field_indices):
                if row[idx] is None:
                    missing_fields.append(name)
            
            if missing_fields:
                print(f"   ⚠ Missing fields: {', '.join(missing_fields)}")
            else:
                print("   ✓ All critical fields are populated")
        else:
            print(f"   ✗ No record found for AnalysisId: {analysis_id}")
            
        # Step 3: Mock N8N response
        print("\n3. Simulating N8N processing...")
        print("   Sending mock response to worker queue...")
        
        mock_response = {
            "analysisId": analysis_id,
            "userId": test_request["userId"],
            "farmerId": test_request["farmerId"],
            "sponsorId": test_request["sponsorId"],
            "timestamp": datetime.utcnow().isoformat() + "Z",
            "plantIdentification": {
                "species": "Solanum lycopersicum",
                "variety": "Roma",
                "growthStage": "Fruiting",
                "confidence": 0.92
            },
            "healthAssessment": {
                "vigorScore": 7.5,
                "severity": "low"
            },
            "nutrientStatus": {
                "primaryDeficiency": "nitrogen"
            },
            "summary": {
                "overallHealthScore": 8.0,
                "primaryConcern": "Slight nitrogen deficiency",
                "prognosis": "Good with proper fertilization",
                "estimatedYieldImpact": "5-10% reduction if untreated",
                "confidenceLevel": 0.88
            },
            "recommendations": ["Apply nitrogen fertilizer", "Monitor growth"],
            "crossFactorInsights": ["Weather conditions favorable"],
            "processingMetadata": {
                "aiModel": "GPT-4-Vision"
            }
        }
        
        # Call the mock endpoint
        mock_url = f"{BASE_URL}/api/test/mock-n8n-response"
        mock_response_result = requests.post(
            mock_url,
            json=mock_response,
            verify=False
        )
        
        if mock_response_result.status_code == 200:
            print("   ✓ Mock response sent successfully")
        else:
            print(f"   ✗ Failed to send mock response: {mock_response_result.text}")
        
        # Step 4: Wait and check for update
        print("\n4. Waiting for worker to process...")
        time.sleep(5)  # Wait for worker to process
        
        print("   Checking database for updated record...")
        cursor.execute(query, (analysis_id,))
        updated_row = cursor.fetchone()
        
        if updated_row:
            print(f"   Status: {updated_row[20]}")
            
            # Check if AI results are populated
            ai_query = """
                SELECT 
                    "PlantSpecies", "PlantVariety", "GrowthStage", 
                    "OverallHealthScore", "PrimaryConcern", "AnalysisResult"
                FROM "PlantAnalyses"
                WHERE "AnalysisId" = %s
            """
            cursor.execute(ai_query, (analysis_id,))
            ai_row = cursor.fetchone()
            
            if ai_row:
                print("\n   AI Analysis Results:")
                print(f"   Plant Species: {ai_row[0]}")
                print(f"   Plant Variety: {ai_row[1]}")
                print(f"   Growth Stage: {ai_row[2]}")
                print(f"   Overall Health Score: {ai_row[3]}")
                print(f"   Primary Concern: {ai_row[4]}")
                
                if ai_row[5]:
                    print("   ✓ Complete analysis result stored")
                else:
                    print("   ⚠ Analysis result field is empty")
                    
                # Final verification
                if ai_row[0] and updated_row[20] == "Completed":
                    print("\n   ✅ SUCCESS: Async flow completed successfully!")
                    print("   All data preserved and AI results stored.")
                else:
                    print("\n   ⚠ PARTIAL SUCCESS: Some data may be missing")
            else:
                print("   ✗ No AI results found")
        else:
            print("   ✗ Record not found after processing")
        
        cursor.close()
        conn.close()
        
    except Exception as e:
        print(f"\n❌ ERROR: {str(e)}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    test_async_flow()
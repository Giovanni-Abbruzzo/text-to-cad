#!/usr/bin/env python3
"""
Test script to verify the /generate_model endpoint functionality.
This tests the complete workflow: parsing -> building -> exporting -> response.
"""

import requests
import json
import sys
import os

# Test endpoint URL
BASE_URL = "http://localhost:8000"
GENERATE_MODEL_URL = f"{BASE_URL}/generate_model"

def test_generate_model_endpoint():
    """Test the /generate_model endpoint with various requests."""
    
    test_cases = [
        {
            "name": "Cylinder with STEP export",
            "request": {
                "instruction": "create a cylinder 20mm high and 15mm diameter",
                "use_ai": False,
                "export_step": True,
                "export_stl": False
            }
        },
        {
            "name": "Holes with both exports",
            "request": {
                "instruction": "make 4 holes with 6mm diameter",
                "use_ai": False,
                "export_step": True,
                "export_stl": True
            }
        },
        {
            "name": "AI parsing with STL export",
            "request": {
                "instruction": "create a 5mm hole",
                "use_ai": True,
                "export_step": False,
                "export_stl": True
            }
        }
    ]
    
    print("Testing /generate_model endpoint...")
    print(f"Endpoint: {GENERATE_MODEL_URL}")
    print("-" * 60)
    
    for i, test_case in enumerate(test_cases, 1):
        print(f"\nTest {i}: {test_case['name']}")
        print(f"Request: {json.dumps(test_case['request'], indent=2)}")
        
        try:
            # Make POST request
            response = requests.post(GENERATE_MODEL_URL, json=test_case['request'], timeout=30)
            
            print(f"Status Code: {response.status_code}")
            
            if response.status_code == 200:
                result = response.json()
                print("✅ SUCCESS")
                print(f"Source: {result['source']}")
                print(f"Summary: {result['summary']}")
                if result['step_url']:
                    print(f"STEP URL: {result['step_url']}")
                if result['stl_url']:
                    print(f"STL URL: {result['stl_url']}")
            else:
                print("❌ FAILED")
                print(f"Error: {response.text}")
                
        except requests.exceptions.RequestException as e:
            print(f"❌ REQUEST FAILED: {str(e)}")
        
        print("-" * 40)

def test_server_health():
    """Check if the server is running."""
    try:
        response = requests.get(f"{BASE_URL}/health", timeout=5)
        if response.status_code == 200:
            print("✅ Server is running")
            return True
        else:
            print(f"❌ Server health check failed: {response.status_code}")
            return False
    except requests.exceptions.RequestException as e:
        print(f"❌ Cannot connect to server: {str(e)}")
        return False

if __name__ == "__main__":
    print("Testing /generate_model endpoint implementation...")
    
    # Check server health first
    if not test_server_health():
        print("\n⚠️  Please ensure the backend server is running:")
        print("   uvicorn main:app --reload --port 8000")
        sys.exit(1)
    
    # Run endpoint tests
    test_generate_model_endpoint()
    
    print("\n🎉 Testing completed!")

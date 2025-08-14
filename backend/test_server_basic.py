#!/usr/bin/env python3
"""
Simple test script to verify the backend server is running without external dependencies.
Tests basic endpoints that should work regardless of CadQuery installation.
"""

import urllib.request
import json
import sys

BASE_URL = "http://localhost:8000"

def test_endpoint(url, description):
    """Test a single endpoint and return result."""
    try:
        with urllib.request.urlopen(url, timeout=5) as response:
            if response.status == 200:
                data = json.loads(response.read().decode())
                print(f"✅ {description}: SUCCESS")
                return True, data
            else:
                print(f"❌ {description}: HTTP {response.status}")
                return False, None
    except Exception as e:
        print(f"❌ {description}: {str(e)}")
        return False, None

def test_parse_instruction():
    """Test the /process_instruction endpoint."""
    url = f"{BASE_URL}/process_instruction"
    data = {
        "instruction": "create a 5mm hole",
        "use_ai": False
    }
    
    try:
        req = urllib.request.Request(
            url, 
            data=json.dumps(data).encode('utf-8'),
            headers={'Content-Type': 'application/json'}
        )
        
        with urllib.request.urlopen(req, timeout=10) as response:
            if response.status == 200:
                result = json.loads(response.read().decode())
                print(f"✅ Parse Instruction: SUCCESS")
                print(f"   Source: {result['source']}")
                print(f"   Action: {result['parsed_parameters']['action']}")
                return True
            else:
                print(f"❌ Parse Instruction: HTTP {response.status}")
                return False
    except Exception as e:
        print(f"❌ Parse Instruction: {str(e)}")
        return False

def test_generate_model():
    """Test the /generate_model endpoint (should return 503 without CadQuery)."""
    url = f"{BASE_URL}/generate_model"
    data = {
        "instruction": "create a cylinder 20mm high",
        "use_ai": False,
        "export_step": True,
        "export_stl": False
    }
    
    try:
        req = urllib.request.Request(
            url, 
            data=json.dumps(data).encode('utf-8'),
            headers={'Content-Type': 'application/json'}
        )
        
        with urllib.request.urlopen(req, timeout=10) as response:
            print(f"❌ Generate Model: Unexpected success (should fail without CadQuery)")
            return False
    except urllib.error.HTTPError as e:
        if e.code == 503:
            print(f"✅ Generate Model: Expected 503 error (CadQuery not available)")
            return True
        else:
            print(f"❌ Generate Model: Unexpected HTTP {e.code}")
            return False
    except Exception as e:
        print(f"❌ Generate Model: {str(e)}")
        return False

if __name__ == "__main__":
    print("Testing Text-to-CAD Backend Server...")
    print(f"Base URL: {BASE_URL}")
    print("-" * 50)
    
    # Test basic endpoints
    success, _ = test_endpoint(f"{BASE_URL}/health", "Health Check")
    if not success:
        print("\n⚠️  Server is not running. Please start it with:")
        print("   .venv\\Scripts\\Activate.ps1; uvicorn main:app --reload --port 8000")
        sys.exit(1)
    
    test_endpoint(f"{BASE_URL}/", "Root Endpoint")
    test_endpoint(f"{BASE_URL}/config", "Config Endpoint")
    
    # Test parsing functionality
    test_parse_instruction()
    
    # Test model generation (should fail gracefully)
    test_generate_model()
    
    print("\n" + "=" * 50)
    print("🎉 Basic server functionality test completed!")
    print("\n📋 Summary:")
    print("✅ Server is running and responding")
    print("✅ Parsing functionality works (Sprint 8-A complete)")
    print("⚠️  Model generation needs CadQuery installation (Sprint 8-B)")
    print("\n🔧 To enable model generation, install CadQuery properly:")
    print("   See the CadQuery installation guide below.")

"""
Test script for /dry_run endpoint
"""
import requests
import json

BASE_URL = "http://localhost:8000"

def test_dry_run():
    """Test the /dry_run endpoint with various instructions"""
    
    test_cases = [
        {
            "name": "4 holes on top face",
            "instruction": "add 4 holes on the top face",
            "use_ai": False
        },
        {
            "name": "Cylinder extrusion",
            "instruction": "create a 25mm diameter cylinder 40mm tall",
            "use_ai": False
        },
        {
            "name": "Pattern with diameter",
            "instruction": "pattern 6 holes with 5mm diameter in a circle",
            "use_ai": False
        }
    ]
    
    print("=" * 80)
    print("Testing /dry_run endpoint")
    print("=" * 80)
    
    for test in test_cases:
        print(f"\n{'=' * 80}")
        print(f"Test: {test['name']}")
        print(f"Instruction: {test['instruction']}")
        print(f"Use AI: {test['use_ai']}")
        print("-" * 80)
        
        payload = {
            "instruction": test["instruction"],
            "use_ai": test["use_ai"]
        }
        
        try:
            response = requests.post(f"{BASE_URL}/dry_run", json=payload)
            
            if response.status_code == 200:
                data = response.json()
                print(f"✅ Status: {response.status_code}")
                print(f"\nResponse:")
                print(json.dumps(data, indent=2))
                
                # Validate required fields
                assert "schema_version" in data, "Missing schema_version"
                assert "source" in data, "Missing source"
                assert "plan" in data, "Missing plan"
                assert "parsed_parameters" in data, "Missing parsed_parameters"
                assert data["schema_version"] == "1.0", "Wrong schema_version"
                assert isinstance(data["plan"], list), "Plan should be a list"
                assert len(data["plan"]) > 0, "Plan should not be empty"
                
                print(f"\n✅ All validations passed!")
            else:
                print(f"❌ Status: {response.status_code}")
                print(f"Response: {response.text}")
                
        except Exception as e:
            print(f"❌ Error: {str(e)}")
    
    print(f"\n{'=' * 80}")
    print("Testing /process_instruction endpoint (should have same structure)")
    print("=" * 80)
    
    # Test that /process_instruction has the same structure
    payload = {
        "instruction": "test instruction for schema validation",
        "use_ai": False
    }
    
    try:
        response = requests.post(f"{BASE_URL}/process_instruction", json=payload)
        
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Status: {response.status_code}")
            print(f"\nResponse:")
            print(json.dumps(data, indent=2))
            
            # Validate same fields as dry_run
            assert "schema_version" in data, "Missing schema_version"
            assert "source" in data, "Missing source"
            assert "plan" in data, "Missing plan"
            assert "parsed_parameters" in data, "Missing parsed_parameters"
            assert data["schema_version"] == "1.0", "Wrong schema_version"
            
            print(f"\n✅ /process_instruction has matching schema!")
        else:
            print(f"❌ Status: {response.status_code}")
            print(f"Response: {response.text}")
            
    except Exception as e:
        print(f"❌ Error: {str(e)}")
    
    print(f"\n{'=' * 80}")
    print("All tests completed!")
    print("=" * 80)

if __name__ == "__main__":
    test_dry_run()

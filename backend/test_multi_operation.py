"""
Test multi-operation support with multi-line instructions
"""
import requests
import json

# Test the actual backend endpoint
BASE_URL = "http://localhost:8000"

def test_multi_operation():
    """Test multi-line instruction parsing"""
    
    # Test 1: Multi-line instruction (newline-separated)
    instruction = """create 80mm base plate 6mm thick
create a 15mm cylinder 30mm tall"""
    
    print("\n" + "=" * 60)
    print("TEST 1: NEWLINE-SEPARATED INSTRUCTIONS")
    print("=" * 60)
    test_instruction(instruction)
    
    # Test 2: Single-line instruction (space-separated)
    instruction2 = "create 100mm base plate 5mm thick create a 20mm cylinder 40mm tall"
    
    print("\n" + "=" * 60)
    print("TEST 2: SINGLE-LINE MULTI-OPERATION")
    print("=" * 60)
    test_instruction(instruction2)


def test_instruction(instruction):
    """Test a specific instruction"""
    
    payload = {
        "instruction": instruction,
        "use_ai": False  # Test rule-based parser
    }
    
    print(f"\nInstruction:\n{instruction}")
    print(f"\nUse AI: {payload['use_ai']}")
    
    try:
        response = requests.post(f"{BASE_URL}/process_instruction", json=payload)
        response.raise_for_status()
        
        result = response.json()
        
        print(f"\n✓ Response received (status {response.status_code})")
        print(f"\nSchema Version: {result.get('schema_version')}")
        print(f"Source: {result.get('source')}")
        
        # Print plan
        print(f"\n📋 PLAN:")
        for step in result.get('plan', []):
            print(f"  • {step}")
        
        # Print operations
        operations = result.get('operations', [])
        print(f"\n🔧 OPERATIONS: {len(operations)}")
        
        for i, op in enumerate(operations, 1):
            print(f"\n  Operation {i}:")
            print(f"    Action: {op.get('action')}")
            params = op.get('parameters', {})
            print(f"    Shape: {params.get('shape')}")
            print(f"    Diameter: {params.get('diameter_mm')}mm")
            print(f"    Height: {params.get('height_mm')}mm")
        
        # Verify expectations
        print(f"\n{'=' * 60}")
        print("VERIFICATION:")
        print(f"{'=' * 60}")
        
        expected_count = 2
        actual_count = len(operations)
        
        if actual_count == expected_count:
            print(f"✓ Operation count: {actual_count}/{expected_count}")
        else:
            print(f"✗ Operation count: {actual_count}/{expected_count} MISMATCH!")
        
        # Check operation 1 (base plate)
        if len(operations) >= 1:
            op1 = operations[0]
            params1 = op1.get('parameters', {})
            shape1 = params1.get('shape', '').lower()
            
            if 'base' in shape1 or 'plate' in shape1:
                print(f"✓ Operation 1: Base plate detected")
            else:
                print(f"✗ Operation 1: Expected base plate, got '{shape1}'")
        
        # Check operation 2 (cylinder)
        if len(operations) >= 2:
            op2 = operations[1]
            params2 = op2.get('parameters', {})
            shape2 = params2.get('shape', '').lower()
            diameter2 = params2.get('diameter_mm')
            height2 = params2.get('height_mm')
            
            if 'cylinder' in shape2:
                print(f"✓ Operation 2: Cylinder detected")
            else:
                print(f"✗ Operation 2: Expected cylinder, got '{shape2}'")
            
            if diameter2 == 15.0:
                print(f"✓ Operation 2 diameter: {diameter2}mm")
            else:
                print(f"✗ Operation 2 diameter: Expected 15mm, got {diameter2}mm")
            
            if height2 == 30.0:
                print(f"✓ Operation 2 height: {height2}mm")
            else:
                print(f"✗ Operation 2 height: Expected 30mm, got {height2}mm")
        
        print(f"\n✓ TEST COMPLETE\n")
        
    except requests.exceptions.ConnectionError:
        print("\n✗ ERROR: Could not connect to backend")
        print("Make sure the backend is running:")
        print("  cd backend")
        print("  .venv\\Scripts\\Activate.ps1")
        print("  uvicorn main:app --reload")
        
    except Exception as e:
        print(f"\n✗ ERROR: {e}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    test_multi_operation()

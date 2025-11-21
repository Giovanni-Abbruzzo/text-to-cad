"""
Test script to verify the diameter/height parsing fix.
Tests both the failing and working cases.
"""
import re

def test_dimension_extraction(instruction):
    """Test dimension extraction from instruction"""
    instruction_lower = instruction.lower()
    
    # Height extraction
    height_mm = None
    height_patterns = [
        r'(?:height|tall|length|depth)\s+(?:of\s+)?([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?',
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s+(?:tall|high|long|deep)(?:\s|$)',
    ]
    
    for pattern in height_patterns:
        match = re.search(pattern, instruction_lower)
        if match:
            try:
                height_mm = float(match.group(1))
                break
            except (ValueError, IndexError):
                continue
    
    # Diameter extraction
    diameter_mm = None
    diameter_patterns = [
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s+(?:diameter|dia|width|wide)(?:\s|$)',
        r'(?:diameter|dia|width|wide)\s*(?:of\s*)?([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?',
        r'([0-9]*\.?[0-9]+)\s*(?:mm|millimeter|millimeters)?\s+(?:across|around)(?:\s|$)'
    ]
    
    for pattern in diameter_patterns:
        match = re.search(pattern, instruction_lower)
        if match:
            try:
                diameter_mm = float(match.group(1))
                break
            except (ValueError, IndexError):
                continue
    
    return diameter_mm, height_mm


# Test cases
test_cases = [
    {
        "instruction": "create a cylinder 25mm diameter 15mm tall",
        "expected_diameter": 25.0,
        "expected_height": 15.0
    },
    {
        "instruction": "create a 20mm diameter cylinder 50mm tall",
        "expected_diameter": 20.0,
        "expected_height": 50.0
    },
    {
        "instruction": "make a cylinder diameter 30mm height 40mm",
        "expected_diameter": 30.0,
        "expected_height": 40.0
    },
    {
        "instruction": "create cylinder 10mm wide 20mm high",
        "expected_diameter": 10.0,
        "expected_height": 20.0
    }
]

print("=" * 60)
print("DIMENSION EXTRACTION TEST")
print("=" * 60)

all_passed = True
for i, test in enumerate(test_cases, 1):
    instruction = test["instruction"]
    expected_diameter = test["expected_diameter"]
    expected_height = test["expected_height"]
    
    diameter, height = test_dimension_extraction(instruction)
    
    diameter_ok = diameter == expected_diameter
    height_ok = height == expected_height
    passed = diameter_ok and height_ok
    
    status = "✓ PASS" if passed else "✗ FAIL"
    
    print(f"\nTest {i}: {status}")
    print(f"  Instruction: \"{instruction}\"")
    print(f"  Expected: Ø{expected_diameter}mm × {expected_height}mm")
    print(f"  Got:      Ø{diameter}mm × {height}mm")
    
    if not diameter_ok:
        print(f"  ERROR: Diameter mismatch! Expected {expected_diameter}, got {diameter}")
    if not height_ok:
        print(f"  ERROR: Height mismatch! Expected {expected_height}, got {height}")
    
    all_passed = all_passed and passed

print("\n" + "=" * 60)
if all_passed:
    print("✓ ALL TESTS PASSED!")
else:
    print("✗ SOME TESTS FAILED")
print("=" * 60)

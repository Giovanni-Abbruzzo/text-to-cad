from pathlib import Path
import sys

from fastapi.testclient import TestClient

BACKEND_DIR = Path(__file__).resolve().parents[1]
if str(BACKEND_DIR) not in sys.path:
    sys.path.insert(0, str(BACKEND_DIR))

import main  # noqa: E402

client = TestClient(main.app)


def test_health_ok():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_dry_run_returns_plan_and_metadata():
    response = client.post(
        "/dry_run",
        json={"instruction": "create a 5mm hole", "use_ai": False},
    )
    assert response.status_code == 200

    payload = response.json()
    assert payload.get("schema_version") == "1.0"
    assert payload.get("source") in {"rule", "ai"}
    assert isinstance(payload.get("plan"), list)
    assert payload["plan"]


def test_process_instruction_returns_schema_version_source():
    response = client.post(
        "/process_instruction",
        json={"instruction": "create a 5mm hole", "use_ai": False},
    )
    assert response.status_code == 200

    payload = response.json()
    assert payload.get("schema_version") == "1.0"
    assert payload.get("source") in {"rule", "ai"}


def test_empty_instruction_422():
    response = client.post("/dry_run", json={"instruction": "", "use_ai": False})
    assert response.status_code == 422

    payload = response.json()
    detail = payload.get("detail", [])
    assert any(item.get("msg") == "Instruction cannot be empty." for item in detail)


def test_plan_returns_questions_for_bike():
    response = client.post(
        "/plan",
        json={"instruction": "make a mountain bike", "use_ai": False},
    )
    assert response.status_code == 200

    payload = response.json()
    assert payload.get("schema_version") == "1.0"
    assert payload.get("state_id")
    assert payload.get("status") == "awaiting_answers"
    assert isinstance(payload.get("plan"), list)
    assert payload["plan"]
    assert isinstance(payload.get("questions"), list)
    assert payload["questions"]


def test_plan_resume_returns_operations():
    initial = client.post(
        "/plan",
        json={"instruction": "make a mountain bike", "use_ai": False},
    )
    assert initial.status_code == 200
    state_id = initial.json().get("state_id")
    assert state_id

    response = client.post(
        "/plan",
        json={
            "state_id": state_id,
            "answers": {
                "frame_size_mm": 450,
                "wheel_diameter_mm": 650,
                "tire_width_mm": 55,
                "handlebar_width_mm": 720,
            },
        },
    )
    assert response.status_code == 200
    payload = response.json()
    assert payload.get("status") == "ready"
    assert isinstance(payload.get("operations"), list)
    assert payload["operations"]

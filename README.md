# Text-to-CAD

Prototype: natural-language → structured CAD commands. FastAPI backend + React (Vite) frontend.

## Backend Quickstart

### Setup & Installation

1. **Create virtual environment:**
   ```bash
   cd backend
   python -m venv .venv
   ```

2. **Activate virtual environment:**
   ```bash
   # Windows
   .venv\Scripts\Activate.ps1
   
   # macOS/Linux
   source .venv/bin/activate
   ```

3. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

4. **Run the server:**
   ```bash
   uvicorn backend.main:app --reload
   ```

### Available Endpoints

- **`GET /health`** - Health check endpoint
- **`GET /`** - API information and available endpoints
- **`POST /process_instruction`** - Process natural language CAD instructions
- **`GET /config`** - View current configuration (excludes sensitive data)
- **`GET /docs`** - Interactive API documentation (Swagger UI)

### Usage Example

```bash
# Test the instruction processing endpoint
curl -X POST "http://localhost:8000/process_instruction" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "extrude a 5mm cylinder with 10mm diameter"}'
```

**Response:**
```json
{
  "instruction": "extrude a 5mm cylinder with 10mm diameter",
  "parsed_parameters": {
    "action": "extrude",
    "shape": "cylinder", 
    "height_mm": 5.0,
    "diameter_mm": 10.0,
    "count": null
  }
}
```

### Important Notes

- **Units**: All dimensions use millimeters as the default unit (indicated by `*_mm` field suffixes)
- **Parsing**: Currently uses naive keyword detection and regex patterns for parameter extraction
- **Validation**: Instructions must be at least 3 characters long and non-empty
- **Response Format**: Always returns consistent JSON shape with `null` for undetected parameters
- **Configuration**: Optional `.env` file support (see `backend/.env.example`)

### Next Sprint Features

The next development sprint will focus on:
- **Persistence**: Database integration for instruction history
- **History API**: Endpoints to retrieve and manage past instructions
- **Enhanced Parsing**: More sophisticated NLP techniques
- **LLM Integration**: Optional AI-powered instruction understanding

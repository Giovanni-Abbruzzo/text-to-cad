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

## Frontend Quickstart

### Setup & Installation

1. **Navigate to frontend directory:**
   ```bash
   cd frontend
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Create environment file:**
   ```bash
   # Create frontend/.env with:
   VITE_API_BASE=http://localhost:8000
   ```

4. **Start development server:**
   ```bash
   npm run dev
   ```

The frontend will be available at `http://localhost:5173`

### Expected Flow

1. **User Input**: Type a natural-language CAD instruction in the text field
2. **API Call**: Frontend POSTs the instruction to `/process_instruction` endpoint
3. **Response Display**: Structured JSON response is rendered in a formatted code block

### Example Usage

1. Enter instruction: `"Extrude a 5mm tall cylinder with 10mm diameter"`
2. Click "Send" button
3. View the structured JSON response with parsed CAD parameters

### Troubleshooting

**Common Issues:**

- **CORS Errors**: Ensure the backend is running and CORS is properly configured for `http://localhost:5173`
- **Port Conflicts**: 
  - Frontend default: `http://localhost:5173` (can be changed in Vite config)
  - Backend default: `http://localhost:8000` (update `VITE_API_BASE` if different)
- **Environment Variables**: Make sure `frontend/.env` exists with correct `VITE_API_BASE` value
- **API Connection**: Verify backend is running at the URL specified in `VITE_API_BASE`

### Next Sprint Features

The next development sprint will focus on:
- **Persistence**: Database integration for instruction history
- **History API**: Endpoints to retrieve and manage past instructions
- **Enhanced Parsing**: More sophisticated NLP techniques
- **LLM Integration**: Optional AI-powered instruction understanding

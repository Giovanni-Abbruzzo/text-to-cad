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

## Persistence & History (Sprint 3)

The backend now includes full database persistence using **SQLite** and **SQLAlchemy** for storing and retrieving command history.

### Database Features

- **SQLite Database**: Lightweight, file-based database (`backend/app.db`)
- **SQLAlchemy ORM**: Modern Python ORM for database operations
- **Auto-initialization**: Database tables are created automatically on startup
- **Command History**: All processed instructions are saved with timestamps
- **JSON Storage**: Parameters are stored as JSON strings for flexible querying

### Database Schema

The `commands` table stores:
- `id` (Primary Key): Auto-incrementing integer
- `prompt` (Text): Original natural language instruction
- `action` (String): Parsed CAD action (e.g., "extrude", "create_hole")
- `parameters` (Text): JSON string of parsed parameters
- `created_at` (DateTime): Timestamp with index for efficient queries

### New Endpoints

#### GET /commands
Retrieve command history in reverse chronological order (newest first).

**Query Parameters:**
- `limit` (optional): Number of commands to return (default: 20, max: 100)

**Example Request:**
```bash
curl "http://localhost:8000/commands?limit=10"
```

**Example Response:**
```json
[
  {
    "id": 3,
    "prompt": "extrude a 1mm tall box that is 3mm wide",
    "action": "extrude",
    "parameters": {
      "shape": "block",
      "height_mm": 1.0,
      "diameter_mm": 3.0,
      "count": null
    },
    "created_at": "2025-08-13T16:27:41.123456"
  },
  {
    "id": 2,
    "prompt": "create a 5mm diameter hole",
    "action": "create_hole",
    "parameters": {
      "shape": null,
      "height_mm": null,
      "diameter_mm": 5.0,
      "count": null
    },
    "created_at": "2025-08-13T16:25:09.123456"
  }
]
```

#### Updated POST /process_instruction
Now returns the saved database record instead of just parsed parameters.

**New Response Format:**
```json
{
  "id": 1,
  "prompt": "extrude a 5mm tall cylinder with 10mm diameter",
  "action": "extrude",
  "parameters": {
    "shape": "cylinder",
    "height_mm": 5.0,
    "diameter_mm": 10.0,
    "count": null
  },
  "created_at": "2025-08-13T10:30:45.123456"
}
```

### Database Troubleshooting

**Common Issues:**

- **Database Permissions**: Ensure write permissions in the `backend/` directory for `app.db` creation
- **Virtual Environment**: Always activate the virtual environment before running:
  ```bash
  # Windows
  .venv\Scripts\Activate.ps1
  
  # macOS/Linux  
  source .venv/bin/activate
  ```
- **Missing Dependencies**: If you see "uvicorn not recognized", install dependencies:
  ```bash
  pip install -r requirements.txt
  ```
- **Database Corruption**: If `app.db` becomes corrupted, delete it and restart the server to recreate
- **SQLite Locked**: Close any database browser tools that might have the database file open
- **Migration Issues**: For schema changes, delete `app.db` and restart (development only)

**Database File Location:**
- The SQLite database is created as `backend/app.db`
- This file contains all command history and should be backed up in production
- The database file is automatically created on first startup

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
4. **History Update**: Command history panel automatically refreshes to show the new command

### Features

- **Real-time Processing**: Instant feedback for natural language instructions
- **Command History**: Automatic display of previous commands with timestamps
- **Auto-refresh**: History panel updates after each successful submission
- **Clean UI**: Minimal, readable interface with consistent styling
- **Error Handling**: User-friendly error messages for failed requests

### Example Usage

1. Enter instruction: `"Extrude a 5mm tall cylinder with 10mm diameter"`
2. Click "Send" button
3. View the structured JSON response with parsed CAD parameters
4. See the new command appear at the top of the History panel

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

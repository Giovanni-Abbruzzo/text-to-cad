# Text-to-CAD

Prototype: natural-language → structured CAD commands. FastAPI backend + React (Vite) frontend.

## 🚀 Quickstart (Local)

Get up and running in under 2 minutes:

### Backend Setup
```bash
cd backend
python -m venv .venv

# Windows
.venv\Scripts\Activate.ps1
# macOS/Linux
source .venv/bin/activate

pip install -r requirements.txt
python -m uvicorn main:app --reload
```

### Frontend Setup
```bash
cd frontend
npm install
npm run dev
```

### Environment Configuration
Create `.env` files for configuration:

**Frontend** (`frontend/.env`):
```
VITE_API_BASE=http://localhost:8000/docs
```

**Backend** (`backend/.env`) - Optional for AI features:
```
OPENAI_API_KEY=your_openai_api_key_here
```

---

## 🎬 Quick Demo (45 seconds)

Perfect for showcasing the system:

1. **"Type: Extrude 3 cylinders that are 15mm tall with 8mm diameter."**
2. **"Send — backend parses (AI or rules) → structured JSON."**
3. **"History shows recent commands with timestamps."**
4. **"Start job automatically — progress bar goes 0→100."**
5. **"This API shape is ready to wire into Fusion/SolidWorks plugins."**

### Key Talking Points
- **"End-to-end: React → FastAPI → SQLite (SQLAlchemy) → (optional LLM) → telemetry."**
- **"Consistent JSON schema; always returns same shape; nulls for unknowns."**
- **"Safe AI fallback; never blocks the UI."**
- **"Containerization/Cloud Run are next; architecture already cleanly separated."**

---

## 🛠️ Troubleshooting

### Common Issues

**CORS Errors**
- Confirm backend CORS settings are correct
- Verify `VITE_API_BASE` in frontend `.env` matches backend URL

**Port Already in Use**
- Backend: Change from 8000 → `uvicorn main:app --reload --port 8001`
- Frontend: Change from 5173 → `npm run dev -- --port 3000`

**Windows Virtual Environment Issues**
- Use: `py -3.11 -m venv .venv` 
- Activate: `.venv\Scripts\Activate.ps1`
- If PowerShell blocked: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

**OpenAI Key Missing**
- Set `use_ai=false` in UI (checkbox unchecked)
- App falls back to rule-based parsing automatically
- No API key needed for basic functionality

**Frontend Won't Connect**
- Check backend is running at `http://localhost:8000`
- Verify `VITE_API_BASE` environment variable
- Try direct API test: `curl http://localhost:8000/health`

---

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
   OR for VScode
   python -m uvicorn main:app --reload
   ```

### Available Endpoints

- **`GET /health`** - Health check endpoint
- **`GET /`** - API information and available endpoints
- **`POST /process_instruction`** - Process natural language CAD instructions and save to database
- **`POST /dry_run`** - Preview instruction parsing without saving (plan preview)
- **`POST /generate_model`** - Generate 3D CAD models and export files
- **`GET /commands`** - Retrieve command history
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

---

## API Contract & Preview Plan

The Text-to-CAD backend provides a **stable JSON contract** designed for integration with CAD plugins like SolidWorks Add-Ins and Fusion360 scripts. All endpoints return consistent, versioned schemas with predictable field structures.

### Core Endpoints

#### POST /process_instruction
Process natural language CAD instructions, save to database, and return parsed parameters with execution plan.

**Request Body:**
```json
{
  "instruction": "add 4 holes on the top face",
  "use_ai": false
}
```

**Request Fields:**
- `instruction` (string, required): Natural language CAD instruction (minimum 3 characters)
- `use_ai` (boolean, optional): Use AI parsing if `true` and API key configured, otherwise rule-based (default: `false`)

**Response (200 OK):**
```json
{
  "schema_version": "1.0",
  "instruction": "add 4 holes on the top face",
  "source": "rule",
  "plan": [
    "Create 4 holes",
    "Arrange in circular pattern (4 instances)"
  ],
  "parsed_parameters": {
    "action": "create_hole",
    "parameters": {
      "count": 4,
      "diameter_mm": null,
      "height_mm": null,
      "shape": null,
      "pattern": null
    }
  }
}
```

**Response Fields:**
- `schema_version` (string): API contract version for stability tracking (currently `"1.0"`)
- `instruction` (string): Echo of the original instruction
- `source` (string): Parsing method used - `"ai"` (OpenAI) or `"rule"` (regex-based)
- `plan` (array of strings): Human-readable steps describing what will be executed
- `parsed_parameters` (object): Structured CAD parameters
  - `action` (string): CAD operation type (`"extrude"`, `"create_hole"`, `"fillet"`, `"pattern"`, `"create_feature"`)
  - `parameters` (object): Extracted parameters (all fields always present, `null` if not detected)
    - `count` (number | null): Number of features/instances
    - `diameter_mm` (number | null): Diameter in millimeters
    - `height_mm` (number | null): Height/thickness in millimeters
    - `shape` (string | null): Shape type (`"cylinder"`, `"block"`, `"sphere"`, etc.)
    - `pattern` (object | null): Pattern information for arrays
      - `type` (string | null): Pattern type (`"circular"`, `"linear"`)
      - `count` (number | null): Number of pattern instances
      - `angle_deg` (number | null): Angular spacing for circular patterns

**Error Response (422 Unprocessable Entity):**
```json
{
  "detail": [
    {
      "loc": ["body", "instruction"],
      "msg": "Instruction must be at least 3 characters long",
      "type": "value_error"
    }
  ]
}
```

**Side Effects:**
- Saves command to database with timestamp
- Creates database record accessible via `GET /commands`

---

#### POST /dry_run
Preview instruction parsing **without** saving to database or executing operations. Perfect for validation and plan preview.

**Request Body:**
```json
{
  "instruction": "create a 25mm diameter cylinder 40mm tall",
  "use_ai": false
}
```

**Request Fields:**
- Same as `/process_instruction`

**Response (200 OK):**
```json
{
  "schema_version": "1.0",
  "instruction": "create a 25mm diameter cylinder 40mm tall",
  "source": "rule",
  "plan": [
    "Create cylinder Ø25.0 mm × 40.0 mm height"
  ],
  "parsed_parameters": {
    "action": "create_feature",
    "parameters": {
      "count": null,
      "diameter_mm": 25.0,
      "height_mm": 40.0,
      "shape": "cylinder",
      "pattern": null
    }
  }
}
```

**Response Fields:**
- Identical structure to `/process_instruction`
- Same `schema_version`, `source`, `plan`, and `parsed_parameters` format

**Key Differences from /process_instruction:**
- ❌ Does NOT save to database
- ❌ Does NOT generate geometry
- ❌ Does NOT create any side effects
- ✅ Perfect for preview/validation workflows
- ✅ Ideal for SolidWorks Add-In contract testing

**Use Cases:**
- Preview what will happen before execution
- Validate instruction syntax without side effects
- Test AI vs. rule-based parsing differences
- SolidWorks Add-In contract validation
- Debugging parsing logic

---

### Field Definitions

#### schema_version
- **Type**: String constant
- **Current Value**: `"1.0"`
- **Purpose**: Enables clients to detect API contract changes
- **Stability**: Will only change when response structure changes in breaking ways

#### source
- **Type**: String enum
- **Values**: `"ai"` | `"rule"`
- **Meaning**:
  - `"ai"`: Parsed using OpenAI GPT model (requires `OPENAI_API_KEY` and `use_ai=true`)
  - `"rule"`: Parsed using regex-based rule engine (fallback or default)

#### plan
- **Type**: Array of strings
- **Purpose**: Human-readable description of execution steps
- **Format**: Each string is a complete sentence describing one operation
- **Examples**:
  - `["Extrude cylinder Ø20 mm × 30 mm height"]`
  - `["Create 6 holes Ø5 mm", "Arrange in circular pattern (6 instances)"]`
  - `["Create base plate 10 mm thick", "Pattern 4 holes Ø6 mm in circular arrangement"]`

#### parsed_parameters
- **Type**: Object with consistent structure
- **Guarantee**: All fields always present (never omitted)
- **Null Handling**: Undetected/unspecified parameters are explicitly `null`
- **Unit Convention**: All numeric dimensions in millimeters (indicated by `_mm` suffix)

---

### Example Workflows

#### Workflow 1: Preview then Execute
```bash
# Step 1: Preview with dry run
curl -X POST "http://localhost:8000/dry_run" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "extrude 20mm cylinder", "use_ai": false}'

# Step 2: Review the plan array and parsed_parameters

# Step 3: Execute if satisfied
curl -X POST "http://localhost:8000/process_instruction" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "extrude 20mm cylinder", "use_ai": false}'
```

#### Workflow 2: AI vs. Rule Comparison
```bash
# Compare AI parsing
curl -X POST "http://localhost:8000/dry_run" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "make 6 holes in a circle", "use_ai": true}'

# Compare rule-based parsing
curl -X POST "http://localhost:8000/dry_run" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "make 6 holes in a circle", "use_ai": false}'
```

#### Workflow 3: SolidWorks Add-In Integration
```vb
' VBA/VB.NET example for SolidWorks Add-In
Dim instruction As String
instruction = "create 4 mounting holes 6mm diameter"

' Step 1: Dry run to validate
Dim dryRunResponse As String
dryRunResponse = HttpPost("http://localhost:8000/dry_run", _
    "{""instruction"":""" & instruction & """,""use_ai"":false}")

' Step 2: Parse response and show plan to user
Dim plan As String
plan = ExtractPlanFromJson(dryRunResponse)
MsgBox "Will execute: " & plan

' Step 3: Execute if user confirms
If MsgBox("Proceed?", vbYesNo) = vbYes Then
    Dim response As String
    response = HttpPost("http://localhost:8000/process_instruction", _
        "{""instruction"":""" & instruction & """,""use_ai"":false}")
    
    ' Step 4: Use parsed_parameters to drive SolidWorks API
    Dim params As Object
    Set params = ParseJsonParameters(response)
    CreateHolesInSolidWorks params
End If
```

---

### Validation Rules

**Instruction Validation:**
- ❌ Empty strings rejected (422 error)
- ❌ Whitespace-only strings rejected (422 error)
- ❌ Strings < 3 characters rejected (422 error)
- ✅ Minimum valid: `"abc"` (3 characters)

**Error Response Format:**
All validation errors return HTTP 422 with Pydantic validation details:
```json
{
  "detail": [
    {
      "loc": ["body", "instruction"],
      "msg": "Instruction cannot be blank or empty",
      "type": "value_error"
    }
  ]
}
```

---

### Contract Stability Guarantees

**What Will NOT Change (Stable):**
- ✅ Field names (`schema_version`, `source`, `plan`, `parsed_parameters`)
- ✅ Field types (string, array, object, null)
- ✅ Presence of fields (all fields always present)
- ✅ Null semantics (null = not detected/not applicable)
- ✅ Unit convention (millimeters for all dimensions)

**What MAY Change (Versioned):**
- 🔄 New fields may be added (clients should ignore unknown fields)
- 🔄 `schema_version` will increment on breaking changes
- 🔄 New action types may be added to `action` enum
- 🔄 Plan format may become more detailed

**Migration Strategy:**
- Check `schema_version` field in client code
- Handle unknown `schema_version` values gracefully
- Ignore unknown fields in responses
- Test with `/dry_run` before deploying changes

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

## AI/LLM Integration (Sprint 4)

The Text-to-CAD system now supports **optional AI-powered instruction parsing** using OpenAI's GPT models. When enabled, the system can provide more sophisticated natural language understanding while maintaining a reliable fallback to rule-based parsing.

### Setup & Configuration

#### 1. Create Environment File
```bash
cd backend
cp .env.example .env
```

#### 2. Configure OpenAI API Key
Edit `backend/.env` and add your OpenAI API key:
```ini
# AI/LLM Configuration
# OpenAI configuration (optional; app falls back to rules if missing)
# The backend will only use AI when use_ai=true is sent by the client and a valid key is present
OPENAI_API_KEY=sk-your-actual-api-key-here
OPENAI_MODEL=gpt-4o
OPENAI_TIMEOUT_S=20
```

#### 3. Get Your OpenAI API Key
1. Visit [OpenAI's API platform](https://platform.openai.com/api-keys)
2. Sign in to your account
3. Click "Create new secret key"
4. Copy the key (starts with `sk-`)

### Usage

#### Backend API
The `/process_instruction` endpoint now accepts an optional `use_ai` parameter:

```bash
# Use AI parsing
curl -X POST "http://localhost:8000/process_instruction" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "create 4 holes in a circular pattern", "use_ai": true}'

# Use rule-based parsing (default)
curl -X POST "http://localhost:8000/process_instruction" \
     -H "Content-Type: application/json" \
     -d '{"instruction": "create 4 holes in a circular pattern", "use_ai": false}'
```

#### Frontend UI
The React frontend now includes:
- **"Use AI" checkbox** - Toggle between AI and rule-based parsing
- **Source indicator** - Shows whether AI or rules were used
- **Structured parameter display** - Clean table format instead of raw JSON
- **Fallback handling** - Automatically falls back to rules if AI fails

### AI Response Format

The AI uses a structured JSON schema to ensure consistent output:

```json
{
  "action": "create_hole" | "extrude" | "fillet" | "pattern" | "create_feature",
  "parameters": {
    "count": number | null,
    "diameter_mm": number | null,
    "height_mm": number | null,
    "shape": string | null,
    "pattern": {
      "type": "circular" | "linear" | null,
      "count": number | null,
      "angle_deg": number | null
    } | null
  }
}
```

#### AI Prompt Instructions
The AI receives this core instruction:
> "You are a CAD command parser. Convert natural language instructions into JSON commands. Return ONLY valid JSON matching the exact schema. Use null for unspecified parameters. Extract numeric values when mentioned. Choose the most appropriate action type."

### Fallback Behavior

The system provides **robust fallback** to ensure reliability:

1. **No API Key**: If `OPENAI_API_KEY` is not configured, uses rule-based parsing
2. **AI Request Failed**: If `use_ai=true` but API call fails, falls back to rules
3. **Invalid Response**: If AI returns malformed JSON, falls back to rules
4. **Network Issues**: If OpenAI API is unreachable, falls back to rules

**Response Format** (both AI and rule-based):
```json
{
  "instruction": "create a 5mm hole",
  "source": "ai" | "rule",
  "parsed_parameters": {
    "action": "create_hole",
    "parameters": {
      "diameter_mm": 5,
      "count": null,
      "height_mm": null,
      "shape": null,
      "pattern": null
    }
  }
}
```

### Cost & Rate Limits

#### OpenAI API Costs
- **gpt-4o**: ~$2.50 per 1M input tokens, ~$10.00 per 1M output tokens
- **gpt-4o-mini**: ~$0.15 per 1M input tokens, ~$0.60 per 1M output tokens
- Typical instruction: ~100-200 tokens total cost

#### Rate Limits
- **Free tier**: 3 requests per minute
- **Paid tier**: 500+ requests per minute (varies by usage tier)
- **Timeout**: 20 seconds (configurable via `OPENAI_TIMEOUT_S`)

#### Cost Management Tips
- Use `gpt-4o-mini` for development (much cheaper)
- Enable AI only when needed via the frontend toggle
- Rule-based parsing is always free and fast

### Security & Best Practices

#### ⚠️ API Key Security
- **Never commit** API keys to version control
- **Use environment variables** (`.env` file is in `.gitignore`)
- **Regenerate keys** if compromised
- **Monitor usage** on OpenAI dashboard

#### Production Considerations
- Set up **API key rotation**
- Monitor **usage and costs**
- Implement **rate limiting** if needed
- Consider **caching** for repeated instructions

### Troubleshooting

#### Common Issues

**"OpenAI API key not configured"**
- Ensure `.env` file exists in `backend/` directory
- Verify `OPENAI_API_KEY` is set correctly
- Restart the backend server after adding the key

**"AI parsing failed, falling back to rules"**
- Check OpenAI API key validity
- Verify internet connection
- Check OpenAI service status
- Review rate limits on your account

**Frontend shows "Rule-based" even with AI enabled**
- Backend may not have API key configured
- Check browser console for errors
- Verify backend logs for AI parsing attempts

#### Debugging
Enable detailed logging by checking backend console output:
```
INFO - Processing instruction: 'create a hole' (use_ai=True)
INFO - Attempting AI parsing with OpenAI
INFO - AI parsing successful: {...}
INFO - Command saved successfully with ID: 1 (source: ai)
```

## Telemetry & Job Simulation (Sprint 5)

The Text-to-CAD system now includes **simulated job execution** with real-time progress tracking to demonstrate how long-running CAD operations would work in production. This telemetry system provides a foundation for future integration with actual CAD software like SolidWorks and Fusion360.

### Job Runner Architecture

The system uses an **in-memory async job runner** that simulates CAD work:

- **In-Memory Storage**: Jobs are stored in a module-level dictionary (`JOBS`) for fast access
- **Async Execution**: Uses `asyncio.create_task()` to run jobs concurrently without blocking the API
- **Progress Simulation**: Jobs progress from 0-100% in realistic increments (0.15s per step)
- **Status Lifecycle**: `queued` → `running` → `succeeded`/`failed`
- **Error Handling**: Comprehensive exception catching with detailed error messages

⚠️ **Important Limitations:**
- **Single Instance Only**: In-memory storage is not suitable for multi-instance production deployments
- **No Persistence**: Jobs are lost on server restart
- **Demo Purpose**: This is a placeholder for real CAD execution steps

### Job Management Endpoints

#### POST /jobs
Start a new async job for CAD processing simulation.

**Request Body (Optional):**
```json
{
  "command_id": 123
}
```

**Example Request:**
```bash
# Start job without command association
curl -X POST "http://localhost:8000/jobs" \
     -H "Content-Type: application/json" \
     -d '{}'

# Start job with command association
curl -X POST "http://localhost:8000/jobs" \
     -H "Content-Type: application/json" \
     -d '{"command_id": 123}'
```

**Response:**
```json
{
  "job_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "queued",
  "progress": 0
}
```

#### GET /jobs/{job_id}
Get the current status of a job by its unique ID.

**Example Request:**
```bash
curl "http://localhost:8000/jobs/550e8400-e29b-41d4-a716-446655440000"
```

**Response (Running Job):**
```json
{
  "job_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "running",
  "progress": 45,
  "error": null,
  "created_at": "2025-08-13T19:00:00.000000",
  "updated_at": "2025-08-13T19:00:15.000000"
}
```

**Response (Completed Job):**
```json
{
  "job_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "succeeded",
  "progress": 100,
  "error": null,
  "created_at": "2025-08-13T19:00:00.000000",
  "updated_at": "2025-08-13T19:00:18.000000"
}
```

**Response (Failed Job):**
```json
{
  "job_id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "failed",
  "progress": 67,
  "error": "Simulation error: Invalid parameter",
  "created_at": "2025-08-13T19:00:00.000000",
  "updated_at": "2025-08-13T19:00:12.000000"
}
```

**404 Response (Job Not Found):**
```json
{
  "detail": "Job with ID '550e8400-e29b-41d4-a716-446655440000' not found. Please check the job ID and try again."
}
```

### Frontend Job Integration

The React frontend automatically integrates job execution with the instruction processing workflow:

#### Automatic Job Triggering
- **After Instruction Processing**: Jobs start automatically after successful `/process_instruction` calls
- **No Manual Intervention**: Users don't need to manually start jobs
- **Seamless UX**: Job progress appears immediately below the instruction form

#### Real-Time Progress Bar
- **Live Updates**: Progress bar updates every 600ms via polling
- **Visual Status**: Color-coded status indicators (blue=running, green=success, red=failed)
- **Progress Percentage**: Shows exact completion percentage (0-100%)
- **Status Text**: Displays current job status with timestamps

#### Polling & Cleanup
- **Automatic Polling**: Frontend polls job status until completion
- **Smart Cleanup**: Intervals are cleared when jobs finish or component unmounts
- **History Refresh**: Command history automatically refreshes after job completion
- **Error Recovery**: Graceful handling of polling errors and job failures

#### UI Components

**Job Status Display:**
```
Job Status
Status: running    45%
████████████░░░░░░░░░░░░░░░░

Job ID: 550e8400-e29b-41d4-a716-446655440000
Started: 8/13/2025, 7:00:00 PM
🔄 Polling for updates...
```

### API Integration Examples

#### Frontend API Helpers
The frontend includes comprehensive job management functions:

```javascript
import { startJob, fetchJob, pollJobUntilComplete } from './api.js'

// Start a job
const job = await startJob(commandId) // commandId optional

// Check job status
const status = await fetchJob(jobId)

// Poll until completion with progress callback
const finalJob = await pollJobUntilComplete(jobId, (job) => {
  console.log(`Progress: ${job.progress}%`)
})
```

#### Backend Job Runner
The backend provides a simple but powerful job management API:

```python
from jobs import start_job, get_job

# Start a job
job_id = start_job(meta={"command_id": 123})

# Check job status
job_status = get_job(job_id)  # Returns dict or None
```

### Future CAD Integration

This telemetry system is designed as a **direct replacement foundation** for real CAD operations:

#### Planned Integration Points
- **SolidWorks API**: Replace simulation with actual SolidWorks macro execution
- **Fusion360 API**: Integrate with Fusion360's Python API for real modeling
- **Progress Tracking**: Map real CAD operation progress to the existing progress system
- **Error Handling**: Capture actual CAD errors and surface them through the existing error system

#### Migration Path
1. **Keep API Contract**: All endpoints (`POST /jobs`, `GET /jobs/{id}`) remain unchanged
2. **Replace Simulation**: Swap `_run_job()` simulation logic with real CAD calls
3. **Maintain Progress**: Update progress based on actual CAD operation stages
4. **Preserve UI**: Frontend progress bars and polling work unchanged

#### Production Considerations
- **Persistent Storage**: Replace in-memory storage with database persistence
- **Multi-Instance**: Add Redis or database-backed job queue for scalability
- **Monitoring**: Add comprehensive logging and monitoring for CAD operations
- **Timeouts**: Implement appropriate timeouts for long-running CAD operations

### Troubleshooting

#### Common Issues

**Job Not Starting**
- Check backend logs for job creation errors
- Verify `/process_instruction` completed successfully
- Ensure backend server is running and accessible

**Progress Bar Not Updating**
- Check browser console for polling errors
- Verify job ID is valid via direct API call
- Check network connectivity to backend

**Jobs Disappearing**
- Jobs are stored in memory and lost on server restart
- This is expected behavior for the demo system
- Production systems will use persistent storage

#### Debugging
Monitor job execution via backend logs:
```
INFO - Starting job for processed instruction...
INFO - Started job c821c37e-89b8-4610-bae4-43423 with meta: {}
INFO - Job c821c37e-89b8-4610-bae4-43423 started running
INFO - Job c821c37e-89b8-4610-bae4-43423 completed successfully
```

## 3D Model Generation (Sprint 7)

The Text-to-CAD system now includes **full 3D geometry generation** powered by **CadQuery**, enabling the creation of actual CAD models from natural language instructions. The system can build simple solids and export them as downloadable STEP and STL files.

### CadQuery Integration

**CadQuery** is a powerful Python library for building parametric 3D CAD models. It provides:

- **Parametric Modeling**: Code-driven CAD with full programmatic control
- **Industry Standards**: Native STEP and STL export capabilities
- **Python Integration**: Seamless integration with the FastAPI backend
- **Robust Geometry**: Reliable solid modeling operations

### Supported Shapes & Operations

The system currently supports these CAD operations:

#### Extruded Cylinder
- **Action**: `"extrude"` with `"shape": "cylinder"`
- **Parameters**: `diameter_mm`, `height_mm`
- **Example**: *"Extrude a 20mm diameter cylinder that is 30mm tall"*

#### Plate with Holes
- **Action**: `"create_hole"`, `"pattern"`, or `"create_feature"`
- **Parameters**: `height_mm` (plate thickness), `diameter_mm` (hole size), `count`, `pattern`
- **Pattern Types**: Circular or linear hole arrangements
- **Example**: *"Create a plate with 6 holes in a circular pattern"*

### Geometry Building API

The geometry package provides clean Python functions for building CAD models:

```python
from geometry import dispatch_build, export_solid

# Build a cylinder from parsed parameters
cylinder_params = {
    "shape": "cylinder",
    "diameter_mm": 25.0,
    "height_mm": 40.0
}
cylinder = dispatch_build("extrude", cylinder_params)

# Build a plate with holes
plate_params = {
    "height_mm": 12.0,
    "diameter_mm": 6.0,
    "count": 6,
    "pattern": {"type": "circular", "count": 6}
}
plate = dispatch_build("create_feature", plate_params)

# Export to downloadable files
step_path = export_solid(cylinder, kind="step", prefix="cylinder")
stl_path = export_solid(plate, kind="stl", prefix="plate")
```

### File Export & Downloads

#### Export Formats
- **STEP Files** (`.step`): Industry-standard CAD format for professional use
- **STL Files** (`.stl`): 3D printing and mesh-based applications

#### File Management
- **Unique Filenames**: Auto-generated with timestamps and UUIDs
  - Format: `model_20250814_010615_a1b2c3d4.step`
- **Automatic Cleanup**: Keeps most recent 75 files, removes older exports
- **Metadata Tracking**: File size, creation time, and format information

#### Download Access
All exported files are served via FastAPI static file mounting:

- **Base URL**: `http://localhost:8000/outputs/`
- **Example URLs**:
  - `http://localhost:8000/outputs/cylinder_20250814_010615_a1b2c3d4.step`
  - `http://localhost:8000/outputs/plate_20250814_010615_a1b2c3d4.stl`

#### File Storage Location
- **Directory**: `backend/outputs/`
- **Auto-Creation**: Directory is created automatically if it doesn't exist
- **Persistence**: Files remain available until cleanup or server restart

### Integration with Existing Workflow

The 3D model generation integrates seamlessly with the existing text-to-CAD pipeline:

1. **Natural Language Input**: *"Create a 25mm diameter cylinder that is 40mm tall"*
2. **Parameter Parsing**: AI or rule-based parsing extracts structured parameters
3. **Geometry Building**: `dispatch_build()` creates CadQuery solid from parameters
4. **File Export**: `export_solid()` generates downloadable STEP/STL files
5. **Download Access**: Files available via `/outputs/` static file endpoint

### Example Usage Workflow

```python
# Complete workflow example
from geometry import dispatch_build, export_solid

# 1. Parse instruction (already implemented in main.py)
parsed_params = {
    "action": "extrude",
    "shape": "cylinder", 
    "diameter_mm": 20.0,
    "height_mm": 30.0
}

# 2. Build 3D geometry
solid = dispatch_build(parsed_params["action"], parsed_params)

# 3. Export to files
step_file = export_solid(solid, kind="step", prefix="my_cylinder")
stl_file = export_solid(solid, kind="stl", prefix="my_cylinder")

# 4. Files are now downloadable at:
# http://localhost:8000/outputs/my_cylinder_20250814_010615_a1b2c3d4.step
# http://localhost:8000/outputs/my_cylinder_20250814_010615_a1b2c3d4.stl
```

### Default Parameters & Fallbacks

The system provides sensible defaults when parameters are missing:

#### Cylinder Defaults
- **Diameter**: 20mm
- **Height**: 30mm

#### Plate with Holes Defaults
- **Plate Size**: 100mm × 80mm
- **Plate Thickness**: 10mm
- **Hole Diameter**: 5mm
- **Hole Count**: 4
- **Pattern**: Circular arrangement

### Future Enhancements

The geometry system is designed for easy extension:

- **Additional Shapes**: Boxes, spheres, complex extrusions
- **Advanced Operations**: Fillets, chamfers, boolean operations
- **Pattern Enhancements**: Grid patterns, custom spacing
- **Assembly Support**: Multi-part CAD assemblies
- **Material Properties**: Density, material specifications for export

## Multi-Operation Support

The Text-to-CAD system now supports **multi-operation instructions**, allowing you to execute multiple CAD commands from a single natural language request. This enables creating complex assemblies with a single instruction.

### Features

- **Multi-Line Instructions**: Separate operations with newlines
- **Single-Line Instructions**: Multiple "create" commands in one line
- **Order-Independent**: Operations execute in the specified order, regardless of shape type
- **Sequential Execution**: Each operation runs one after another
- **Error Tolerance**: Failed operations don't stop subsequent ones
- **Progress Reporting**: Clear status for each operation

### Usage Examples

#### Multi-Line Format
```
create 100mm base plate 5mm thick
create a 20mm cylinder 40mm tall
create 4 holes with 3mm diameter
```

#### Single-Line Format
```
create 100mm base plate 5mm thick create a 20mm cylinder 40mm tall create 4 holes with 3mm diameter
```

Both formats produce the same result: a base plate, cylinder, and holes executed sequentially.

### API Response Format

The backend returns an `operations` array with all parsed operations:

```json
{
  "schema_version": "1.0",
  "instruction": "create 100mm base plate 5mm thick create a 20mm cylinder 40mm tall",
  "source": "rule",
  "plan": [
    "Create base plate 100.0 mm × 5.0 mm thick",
    "Create cylinder Ø20.0 mm × 40.0 mm height"
  ],
  "parsed_parameters": {
    "action": "create_feature",
    "parameters": {
      "shape": "base_plate",
      "diameter_mm": 100.0,
      "height_mm": 5.0
    }
  },
  "operations": [
    {
      "action": "create_feature",
      "parameters": {
        "shape": "base_plate",
        "diameter_mm": 100.0,
        "height_mm": 5.0
      }
    },
    {
      "action": "create_feature",
      "parameters": {
        "shape": "cylinder",
        "diameter_mm": 20.0,
        "height_mm": 40.0
      }
    }
  ]
}
```

### SolidWorks Add-In Integration

The SolidWorks add-in automatically detects and executes multi-operation instructions:

1. **Detection**: Checks if `operations` array has multiple items
2. **Sequential Execution**: Loops through each operation in order
3. **Progress Reporting**: Shows status for each operation
4. **Completion Summary**: Reports how many operations succeeded

### Backward Compatibility

Single-operation instructions continue to work exactly as before:
- `parsed_parameters` field maintained for compatibility
- Single-operation code path unchanged
- All existing functionality preserved

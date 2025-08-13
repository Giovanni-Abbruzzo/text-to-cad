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

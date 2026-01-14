# Project Overview — Text‑to‑CAD

## Goal
Turn natural language into **parametric CAD** (STL/STEP) through a chat interface. Provide a plugin‑style UX reminiscent of Copilot/Windsurf inside CAD tools.

## MVP (interview demo)
1. User enters: *"Create a 50x30x10 mm block with a 8 mm center hole"*.
2. System parses to a tiny DSL.
3. Generator outputs a valid STL (and later STEP).
4. User downloads file; UI shows parameters + trace.

## Non‑Goals (MVP)
- Full constraint solver
- Complex feature tree editing
- Full SolidWorks/Fusion SDK integration (mocked first)

## Constraints / Defaults
- Units default to mm; support in, cm.
- Shapes: `cube` (w,h,d) and `cylinder` (r, h).
- Deterministic parse; safe fallbacks.

## Repo Tour
- `frontend/` — React chat UI
- `backend/` — FastAPI + parsing + simple STL generator
- `ai_model/` — prompt & DSL notes
- `docs/` — architecture, roadmap, integration notes
- `docker/` — container builds; `docker-compose.yml`

## Sprints
Sprint 1 — Backend MVP (FastAPI)
Goal: A working API with core endpoints and simple parsing.

Endpoint GET /health -> returns {"status":"ok"}

Endpoint POST /process_instruction -> accepts {"instruction": string, "use_ai": boolean}; returns
{schema_version, source, plan, parsed_parameters, operations}

Endpoint POST /dry_run -> accepts the same request body; returns the same response shape without DB writes

Naive parsing: detect keywords like “hole/extrude/fillet” and numbers

DoD / Acceptance Tests:

uvicorn main:app --reload runs with no errors

http://localhost:8000/docs shows /process_instruction and /dry_run

POST /process_instruction returns schema_version, source, plan, parsed_parameters, and operations


Sprint 2 — Frontend MVP (React, Vite)
Goal: Minimal UI that talks to the backend.

Input for natural language instruction

“Send” button → POST to /process_instruction

Show JSON response

.env with VITE_API_BASE=http://localhost:8000

DoD / Acceptance Tests:

npm run dev runs at http://localhost:5173

Sending text shows backend response


Sprint 3 — Persistence & History (SQLite + SQLAlchemy)
Goal: Save commands and show history.

Add SQLAlchemy model Command(id, prompt, action, parameters, created_at)

Save each process_instruction call

New endpoint GET /commands → list recent commands

Frontend “History” section (simple list)

DoD / Acceptance Tests:

After several sends, /commands returns multiple items in reverse chronological order

Frontend displays a history list



Sprint 4 — AI/LLM Integration (OpenAI or local)
Goal: Natural language → structured JSON via LLM (for better parsing).

In /process_instruction, add optional LLM call:

Prompt the model to return strict JSON: { "action": "<create_hole|extrude|...>", "parameters": { ... } }
The API response shape remains {schema_version, source, plan, parsed_parameters, operations}

Fallback to naive parsing if API key is missing

(Optional) Add a tiny “Use AI” toggle in frontend

DoD / Acceptance Tests:

With an API key, the response is structured and more accurate than naive rules

No crash when AI is off (fallback works)



Sprint 5 — “Feels Alive” Telemetry (Simulated)
Goal: Simulate job progress on backend; show progress bar on frontend.

Backend: create a /jobs mini-simulator

POST /jobs → start a fake job (id)

GET /jobs/{id} → returns {status, progress} (0→100)

Simple in-memory dict or short-lived SQLite row + background task

Frontend: after sending command, start a job and poll every 2–3s; render progress bar

DoD / Acceptance Tests:

Submitting a command starts a “job” that progresses to 100%

UI shows live progress



Sprint 6 — UX Polish + Demo Script
Goal: Make it clean and confidently demo-able.

Clean spacing, headings, simple styles

Add a top-level “What this does” in README

Add a short demo flow in README

Prepare a 30–45s “demo script” (what you’ll say)

DoD / Acceptance Tests:

Someone can clone the repo, follow README, and reproduce your demo locally

You can run the demo flow without thinking




Optional Sprint 7 — Docker (local) & GCP (Cloud Run)
Goal: Containerize backend and (optionally) deploy.

Add backend/Dockerfile and docker-compose.yml

Verify docker compose up --build works locally

Deploy backend image to Cloud Run (keep frontend local or deploy to Netlify)

Update frontend .env to point at Cloud Run URL

DoD / Acceptance Tests:

Backend runs in Docker locally

Cloud Run URL responds to /health and /process_instruction

# Text-to-CAD - Full Project Guide (NEW)

## 1. New Device Setup and Getting Started

### 1.1 Prerequisites
- Windows 10/11
- SolidWorks (2022+ recommended) with SolidWorks API interop assemblies installed
- Visual Studio 2022 (or 2019) with .NET Framework 4.7.2 Developer Pack
- Python 3.11+ (3.10+ is usually fine) and pip
- Node.js 18+ and npm
- Git
- Optional: OpenAI API key for AI parsing
- Optional: CMake + C++ build tools for whisper.cpp (voice input)
- Optional: CadQuery/OCP for /generate_model

These prerequisites exist because the project spans three different runtimes. The add-in must run inside SolidWorks via COM, so it depends on the .NET Framework and SolidWorks PIAs; the backend and frontend are independent web services, so they use Python and Node. The optional tools only unlock advanced features (AI parsing, local speech-to-text, and CadQuery exports), but the core rule-based parsing and SolidWorks execution path works without them.

### 1.2 Clone the repo
```powershell
git clone <your-repo-url>
cd text-to-cad
```

This step puts the full repo on disk and preserves git history, which is important because many features were added incrementally across sprints. Keeping the history makes it easier to audit behavior changes, understand why guardrails were introduced, and bisect issues if a future change causes regressions.

### 1.3 Backend setup (FastAPI)
```powershell
cd backend
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt

# Optional: enable AI parsing
$env:OPENAI_API_KEY="<your_key>"
# Optional overrides
$env:OPENAI_MODEL="gpt-4o-mini"
$env:OPENAI_TIMEOUT_S="20"

python -m uvicorn main:app --reload
```

Run backend tests:
```powershell
cd backend
.venv\Scripts\Activate.ps1
pytest -q
```

The backend uses FastAPI with Pydantic validation, so running the server with `--reload` gives rapid feedback while iterating on parsers and planner logic. The tests focus on the contract surface (health, dry_run, process_instruction, planner loop) to keep the stack lightweight and free of external services, which is critical for local-first development and repeatable QA.

### 1.4 Frontend setup (React)
```powershell
cd frontend
npm install
# Create frontend .env so the UI points to the backend root (NOT /docs)
echo VITE_API_BASE=http://localhost:8000 > .env
npm run dev
```

The frontend is intentionally minimal and optimized for fast iteration. Vite provides quick startup and hot reload, while the React UI mirrors the add-in workflow so you can validate parsing, planning, and history behaviors without launching SolidWorks.

### 1.5 SolidWorks Add-in setup
1) Open `solidworks-addin/TextToCad.SolidWorksAddin.csproj` in Visual Studio.
2) Restore NuGet packages.
3) Confirm SolidWorks PIA reference paths under `Reference` entries:
   - `SolidWorks.Interop.sldworks.dll`
   - `SolidWorks.Interop.swconst.dll`
   - `SolidWorks.Interop.swpublished.dll`
4) Build x64 (Debug or Release). The project targets .NET Framework 4.7.2.
5) Register the add-in:
```powershell
cd solidworks-addin
.\register_addin_debug.bat
# or
.\register_addin.ps1
```
6) Launch SolidWorks -> Tools -> Add-ins -> enable Text-to-CAD.

These steps are required because SolidWorks loads add-ins via COM registration, not via a standalone executable. The project targets .NET Framework 4.7.2 for maximum compatibility with SolidWorks API interop libraries, and building x64 ensures the add-in matches the SolidWorks process architecture.

### 1.6 Optional: Whisper voice input (local)
1) Build whisper.cpp (you already cloned it):
```powershell
cd ..\whisper.cpp
cmake -S . -B build -A x64
cmake --build build --config Release
```
2) Download a model (example):
```powershell
cmd /c models\download-ggml-model.cmd base.en
```
3) Update `solidworks-addin/app.config`:
```xml
<add key="UseWhisper" value="true" />
<add key="WhisperCliPath" value="C:\WindsurfProjects\Engen\whisper.cpp\build\bin\Release\whisper-cli.exe" />
<add key="WhisperModelPath" value="C:\WindsurfProjects\Engen\whisper.cpp\ggml-base.en.bin" />
<add key="WhisperLanguage" value="en" />
```
4) Build the add-in so Visual Studio copies `app.config` to `bin\Debug\TextToCad.SolidWorksAddin.dll.config`.

Whisper is optional but valuable for voice-driven workflows, and the add-in reads its paths directly from `app.config`. The build step is necessary because the runtime config is the copied DLL config, so keeping those in sync avoids silent misconfiguration.

### 1.7 Optional: CadQuery / OCP for /generate_model
If you need the backend to generate STEP/STL via CadQuery:
```powershell
cd backend
.venv\Scripts\Activate.ps1
pip install cadquery
```
If OCP is missing, the backend will warn and `/generate_model` will be disabled.

CadQuery is only required if you want server-side exports (STEP/STL) without SolidWorks. The primary geometry path still runs through SolidWorks, so this dependency is deliberately optional to keep setup fast for add-in-centric workflows.

### 1.8 Example usage and expected outputs
Backend example:
```powershell
curl -X POST http://localhost:8000/process_instruction \
  -H "Content-Type: application/json" \
  -d "{\"instruction\":\"create base plate 120x80x6 mm and add 4 holes diameter 6mm in a circular pattern\",\"use_ai\":false}"
```
Expected response shape (abridged):
```json
{
  "schema_version": "1.0",
  "instruction": "create base plate 120x80x6 mm and add 4 holes diameter 6mm in a circular pattern",
  "source": "rule",
  "plan": [
    "Create base plate 120.0 x 80.0 x 6.0 mm",
    "Create 4 holes diameter 6.0 mm",
    "Arrange in circular pattern (4 instances)"
  ],
  "parsed_parameters": {
    "action": "create_feature",
    "parameters": {
      "shape": "base_plate",
      "length_mm": 120.0,
      "width_mm": 80.0,
      "height_mm": 6.0
    }
  },
  "operations": [
    { "action": "create_feature", "parameters": { "shape": "base_plate", "length_mm": 120.0, "width_mm": 80.0, "height_mm": 6.0 } },
    { "action": "create_hole", "parameters": { "count": 4, "diameter_mm": 6.0, "pattern": { "type": "circular", "count": 4 } } }
  ]
}
```

Add-in examples (expected geometry):
- "create base plate 120x80x6 mm" -> rectangular plate on Top Plane.
- "add 4 holes diameter 6mm in a circular pattern, bolt circle diameter 40mm" -> through-all cut pattern on the plate.
- "fillet 1 mm on all edges" -> fillets applied to all sharp edges of the most recent body.

Planner example:
- Instruction: "make a mountain bike"
- Expected: a plan list + questions (frame size, wheel diameter, handlebar width). After answering, a 5-7 step placeholder bike model is created with spaced parts.

These examples are the fastest way to validate the end-to-end contract. They exercise parsing, plan generation, and multi-operation execution, and they mirror the most common user workflows inside the SolidWorks TaskPane. If any of these outputs deviate from expectations, it usually indicates a schema mismatch or a builder routing issue rather than a UI problem.

Taken together, Section 1 is a complete onboarding sequence that yields a working local stack with optional AI and voice features. It is designed so that a new developer can get deterministic rule-based behavior first, then incrementally turn on AI parsing, replay, and voice input as confidence grows.


## 2. System Architecture (and why it is built this way)

### 2.1 High-level flow
User -> SolidWorks TaskPane -> FastAPI backend -> Parsed operations -> SolidWorks builders -> Replay log

The architecture is intentionally split so that:
- Geometry creation always happens inside SolidWorks (COM API requirement).
- Parsing/planning is backend-driven so it can evolve (AI + rules) without breaking the add-in.
- The frontend can be used for fast iteration, debugging, and history review without SolidWorks running.

This high-level flow captures the minimum viable data path from text to geometry. It is intentionally short so you can sanity-check the pipeline quickly, but the real system has multiple validation, normalization, logging, and replay layers that make it robust and debuggable. At this level you can treat the backend as a deterministic transformer from instruction text to a stable operations list, and the add-in as the authoritative executor inside SolidWorks. The plan-and-operations boundary is the contract; it is versioned and designed to stay stable so both the backend and add-in can evolve without breaking each other. This overview intentionally compresses auxiliary loops (planner Q/A, replay logging, and undo tracking) so the primary control path is obvious before diving into the detailed flow below.

It is also important to note that this flow has two primary entry modes: `/process_instruction` for execution and `/dry_run` for preview. Both follow the same parse-and-plan pipeline, but only the execution path writes to the database and triggers geometry creation. That split is central to safe usage: the preview lets you see exactly what will happen before SolidWorks changes, while the execution path commits changes and logs replay data for reproducibility.

### 2.1.1 Extremely DetailedFlow
Below is the full end-to-end flow, including validation, fallback logic, execution routing, and logging:
- User enters text in the TaskPane (or frontend) and selects AI mode or rule-based mode.
- The client builds a request payload with `instruction` and `use_ai` and calls `/process_instruction` or `/dry_run`.
- FastAPI validates the request body with Pydantic; too-short instructions return HTTP 422.
- The backend splits the instruction into multiple operations (newline or multi-command parsing).
- Each operation runs through `parse_instruction_internal`, which attempts AI parsing if enabled and falls back to rule parsing when AI fails or returns empty parameters.
- Parsed results are normalized into a consistent schema (stable keys, nulls for missing values, canonical action names).
- A human-readable plan list is generated from the normalized parameters for UI display.
- `/process_instruction` persists each operation to SQLite; `/dry_run` does not write to the database.
- The response returns `schema_version`, `source`, `plan`, `parsed_parameters`, and `operations` for deterministic client execution.
- The add-in receives the response, renders the plan and step list, and logs results to the TaskPane UI.
- On Execute/Run Next, `ExecuteController` routes each operation to the correct builder based on action and shape.
- Builders select planes/faces, create sketches, and extrude or cut features using SolidWorks API calls, converting mm to meters as needed.
- Each operation is wrapped in an UndoScope and logged with success/failure details.
- ReplayLogger appends a JSONL entry (session id, sequence, inputs, outputs) for later replay.
- The UI updates status, and Undo removes the last feature by name when requested.

This detailed flow exists to guarantee repeatability and debuggability. Every layer (validation, normalization, routing, logging) is explicit so that failures can be traced to a specific step rather than being opaque SolidWorks errors. The sequence highlights where data is persisted (SQLite history and planner state), where schema normalization is enforced, and where units are converted for SolidWorks (mm to meters). It also makes clear which steps are side-effecting (database writes, geometry creation, replay log append) versus purely informational (plan generation, dry-run parsing). That distinction matters because it is how the system ensures previews are safe and executions are auditable.

The flow also exposes the error-handling boundaries: invalid input fails fast in FastAPI with 422 responses, AI parsing failures are caught and downgraded to rule parsing, and SolidWorks feature failures are logged with explicit error messages and optional rollbacks. This tiered error handling is a deliberate design choice to keep the system robust under real-world usage where input quality and CAD constraints vary widely.

### 2.1.2 Extremely Detailed Why this
The system is split into backend parsing and in-process CAD execution because SolidWorks COM APIs must run inside the SolidWorks process and cannot be safely driven from a remote service. The backend uses FastAPI because it provides automatic schema validation, JSON serialization, and interactive docs while staying lightweight for local-first development. Parsing is centralized in Python to enable rapid iteration on language rules and AI prompts without rebuilding the add-in, and the fallback strategy ensures deterministic behavior when AI fails or is unavailable. Pydantic models enforce input constraints early, which prevents invalid instructions from ever reaching the SolidWorks API layer. SQLite is used for history and planner state because it is file-based, zero-config, and ideal for local tools; it also makes it easy to inspect, snapshot, or reset state during QA without introducing a separate database service.

Python is preferred here because its ecosystem makes natural language parsing and prompt engineering fast to iterate on, and because it pairs naturally with FastAPI and SQLAlchemy. The design also avoids heavy message buses or service meshes, since the system is intended to run locally on a single workstation; adding those layers would introduce complexity without solving the core CAD constraints.

The add-in is built with .NET Framework and WinForms because SolidWorks TaskPanes natively support WinForms controls and COM interop. This is why React or WPF are not used inside SolidWorks, even though they might be preferable for a standalone UI; running a browser engine or a modern UI stack inside SolidWorks introduces stability and deployment risk. JSON is the interchange format because it maps cleanly to both Python and C#, preserves optional fields, and can be recorded as JSONL replay logs for deterministic runs. The frontend is a separate React app so that planning and parsing can be tested without launching SolidWorks, which reduces iteration time and lowers the barrier for non-CAD contributors to verify behavior. This split also allows multiple clients (TaskPane, web UI, tests) to exercise the same backend contract without coupling execution logic to a single UI surface.

JSON was chosen instead of gRPC or binary protocols because it is human-readable, easy to debug, and consistent with the OpenAPI documentation that FastAPI produces. That readability is crucial when QA involves inspecting parsed parameters and plan steps, which is a frequent task in this project.

This design trades some duplication (frontend vs add-in UI) for much higher reliability and developer velocity. The duplication is intentional: it allows the same parsing and planning logic to be validated in a controlled environment before CAD execution, which reduces the likelihood of geometry-breaking regressions. It also creates a clean seam where future planners, toolchains, or custom LLMs can be swapped in without destabilizing SolidWorks execution, because the only integration point is the stable operations schema. This is the core architectural principle of the project: keep the CAD executor strict and predictable, and keep the language/planning system flexible and rapidly iterated.

### 2.1.3 Extremely Detailed Improvments
Potential improvements fall into three categories: schema governance, execution resilience, and observability. For schema governance, a shared versioned schema package (generated from a single source) would eliminate drift between backend and add-in and allow strict compatibility checks. For execution resilience, a preflight geometry validator could estimate whether operations are feasible (fillet radii, hole positions) before SolidWorks attempts the feature, reducing rollbacks. For observability, structured logs and replay metadata could include feature tree ids, rebuild status, and timing information to support automated regression analysis.

Additional improvements include introducing a background job queue for long-running operations, a builder registry to enable plug-in feature modules, and a more explicit design-state model that tracks created parts, references, and mates. A geometry proxy service could be added to compute rough bounding boxes or collision checks without committing features to SolidWorks, enabling a "preview" mode for complex assemblies. These upgrades would not change the basic flow, but they would make the system more scalable and reliable for larger models and multi-part assemblies. Concretely, a shared schema package could be generated from OpenAPI and consumed by both Python and C# to enforce strict version compatibility; a builder registry could use reflection or an explicit map to discover new builders without touching the router; and a preflight validator could run geometry heuristics (min radii, hole-to-edge distances, sketch closure checks) before the COM feature call is made. Each of these improvements targets a known CAD pain point: avoiding hard-to-debug failures inside the SolidWorks feature engine and making large operation sequences easier to audit.

From an implementation standpoint, these improvements are incremental rather than disruptive. The schema package can be introduced alongside existing DTOs, the validator can run as a pre-step in ExecuteController, and the registry can wrap the existing switch-based routing. That makes the upgrade path practical while preserving backward compatibility for existing operation payloads.

### 2.2 Components

Backend (FastAPI)
- Parses natural language with AI or deterministic rules.
- Produces a stable JSON schema (schema_version=1.0) with plan steps.
- Stores command history and planner state in SQLite (`backend/app.db`).
- Exposes endpoints for parsing, dry runs, planner Q/A loop, replay ingestion, and jobs.

The backend is the system's language and planning brain. It is intentionally decoupled from SolidWorks so that parsing behavior can change rapidly without requiring add-in recompilation, and so that planners can accumulate state across multiple user interactions. This layer owns normalization and schema guarantees: every instruction, whether AI or rule-derived, must be converted into the same `action` + `parameters` structure before anything can execute. It also defines the planner loop (questions, answers, and state_id), stores command history for auditing, and exposes an explicit API surface that can be consumed by the add-in, the web frontend, or test scripts. In practice, this means all feature behavior is traceable in Python, and SolidWorks only sees fully structured operations rather than raw language, which dramatically reduces uncertainty at execution time.

Internally, this component is responsible for input validation, multi-operation splitting, and plan text generation. It also centralizes optional AI configuration (model, timeout, prompt overrides) so that switching between AI and rule parsing does not require any client-side changes. The backend’s persistence layer provides long-lived planner state and history logs that are essential for replay, auditing, and iterative workflows, and the `/generate_model` path (when CadQuery is installed) provides a server-side geometry export alternative that does not require SolidWorks to be running.

From a code-structure perspective, the backend concentrates everything that turns ambiguous language into a deterministic operation list. The API layer in `backend/main.py` owns request validation, error responses, and schema_version enforcement, while helper modules (`backend/llm.py`, `backend/db.py`, `backend/models.py`, and `backend/geometry/`) provide narrower responsibilities. This organization keeps pure parsing logic isolated from persistence and export logic, which makes it easier to test each piece independently and swap out dependencies (for example, an alternative LLM client or a different storage backend) without rewriting the endpoints. It also ensures that every response passes through a single normalization funnel, so downstream clients never need to handle special cases.

Another practical consequence of this design is clear ownership of error semantics and endpoint behavior. The backend enforces consistent responses for `/process_instruction`, `/dry_run`, and planner endpoints, including schema_version, source, parsed_parameters, and plan text, which means client code can be simple and predictable. It also centralizes history persistence and replay ingestion, so you can trace every operation sequence back to the instruction that generated it. That traceability is what makes QA and regression testing possible without re-running SolidWorks for every change.

Frontend (React)
- Simple control surface for /process_instruction, /dry_run, and /plan.
- Displays plan, parsed parameters, operations, and recent history.
- Polls lightweight background jobs for UI feedback.

The frontend is a developer-facing surface that mirrors the add-in workflow. It makes it easy to test parsing and planning in isolation, which is essential when iterating on language rules or debugging AI parsing outputs. Because it speaks the same API contract as the add-in, it can surface schema mismatches early and provide a quick visual check of plan/operation ordering without launching SolidWorks. It also provides a safe place to prototype planner interactions, answer formatting, and history views before those UX patterns are committed to the TaskPane. In other words, it is a sandbox that reduces the cost of experimentation while keeping the core contract unchanged.

The frontend’s job polling and history views also provide a sanity check for backend persistence and job status endpoints. By keeping API calls in `frontend/src/api.js` and UI state in `App.jsx`, it becomes straightforward to see exactly how responses are interpreted and rendered, which is useful when debugging changes to response schemas or planner outputs. This keeps the UI layer simple, predictable, and tightly aligned with the backend contract.

From a dependency standpoint, the frontend relies only on the backend HTTP API and environment configuration (for example, `VITE_API_BASE`). It does not embed any CAD logic and it does not call SolidWorks, which is intentional: the UI is a contract inspector and a QA surface, not an execution engine. This makes the frontend resilient to add-in changes and lets backend developers iterate on parsing or planner behavior with a fast feedback loop. If a response shape changes, the frontend is where it becomes visible immediately, which is why the UI intentionally renders raw parsed parameters and plan strings instead of hiding them behind abstractions.

Because it is a lightweight diagnostics client, the frontend acts as an early warning system for schema drift. It shows operations arrays, plan steps, and error messages exactly as returned by the backend, which makes discrepancies easy to spot before they reach CAD execution. That visibility matters in a project where schema stability is a core requirement: the frontend allows you to validate that rule-based and AI-based parsing produce equivalent outputs and that planner answers are correctly translated into executable operations without needing to open SolidWorks.

SolidWorks Add-in (C# .NET Framework)
- Hosts a TaskPane UI to submit commands, view plans, run step-by-step, and undo.
- Executes operations using SolidWorks API builders (base plate, holes, cylinder, fillet, chamfer).
- Logs to UI and file; records replay JSONL for reproducibility.
- Optional voice input via System.Speech or Whisper CLI.

The add-in is the execution layer that turns JSON operations into real features in the SolidWorks feature tree. It must be tightly integrated with the SolidWorks API and is therefore implemented in C# with COM interop and WinForms. This layer owns all geometry creation, selection, unit conversion, undo management, and feature logging, because those are tasks that only SolidWorks can reliably perform. It also implements step-by-step execution and replay controls, which means it needs direct access to feature names and rebuild behavior. By keeping execution in-process and heavily logged, the add-in can provide precise error messages (for example, fillet failures or selection issues) and allow the user to recover via undo or replay without leaving SolidWorks.

The add-in also manages UI state that is tightly coupled to execution, such as which steps have been run, which can be undone, and whether a replay session is active. It relies on configuration in `app.config` to determine API base URLs, replay logging options, and voice input settings, which means operational behavior can be changed without recompilation. Finally, because SolidWorks expects operations to be unit-consistent and selection-driven, the add-in’s utility classes enforce mm-to-meter conversions and robust face selection, preventing subtle errors that would otherwise appear as failed feature inserts.

At a code dependency level, the add-in sits on top of SolidWorks PIAs and COM interop, which makes it the only layer that can legally and safely create geometry. `TaskPaneControl.cs` drives user interaction and calls into `ApiClient` for backend requests, while `ExecuteController` and the builders transform parsed operations into `FeatureManager` calls. Utility classes (`Selection`, `Units`, `UndoScope`, `Logger`) are shared across builders to enforce consistent selection, unit conversions, and rollback behavior. This layered structure keeps UI concerns separate from feature creation, but still allows the add-in to coordinate step execution, replay logging, and undo in one place.

The add-in is also the arbiter of side effects. It decides when to rebuild, when to log, and when to abort execution if parameters are invalid. This is why its log output is so verbose: the COM API can fail silently without detailed context, so the add-in captures the exact parameters and feature names that were attempted. Downstream, replay and undo rely on those recorded feature names to reproduce or reverse operations, so consistent naming and logging here is a prerequisite for reliable QA and reproducibility.

Overall, Section 2.2 explains how each component is deliberately scoped to its strengths: FastAPI for parsing and planning, React for lightweight inspection and QA, and the SolidWorks add-in for authoritative geometry creation. The interfaces between these components are intentionally narrow and JSON-based so you can reason about each layer independently. This separation is what makes the system testable and maintainable: language logic can evolve without touching COM code, and CAD execution can be hardened without destabilizing parsing behavior. It also makes future expansions (planner memory, part libraries, assembly workflows) easier because they can be attached to the backend or add-in without rewriting the entire stack.

From an operational perspective, this split also supports incremental adoption. A team can run the backend and frontend alone to validate parsing, then introduce the add-in for execution once the contract is stable. That staged workflow is deliberate: it helps prevent CAD-side debugging from masking parsing issues and ensures each layer is solid before adding the next one.

This separation also supports multiple user personas. Backend developers can focus on language understanding and planner behavior with unit tests and API calls, while CAD specialists can focus on builder correctness and SolidWorks-specific constraints without needing to touch parsing logic. Because the contract is explicit JSON, you can record and replay operations across layers, which simplifies debugging and makes collaborative workflows possible. In short, the architecture allows each discipline (AI, web, CAD automation) to evolve with minimal interference, while still producing a single coherent execution pipeline.

### 2.3 Why this architecture (tradeoffs and rationale)
- SolidWorks COM APIs must run in-process, so builders are inside the add-in.
- The backend is stateless for parsing but still stores history and planner state to allow long-running Q/A loops.
- A rule-based parser provides deterministic fallback even when AI is unavailable or unreliable.
- JSON operations keep the contract explicit and allow replay, testing, and future automation.

This structure intentionally avoids networked geometry execution, which would be brittle and slower given SolidWorks COM constraints and the requirement to run inside the SolidWorks process. It also acknowledges that AI parsing is inherently probabilistic, so it is paired with deterministic rule parsing and a stable schema to keep execution predictable and safe. The tradeoff is that there are two UI surfaces (TaskPane and web) and a strict contract boundary that must be maintained, but the benefit is that failures are isolated: parsing bugs do not corrupt geometry, and geometry failures do not break the backend service. This separation also keeps the system usable without internet access, which matters for local-first and on-prem environments where CAD tools are often deployed.

Alternative designs were intentionally avoided. Running SolidWorks via remote automation would introduce latency, licensing complications, and brittle session management, while embedding AI directly inside the add-in would make prompt iteration and model changes much harder. The chosen architecture keeps the highest-risk component (the CAD executor) small and deterministic, while allowing the most variable component (language understanding) to evolve independently.

There are real tradeoffs in this split. It requires a local backend process to be running whenever you want AI or rule parsing, and it introduces network boundaries between the UI and the executor. It also means the system has to maintain a stable schema contract and keep two UIs aligned (TaskPane and frontend). These costs are accepted because they buy clarity and testability: the backend can be validated with automated tests and replay logs, and the add-in can focus purely on geometry correctness without maintaining a parallel parsing stack.

Another tradeoff is that the backend is not just stateless parsing: it stores planner state and history, which adds persistence concerns. That persistence is deliberate because it enables multi-step planning and replay, but it also means the system must manage migrations and schema evolution in SQLite. The architecture handles this by keeping the schema minimal and by treating the database as local, disposable state rather than a shared service, which is appropriate for a local-first CAD tool.

### 2.4 Architectural improvements (future refactors that would help)
- Share a single schema package between backend and add-in to eliminate drift.
- Add a versioned operation registry so new builders can be added without touching the router.
- Use a background task queue (RQ/Celery) for heavy operations and richer job progress.
- Introduce a geometry validation layer (preflight checks for impossible features) before execution.
- Replace ad-hoc plan strings with typed plan objects for better UI/UX control.

These improvements aim to make the system more scalable and safer as complexity grows. Schema versioning and typed plans reduce integration risk by making contract drift explicit and testable. Preflight validation and background execution reduce run-time failures and UI blocking by catching impossible operations before they hit the SolidWorks feature engine and by moving long-running work off the UI thread. A builder registry and shared schema package would reduce boilerplate and allow new features to be added without touching core routing logic, which keeps the system extensible while preserving deterministic behavior.

The improvement list is intentionally pragmatic: each item is a direct response to failure modes observed in CAD automation (silent feature failures, ambiguous schema, and hard-to-reproduce bugs). Implementing them incrementally would provide measurable gains in stability without disrupting the existing workflow, and each enhancement can be introduced behind configuration flags to keep the system backwards compatible.

More concretely, a shared schema package could be generated from the FastAPI OpenAPI spec and consumed by C# via a generated client or a hand-written DTO set, eliminating drift in field names like `pattern_radius_mm` or `extrude_midplane`. A versioned operation registry could replace the current switch routing in `ExecuteController` with a dictionary keyed by action+shape, making new builders discoverable without touching core logic. Typed plan objects would allow the UI to render richer plan metadata (inputs, dependencies, warnings) instead of plain strings, which would improve step-by-step UX and allow better error grouping. Preflight validators could live beside builders and perform cheap geometry checks (e.g., min edge length vs fillet radius) before attempting a COM feature, reducing hard failures and improving user feedback.

Taken together, Section 2 provides both the conceptual map and the engineering rationale for the architecture, and it outlines a clear path to scale from single-part demos to robust multi-part workflows. The key theme is separation of concerns: language understanding and planning live in the backend, while geometry execution and CAD-specific constraints live in the add-in. That separation is what makes the system reliable under real-world CAD constraints and what allows future features like assemblies, mates, and part libraries to be added without rewriting the core pipeline.

This section is deliberately redundant because it is the foundation for every other part of the system. If you internalize only one thing, it should be that the contract boundary (JSON operations) is the critical seam. Everything else exists to keep that seam stable: the backend normalizes language into operations, the add-in executes operations with strict selection and unit handling, and the UI layers expose the plan so humans can validate it. The architectural choices are therefore less about technology fashion and more about risk management: they minimize the number of places where ambiguous language can leak into geometry creation, and they make it straightforward to audit and improve the system over time.


## 3. Codebase Review (features and sprints, with file references)

### 3.1 Backend (FastAPI)
- API contract + parsing: `backend/main.py`
  - `/process_instruction` and `/dry_run` return schema_version, source, plan, parsed_parameters, and operations.
  - `parse_instruction_internal` centralizes AI + rule parsing with normalization.
- AI parsing: `backend/llm.py`
  - OpenAI client, structured JSON prompt, and robust JSON parsing with fallback.
- Rule-based parsing: `backend/main.py` (`parse_cad_instruction` and normalization helpers)
  - Regex + keyword parsing for cylinders, base plates, hole patterns, fillets, chamfers.
- Planner (Planner-1): `backend/main.py`, `backend/models.py`
  - `/plan` creates a persistent `design_states` record and returns questions + plan.
  - `/plan/{state_id}` resumes and returns operations after answering questions.
- History and persistence: `backend/db.py`, `backend/models.py`
  - SQLite database stores command history and planner state.
- Jobs: `backend/jobs.py`
  - Lightweight job tracking (polled by frontend UI).
- CadQuery output (optional): `backend/geometry/model_builder.py`
  - Map parsed parameters to CadQuery solids for export.
- Tests: `backend/tests/test_api.py`
  - Health, dry_run, process_instruction, validation, and planner flows.
- Configuration: `backend/config.py`
  - Environment-driven settings with safe defaults for local-first workflows.
- Export pipeline: `backend/geometry/exporter.py`
  - STEP/STL export helpers and output file management for `/generate_model`.

FastAPI was chosen over heavier frameworks because it gives automatic request validation, OpenAPI docs, and strong typing via Pydantic while staying lightweight for local dev. SQLAlchemy with SQLite keeps persistence simple and portable, which matches the local-first requirement and avoids external services. The rule-based parser exists to guarantee deterministic behavior; AI parsing is layered on top for convenience but is never required for correctness. CadQuery is used for optional exports because it wraps OpenCascade with a concise Python API, which is far less verbose than direct OCC BREP manipulation for basic solids. The backend also centralizes multi-operation splitting, plan generation, and error handling so that both the add-in and frontend never have to interpret raw text themselves. This design keeps text parsing, parameter normalization, and planner logic in one place, which is essential for maintainability as the supported operation set expands.

Within this backend layer, the most critical behaviors are schema normalization and validation. Pydantic models ensure that missing or malformed inputs fail fast, while normalization ensures that fields like `diameter_mm`, `height_mm`, and `pattern` are always present in predictable form. AI parsing is intentionally bounded: the system prompt enforces JSON-only responses, and any invalid or empty AI outputs are immediately discarded in favor of rule parsing. This guarantees that even if AI is unreliable, the backend still emits consistent data that the add-in can execute.

The test suite in `backend/tests/test_api.py` anchors these guarantees by exercising contract-level behaviors, including validation errors and planner responses. These tests are intentionally lightweight so they can run in any local environment, which reinforces the project's local-first goal.

From a dependency perspective, `backend/main.py` is the orchestrator: it pulls configuration from `backend/config.py`, uses Pydantic models for request/response shapes, and delegates AI parsing to `backend/llm.py` while persisting history through `backend/db.py` and `backend/models.py`. Planner endpoints add a second layer of state: they write to `design_states`, accept Q/A answers, and then call the same parsing pipeline to build operations. This means planner and non-planner flows are intentionally unified; there is no separate execution model. Downstream, every client (add-in, frontend, tests) depends on these endpoints to return schema-stable data, so changes here ripple through the entire system and must be made with contract compatibility in mind.

### 3.2 Frontend (React)
- UI + API wiring: `frontend/src/App.jsx` and `frontend/src/api.js`
  - Instruction input, AI toggle, planner Q/A loop, job polling, command history.
  - Uses `VITE_API_BASE` to point at backend root.

React with Vite was selected because it provides fast hot-reload and a low-friction developer experience without imposing heavy architectural constraints. React is used instead of vanilla JS to keep state transitions (planner answers, job polling, history) explicit and testable, and Vite replaces heavier bundlers to reduce setup time. The frontend is intentionally thin: it is a debugging and validation surface rather than a production UI, which keeps the focus on correctness of parsing and planning logic. The UI mirrors key TaskPane behaviors (plan preview, answer submission, history display) so you can validate backend changes without introducing the SolidWorks runtime into the loop. This decision reduces turnaround time for parser and planner changes and makes it easier to spot schema drift before it reaches COM execution.

The frontend deliberately avoids complex client-side state machines or heavy data frameworks because it should never become the source of truth for CAD logic. Instead, it treats the backend as authoritative and simply renders the responses so you can verify plan structure, parameter values, and error handling. This separation keeps the UI from drifting into logic that would later diverge from the add-in, which is a common failure mode in systems that maintain two separate user interfaces.

Job polling is intentionally simple (interval-based) because job processing is lightweight and local. If long-running jobs are introduced later, this polling can be replaced with server-sent events or WebSockets without changing the core UI responsibilities.

At the file level, the frontend stays intentionally small: `App.jsx` handles user input, request triggers, and response rendering in a single place, while `api.js` isolates the HTTP calls so request signatures stay consistent. This design keeps debugging straightforward: if a response looks wrong, you can trace it directly to a single fetch call and a single render path. The frontend does not cache or mutate backend data; it renders it as-is, which makes it a truthful window into backend behavior rather than a second interpretation layer.

### 3.3 SolidWorks Add-in (C#)
- Add-in entrypoint: `solidworks-addin/src/Addin.cs`
  - COM registration, TaskPane creation, backend connectivity check.
- TaskPane UI: `solidworks-addin/src/TaskPaneControl.cs`
  - Plan preview, step execution, run next, undo last, replay controls, voice input.
- API client + config: `solidworks-addin/src/ApiClient.cs`, `solidworks-addin/src/Utils/AddinConfig.cs`
  - Reads base URL + timeouts from app.config and offers TestConnection.
- Execution routing: `solidworks-addin/src/Controllers/ExecuteController.cs`
  - Routes parsed operations to the correct builder and validates parameters.
- Builders (geometry creation):
  - Base plate: `solidworks-addin/src/Builders/BasePlateBuilder.cs`
  - Hole patterns: `solidworks-addin/src/Builders/CircularHolesBuilder.cs`
  - Cylinders: `solidworks-addin/src/Builders/ExtrudedCylinderBuilder.cs`
  - Fillet: `solidworks-addin/src/Builders/FilletBuilder.cs`
  - Chamfer: `solidworks-addin/src/Builders/ChamferBuilder.cs`
- Utilities:
  - Selection and face targeting: `solidworks-addin/src/Utils/Selection.cs`
  - Units conversion (mm <-> meters): `solidworks-addin/src/Utils/Units.cs`
  - Undo scopes: `solidworks-addin/src/Utils/UndoScope.cs`
  - Logging: `solidworks-addin/src/Utils/Logger.cs`
  - Replay logging: `solidworks-addin/src/Utils/ReplayLogger.cs`
- Replay (SW-7): `solidworks-addin/src/Models/ReplayEntry.cs`, `ReplayLogger.cs`
  - JSONL replay file per session with sequence numbers and metadata.
- UX-1 plan execution: TaskPane steps list with run selected / run next / undo.
- Voice-1: System.Speech and whisper.cpp CLI with confirmation flow.

The add-in uses .NET Framework and WinForms because SolidWorks expects COM-based add-ins and TaskPane UI built on Win32-compatible tooling. .NET Core or WPF would require additional hosting layers and are less reliable in the SolidWorks TaskPane environment. This keeps the integration stable and makes it possible to call SolidWorks API methods directly without inter-process IPC. C# is used for strong interop typing and for convenient access to the SolidWorks PIAs, which is far more reliable than calling COM from unmanaged code in this context. The builder classes are intentionally separated so each feature (plate, holes, cylinder, fillet, chamfer) has its own guardrails, and the ExecuteController provides a single routing point so behavior is consistent across preview and execution paths. Utilities like Units, UndoScope, and Selection exist because SolidWorks APIs expect meters and require explicit selection, and these helpers make those operations safe and repeatable across the codebase.

The add-in is also where replay logging, step execution, and undo semantics intersect with the SolidWorks feature tree. That means it must track feature names, handle rebuild calls, and surface errors in a user-friendly way, all while staying responsive inside the SolidWorks UI thread. The TaskPane combines these concerns so that a user can see the plan, run steps individually, and recover from failures without leaving SolidWorks, which is critical for trust in an AI-assisted CAD workflow.

Because the add-in consumes the backend schema directly, any schema changes require careful coordination. This is why the router and parameter validation logic are deliberately conservative and why the add-in logs both inputs and outcomes for each operation: it provides a reliable audit trail for debugging and ensures that the execution layer can be trusted even when AI parsing varies.

At an implementation level, `TaskPaneControl.cs` is the glue that binds UI events to the execution pipeline: it collects instructions, sends them to the backend via `ApiClient`, renders plan steps, and then calls into `ExecuteController` for each step. `ExecuteController` is the single routing point that decides which builder runs and whether parameter validation passes. Each builder (plate, cylinder, holes, fillet, chamfer) depends on shared utilities for consistent selection and unit conversion, which is why those utilities are kept in a common folder and are not duplicated. These dependencies matter because the undo, replay, and step execution features all sit on top of the same builder stack; if a builder misreports its feature name or skips a rebuild, it can break undo or replay even if the geometry was created successfully.

### 3.4 Config and data artifacts
- Backend config/env: `backend/.env`, `backend/config.py`
- Add-in config: `solidworks-addin/app.config` (copied to DLL config on build)
- Replay logs: `%APPDATA%\\TextToCad\\replay\\replay_*.jsonl`
- Backend SQLite: `backend/app.db`

Configuration is intentionally explicit and file-based to support offline and on-prem usage. The add-in config is duplicated into the DLL config at build time because that is how .NET Framework loads settings, and the replay and database artifacts are simple files to avoid external service dependencies. This means environment setup is transparent: if behavior changes, you can trace it to a visible config key rather than hidden defaults. It also makes CI and manual QA straightforward because all runtime knobs are in one place and can be documented, versioned, and reproduced across machines.

This section is deliberately short in code but significant in impact because it governs how the system behaves in different environments. In practice, most integration issues are configuration issues (wrong API base URL, missing whisper path, logging disabled), so having a clear, centralized list of data artifacts and config files dramatically reduces setup time and debugging effort.

The database and replay logs are also part of the system's operational footprint. They provide an audit trail of what was executed and when, which is critical for debugging and for future replay-based regression testing.

The config files also serve as the system's operational interface. `backend/config.py` reads environment variables and `.env` values to define defaults like model names, timeouts, and feature toggles, while the add-in's `app.config` (and its built `TextToCad.SolidWorksAddin.dll.config`) defines API base URL, replay options, and voice settings. This split lets you change runtime behavior without rebuilding code, but it also means misconfiguration is the most common source of "mysterious" failures. The documentation emphasizes these files because they determine whether the add-in can reach the backend, whether replay is written, and whether voice transcription can run, which directly affects the user experience.

Section 3 is the technical map of the codebase. It explains not just where each feature lives, but why each technology was chosen to fit SolidWorks constraints, local-first requirements, and the need for deterministic execution with optional AI enhancement. Each subsection effectively corresponds to a sprint milestone: parsing and contract hardening in the backend, UI validation in the frontend, and feature builders plus replay and UX improvements in the add-in. If you read only these paragraphs, you should still understand how the parts interlock, why specific tooling was chosen, and which files to inspect when you need to change behavior.

This section is also meant to serve as a navigation guide for developers. When you need to modify parsing, you go to the backend; when you need to change user workflow or step execution, you go to the add-in; when you need a quick sanity check on responses, you use the frontend. By keeping these boundaries clear, the repo remains approachable even as the feature set grows.

The most important cross-cutting concerns to keep in mind are units, selection, and schema stability. Units are normalized in the backend but converted to meters in the add-in, so any new feature must respect that conversion. Selection is handled through shared utilities, so any new builder should reuse those selection helpers instead of inventing new selection logic. Schema stability is enforced by parsing and normalization in `backend/main.py` and by conservative routing in `ExecuteController`; this means feature additions should extend the schema rather than alter existing fields. Understanding these cross-cutting concerns is what allows a developer to safely extend the system without breaking existing workflows.


## 4. Most Important and Novel Code Sections (with full snippets)

### 4.1 Unified parsing + fallback (critical reliability layer)
File: `backend/main.py`
First line: `def parse_instruction_internal(text: str, use_ai: bool) -> dict:`
Last line: `    }`
```python
def parse_instruction_internal(text: str, use_ai: bool) -> dict:
    """
    Internal helper function to parse natural language CAD instructions.
    
    This function encapsulates the parsing logic that can be used by both
    /process_instruction and /generate_model endpoints. It handles AI parsing
    attempts with fallback to rule-based parsing.
    
    Parsing Logic:
    - If use_ai=True and OPENAI_API_KEY is set: attempts AI parsing via OpenAI
    - If AI fails or use_ai=False: falls back to rule-based parsing
    - Always returns consistent normalized format with source indication
    
    Args:
        text (str): Natural language instruction to parse
        use_ai (bool): Whether to attempt AI parsing first
        
    Returns:
        dict: Parsed result with structure:
            {
                "result": {
                    "action": str,
                    "parameters": {
                    "count": Optional[int],
                    "diameter_mm": Optional[float],
                    "height_mm": Optional[float],
                    "width_mm": Optional[float],
                    "length_mm": Optional[float],
                    "depth_mm": Optional[float],
                    "radius_mm": Optional[float],
                    "angle_deg": Optional[float],
                    "shape": Optional[str],
                    "pattern": Optional[dict]
                }
                },
                "source": str  # "ai" or "rule"
            }
    """
    logger.info(f"Parsing instruction: '{text}' (use_ai={use_ai})")
    
    parsed_result = None
    source = "rule"  # Default to rule-based
    
    # Try AI parsing if requested and API key is available
    if use_ai and os.getenv("OPENAI_API_KEY"):
        try:
            logger.info("Attempting AI parsing with OpenAI")
            parsed_result = parse_instruction_with_ai(text)
            parsed_result = normalize_parsed_result(parsed_result)
            source = "ai"
            if not _has_any_parameter(parsed_result.get("parameters")):
                logger.warning("AI parsing returned empty parameters; falling back to rule-based parsing")
                parsed_result = None
                source = "rule"
            else:
                logger.info(f"AI parsing successful: {parsed_result}")
        except LLMParseError as e:
            logger.warning(f"AI parsing failed, falling back to rules: {e}")
            parsed_result = None  # Will trigger fallback
        except ValueError as e:
            logger.warning(f"AI parsing returned invalid structure, falling back to rules: {e}")
            parsed_result = None
        except Exception as e:
            logger.error(f"Unexpected error in AI parsing, falling back to rules: {e}")
            parsed_result = None  # Will trigger fallback
    elif use_ai:
        logger.info("AI requested but OPENAI_API_KEY not configured, using rule-based parsing")
    
    # Fallback to rule-based parsing if AI failed or wasn't used
    if parsed_result is None:
        logger.info("Using rule-based parsing")
        parsed_params = parse_cad_instruction(text)
        source = "rule"
        
        raw_result = {
            "action": parsed_params.action,
            "parameters": parsed_params.dict(exclude={"action"}),
        }
        parsed_result = normalize_parsed_result(raw_result)
    
    # Log parsing results
    logger.info(f"Parsing complete - Source: {source}, Action: {parsed_result.get('action')}, "
               f"Parameters: {parsed_result.get('parameters')}")
    
    return {
        "result": parsed_result,
        "source": source
    }
```

Key behaviors and why they matter:
- Attempts AI parsing only when requested and configured, which keeps deterministic rule parsing as the baseline.
- Normalizes results into a stable schema so add-in routing never depends on AI-specific output formats.
- Logs each decision branch so failures can be traced to AI, normalization, or rule parsing.

This function is the contract backbone for every downstream feature. It turns freeform text into predictable JSON, and its explicit fallback logic is what keeps the add-in reliable even when AI behavior is noisy or unavailable.

It lives in `backend/main.py` and depends on `parse_instruction_with_ai` (from `backend/llm.py`), `parse_cad_instruction` (rule-based parsing in `backend/main.py`), and `normalize_parsed_result` (schema normalization). Both `/process_instruction` and `/dry_run` call this helper for every operation, and the planner uses it internally when constructing bike operations. If this function changes, it affects every client: the SolidWorks add-in, the React frontend, and the test scripts all rely on its schema shape and fallback behavior. That is why it is treated as a critical stability point and heavily logged.

Internally the logic is intentionally defensive: it attempts AI parsing only when the API key is present and immediately validates the output by checking for any meaningful parameters. If the AI output is empty or malformed, the function discards it and falls back to deterministic parsing, ensuring the system never propagates ambiguous data into the execution layer. It also always returns a consistent `source` flag so downstream UI can explain whether AI or rules were used, which is critical for user trust and for debugging inconsistent behavior.

Downstream behavior depends on the exact shape of this return object. The history database persists the parsed parameters that originate here, the planner uses the same structure to assemble multi-step operations, and the add-in expects fields like `shape`, `diameter_mm`, and `pattern` to be present or explicitly null. Any changes to the normalization or defaulting behavior in this function will cascade into UI rendering, replay logs, and execution routing, which is why the function is guarded by tests and logging at every branch. In short, this is the single highest-leverage point for correctness in the entire pipeline.

### 4.2 Bike planner operations builder (Planner-1 core)
File: `backend/main.py`
First line: `def _build_bike_operations(answers: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], List[str]]:`
Last line: `    return operations, notes`
```python
def _build_bike_operations(answers: Dict[str, Any]) -> Tuple[List[Dict[str, Any]], List[str]]:
    frame_size = _coerce_float(answers.get("frame_size_mm")) or 450.0
    wheel_diameter = _coerce_float(answers.get("wheel_diameter_mm")) or 650.0
    tire_width = _coerce_float(answers.get("tire_width_mm")) or 55.0
    handlebar_width = _coerce_float(answers.get("handlebar_width_mm")) or 720.0

    wheelbase = max(frame_size * 2.0, wheel_diameter * 1.5)
    frame_length = wheelbase * 0.7
    frame_width = max(frame_size * 0.05, 20.0)
    frame_thickness = max(frame_size * 0.015, 6.0)

    wheel_height = max(tire_width, 12.0)
    head_tube_diameter = max(handlebar_width * 0.03, 14.0)
    head_tube_height = max(frame_size * 0.3, 120.0) * 3.0
    seatpost_diameter = max(head_tube_diameter * 0.7, 12.0)
    seatpost_height = max(frame_size * 0.35, 160.0)

    seat_length = max(frame_size * 0.35, 140.0)
    seat_width = max(frame_size * 0.12, 45.0)
    seat_thickness = max(frame_thickness * 0.6, 6.0)

    handlebar_diameter = max(handlebar_width * 0.03, 20.0)
    handlebar_length = max(handlebar_width, wheel_diameter * 0.6)

    front_wheel_x = wheelbase / 2.0
    rear_wheel_x = -wheelbase / 2.0
    seatpost_x = rear_wheel_x + wheelbase * 0.4
    wheel_radius = wheel_diameter / 2.0
    headtube_clearance = wheel_radius + (head_tube_diameter * 1.5)
    headtube_preferred = front_wheel_x - headtube_clearance
    frame_half = (frame_length / 2.0) - head_tube_diameter
    headtube_x = min(headtube_preferred, frame_half)
    handlebar_center_y = (frame_thickness / 2.0) + head_tube_height + (handlebar_diameter / 2.0)

    instructions = [
        f"create base plate {frame_length:.0f} x {frame_width:.0f} x {frame_thickness:.0f} mm",
        f"create cylinder diameter {seatpost_diameter:.0f} mm height {seatpost_height:.0f} mm",
        f"create base plate {seat_length:.0f} x {seat_width:.0f} x {seat_thickness:.0f} mm",
        f"create cylinder diameter {head_tube_diameter:.0f} mm height {head_tube_height:.0f} mm",
        f"create cylinder diameter {handlebar_diameter:.0f} mm height {handlebar_length:.0f} mm",
        f"create cylinder diameter {wheel_diameter:.0f} mm height {wheel_height:.0f} mm",
        f"create cylinder diameter {wheel_diameter:.0f} mm height {wheel_height:.0f} mm",
    ]

    operations = []
    for index, instruction in enumerate(instructions):
        parse_result = parse_instruction_internal(instruction, use_ai=False)
        parsed_result = parse_result["result"]
        parameters = parsed_result.get("parameters", {})

        if index == 0:
            parameters["extrude_midplane"] = True
        elif index == 1:
            parameters["center_x_mm"] = seatpost_x
            parameters["center_z_mm"] = 0.0
            parameters["axis"] = "y"
            parameters["use_top_face"] = True
        elif index == 2:
            parameters["center_x_mm"] = seatpost_x
            parameters["center_z_mm"] = 0.0
            parameters["use_top_face"] = True
        elif index == 3:
            parameters["center_x_mm"] = headtube_x
            parameters["center_z_mm"] = 0.0
            parameters["axis"] = "y"
            parameters["use_top_face"] = True
        elif index == 4:
            parameters["center_x_mm"] = headtube_x
            parameters["center_y_mm"] = handlebar_center_y
            parameters["axis"] = "z"
            parameters["extrude_midplane"] = True
        elif index == 5:
            parameters["center_x_mm"] = rear_wheel_x
            parameters["center_y_mm"] = 0.0
            parameters["axis"] = "z"
            parameters["extrude_midplane"] = True
        elif index == 6:
            parameters["center_x_mm"] = front_wheel_x
            parameters["center_y_mm"] = 0.0
            parameters["axis"] = "z"
            parameters["extrude_midplane"] = True

        parsed_result["parameters"] = parameters
        operations.append(parsed_result)

    notes = [
        "Planner outputs a simplified single-part placeholder with parts spaced along X/Y.",
        "Assembly, mates, and accurate positioning will be handled in Assembly-1.",
    ]

    return operations, notes
```

Key behaviors and why they matter:
- Converts high-level planner answers into concrete operations with explicit geometry parameters.
- Injects placement and axis hints so the add-in can build multi-part placeholders without manual positioning.
- Keeps the plan repeatable by always generating the same operation order for the same inputs.

This planner builder is intentionally conservative: it creates a simplified, single-part placeholder model, but it encodes enough spatial structure for later assembly workflows. It is a bridge between natural language planning and the deterministic operation schema used by the add-in.

It is implemented in `backend/main.py` and relies on the same parsing pipeline used for user instructions, which ensures the resulting operations follow the exact schema the add-in expects. The planner endpoints (`/plan`, `/plan/{state_id}`) depend on this function to translate abstract design answers into executable geometry steps. Downstream, the add-in executes these operations in order using the same builders as normal commands, which is why this function injects explicit placement parameters (center coordinates, axis, midplane) instead of leaving them implicit. Any change here impacts planner outputs, bike placeholder geometry, and the UX-1 step list, so it must remain stable and well-tested.

The function also encodes the geometry heuristics that make the placeholder model look plausible. It computes wheelbase, frame dimensions, and tube sizes from a small set of user answers, then derives relative offsets so parts are spaced along X/Y rather than stacked at the origin. It chooses axes explicitly (for example, wheels along the Z axis, posts along Y) so the add-in can align features without needing additional context. This is why the planner outputs can be executed step-by-step and still form a coherent arrangement.

This builder depends on `parse_instruction_internal` so that every generated operation is identical in shape to a user-provided instruction. That dependency is deliberate: it means the planner never produces an operation the add-in cannot understand, and it keeps all normalization logic in one place. The downstream add-in relies on fields like `center_x_mm`, `center_y_mm`, `axis`, and `use_top_face` to select faces and place sketches, so incorrect defaults here will manifest as mispositioned geometry or inverted extrusions. In that sense, this planner builder is both a geometry heuristic module and a schema compliance test; if it produces a valid placeholder bike, the operation schema is likely robust for more complex designs later.

### 4.3 Operation router (single source of truth for builder mapping)
File: `solidworks-addin/src/Controllers/ExecuteController.cs`
First line: `private bool ExecuteSingleOperation(IModelDoc2 model, ParsedParameters parsed)`
Last line: `        }`
```csharp
        private bool ExecuteSingleOperation(IModelDoc2 model, ParsedParameters parsed)
        {
            try
            {
                if (parsed == null || parsed.ParametersData == null)
                {
                    _log.Error("Invalid parsed parameters");
                    return false;
                }

                var data = parsed.ParametersData;

                // Validate parameters
                if (!ValidateParameters(data))
                {
                    _log.Error("Parameter validation failed");
                    return false;
                }

                // Route to appropriate builder based on action and shape
                string action = parsed.Action?.ToLowerInvariant() ?? "";
                string shape = data.Shape?.ToLowerInvariant() ?? "";
                bool hasChamferParams = data.ChamferDistanceMm.HasValue || !string.IsNullOrWhiteSpace(data.ChamferTarget);
                bool hasFilletParams = !string.IsNullOrWhiteSpace(data.FilletTarget);

                _log.Info($"Routing operation: action='{action}', shape='{shape}'");

                // Handle fillet operation
                if (action == "fillet" || shape == "fillet" || (hasFilletParams && string.IsNullOrEmpty(shape)))
                {
                    return ExecuteFillet(model, data);
                }

                // Handle base plate / block
                if (shape.Contains("base") || shape.Contains("plate") || shape.Contains("rectangular") ||
                    shape.Contains("block") || shape.Contains("box") || shape.Contains("cube") || shape == "base_plate")
                {
                    return ExecuteBasePlate(model, data);
                }

                // Handle cylinder
                if (shape == "cylinder" || shape == "cylindrical")
                {
                    return ExecuteCylinder(model, data);
                }

                // Handle holes
                if (action.Contains("hole") || data.Pattern != null)
                {
                    return ExecuteHoles(model, data);
                }

                // Handle chamfer
                if (action.Contains("chamfer") || shape.Contains("chamfer") || (hasChamferParams && string.IsNullOrEmpty(shape)))
                {
                    return ExecuteChamfer(model, data);
                }

                _log.Warn($"Unhandled operation: action='{action}', shape='{shape}'");
                return false;
            }
            catch (Exception ex)
            {
                _log.Error($"ExecuteSingleOperation failed: {ex.Message}");
                return false;
            }
        }
```

Key behaviors and why they matter:
- Centralizes operation routing, so new builders can be added without duplicating parsing logic.
- Uses action and shape heuristics plus guardrails to pick the correct builder reliably.
- Returns clear failures when a request is unsupported, avoiding silent no-ops.

This router is the add-in's single source of truth for execution. By keeping routing logic in one place, the system stays predictable and makes it clear where to extend support for new actions or shapes.

It lives in `solidworks-addin/src/Controllers/ExecuteController.cs` and depends on the model classes (`ParsedParameters` and `Parameters`) plus the builder classes (BasePlateBuilder, CircularHolesBuilder, ExtrudedCylinderBuilder, FilletBuilder, ChamferBuilder). The TaskPane calls into this controller for both full execution and step-by-step runs, and Replay logging wraps around its results. If routing is incorrect, the add-in will either execute the wrong builder or silently skip operations, so this method is central to correctness and must align precisely with backend action/shape naming conventions.

The router also implements subtle fallback behaviors: it treats fillet and chamfer as operations even when `shape` is empty, and it routes holes when a pattern is present even if the action name is ambiguous. This is deliberate because AI parsing can omit or generalize `shape`, so the router prioritizes intent-related parameters over strict shape matching. That design choice prevents common failures such as a fillet command being interpreted as a generic create_feature action.

This method is also the primary enforcement point for parameter validation. The `ValidateParameters` check runs before routing, which means invalid dimensions are rejected before any SolidWorks API calls are made. That validation protects the builders from malformed inputs and keeps error reporting consistent in the TaskPane. Downstream features like Undo and Replay assume that a failed operation returns `false` cleanly; this method ensures that a failed routing decision does not accidentally create partial geometry. In other words, the router is not just a dispatcher, it is the gatekeeper that keeps execution atomic and auditable.

### 4.4 Face targeting for placement (critical for multi-part placement)
File: `solidworks-addin/src/Utils/Selection.cs`
First line: `public static IFace2 GetTopMostPlanarFaceAt(`
Last line: `        }`
```csharp
        public static IFace2 GetTopMostPlanarFaceAt(
            IModelDoc2 model,
            double xMm,
            double zMm,
            ILogger logger = null,
            double toleranceMm = 0.1)
        {
            if (model == null)
            {
                logger?.Error("GetTopMostPlanarFaceAt: model is null");
                return null;
            }

            try
            {
                if (model.GetType() != (int)swDocumentTypes_e.swDocPART)
                {
                    logger?.Warn($"GetTopMostPlanarFaceAt: Document type is {model.GetType()}, not Part");
                    return null;
                }

                IPartDoc partDoc = (IPartDoc)model;
                object[] bodies = (object[])partDoc.GetBodies2(
                    (int)swBodyType_e.swSolidBody,
                    true
                );

                if (bodies == null || bodies.Length == 0)
                {
                    logger?.Warn("GetTopMostPlanarFaceAt: No solid bodies found");
                    return null;
                }

                double x = Units.MmToM(xMm);
                double z = Units.MmToM(zMm);
                double tol = Units.MmToM(toleranceMm);

                logger?.Info($"Searching for top planar face at X={xMm} mm, Z={zMm} mm");

                IFace2 topFace = null;
                double maxY = double.MinValue;

                foreach (object bodyObj in bodies)
                {
                    IBody2 body = bodyObj as IBody2;
                    if (body == null) continue;

                    object[] faces = (object[])body.GetFaces();
                    if (faces == null) continue;

                    foreach (object faceObj in faces)
                    {
                        IFace2 face = faceObj as IFace2;
                        if (face == null) continue;

                        ISurface surface = face.GetSurface() as ISurface;
                        if (surface == null || !surface.IsPlane())
                            continue;

                        double[] box = (double[])face.GetBox();
                        if (box == null || box.Length < 6)
                            continue;

                        double xMin = box[0] - tol;
                        double xMax = box[3] + tol;
                        double zMin = box[2] - tol;
                        double zMax = box[5] + tol;

                        if (x < xMin || x > xMax || z < zMin || z > zMax)
                            continue;

                        double centerY = (box[1] + box[4]) / 2.0;
                        if (centerY > maxY)
                        {
                            maxY = centerY;
                            topFace = face;
                        }
                    }
                }

                if (topFace != null)
                {
                    logger?.Info($"Found topmost planar face at location (Y={Units.MToMm(maxY):F2} mm)");
                }
                else
                {
                    logger?.Warn("No planar face found at target location");
                }

                return topFace;
            }
            catch (Exception ex)
            {
                logger?.Error($"Exception finding planar face at location: {ex.Message}");
                return null;
            }
        }
```

Key behaviors and why they matter:
- Searches only planar faces and selects the top-most candidate at a target location.
- Uses bounding boxes and tolerance to keep selection robust under small geometry changes.
- Logs both success and failure details for debugging selection issues.

This function makes multi-part placement possible without manual selection by the user. It creates a repeatable way to find a suitable face near a coordinate, which is critical for stacking parts like seatposts or handlebars on top of a frame.

It lives in `solidworks-addin/src/Utils/Selection.cs` and depends on SolidWorks body and face traversal, bounding box extraction, and unit conversion helpers in `Units.cs`. Builders like BasePlateBuilder and ExtrudedCylinderBuilder call it when the operation specifies `use_top_face` or explicit placement coordinates. If this function returns the wrong face, downstream sketches will be created on unintended surfaces, so its tolerance handling and logging are essential for diagnosing placement issues in multi-body models.

The implementation iterates over every solid body, inspects each face, and filters by planar surfaces whose bounding boxes contain the requested X/Z coordinates within a small tolerance. It then chooses the face with the highest Y coordinate, which corresponds to the top-most face in the SolidWorks coordinate system used by this project. This is a pragmatic approach that avoids expensive geometric queries while still yielding stable results for flat reference faces.

The choice of X/Z filtering and Y-max selection is specifically tied to how the rest of the system defines placement. Planner operations and builders use X/Z as horizontal placement axes and Y as vertical height, so this function enforces that convention in a single place. Downstream, operations like seatpost creation and top-plate placement rely on `use_top_face` to attach geometry to existing parts instead of the global plane, which is the main reason this helper exists. If you change coordinate conventions or introduce rotated reference planes, this function would be the first place to update because it encodes the project's default spatial assumptions.

### 4.5 Replay log append (deterministic JSONL session logging)
File: `solidworks-addin/src/Utils/ReplayLogger.cs`
First line: `public static void AppendEntry(ReplayEntry entry)`
Last line: `        }`
```csharp
        public static void AppendEntry(ReplayEntry entry)
        {
            if (!ReplayLoggingEnabled)
                return;

            if (_isPaused)
                return;

            if (entry == null)
                return;

            if (!EnsureSession())
                return;

            lock (LockObject)
            {
                entry.SessionId = _sessionId;
                entry.Sequence = ++_sequence;
                entry.TimestampUtc = DateTime.UtcNow.ToString("o");

                string json = JsonConvert.SerializeObject(entry, Formatting.None);
                File.AppendAllText(_replayFilePath, json + Environment.NewLine);
            }
        }
```

Key behaviors and why they matter:
- Appends replay entries only when logging is enabled and the session is active.
- Adds sequence numbers and timestamps for deterministic replay ordering.
- Writes JSONL so each operation is independently parseable and resilient to partial writes.

Replay logging is the foundation for reproducibility, debugging, and future regression tests. By logging each operation in a strict order, the system can later reconstruct and compare the exact sequence of geometry operations.

This method lives in `solidworks-addin/src/Utils/ReplayLogger.cs` and is called from execution paths after a feature succeeds or fails. It depends on configuration settings from `app.config` and uses Newtonsoft.Json for serialization. The Replay UI buttons and backend `/replay` endpoint both rely on the JSONL output format defined here, so changes to this function ripple into replay tooling and any future regression harness that replays operations. That dependency chain is why the JSONL format is intentionally minimal and stable.

The implementation uses a lock to ensure that concurrent execution paths do not interleave writes, and it increments a sequence counter so each entry can be replayed in the exact order of execution. This is especially important when operations are executed step-by-step or when a session is paused and resumed. The JSONL format also makes partial log recovery possible if the application crashes mid-session, which is a realistic scenario during CAD automation.

Replay entries also serve as a future contract test fixture: the add-in, backend replay endpoint, and any replay-based QA tooling all interpret the same JSONL records. That means this append method effectively defines the replay schema, and any changes here must be coordinated with `ReplayEntry.cs`, the UI replay buttons, and backend replay ingestion. The add-in relies on session ids and sequences to resume and pause logging correctly, so the `EnsureSession` and `ReplayLoggingEnabled` gating logic is critical for correctness when multiple part files are edited in a single SolidWorks session. Without those guards, replays could become interleaved or corrupted, which would undermine the core reproducibility goal.

### 4.6 Whisper voice pipeline (record -> transcribe -> confirm)
File: `solidworks-addin/src/TaskPaneControl.cs`
First line: `private void StartWhisperCapture()`
Last line: `            return string.Empty;`
```csharp
        private void StartWhisperCapture()
        {
            if (!ValidateWhisperConfig(out string message))
            {
                AppendLog(message, Color.Orange);
                UpdateVoiceStatus("Voice: whisper not configured");
                return;
            }

            _lastVoiceTranscript = string.Empty;
            txtVoiceTranscript.Text = string.Empty;
            _voiceRecordingPath = Path.Combine(Path.GetTempPath(), $"texttocad_voice_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            if (!TryMciCommand($"open new Type waveaudio Alias {VoiceRecordingAlias}", out string error))
            {
                AppendLog($"Voice record start failed: {error}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                return;
            }

            if (!TryMciCommand($"record {VoiceRecordingAlias}", out error))
            {
                AppendLog($"Voice record start failed: {error}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                TryMciCommand($"close {VoiceRecordingAlias}", out _);
                return;
            }

            _isVoiceRecording = true;
            btnVoiceRecord.Text = "Stop";
            btnVoiceConfirm.Enabled = false;
            btnVoiceCancel.Enabled = true;
            UpdateVoiceStatus("Voice: recording (whisper)...");
        }

        private void StopWhisperCapture(bool transcribe)
        {
            if (!_isVoiceRecording)
            {
                return;
            }

            if (!TryMciCommand($"save {VoiceRecordingAlias} \"{_voiceRecordingPath}\"", out string error))
            {
                AppendLog($"Voice record save failed: {error}", Color.Red);
            }

            TryMciCommand($"close {VoiceRecordingAlias}", out _);

            _isVoiceRecording = false;
            btnVoiceRecord.Text = "Record";

            if (!transcribe)
            {
                UpdateVoiceStatus("Voice: stopped");
                return;
            }

            if (string.IsNullOrWhiteSpace(_voiceRecordingPath) || !File.Exists(_voiceRecordingPath))
            {
                AppendLog("Voice recording not found for transcription.", Color.Red);
                UpdateVoiceStatus("Voice: error");
                return;
            }

            btnVoiceRecord.Enabled = false;
            UpdateVoiceStatus("Voice: transcribing...");
            _ = TranscribeWhisperAsync(_voiceRecordingPath);
        }

        private async Task TranscribeWhisperAsync(string wavPath)
        {
            string transcript = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                transcript = await Task.Run(() => RunWhisperCli(wavPath, out errorMessage));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (string.IsNullOrWhiteSpace(transcript))
            {
                AppendLog($"Whisper transcription failed: {errorMessage}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                btnVoiceRecord.Enabled = true;
                return;
            }

            _lastVoiceTranscript = transcript.Trim();
            SetVoiceTranscript(_lastVoiceTranscript);
            UpdateVoiceStatus("Voice: captured (whisper)");

            if (btnVoiceConfirm.InvokeRequired)
            {
                btnVoiceConfirm.BeginInvoke(new Action(() => btnVoiceConfirm.Enabled = true));
            }
            else
            {
                btnVoiceConfirm.Enabled = true;
            }

            btnVoiceRecord.Enabled = true;
        }

        private string RunWhisperCli(string wavPath, out string error)
        {
            error = string.Empty;

            if (!ValidateWhisperConfig(out error))
            {
                return string.Empty;
            }

            string outputBase = Path.Combine(Path.GetTempPath(), $"texttocad_whisper_{Guid.NewGuid()}");
            string outputTxt = outputBase + ".txt";
            string args = $"-m \"{_whisperModelPath}\" -f \"{wavPath}\" -l {_whisperLanguage} -otxt -of \"{outputBase}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = _whisperExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    error = "Failed to start Whisper CLI process.";
                    return string.Empty;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(error))
                {
                    error = string.IsNullOrWhiteSpace(stderr) ? $"Whisper exited with code {process.ExitCode}." : stderr.Trim();
                }

                if (File.Exists(outputTxt))
                {
                    return File.ReadAllText(outputTxt).Trim();
                }

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    return stdout.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                error = "Whisper did not return any transcript.";
            }

            return string.Empty;
        }
```

Key behaviors and why they matter:
- Records audio locally, transcribes with whisper-cli, and requires explicit confirmation before execution.
- Separates record, transcribe, and confirm states to prevent accidental execution from noisy audio.
- Writes transcripts back to the UI so users can edit or reject the recognized text.

This pipeline converts voice into a safe, reviewable command. It is designed to reduce accidental actions by forcing an explicit confirmation step and by keeping the transcription visible and editable before the system executes any CAD operations.

It lives in `solidworks-addin/src/TaskPaneControl.cs` and relies on Windows MCI audio capture, whisper-cli for transcription, and add-in configuration values for executable/model paths. The confirm action copies the transcript into the main instruction flow, which means all existing parsing and execution logic is reused rather than duplicated for voice. This keeps voice input as a thin layer over the established contract instead of a parallel path, reducing risk and maintenance cost.

The implementation is asynchronous by design: recording happens on the UI thread, transcription runs in a background task, and results are marshaled back to the UI to update the transcript box. This keeps the TaskPane responsive during transcription and prevents SolidWorks from freezing. It also enforces a clear lifecycle (record -> stop -> transcribe -> confirm), which is essential for safe CAD execution in noisy environments.

The voice subsystem depends on configuration keys (whisper executable path, model path, and toggle flags) and on external binaries for transcription. That dependency is why the UI includes explicit validation messages and why the transcript is editable: even a correct model can mis-hear domain-specific terms. Downstream, the confirm action feeds the transcription into the same instruction pipeline used by typed input, so all parsing, planning, and execution logic remains centralized and consistent. That design keeps voice as a thin input layer rather than a separate command path, which dramatically reduces the risk of divergent behavior between voice and text.

Section 4 highlights the most critical implementation points in the system and explains how they enforce determinism, debuggability, and safe execution. These code paths are the primary places to inspect when diagnosing unexpected behavior or adding new core features. If you are trying to understand how text becomes geometry, or why a feature failed, these sections are the shortest path to the answer because they sit exactly at the boundaries between parsing, routing, selection, and execution. They also capture the architectural intent: keep the schema stable, keep execution centralized, and keep replay logs explicit so changes can be verified.

Each subsection in Section 4 was chosen because it sits on a fault line: parsing stability, planner translation, execution routing, selection, replay, and voice input. Changes in these areas tend to have outsized impact on the system, which is why the documentation ties them to both upstream dependencies (configuration, parsing output, planner state) and downstream effects (feature creation, undo, replay). If you are introducing a new feature or refactoring existing behavior, these are the places where you should add tests, logs, and documentation first, because they define how the system behaves under real-world uncertainty.


## 5. Future Work (deep detail, industry readiness, and custom LLM)

### 5.1 Geometry and CAD robustness
- Assembly-1: create separate parts and apply mates (wheels, frame, handlebar, seatpost) so geometry is not all in one part.
- Feature tracking: tag features with stable names and track ownership per operation so undo/repair can target exact features.
- Fail-repair loop: when a fillet/chamfer fails, automatically try smaller radii and log the repair attempt.
- Topology-aware selection: improve edge filtering (exclude internal edges, tiny edges, or edges below radius threshold).

These improvements focus on making geometry creation more reliable at scale. The key idea is to track and validate features proactively, so operations can be repaired or rolled back in a targeted way rather than forcing the user to manually clean up failed features.

In practice this means adding explicit feature identifiers, capturing feature tree nodes per operation, and using those identifiers for undo and replay instead of relying on heuristic selection. It also means validating parameters against geometry constraints before creating features (for example, fillet radius vs edge length) and adding selective rebuild checks after each step. Together, these measures reduce silent failures and make complex multi-step commands safer to execute.

More detailed robustness work would include capturing and storing stable references to faces and edges (for example, through SolidWorks persistent IDs), so later operations can target the correct geometry even after rebuilds. It would also include a structured failure taxonomy that distinguishes selection failures, sketch failures, and feature failures, because each category has different recovery options. Implementing these changes would make undo and replay more reliable and would give the planner better feedback about what kinds of operations are safe on the current model state.

### 5.2 Planner and design-state upgrades
- Design state memory: store part library instances, constraints, and named features so planners can reference existing geometry.
- Clarifying questions: ask only for missing parameters and auto-fill defaults with justification.
- Multi-step plan execution: planner should return typed steps that map 1:1 to executable operations.
- Persistent multi-part states: support continuing a bike across multiple sessions, not just per request.

Planner upgrades turn one-off prompts into an ongoing design conversation. By persisting state and asking only targeted questions, the system can behave more like a CAD assistant that remembers context instead of a stateless text parser.

The goal is to maintain a coherent design narrative: what parts exist, what operations have been applied, and what dimensions remain unknown. This would allow the planner to ask fewer questions over time, reuse prior answers, and produce more consistent multi-step plans that align with user intent. It also opens the door to resumable sessions where the system can continue a design days later without re-deriving the entire plan.

Concretely, this implies a structured state model that records parts, features, and constraints as first-class entities rather than a flat list of operations. A planner could then query that state to determine whether a wheel already exists, whether a handlebar width was provided earlier, or whether a prior answer conflicts with a new request. It would also enable conflict resolution and clarification prompts that are grounded in actual model state rather than guesswork. Implementing this would likely require schema changes in the planner state table and a richer in-memory representation of design state that can be serialized into the database for long-term continuity.

### 5.3 Replay system upgrades
- Diff mode: compare replay runs before/after a change and highlight which features or dimensions changed.
- Replay-as-tests: treat replay files as CAD regression tests that must succeed before releases.
- Replay repair: auto-adjust parameters when a feature fails, and record the patched values.

Replay is the foundation for deterministic QA. Adding diff and repair layers makes it possible to validate that refactors do not change geometry unintentionally and to automatically recover from common feature failures.

In a CAD context, a replay file acts like a golden record: if a change alters feature count, order, or dimensions, it becomes immediately visible. Repair logic can also be grounded in replay history by applying conservative adjustments (for example, smaller radii) and recording the modified parameters, which creates a feedback loop for improving default heuristics.

A full replay diff system would likely compare both the operation logs and the resulting feature tree metadata. That could include comparing feature names, rebuild statuses, and key dimensions extracted from the model after each step. This requires capturing additional metadata during execution (feature ids, face counts, bounding boxes) and storing it alongside replay entries, which is why replay logging is positioned as a foundational system rather than a simple debugging convenience. With that metadata in place, diffs can become actionable: they can point to the exact operation and parameter that changed the model, not just that the model changed.

### 5.4 Voice and UX improvements
- Grammar hints for engineering terms (bolt circle, chamfer, fillet, diameter, centerline) to reduce misrecognition.
- Per-command confirmation: allow quick edit of transcript before execution (already partially implemented).
- Hotword and push-to-talk: configurable microphone controls with visual feedback.
- Plan visualizer: show steps with miniature previews and estimated dimensions.

Voice input is only valuable if it is safe and accurate. These enhancements focus on reducing recognition errors and giving the user more control over what will be executed, which is critical in a CAD environment where mistakes can be costly.

Improved UX here means friction where it matters: clearer transcription display, explicit confirmation, and fast correction workflows. Voice is best treated as an accelerator, not a replacement for inspection, so the system should always surface what it heard and allow edits before execution. Coupled with custom vocabulary hints, this keeps voice-driven CAD reliable even in noisy environments.

A practical next step is to add a lightweight domain-specific normalization pass after transcription. For example, mapping "hole" vs "whole" or enforcing numeric patterns like "100 by 80 by 6" into a canonical "100 x 80 x 6 mm" format can reduce misfires without needing a full custom speech model. This could be implemented as a simple text post-processor in the add-in before the confirm step, which would improve accuracy while preserving the user's ability to edit. Longer term, adding word-level confidence scores and highlighting low-confidence tokens would make the correction process even safer.

### 5.5 Part library and domain coverage
- Parametric library (wheels, frame tubes, handlebars, seatpost, stem, drivetrain).
- Template-driven assemblies that can be scaled by user inputs.
- Standardized metadata for each part (clearance, mass estimate, material).

A part library allows complex models to be built from reusable, validated components. This accelerates modeling, improves consistency, and makes future assembly workflows far more predictable.

The library would define parametric defaults, required inputs, and naming conventions for each part so planners can compose assemblies without guessing dimensions. It also reduces error rates because each generator can encode domain-specific constraints (for example, rim diameter ranges or frame tube thickness limits) that would be difficult to infer from freeform text alone.

From an implementation standpoint, a part library would likely be a set of builder modules with explicit parameter schemas and documented constraints. The planner could reference these schemas to ask precise questions (for example, "wheel diameter mm" vs "rim width mm"), and the add-in could use the same schemas to validate and execute. Each generator would carry metadata such as coordinate system, attachment points, and default mates, which is essential for Assembly-1. This turns part creation from a vague instruction into a reproducible, parameterized function that can be reused across many designs.

### 5.6 Industry readiness
- Configuration profiles for units, tolerance policies, and default material.
- Validation rules for min wall thickness, min fillet radius, and other manufacturability constraints.
- Export pipelines (STEP, STL) with validation and preflight checks.

Industry-grade workflows require more than just geometry creation. They require consistent units, manufacturability validation, and repeatable export pipelines that produce dependable files for downstream fabrication.

This also includes auditability: being able to explain how a model was generated, which parameters were used, and which version of the code produced it. That level of rigor is necessary for regulated or production environments where CAD output feeds directly into manufacturing or simulation pipelines.

Industry readiness also implies standardized validation gates. For example, before exporting a STEP or STL file, the system should run a validation checklist that verifies units, checks for self-intersections, and confirms that feature rebuilds succeeded. It should also capture a provenance record that ties the output file to replay logs, schema versions, and add-in build versions. These additions are less about adding new modeling capabilities and more about making the output trustworthy and repeatable, which is essential when CAD models become part of formal engineering documentation or production workflows.

### 5.7 Custom LLM training plan
1) Dataset capture
   - Use replay logs + planner answers as structured training data.
   - Collect text -> operations pairs across real user workflows.
2) Data cleaning
   - Normalize units and map synonyms.
   - Remove ambiguous or failed operations.
3) Evaluation harness
   - Build a regression set with expected JSON outputs (schema_versioned).
   - Track accuracy by action type (plate, cylinder, holes, fillet, chamfer).
4) Fine-tuning
   - Fine-tune a small instruction model on text->operation mapping.
   - Use system prompts to enforce schema validation.
5) Inference strategy
   - Run fine-tuned model first, then fall back to rule-based parsing.
   - For high-risk features (fillet, chamfer), validate before executing.
6) Continuous improvement
   - Use failed operations as new training examples.
   - Add user feedback loops for corrections and replays.

This training plan is designed to produce a model that understands CAD-specific language and outputs consistent operation schemas. The emphasis on replay logs and corrections ensures the model improves based on real execution outcomes rather than purely synthetic examples.

The focus on schema fidelity is intentional: a model that generates plausible text but inconsistent JSON would be a net loss. By anchoring training to replay data and validated operations, the model can learn not just language patterns but also the engineering constraints that make operations executable in SolidWorks. This keeps the AI aligned with the deterministic execution engine rather than drifting into natural language paraphrase.

In practice, this also means building a gating and evaluation pipeline around the model. Every model update should be run against a regression suite of known instructions and expected JSON outputs, and the system should refuse to use a model that drops below a precision threshold on critical operations like fillets or chamfers. If the model produces ambiguous outputs, the system should fall back to rule-based parsing by default. This is not just a safety feature; it is a way to ensure that the model contributes real value rather than introducing noise into a deterministic CAD workflow.

### 5.8 Future SolidWorks CAD feature set (not yet implemented)
- Additional primitives: spheres, cones, torus, and parametric tubes with wall thickness.
- Sketching tools: point, line, arc, spline, 3D sketch, and constrained sketches.
- Advanced features: revolve, sweep, loft, shell, draft, rib, and thin feature extrudes.
- Cuts and surfaces: swept cuts, lofted cuts, boundary surfaces, and surface trims.
- Patterns and mirrors: linear, circular, curve-driven, and mirror features.
- Hole Wizard integration: tapped holes, countersinks, counterbores, and thread callouts.
- Fillet/chamfer variants: variable radius fillets, face fillets, and distance-distance chamfers.
- Text and embossing: emboss/deboss text on faces and wrap text to curved surfaces.
- Reference geometry: planes, axes, coordinate systems, and reference points.
- Feature tree controls: naming, grouping, suppression, and rebuild order management.

These additions would move the system from a proof-of-concept to a broad SolidWorks feature surface. Each feature should map to explicit operations with predictable parameter sets so the planner and the add-in can reason about them safely.

The key is to treat each new SolidWorks feature as a first-class operation with its own validation rules, selection strategy, and failure modes. That approach allows the planner to compose complex parts confidently and lets the add-in apply guardrails that match SolidWorks expectations. It also keeps the schema extensible: new features can be introduced without breaking existing operations or relying on ambiguous "do everything" commands.

To implement these features well, each operation would need explicit schema fields for required references (for example, sweep requires a profile sketch and a path sketch, loft requires multiple profiles, and shell requires a thickness and face selection). The add-in would need reliable selection utilities to capture those references and feature builders that can build the correct sketch geometry before applying the feature. This is why the roadmap emphasizes face mapping and feature tracking: advanced features depend heavily on consistent selection and feature naming, and those capabilities must be in place before complex operations can be executed safely.

Section 5 captures the roadmap to evolve Text-to-CAD from single-part demos into a full-featured CAD assistant. It includes both infrastructure improvements (replay, planner memory, validation) and CAD feature coverage, which together enable reliable, industry-grade workflows.

The intent is to sequence work so that reliability and reproducibility scale alongside feature breadth. Infrastructure upgrades (replay, validation, planner state) reduce the risk of adding new operations, while CAD feature expansion increases the system's expressive power. This combined strategy is what will allow the project to move from a helpful prototype to a dependable engineering tool.

An equally important point is that these roadmap items are interdependent. A richer planner without replay and validation would still produce fragile geometry, while a broad feature set without planner state would lead to inconsistent or conflicting commands. By treating infrastructure and feature breadth as a coupled evolution, the project can avoid the common trap of adding capabilities faster than the system can reliably execute them. This section therefore acts as a sequencing guide: it explains not just what to build, but why the order matters for trust and maintainability.

---
End of README__NEW.md (appendices follow)

## Appendix A. Complete Repository File Inventory and Purpose

This appendix lists every tracked file and its purpose. Local-only/generated artifacts are grouped in Appendix B.

Root
- `text-to-cad.sln`: Visual Studio solution that loads the SolidWorks add-in project.
- `PROJECT_OVERVIEW.md`: High-level project summary and milestones.
- `README.md`: Shorter quickstart and API overview.
- `README__NEW.md`: This comprehensive, single-source doc.
- `.gitignore`: Git ignore rules for build outputs and local artifacts.
- `.github/.gitkeep`: Placeholder so the `.github` directory stays in git (no workflows yet).
- `ai_model/.gitkeep`: Placeholder for future ML artifacts (dataset, fine-tuned models).
- `docker/.gitkeep`: Placeholder for future containerization artifacts.
- `docs/.gitkeep`: Placeholder to keep the `docs` directory tracked.
- `.vs/` (directory): Visual Studio workspace cache; local-only (see Appendix B).

These root items define the repo entry points and guardrails for tooling. The solution file and primary READMEs orient new developers, while `.gitkeep` placeholders reserve space for planned assets without forcing early structure.

Docs (sprint and thesis notes)
- `docs/thesis/README.md`: Index of thesis/sprint documentation.
- `docs/thesis/BUGFIX_CREATECENTERRECTANGLE.md`: Fix note for rectangle sketch creation.
- `docs/thesis/IMPLEMENTATION_SUMMARY.md`: Implementation summary for recent sprints.
- `docs/thesis/INTEGRATION_COMPLETE.md`: Integration status notes.
- `docs/thesis/MULTI_OPERATION_FIXES.md`: Multi-operation parsing/execution fixes.
- `docs/thesis/MULTI_OPERATION_SUPPORT.md`: Multi-operation feature support details.
- `docs/thesis/ORDER_BUG_FIX.md`: Ordering/sequence bug fix notes.
- `docs/thesis/REBUILD_CHECKLIST.md`: Rebuild and validation checklist.
- `docs/thesis/REORGANIZATION_SUMMARY.md`: Repo reorganization notes.
- `docs/thesis/SPRINT_SW2_SUMMARY.md`: SW-2 summary.
- `docs/thesis/SPRINT_SW3_SUMMARY.md`: SW-3 summary.
- `docs/thesis/SPRINT_SW4_SUMMARY.md`: SW-4 summary.
- `docs/thesis/SPRINT_SW5_SUMMARY.md`: SW-5 summary.

The docs/thesis folder is a historical record of sprint decisions and fixes. It is not required for runtime behavior, but it provides deep context for why certain guardrails or parsing fixes exist.

Backend (FastAPI)
- `backend/.env`: Local environment variables for development (not required).
- `backend/.env.example`: Example environment variable template.
- `backend/requirements.txt`: Backend Python dependencies.
- `backend/config.py`: Environment/config loader (defaults + safe fallbacks).
- `backend/db.py`: SQLAlchemy engine and session management.
- `backend/models.py`: ORM models (`Command`, `DesignState`).
- `backend/jobs.py`: Lightweight job tracking endpoints/data.
- `backend/llm.py`: OpenAI parsing client + prompt schema.
- `backend/main.py`: FastAPI app, parsing, planner, replay, and endpoints.
- `backend/tests/test_api.py`: Minimal pytest coverage for endpoints.
- `backend/app.db`: SQLite database file for command history and planner state.
- `backend/geometry/__init__.py`: Geometry package marker.
- `backend/geometry/model_builder.py`: CadQuery build functions (plate, holes, cylinder).
- `backend/geometry/exporter.py`: STEP/STL export helpers and outputs directory management.
- `backend/outputs/` (directory): Exported CAD files (created at runtime; see Appendix B).

The backend file list captures the full parsing, planning, and persistence stack. These files define the schema contract, the AI and rule parsers, and the optional CadQuery export path used for server-side geometry outputs.

Frontend (React)
- `frontend/README.md`: Frontend setup notes.
- `frontend/index.html`: Vite HTML entry point.
- `frontend/vite.config.js`: Vite build configuration.
- `frontend/eslint.config.js`: Lint configuration.
- `frontend/package.json`: Frontend dependencies and scripts.
- `frontend/package-lock.json`: Locked dependency versions.
- `frontend/public/vite.svg`: Vite placeholder asset.
- `frontend/src/main.jsx`: React entry point.
- `frontend/src/App.jsx`: UI logic (instruction, planner, history, jobs).
- `frontend/src/api.js`: API client wrappers.
- `frontend/src/index.css`: Global styles.
- `frontend/src/App.css`: Component styles.
- `frontend/src/assets/react.svg`: React placeholder asset.

The frontend files are intentionally minimal and focused on testability. They provide just enough UI to exercise parsing, planning, and job flows without introducing a heavy or production-grade web stack.

SolidWorks Add-in (C# .NET Framework)
- `solidworks-addin/TextToCad.SolidWorksAddin.csproj`: Add-in project file.
- `solidworks-addin/packages.config`: NuGet dependency list (Newtonsoft.Json).
- `solidworks-addin/nuget.exe`: Local NuGet CLI used by build scripts.
- `solidworks-addin/app.config`: Add-in app settings (API base, logging, replay, Whisper).
- `solidworks-addin/Build`: Empty marker file (placeholder for build pipeline tooling).
- `solidworks-addin/build.ps1`: Local build helper for the add-in.
- `solidworks-addin/test_path.bat`: Diagnostic script for path resolution.
- `solidworks-addin/register_addin.bat`: Register add-in (standard).
- `solidworks-addin/register_addin_debug.bat`: Register add-in (debug).
- `solidworks-addin/register_addin.ps1`: PowerShell register script (standard).
- `solidworks-addin/Register-Addin.ps1`: Alternate PowerShell register script.
- `solidworks-addin/unregister_addin.bat`: Unregister add-in.
- `solidworks-addin/README_Addin.md`: Add-in README and usage notes.
- `solidworks-addin/QUICKSTART.md`: Short add-in quickstart.
- `solidworks-addin/HOW_TO_USE.md`: UI/feature instructions.
- `solidworks-addin/TROUBLESHOOTING.md`: Common add-in issues and fixes.

These add-in files are the operational center for SolidWorks integration. Scripts and docs exist to make registration and troubleshooting repeatable, which is critical for COM add-ins that often fail silently when misconfigured.

SolidWorks Add-in source (`solidworks-addin/src/`)
- `Addin.cs`: ISwAddin entry point and TaskPane creation.
- `TaskPaneHost.cs`: Hosts the WinForms TaskPane.
- `TaskPaneControl.cs`: Main UI control (plan, execute, replay, voice).
- `TaskPaneControl.Designer.cs`: Auto-generated UI layout (WinForms).
- `TaskPaneControl.resx`: UI resources for TaskPaneControl.
- `ApiClient.cs`: HTTP client for backend calls.
- `ErrorHandler.cs`: Centralized error display/logging helpers.
- `Logger.cs`: Global logger wrapper for add-in load lifecycle.
- `Controllers/ExecuteController.cs`: Operation router and executor.
- `Builders/BasePlateBuilder.cs`: Plate creation (Top Plane, extrusion).
- `Builders/CircularHolesBuilder.cs`: Hole pattern cuts on top face.
- `Builders/ExtrudedCylinderBuilder.cs`: Cylinder boss-extrude creation.
- `Builders/FilletBuilder.cs`: Fillet operations on edges.
- `Builders/ChamferBuilder.cs`: Chamfer operations on edges.
- `Interfaces/ILogger.cs`: Logger interface for builders/utils.
- `Models/InstructionRequest.cs`: Request model for backend input.
- `Models/InstructionResponse.cs`: Response model for parsing/plan.
- `Models/ParsedParameters.cs`: Normalized operation parameters.
- `Models/PlannerRequest.cs`: Planner request model.
- `Models/PlannerResponse.cs`: Planner response model.
- `Models/ReplayEntry.cs`: Replay JSONL schema model.
- `Properties/AssemblyInfo.cs`: Assembly metadata + GUID.
- `Utils/AddinConfig.cs`: Reads app.config settings safely.
- `Utils/Logger.cs`: Thread-safe UI/file logger.
- `Utils/ReplayLogger.cs`: Replay logging/session management.
- `Utils/Units.cs`: mm <-> meter conversion helpers.
- `Utils/UndoScope.cs`: Undo wrapper for SolidWorks features.
- `Utils/Selection.cs`: Face/plane selection helpers.
- `Utils/FaceMapping.cs`: Mapping helpers for faces/edges.

The source tree is where all CAD behavior is implemented. Builders, controllers, and utilities are split to keep geometry creation isolated from routing, logging, and selection logic, which makes it easier to test and extend.

SolidWorks Add-in third-party dependency (`solidworks-addin/Newtonsoft.Json.13.0.3/`)
- `README.md`: Package readme.
- `LICENSE.md`: Package license.
- `packageIcon.png`: Package icon.
- `lib/net20/Newtonsoft.Json.dll` and `.xml`: .NET 2.0 build.
- `lib/net35/Newtonsoft.Json.dll` and `.xml`: .NET 3.5 build.
- `lib/net40/Newtonsoft.Json.dll` and `.xml`: .NET 4.0 build.
- `lib/net45/Newtonsoft.Json.dll` and `.xml`: .NET 4.5 build.
- `lib/netstandard1.0/Newtonsoft.Json.dll` and `.xml`: .NET Standard 1.0 build.
- `lib/netstandard1.3/Newtonsoft.Json.dll` and `.xml`: .NET Standard 1.3 build.
- `lib/netstandard2.0/Newtonsoft.Json.dll` and `.xml`: .NET Standard 2.0 build.
- `lib/net6.0/Newtonsoft.Json.dll` and `.xml`: .NET 6.0 build.

These dependency files are bundled locally to avoid NuGet restore issues inside SolidWorks build environments. Newtonsoft.Json is required for deterministic JSON serialization of replay logs and backend responses.

Testing harness (manual / script-based)
- `testing/README.md`: How to run manual test scripts.
- `testing/test_dry_run.py`: dry_run endpoint checks.
- `testing/test_generate_model.py`: CadQuery model generation checks.
- `testing/test_multi_operation.py`: multi-operation parsing/execution checks.
- `testing/test_order.py`: operation ordering checks.
- `testing/test_parsing_fix.py`: parsing regression checks.
- `testing/test_server_basic.py`: server availability checks.

The testing folder provides ad hoc scripts that validate contract behavior and common parsing edge cases. They are separate from pytest unit tests so they can be run independently during manual QA or before demos.

Appendix A is the canonical inventory of tracked files. It is meant to answer "what does this file do" without needing to open each file first. Use it as a map when onboarding new contributors or when auditing changes across sprints: every file is listed in the context of its role in parsing, execution, UI, or documentation. If a file is not in this list, it should be treated as generated or local-only and therefore not relied on for core behavior.

## Appendix B. Generated and Local-Only Artifacts (safe to delete/recreate)

- `.vs/`: Visual Studio workspace cache and user settings.
- `backend/.venv/`, `backend/.venv311/`: Local Python virtual environments.
- `backend/__pycache__/`: Python bytecode cache.
- `backend/outputs/`: Exported CAD files from CadQuery.
- `frontend/node_modules/`: Node package installation output.
- `solidworks-addin/bin/` and `solidworks-addin/obj/`: Build artifacts for Debug/Release.
- `solidworks-addin/bin/Debug/TextToCad.SolidWorksAddin.dll.config`: Generated config copy from `app.config`.
- `%APPDATA%/TextToCad/logs/` and `%APPDATA%/TextToCad/replay/`: Runtime logs and replay JSONL files.

Appendix B lists artifacts that are safe to delete or regenerate. They are intentionally excluded from source control so the repo stays clean and reproducible across machines. This separation prevents build outputs and local caches from polluting the repo, and it ensures that a fresh clone can be brought to a known state by following Section 1. If an unexpected behavior appears, clearing these artifacts is often the quickest way to rule out stale cache or misbuilt binaries.

## Appendix C. API Endpoint Reference (complete list)

- `GET /health`: Health check (returns `{ "status": "ok" }`).
- `GET /`: Root endpoint (basic status).
- `POST /process_instruction`: Parse and store instruction; returns plan + operations.
- `POST /dry_run`: Parse without saving; returns plan + operations.
- `POST /plan`: Planner entry; returns plan + questions + state_id, or operations if complete.
- `GET /plan/{state_id}`: Fetch existing planner state.
- `POST /replay`: Replay JSONL payload execution (for deterministic runbacks).
- `GET /commands`: Return command history from SQLite.
- `GET /config`: Return backend config info (non-sensitive).
- `POST /generate_model`: Build CadQuery model and export (requires CadQuery/OCP).
- `POST /jobs`: Create a background job record.
- `GET /jobs/{job_id}`: Poll job status.

This endpoint list is the complete backend surface area. Keeping it here ensures you can reason about all supported capabilities and confirm that clients are using the correct routes and payloads. It also serves as a quick verification checklist when updating schemas or adding features: if a new endpoint is introduced or an existing one changes, this list should be updated to keep the contract visible and explicit.

## Appendix D. Configuration Reference (backend, frontend, add-in)

Backend environment variables (from `backend/config.py` and `backend/llm.py`)
- `OPENAI_API_KEY`: Enables AI parsing when set.
- `OPENAI_MODEL`: Model name (default `gpt-4o-mini`).
- `OPENAI_TIMEOUT_S`: OpenAI request timeout seconds (default 20).
- `OPENAI_BASE_URL`: Optional custom OpenAI base URL (local gateway or proxy).
- `OPENAI_ORG`: Optional OpenAI organization ID.
- `OPENAI_PROJECT`: Optional OpenAI project ID.
- `OPENAI_SYSTEM_PROMPT`: Optional override for AI system prompt.
- `OPENAI_SYSTEM_PROMPT_PATH`: Optional path to prompt file.
- `DEBUG`: Enable debug logging.
- `LOG_LEVEL`: Log level (info, debug, etc.).
- `USE_LLM`: Global LLM toggle (optional).
- `OPENAI_MAX_TOKENS`: Max tokens for AI parsing.
- `DATABASE_URL`: Database connection (default sqlite).
- `DB_ECHO`: SQLAlchemy echo logging.
- `API_HOST`: Backend host binding.
- `API_PORT`: Backend port.
- `CORS_ORIGINS`: Allowed CORS origins.
- `SECRET_KEY`: Reserved for future auth features.
- `ACCESS_TOKEN_EXPIRE_MINUTES`: Reserved for future auth features.

These variables control backend behavior and AI parsing. The defaults are intentionally local-first; you only need to set the OpenAI variables if you want AI parsing or custom prompts. By keeping them centralized, you can reason about how parsing behavior changes between machines and avoid hidden configuration drift. This also makes it easier to replicate bugs: you can share a small list of environment variables alongside a reproduction instruction to get consistent behavior across systems.

Frontend environment variables
- `VITE_API_BASE`: Backend root URL (example `http://localhost:8000`).

The frontend has a single required configuration to avoid confusion. Pointing at the backend root keeps all API routes consistent across environments. This reduces the chance of pointing at `/docs` or another partial path, which is a common setup error when developers are onboarding.

Add-in `app.config` keys
- `ApiBaseUrl`: Backend base URL.
- `ApiTimeoutSeconds`: Backend timeout for requests.
- `LogLevel`: Logging level (Debug/Info/Warning/Error).
- `EnableFileLogging`: Toggle file logging.
- `EnableReplayLogging`: Toggle replay JSONL logging.
- `ReplayLogDirectory`: Override replay directory (empty = default).
- `UseWhisper`: Toggle whisper transcription.
- `WhisperCliPath`: Path to `whisper-cli.exe`.
- `WhisperModelPath`: Path to a whisper model file.
- `WhisperLanguage`: Language code for whisper (e.g., `en`).

The add-in config defines all runtime behavior that users commonly want to change without recompiling. Keeping these settings centralized makes troubleshooting much easier because the TaskPane, logger, and replay system all draw from the same source. It also allows the add-in to be deployed across multiple machines with different backend URLs or voice settings without building different binaries, which is essential for testing and staged rollouts.

Appendix D is the configuration quick reference. It exists so you can set up or debug environments without searching through code for default values.

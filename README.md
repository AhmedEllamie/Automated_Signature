# Automated Signature System (Plotter + A4 Scanner)

This repository is a combined system where both services work together:

- `plotter_signature` (root): print/signature automation service.
- `a4-flating/`: document scanner service that captures, rectifies, and validates scanned pages.

The intended production flow is:

1. Plotter/Flask starts a scan request.
2. Scanner service captures and processes an image.
3. Plotter fetches the scan result and continues print/signature workflow.

## Repository Layout

```text
.
|-- plotter_signature/                 # Main plotter package (root project)
|-- docs/                              # Plotter docs + scanner integration docs
|-- deploy/                            # Plotter deployment files
|-- appsettings.json                   # Plotter runtime defaults
|-- requirements.txt                   # Plotter dependencies
|-- pyproject.toml
|-- a4-flating/                        # Scanner subtree project
|   |-- main.py                        # Scanner app entrypoint
|   |-- run_scanner_service.py         # Scanner HTTP service launcher
|   |-- scanner/
|   |-- scanner_service/
|   |-- requirements.txt
|   |-- AUTOMATION_INTEGRATION.md
|   `-- README.md
`-- README.md
```

## How The Two APIs Work Together

### Main integration contract

- Plotter side calls scanner HTTP APIs.
- Scanner runs as a separate local service (default `127.0.0.1:8008`).
- Plotter can call manual config + capture endpoints, then download PNG result.

Reference docs:

- `docs/FLASK_SCANNER_HTTP_INTEGRATION.md`
- `a4-flating/AUTOMATION_INTEGRATION.md`

### Typical API sequence

1. `POST /session/focus-mode` (optional focus mode setup)
2. `POST /session/focus-adjust` (optional focus adjustment)
3. `POST /session/quad-points` (manual 4 points) or `POST /session/manual-config`
4. `POST /capture/start` (or legacy `POST /jobs`)
5. Poll `GET /capture/{capture_id}/status` (or `GET /jobs/{job_id}`)
6. Download result `GET /capture/{capture_id}/result` (or `GET /jobs/{job_id}/image`)

## Prerequisites

- Python 3.10+
- Windows/Linux
- Camera connected for scanner
- Optional OCR support (Tesseract) for scanner OCR readability mode

Windows OCR install (optional):

```powershell
winget install --id tesseract-ocr.tesseract --accept-source-agreements --accept-package-agreements
```

## Setup (Single Environment For Full System)

From repo root:

### Windows (PowerShell)

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
pip install -r .\a4-flating\requirements.txt
pip install -e .
```

### Linux / macOS

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
pip install -r ./a4-flating/requirements.txt
pip install -e .
```

## Run The Complete System

Run in two terminals (same venv):

### Terminal 1 - Scanner HTTP Service

```bash
python a4-flating/run_scanner_service.py
```

Defaults:

- Host: `127.0.0.1`
- Port: `8008`

Optional env vars:

- `SCANNER_SERVICE_HOST`
- `SCANNER_SERVICE_PORT`
- `SCANNER_SERVICE_TOKEN`

### Terminal 2 - Plotter Flask UI + API

```bash
python -m plotter_signature serve-flask --host 0.0.0.0 --port 5001
```

Open:

- `http://localhost:5001/`
- `http://localhost:5001/configuration`

Alternative Plotter API-only mode:

```bash
python -m plotter_signature serve-api --host 0.0.0.0 --port 5000
```

## Plotter Configuration

Configure `appsettings.json` at repo root:

```json
{
  "Printer": { "ComPort": "COM5", "BaudRate": 250000 },
  "PrintRetry": { "MaxRetries": 3, "RetryDelayMs": 1000 },
  "ApprovalService": {
    "Endpoint": "",
    "ApiKey": "",
    "TimeoutSeconds": 30,
    "UseMockService": true
  }
}
```

API auth:

- Server env: `PLOTTER_API_KEY=<secret>`
- Client header: `X-API-Key: <secret>`

## Scanner Configuration

Scanner core config file:

- `a4-flating/scanner/config.py`

Common scanner env vars:

- `SCAN_CAMERA_INDEX`
- `SCAN_CAMERA_BACKEND`
- `SCAN_CAMERA_FOURCC`
- `SCAN_UPLOAD_URL`
- `SCAN_UPLOAD_TOKEN`
- `SCAN_CAPTURE_RESET_URL`
- `SCAN_CAPTURE_RESET_TOKEN`
- `SCAN_UNREADABLE_NOTIFY_URL`
- `SCAN_UNREADABLE_NOTIFY_TOKEN`

If `SCANNER_SERVICE_TOKEN` is enabled, send one of:

- `Authorization: Bearer <token>`
- `X-Scanner-Token: <token>`

## Validation / Smoke Test

1. Start scanner service.
2. Check health:
   - `GET http://127.0.0.1:8008/health`
3. Start Plotter Flask app.
4. Trigger scanner capture from plotter workflow.
5. Confirm scanner returns result and plotter receives/uses it.

## Useful Commands

Plotter CLI help:

```bash
python -m plotter_signature --help
```

Scanner standalone UI mode (without plotter):

```bash
python a4-flating/main.py
```

Scanner service alternative launcher:

```bash
cd a4-flating
python -m scanner_service
```

## Documentation Map

Plotter docs:

- `docs/TECHNICAL_DOCUMENTATION.md`
- `docs/UBUNTU_RELEASE_GUIDE.md`
- `docs/api-pre-security/README.md`
- `docs/doxygen/README.md`

Integration docs:

- `docs/FLASK_SCANNER_HTTP_INTEGRATION.md`
- `a4-flating/AUTOMATION_INTEGRATION.md`
- `a4-flating/README.md`

## Troubleshooting

- **Scanner not reachable:** confirm scanner service is running on `127.0.0.1:8008`.
- **Auth failures:** verify `PLOTTER_API_KEY` and scanner token headers.
- **No camera frame:** check camera index/backend in scanner config.
- **OCR check fails:** install Tesseract or switch scanner readability mode to `fast`.
- **Python import errors:** ensure venv is active and `pip install -e .` has run.

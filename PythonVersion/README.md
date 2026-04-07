# PythonVersion

Python port of the selected printer workflow files from the C# project.

## What is included

- Printer contract and implementation (`IPrinterService`, `PrinterService`)
- SVG to G-code converter (`SvgConverter`) with path parsing and curve flattening
- Printer API controller parity via FastAPI routes
- Dependency wiring module
- Printer settings model
- Print-approval workflow service
- In-memory request-log store (mock parity, no database)
- CLI entrypoint (`main.py`) for project operations

## Folder layout

- `api/` - FastAPI app and printer routes
- `models/` - DTOs/contracts/settings
- `services/printer/` - serial printer + SVG converter
- `services/approval/` - mock approval service
- `services/print_approval/` - approval orchestration
- `stores/` - in-memory request log backend
- `main.py` - CLI

## Install

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r PythonVersion/requirements.txt
```

On Windows PowerShell:

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r PythonVersion\requirements.txt
```

## Run API

```bash
python -m PythonVersion.main serve-api --host 0.0.0.0 --port 5000
```

Routes are available under `/printer`, for example:

- `POST /printer/connect`
- `POST /printer/disconnect`
- `GET /printer/status`
- `POST /printer/generate`
- `POST /printer/print`
- `POST /printer/print/bulk`
- `POST /printer/print-with-approval`
- `GET /printer/requests/{request_id}`

## Run CLI

```bash
python -m PythonVersion.main --help
```

Examples:

```bash
python -m PythonVersion.main connect --com-port /dev/ttyUSB0 --baud-rate 250000
python -m PythonVersion.main status
python -m PythonVersion.main generate --svg ./signature.svg --print-request-json '{"scale":1,"rotation":0,"invertY":true}'
python -m PythonVersion.main print --svg ./signature.svg --print-request-json ./print_request.json --auto-connect
python -m PythonVersion.main bulk-print --svg ./signature.svg --print-request-json ./print_request.json --copies 3 --auto-connect
python -m PythonVersion.main distance-stats
python -m PythonVersion.main reset-distance
python -m PythonVersion.main set-pen-max-distance --meters 2.5
python -m PythonVersion.main pen-change-start --auto-connect
python -m PythonVersion.main pen-change-finish --auto-connect
python -m PythonVersion.main print-with-approval --signature-svg ./signature.svg --request-json ./approval_request.json --paper-image ./paper.jpg --auto-connect
python -m PythonVersion.main get-request --request-id 11111111-1111-1111-1111-111111111111
```

`generate` now includes `svgTotalDistanceMm`, and print responses include:
- `svg_total_distance_mm` (planned SVG travel)
- `executed_distance_mm` (actual executed SVG travel)
- `execution_percent` (actual/plan, excludes reset/eject commands)
- `cumulative_distance_mm` (persisted total across jobs, reset with `reset-distance`)
- `status` includes `remaining_pen_percent`, computed as:
  - `((max_pen_distance_m - (cumulative_distance_mm / 1000)) / max_pen_distance_m) * 100`

## Linux Ubuntu notes

- Serial ports are typically `/dev/ttyUSB0` or `/dev/ttyACM0` (not `COM5`).
- Add your user to `dialout` so Python can access serial devices:

```bash
sudo usermod -aG dialout $USER
newgrp dialout
```

- Confirm the printer device:

```bash
ls /dev/ttyUSB* /dev/ttyACM*
```

- Update port settings in command arguments or in config loading.

## Config source

`dependency_injection.py` attempts to read defaults from `UUNATEK.API/appsettings.json` if present, then builds Python service instances from those values.

## Current parity scope

- Approval behavior uses mock approval service.
- Request logs are stored in memory only (no SQL persistence).
- Core printer protocol and SVG conversion logic follow the C# implementation.


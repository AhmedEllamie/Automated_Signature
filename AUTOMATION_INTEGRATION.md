# Automatic Paper Scanner Integration Guide

This project now supports fully automatic scanning:

- detects the paper in live camera mode
- flattens (perspective-corrects) it
- checks readability
- if readable: saves locally and/or uploads
- if unreadable: can send an API request to ask for recapture

No `S` key is required.

## 1) Quick Start

1. Install dependencies:

```bash
pip install -r requirements.txt
```

2. Optional (for OCR readability mode):
- Install Tesseract OCR and set `tesseract_cmd` if not in PATH.

3. Run webcam mode:

```bash
python main.py
```

The scanner starts in `AUTO` mode and captures automatically when the page is stable.

## 2) Automatic Workflow

For each stable detected document:

1. Detect page quad.
2. Warp to A4-like top-down output.
3. Run readability gate.
4. If readable:
   - save file to `output/` (or upload from memory depending config)
5. If unreadable:
   - skip save
   - optionally call unreadable notification API

To avoid duplicates, auto-capture rearms only after the page disappears for a short period.

## 3) Key Config Options

Edit `scanner/config.py`:

- `auto_capture_enabled`: enable/disable fully automatic capture.
- `auto_capture_stable_frames`: frames required before auto-capture.
- `auto_capture_cooldown_seconds`: minimum delay between capture attempts.
- `auto_rearm_missing_frames`: frames without document before next capture is allowed.
- `enable_readability_check`: run readability verification.
- `require_readable_to_save`: block save/upload when unreadable.
- `upload_enabled`, `upload_url`, `upload_token`: readable-scan upload target.
- `upload_from_memory`: upload without creating local file first.
- `unreadable_notify_enabled`, `unreadable_notify_url`, `unreadable_notify_token`: API request when unreadable.

## 4) CLI Overrides

You can set API endpoints from command line:

```bash
python main.py \
  --upload-url "https://api.example.com/upload" \
  --upload-token "YOUR_UPLOAD_TOKEN" \
  --unreadable-notify-url "https://api.example.com/scan/unreadable" \
  --unreadable-notify-token "YOUR_NOTIFY_TOKEN"
```

## 5) API Contract

### 5.1 Readable scan upload

- Method: `POST`
- Content: `multipart/form-data`
- Field name: `file` (configurable by `upload_field_name`)
- File type: `image/png`
- Auth: `Authorization: Bearer <token>` (optional)

### 5.2 Unreadable notification request

- Method: `POST`
- Content: JSON
- Auth: `Authorization: Bearer <token>` (optional)

Payload example:

```json
{
  "event": "scan_unreadable",
  "detector_confidence": 0.83,
  "readability_confidence": 21.4,
  "readability_tokens": 1,
  "reason": "Low readability",
  "action": "recapture_requested"
}
```

## 6) Integration Patterns

### Pattern A: Save locally + process later

- Keep `upload_enabled = False`.
- Watch `output/` from another service and process new files.

### Pattern B: Direct backend upload

- Set `upload_enabled = True`.
- Use `upload_from_memory = True` for stateless operation.

### Pattern C: Feedback loop to external system

- Configure `unreadable_notify_url`.
- Your backend receives unreadable events and can trigger:
  - UI message to user
  - buzzer/light feedback
  - retry instruction

## 7) Minimal Backend Example (FastAPI)

```python
from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse

app = FastAPI()

@app.post("/upload")
async def upload(file: UploadFile = File(...)):
    content = await file.read()
    # TODO: store/process image
    return {"ok": True, "bytes": len(content), "filename": file.filename}

@app.post("/scan/unreadable")
async def unreadable_event(payload: dict):
    # TODO: trigger recapture workflow in your system
    return JSONResponse({"ok": True, "received": payload})
```

## 8) Notes for Embedding in Another Project

- Run this scanner as a standalone process and integrate through HTTP APIs.
- Or import `main.py` logic and call `run_webcam(cfg)` from your own app.
- Keep scan thresholds configurable per camera/lighting setup.
- Start with `readability_mode = "fast"` on low-power devices, switch to `"ocr"` when possible.

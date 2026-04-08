# Flask to Scanner HTTP Integration

This guide explains how a separate Flask system can integrate with this scanner service using async jobs.

## 1) Start scanner service

Run from scanner repo:

```bash
python run_scanner_service.py
```

Optional environment variables:

- `SCANNER_SERVICE_HOST` (default: `127.0.0.1`)
- `SCANNER_SERVICE_PORT` (default: `8008`)
- `SCANNER_SERVICE_TOKEN` (optional bearer token)

Base URL example:

`http://127.0.0.1:8008`

## 2) Required request sequence

1. Configure manual focus + 4 points:
   - `POST /session/manual-config`
2. Create capture job:
   - `POST /jobs`
3. Poll job status:
   - `GET /jobs/{job_id}`
4. Download rectified image:
   - `GET /jobs/{job_id}/image`

## 3) API details

### Live stream for accurate 4-point selection

- `GET /stream.mjpg`
- Returns MJPEG stream (`multipart/x-mixed-replace`)
- Use this in your UI to let user click exact corner points from live camera view.
- Query params:
  - `fps` (default `10`, range `1..25`)
  - `width` (default `0` = original width)
  - `fisheye` (`1` default, set `0` to disable undistort in stream)

Important behavior:

- Stream holds camera lock while connected.
- If a capture job is running, stream returns `409 camera_busy`.
- While stream is open, capture jobs also wait/fail with busy behavior depending caller timing.
- Recommended: close stream before `POST /jobs`.

### Health

- `GET /health`
- Response:

```json
{"ok": true, "status": "ready"}
```

### Set manual config

- `POST /session/manual-config`
- Body:

```json
{
  "autofocus_enabled": false,
  "manual_focus_value": 35,
  "quad_points": [[100, 120], [1700, 130], [1710, 980], [120, 990]]
}
```

- Success response:

```json
{
  "ok": true,
  "manual_config": {
    "autofocus_enabled": false,
    "manual_focus_value": 35.0,
    "quad_points": [[100.0, 120.0], [1700.0, 130.0], [1710.0, 980.0], [120.0, 990.0]],
    "frame_width": 1920,
    "frame_height": 1080,
    "valid": true,
    "validation_message": "ok",
    "updated_at": "2026-01-01T00:00:00+00:00"
  }
}
```

### Create job

- `POST /jobs`
- Body:

```json
{
  "mode": "manual",
  "readability_required": true,
  "timeout_seconds": 15
}
```

- Response (`202`):

```json
{
  "ok": true,
  "job": {
    "job_id": "uuid",
    "mode": "manual",
    "status": "queued",
    "created_at": "2026-01-01T00:00:00+00:00",
    "started_at": null,
    "finished_at": null,
    "error": null,
    "detail": null,
    "metadata": {}
  }
}
```

### Poll job

- `GET /jobs/{job_id}`
- Terminal statuses: `succeeded`, `failed`

### Download image

- `GET /jobs/{job_id}/image`
- Returns `image/png` on success
- Returns `409` if job not ready or failed

## 4) Authentication

If `SCANNER_SERVICE_TOKEN` is set, include either:

- `Authorization: Bearer <token>`
- or `X-Scanner-Token: <token>`

`/health` is public.

## 5) Flask example (requests)

```python
import time
import requests

BASE = "http://127.0.0.1:8008"
TOKEN = "your-token"
HEADERS = {"Authorization": f"Bearer {TOKEN}"}

# 1) configure manual mode
cfg = {
    "autofocus_enabled": False,
    "manual_focus_value": 35,
    "quad_points": [[100, 120], [1700, 130], [1710, 980], [120, 990]],
}
r = requests.post(f"{BASE}/session/manual-config", json=cfg, headers=HEADERS, timeout=10)
r.raise_for_status()

# 2) create job
r = requests.post(
    f"{BASE}/jobs",
    json={"mode": "manual", "readability_required": True, "timeout_seconds": 15},
    headers=HEADERS,
    timeout=10,
)
r.raise_for_status()
job_id = r.json()["job"]["job_id"]

# 3) poll status
while True:
    r = requests.get(f"{BASE}/jobs/{job_id}", headers=HEADERS, timeout=10)
    r.raise_for_status()
    job = r.json()["job"]
    if job["status"] in ("succeeded", "failed"):
        break
    time.sleep(0.4)

if job["status"] != "succeeded":
    raise RuntimeError(f"Capture failed: {job.get('error')} - {job.get('detail')}")

# 4) download rectified PNG
r = requests.get(f"{BASE}/jobs/{job_id}/image", headers=HEADERS, timeout=15)
r.raise_for_status()
with open("rectified.png", "wb") as f:
    f.write(r.content)
```

## 6) Browser/UI stream usage

Simple HTML:

```html
<img src="http://127.0.0.1:8008/stream.mjpg?fps=12&width=1280" />
```

If token is required, browsers cannot easily set custom auth headers for `<img>`.
Use one of these patterns:

1. Same-network trusted setup without token for local service
2. Backend proxy endpoint in Flask that forwards stream with auth header

Example Flask proxy:

```python
from flask import Response, stream_with_context
import requests

@app.get("/scanner/live")
def scanner_live():
    upstream = requests.get(
        "http://127.0.0.1:8008/stream.mjpg?fps=12&width=1280",
        headers={"Authorization": "Bearer your-token"},
        stream=True,
        timeout=30,
    )
    upstream.raise_for_status()
    return Response(
        stream_with_context(upstream.iter_content(chunk_size=8192)),
        content_type=upstream.headers.get("Content-Type", "multipart/x-mixed-replace; boundary=frame"),
    )
```
## 7) Built-in helper modules

This repo also provides:

- `scanner_service/client.py`: reusable Python client
- `scanner_service/flask_bridge.py`: Flask blueprint adapter

## 8) Flask UI bridge endpoints (this project)

The Flask UI in `PythonVersion/flask_app` now exposes scanner proxy endpoints:

- `GET /api/scanner/stream.mjpg`
  - Proxies scanner `GET /stream.mjpg`.
  - Supports query params: `fps`, `width`, `fisheye`.
  - Used by Configuration page "Show stream" button for accurate 4-point setup.

- `POST /api/scanner/capture-manual`
  - Body must include:
    - `autofocus_enabled` (bool)
    - `manual_focus_value` (number)
    - `quad_points` (array of 4 `[x,y]` points)
  - Server flow:
    1. Calls scanner `POST /session/manual-config`
    2. Calls scanner `POST /jobs` with manual mode
    3. Polls scanner `GET /jobs/{job_id}` until terminal status
    4. Downloads scanner `GET /jobs/{job_id}/image`
    5. Stores image as latest capture (`/api/capture/latest/image`)
  - Response includes `imageUrl` for the main dashboard preview.


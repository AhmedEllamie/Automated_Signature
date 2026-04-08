# Adaptive A4 Document Scanner & Perspective Rectifier

Python-based computer vision project for detecting skewed A4 paper from a live webcam feed and converting it into a clean, top-down scan using perspective correction.

## Why This Project

Flatbed scanners are not always available, and mobile captures often produce tilted, distorted images. This project solves that by automatically detecting a document's four corners and applying a homography transform to generate a rectified A4-like output in real time.

## Core Objectives

- Automatic rectification of camera tilt and keystone distortion.
- Hybrid workflow with auto-detection and manual corner override.
- Real-time preview so the user can align paper before saving.

## Technical Approach

### 1) Frame Acquisition

- Capture frames from a UVC-compatible USB camera.
- Keep camera fixed for stable geometry and repeatable results.

### 2) Segmentation and Edge Extraction

- Convert frame to grayscale.
- Apply thresholding to isolate paper from a dark background.
- Use edge detection (for example Canny) to extract boundaries.

### 3) Contour and Corner Detection

- Find candidate contours.
- Approximate polygon with `approxPolyDP`.
- Select a 4-point contour representing the document.

### 4) Corner Ordering

- Sort corners into a consistent order:
  - Top-left
  - Top-right
  - Bottom-right
  - Bottom-left
- Stable ordering guarantees correct perspective mapping.

### 5) Perspective Rectification

- Compute a 3x3 homography matrix.
- Warp the source quadrilateral to a target A4 ratio (1:1.414).
- Produce a flat, readable scan.

### 6) Final Enhancement (optional)

- Apply gamma correction to gently lift mid-tones.
- Apply CLAHE on LAB luminance channel for local contrast recovery.
- Boost saturation to improve color separation on stamps/highlights.
- Apply sharpening to improve text edge clarity.

## Features

- Auto-Detect mode for fast operation.
- Manual mode for damaged sheets or difficult lighting.
- Real-time corrected preview.
- Save-ready output for archiving or downstream OCR.

## Ideal Operating Conditions

- Background: matte black, non-reflective.
- Lighting: uniform overhead light, minimal shadows.
- Hardware: standard USB webcam (UVC).
- Camera: fixed height and angle.

## Camera resolution and flattened output quality

- The **live stream** is whatever your driver delivers after OpenCV sets width/height (and optional **MJPEG** `camera_fourcc`). If the console prints something like `1920x1080` while you asked for `3840x2160`, the camera or USB bandwidth did not accept full 4K — fix drivers, cable, or try `camera_fourcc = "MJPG"`.
- **Preview windows** are resized only for display (`max_display_width` / `max_display_height`). They do **not** reduce the resolution of the saved rectified image.
- **Flattened image sharpness** is mainly limited by (1) real capture resolution, (2) `warp_short_side` / `scale_warp_to_capture` / `warp_capture_scale`, and (3) `warp_interpolation` (`cubic` or `lanczos4` is sharper than `linear`, but slower).
- On **Orange Pi**, lower `frame_width` / `frame_height`, set `scale_warp_to_capture = False`, use a smaller `warp_short_side`, and `warp_interpolation = "linear"`.

## Project Structure

Current layout:

```text
.
|-- main.py
|-- scanner/
|   |-- __init__.py
|   |-- config.py
|   |-- preprocess.py
|   |-- detect.py
|   |-- geometry.py
|   |-- warp.py
|   `-- ui.py
|-- output/
|-- requirements.txt
`-- README.md
```

## Installation

1. Create and activate a virtual environment:

```bash
python -m venv .venv
```

On Windows PowerShell:

```bash
.venv\Scripts\Activate.ps1
```

2. Install dependencies:

```bash
pip install -r requirements.txt
```

### Windows OCR dependency (for readability module)

Install Tesseract OCR engine:

```bash
winget install --id tesseract-ocr.tesseract --accept-source-agreements --accept-package-agreements
```

If `tesseract` is not in PATH, pass it explicitly:

```bash
python main.py --image "C:\path\to\your_photo.jpg" --verify-readable --tesseract-cmd "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

### Ubuntu (Orange Pi) setup

Install system packages:

```bash
sudo apt update
sudo apt install -y python3 python3-venv python3-pip tesseract-ocr libgl1 libglib2.0-0 v4l-utils
```

Create environment and install Python dependencies:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

Ubuntu release/deployment guide (systemd + env file + verification):

- `UBUNTU_RELEASE_GUIDE.md`
- `deploy/ubuntu/scanner-service.service`
- `deploy/ubuntu/a4-scanner.env.example`

Linux-friendly camera defaults are now automatic:

- `camera_index` defaults to `0` on Linux.
- `camera_backend` defaults to empty string on Linux (V4L2/CAP_ANY path).
- You can override with env vars: `SCAN_CAMERA_INDEX`, `SCAN_CAMERA_BACKEND`, `SCAN_CAMERA_FOURCC`.

If you do not yet have a requirements file, typical packages are:

```bash
pip install opencv-python numpy
```

## Usage

Run the scanner script:

```bash
python main.py
```

### Async HTTP scanner service (Option A)

Run scanner as a standalone local service:

```bash
python run_scanner_service.py
```

or:

```bash
python -m scanner_service
```

Default bind is `127.0.0.1:8008`. Override with:

- `SCANNER_SERVICE_HOST`
- `SCANNER_SERVICE_PORT`
- `SCANNER_SERVICE_TOKEN` (optional bearer token for all endpoints except `/health`)

Service endpoints:

- `GET /health`
- `GET /session/manual-config`
- `POST /session/manual-config` (set autofocus/manual-focus + 4-point quad)
- `POST /jobs` (enqueue manual capture job)
- `GET /jobs/{job_id}`
- `GET /jobs/{job_id}/image`

Manual async flow:

1. POST manual config (focus + 4 points)
2. POST job (`mode=manual`)
3. Poll job status
4. Download rectified PNG

There is also a reusable HTTP client in `scanner_service/client.py` and a Flask bridge blueprint in `scanner_service/flask_bridge.py` for integration from another repo.

In webcam mode, scanning is fully automatic by default:
- detect page
- flatten page
- readability gate
- save/upload if readable
- no REST API required unless you explicitly enable upload/notify/reset endpoints

Run on a saved image (single-shot validation):

```bash
python main.py --image "C:\path\to\your_photo.jpg"
```

Disable local file saving (rectified image is processed/uploaded but not written to `output/`):

```bash
python main.py --save-local false
```

Disable camera autofocus (optional) and set manual focus value:

```bash
python main.py --autofocus off --manual-focus 30
```

Ubuntu/Orange Pi headless image mode (no windows):

```bash
python3 main.py --image ./Image_1.jpeg --no-gui
```

In image mode, the app:
- detects the page once,
- shows input + rectified result windows,
- saves the rectified output into `output/`,
- prints confidence and saved output path.

Run readability verification (OCR-based):

```bash
python main.py --image "C:\path\to\your_photo.jpg" --verify-readable
```

Readability modes (in `scanner/config.py`):

- `readability_mode = "fast"`: lightweight, no OCR engine, best for Orange Pi.
- `readability_mode = "ocr"`: uses Tesseract OCR, more accurate but heavier.

Recommended Orange Pi setup:

- `enable_readability_check = True`
- `require_readable_to_save = True`
- `readability_mode = "fast"`

Make saving conditional on readability (set in config):

- In `scanner/config.py`:
  - `enable_readability_check = True`
  - `require_readable_to_save = True`

When enabled, the app will not save a flattened image unless OCR marks it as readable.

Upload flattened image to API (optional cleanup):

```bash
python main.py --image "C:\path\to\your_photo.jpg" --upload-url "https://your-api.example/upload"
```

Upload with bearer token:

```bash
python main.py --image "C:\path\to\your_photo.jpg" --upload-url "https://your-api.example/upload" --upload-token "YOUR_TOKEN"
```

You can also configure upload through environment variables:

```bash
SCAN_UPLOAD_URL=https://your-api.example/upload
SCAN_UPLOAD_TOKEN=YOUR_TOKEN
```

Then run normally (without `--upload-url`), after setting:
- `upload_enabled=True` in code config, or
- passing `--upload-url`.

Upload storage strategy (in `scanner/config.py`):
- `upload_from_memory = True`: does not create an output file on successful upload (saves to `output/` only if upload fails and `save_on_upload_fail=True`).
- `upload_from_memory = False`: saves to disk, uploads from the saved file, then deletes it if `delete_after_upload_success=True`.
- `save_on_upload_fail = True`: keeps a local copy in case the upload fails (useful for debugging).

Keyboard controls:

- `a`: switch to Auto mode
- `m`: switch to Manual mode and enter FOCUS step
- `n`: in Manual mode, switch to POINTS step (4-corner selection)
- `p`: in Manual mode, return to FOCUS step
- `s`: save current rectified frame (with enhancement if enabled)
- `r`: reset manual points
- `f`: toggle camera autofocus on/off (press again to return to auto)
- `-`: focus in (near), in Auto or Manual FOCUS step
- `+`: focus out (far), in Auto or Manual FOCUS step
- `1` / `2`: fallback focus in/out keys for keyboard layouts where +/- are hard to type
- `q`: quit

Manual workflow:
- Press `m` -> adjust focus with `+/-` (or `1/2`) in FOCUS step.
- Press `n` -> enter POINTS step and click 4 corners.
- Press `s` -> save warped result.
- Press `s` again in Manual mode to save again (no capture reset required in Manual mode).
- Press `p` any time in Manual mode to return to FOCUS step.

Auto capture behavior is configurable in `scanner/config.py`:
- `auto_capture_enabled`
- `auto_capture_stable_frames`
- `single_capture_until_api_reset` (default `True`; one readable save per cycle, then lock)
- `capture_reset_url`
- `capture_reset_poll_interval_seconds`

Camera focus behavior is configurable in `scanner/config.py`:
- `camera_autofocus_enabled`
- `camera_manual_focus`
- `camera_focus_step`

Enhancement behavior is configurable in `scanner/config.py`:
- `apply_scan_enhancement`
- `enhance_gamma`
- `enhance_clahe_clip_limit`
- `enhance_clahe_tile_size`
- `enhance_saturation_boost`
- `enhance_sharpen_strength`

Output orientation behavior is configurable in `scanner/config.py`:
- `auto_rotate_landscape_to_portrait` (default `True`)
- `landscape_rotation_direction` (`"ccw"` or `"cw"`)

If `capture_reset_url` is empty, lock is cleared manually with `r`.

Reset API + unreadable notify API can be configured by CLI:

```bash
python main.py --capture-reset-url "https://your-api.example/scan/reset" --capture-reset-token "YOUR_TOKEN" --unreadable-notify-url "https://your-api.example/scan/unreadable" --unreadable-notify-token "YOUR_TOKEN"
```

Or by environment variables:

```bash
SCAN_CAPTURE_RESET_URL=https://your-api.example/scan/reset
SCAN_CAPTURE_RESET_TOKEN=YOUR_TOKEN
SCAN_UNREADABLE_NOTIFY_URL=https://your-api.example/scan/unreadable
SCAN_UNREADABLE_NOTIFY_TOKEN=YOUR_TOKEN
```

For full integration details, see `AUTOMATION_INTEGRATION.md`.

## Input -> Output Pipeline

```text
Camera Frame
  -> Grayscale + Threshold
  -> Edge Detection
  -> Contour Selection (4 points)
  -> Corner Sorting
  -> Homography Warp
  -> Gamma + CLAHE(LAB) + Saturation + Sharpen (optional)
  -> Rectified A4 Output
```

## Evaluation and Metrics

The app prints run metrics when you quit:

- Frames processed
- Elapsed seconds
- Average FPS
- Auto detect hit rate
- Mean confidence

## Known Limitations

- Shiny paper can create highlights and false edges.
- Heavy shadows may break contour detection.
- Busy or bright background reduces segmentation quality.
- Folded/torn corners can reduce corner accuracy.

## Recommended Improvements

- Adaptive thresholding for variable lighting.
- Temporal smoothing across frames for stable corners.
- Confidence scoring for detected quadrilaterals.
- OCR integration (for example `pytesseract`) after rectification.
- Export directly to PDF.
- Support additional paper sizes (A5, Letter, Legal).

## Troubleshooting

- **No paper detected:** improve contrast (darken background, increase light).
- **Wrong orientation:** verify corner ordering logic.
- **Jitter in corners:** keep camera fixed and add frame-to-frame smoothing.
- **Low FPS:** reduce frame resolution and avoid unnecessary copies.
- **Readability check not working:** install Tesseract OCR engine on your OS (the `pytesseract` package alone is not enough).

## Documentation Usage

- Place this file in your repository root as `README.md`.
- For report/presentation slides, reuse:
  - Problem statement from **Why This Project**
  - Method from **Technical Approach**
  - Impact from **Evaluation and Metrics**
  - Future plan from **Recommended Improvements**

## License

Add your preferred license (MIT is common for student projects).


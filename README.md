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

If you do not yet have a requirements file, typical packages are:

```bash
pip install opencv-python numpy
```

## Usage

Run the scanner script:

```bash
python main.py
```

Run on a saved image (single-shot validation):

```bash
python main.py --image "C:\path\to\your_photo.jpg"
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
- `m`: switch to Manual mode
- `r`: reset manual points
- `s`: save current rectified frame
- `q`: quit

## Input -> Output Pipeline

```text
Camera Frame
  -> Grayscale + Threshold
  -> Edge Detection
  -> Contour Selection (4 points)
  -> Corner Sorting
  -> Homography Warp
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


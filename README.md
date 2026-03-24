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

Use this as a reference layout (adjust to your actual files):

```text
.
|-- main.py
|-- scanner/
|   |-- preprocess.py
|   |-- detect.py
|   |-- warp.py
|   `-- utils.py
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

If you do not yet have a requirements file, typical packages are:

```bash
pip install opencv-python numpy
```

## Usage

Run the scanner script:

```bash
python main.py
```

Suggested controls (replace with your real key bindings):

- `a`: switch to Auto mode
- `m`: switch to Manual mode
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

Add measured results from your environment:

- Average processing speed (FPS): `TBD`
- Auto-detection success rate: `TBD`
- Average rectification time per frame: `TBD`
- Output resolution used: `TBD`

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

## Documentation Usage

- Place this file in your repository root as `README.md`.
- For report/presentation slides, reuse:
  - Problem statement from **Why This Project**
  - Method from **Technical Approach**
  - Impact from **Evaluation and Metrics**
  - Future plan from **Recommended Improvements**

## License

Add your preferred license (MIT is common for student projects).


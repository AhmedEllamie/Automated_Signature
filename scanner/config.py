from dataclasses import dataclass
import os


@dataclass
class ScannerConfig:
    camera_index: int = 1
    # Windows: OpenCV may default to a backend that only offers 1080p even when the cam is 4K-capable.
    # If console shows 1920x1080 but you requested 4K, try "MSMF" or "DSHOW" (one often negotiates higher modes).
    camera_backend: str = "DSHOW"
    # Requested capture size (USB cams may fall back if unsupported — check console for actual size).
    frame_width: int = 3840
    frame_height: int = 2160
    # Use MJPEG on many UVC cameras so 4K is possible over USB; set "" to let the driver choose.
    camera_fourcc: str = "MJPG"
    preview_scale: float = 1.0

    gaussian_kernel: int = 5
    canny_low: int = 75
    canny_high: int = 180
    binary_threshold: int = 140

    min_area_ratio: float = 0.12
    max_area_ratio: float = 0.95
    min_edge_px: float = 45.0

    a4_ratio: float = 1.41421356237
    # Output rectified width (short side of A4 portrait target). Higher = sharper file, more CPU/RAM.
    warp_short_side: int = 2200
    # Tie warp output to camera resolution so 4K input is not thrown away (Orange Pi: lower caps / disable).
    # On a PC with 1080p capture, warp_capture_scale=0.58 yields ~626px then clamps to warp_short_side_min (900),
    # so saved scans stay soft unless you raise the scale (e.g. 1.0) or set scale_warp_to_capture=False.
    scale_warp_to_capture: bool = True
    warp_capture_scale: float = 0.58
    warp_short_side_min: int = 900
    warp_short_side_max: int = 4000
    # Perspective resampling: linear (fast), cubic (default), lanczos4 (slowest, often sharpest).
    warp_interpolation: str = "cubic"
    max_display_width: int = 900
    max_display_height: int = 700

    smoothing_alpha: float = 0.24
    confidence_threshold: float = 0.62
    start_mode: str = "AUTO"  # AUTO starts live flattening immediately

    save_dir: str = "output"
    apply_scan_enhancement: bool = False

    # Optional readability verification (OCR based)
    enable_readability_check: bool = True
    readability_mode: str = "ocr"  # "fast" for Orange Pi, "ocr" for Tesseract
    min_readability_confidence: float = 45.0
    tesseract_cmd: str = os.getenv("TESSERACT_CMD", "")
    require_readable_to_save: bool = True

    # Optional API upload config
    upload_enabled: bool = False
    upload_url: str = os.getenv("SCAN_UPLOAD_URL", "")
    upload_token: str = os.getenv("SCAN_UPLOAD_TOKEN", "")
    upload_timeout_seconds: int = 15
    upload_field_name: str = "file"

    # Upload cleanup / storage strategy
    # - upload_from_memory=True: do not write a file to disk unless upload fails (save_on_upload_fail).
    # - upload_from_memory=False: save to disk, upload from file, then optionally delete after successful upload.
    upload_from_memory: bool = True
    delete_after_upload_success: bool = True
    save_on_upload_fail: bool = True


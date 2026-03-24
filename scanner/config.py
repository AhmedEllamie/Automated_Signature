from dataclasses import dataclass
import os


@dataclass
class ScannerConfig:
    camera_index: int = 0
    frame_width: int = 1280
    frame_height: int = 720
    preview_scale: float = 1.0

    gaussian_kernel: int = 5
    canny_low: int = 75
    canny_high: int = 180
    binary_threshold: int = 140

    min_area_ratio: float = 0.12
    max_area_ratio: float = 0.95
    min_edge_px: float = 45.0

    a4_ratio: float = 1.41421356237
    warp_short_side: int = 900
    max_display_width: int = 900
    max_display_height: int = 700

    smoothing_alpha: float = 0.24
    confidence_threshold: float = 0.62
    start_mode: str = "AUTO"  # AUTO starts live flattening immediately

    save_dir: str = "output"
    apply_scan_enhancement: bool = False

    # Optional readability verification (OCR based)
    enable_readability_check: bool = True
    readability_mode: str = "fast"  # "fast" for Orange Pi, "ocr" for Tesseract
    min_readability_confidence: float = 45.0
    tesseract_cmd: str = os.getenv("TESSERACT_CMD", "")
    require_readable_to_save: bool = True

    # Optional API upload config
    upload_enabled: bool = False
    upload_url: str = os.getenv("SCAN_UPLOAD_URL", "")
    upload_token: str = os.getenv("SCAN_UPLOAD_TOKEN", "")
    upload_timeout_seconds: int = 15
    upload_field_name: str = "file"


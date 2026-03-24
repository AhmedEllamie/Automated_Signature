from dataclasses import dataclass


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

    save_dir: str = "output"
    apply_scan_enhancement: bool = False


from __future__ import annotations

import cv2
import numpy as np

from .config import ScannerConfig
from .geometry import edge_lengths


def a4_target_size(short_side: int, a4_ratio: float) -> tuple[int, int]:
    width = int(short_side)
    height = int(short_side * a4_ratio)
    return width, height


def compute_warp_short_side(frame_width: int, frame_height: int, cfg: ScannerConfig) -> int:
    if (
        cfg.scale_warp_to_capture
        and frame_width > 0
        and frame_height > 0
    ):
        s = int(min(frame_width, frame_height) * cfg.warp_capture_scale)
    else:
        s = int(cfg.warp_short_side)
    return max(cfg.warp_short_side_min, min(s, cfg.warp_short_side_max))


def _interpolation_from_config(cfg: ScannerConfig) -> int:
    name = (cfg.warp_interpolation or "cubic").strip().lower()
    mapping = {
        "linear": cv2.INTER_LINEAR,
        "cubic": cv2.INTER_CUBIC,
        "lanczos4": cv2.INTER_LANCZOS4,
        "lanczos": cv2.INTER_LANCZOS4,
    }
    return mapping.get(name, cv2.INTER_CUBIC)


def _source_quad_for_portrait_output(quad: np.ndarray, cfg: ScannerConfig | None) -> np.ndarray:
    q = quad.astype(np.float32)
    if cfg is None or not getattr(cfg, "auto_rotate_landscape_to_portrait", False):
        return q

    top, right, bottom, left = edge_lengths(q)
    horizontal = (top + bottom) * 0.5
    vertical = (right + left) * 0.5

    # Already portrait-like in the source view: keep original orientation.
    if horizontal <= vertical * 1.02:
        return q

    direction = (getattr(cfg, "landscape_rotation_direction", "ccw") or "ccw").strip().lower()
    if direction in {"cw", "clockwise", "right"}:
        # Make left edge become the top edge in output.
        return np.roll(q, 1, axis=0).astype(np.float32)  # [bl, tl, tr, br]
    # Default: make right edge become the top edge in output.
    return np.roll(q, -1, axis=0).astype(np.float32)  # [tr, br, bl, tl]


def warp_document(
    frame: np.ndarray,
    quad: np.ndarray,
    dst_size: tuple[int, int],
    cfg: ScannerConfig | None = None,
) -> np.ndarray:
    width, height = dst_size
    src_quad = _source_quad_for_portrait_output(quad, cfg)
    dst = np.array(
        [[0, 0], [width - 1, 0], [width - 1, height - 1], [0, height - 1]],
        dtype=np.float32,
    )
    matrix = cv2.getPerspectiveTransform(src_quad, dst)
    interp = _interpolation_from_config(cfg) if cfg is not None else cv2.INTER_LINEAR
    warped = cv2.warpPerspective(
        frame,
        matrix,
        (width, height),
        flags=interp,
        borderMode=cv2.BORDER_REPLICATE,
    )
    return warped


def enhance_for_scan(warped: np.ndarray) -> np.ndarray:
    gray = cv2.cvtColor(warped, cv2.COLOR_BGR2GRAY)
    enhanced = cv2.adaptiveThreshold(
        gray,
        255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY,
        31,
        9,
    )
    return cv2.cvtColor(enhanced, cv2.COLOR_GRAY2BGR)


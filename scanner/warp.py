from __future__ import annotations

import cv2
import numpy as np


def a4_target_size(short_side: int, a4_ratio: float) -> tuple[int, int]:
    width = int(short_side)
    height = int(short_side * a4_ratio)
    return width, height


def warp_document(frame: np.ndarray, quad: np.ndarray, dst_size: tuple[int, int]) -> np.ndarray:
    width, height = dst_size
    dst = np.array(
        [[0, 0], [width - 1, 0], [width - 1, height - 1], [0, height - 1]],
        dtype=np.float32,
    )
    matrix = cv2.getPerspectiveTransform(quad.astype(np.float32), dst)
    warped = cv2.warpPerspective(frame, matrix, (width, height))
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


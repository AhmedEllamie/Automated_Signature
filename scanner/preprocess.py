from __future__ import annotations

import cv2
import numpy as np

from .config import ScannerConfig


def to_gray(frame: np.ndarray) -> np.ndarray:
    return cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)


def make_edge_mask(frame: np.ndarray, cfg: ScannerConfig) -> np.ndarray:
    gray = to_gray(frame)
    blur = cv2.GaussianBlur(gray, (cfg.gaussian_kernel, cfg.gaussian_kernel), 0)
    edges = cv2.Canny(blur, cfg.canny_low, cfg.canny_high)
    return edges


def make_binary_mask(frame: np.ndarray, cfg: ScannerConfig) -> np.ndarray:
    gray = to_gray(frame)
    _, mask = cv2.threshold(gray, cfg.binary_threshold, 255, cv2.THRESH_BINARY)
    return mask


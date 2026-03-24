from __future__ import annotations

from dataclasses import dataclass
import os

import cv2
import numpy as np

try:
    import pytesseract
except Exception:  # pragma: no cover - optional dependency
    pytesseract = None


@dataclass
class ReadabilityResult:
    readable: bool
    mean_confidence: float
    token_count: int
    message: str


def verify_readability(
    image: np.ndarray,
    min_confidence: float = 45.0,
    tesseract_cmd: str = "",
) -> ReadabilityResult:
    if pytesseract is None:
        return ReadabilityResult(
            readable=False,
            mean_confidence=0.0,
            token_count=0,
            message="pytesseract not installed. Install it to enable readability checks.",
        )

    if tesseract_cmd:
        pytesseract.pytesseract.tesseract_cmd = tesseract_cmd
    elif os.name == "nt":
        default_win_cmd = r"C:\Program Files\Tesseract-OCR\tesseract.exe"
        if os.path.exists(default_win_cmd):
            pytesseract.pytesseract.tesseract_cmd = default_win_cmd

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    try:
        ocr_data = pytesseract.image_to_data(gray, output_type=pytesseract.Output.DICT)
    except Exception as exc:
        return ReadabilityResult(
            readable=False,
            mean_confidence=0.0,
            token_count=0,
            message=f"OCR unavailable: {exc}",
        )

    confidences: list[float] = []
    tokens: list[str] = []
    for i, txt in enumerate(ocr_data["text"]):
        txt = (txt or "").strip()
        if not txt:
            continue
        try:
            conf = float(ocr_data["conf"][i])
        except Exception:
            conf = -1.0
        if conf >= 0:
            confidences.append(conf)
            tokens.append(txt)

    if not confidences:
        return ReadabilityResult(
            readable=False,
            mean_confidence=0.0,
            token_count=0,
            message="No OCR text detected.",
        )

    mean_conf = float(np.mean(confidences))
    readable = mean_conf >= min_confidence and len(tokens) >= 3
    return ReadabilityResult(
        readable=readable,
        mean_confidence=mean_conf,
        token_count=len(tokens),
        message="Readable" if readable else "Low readability",
    )


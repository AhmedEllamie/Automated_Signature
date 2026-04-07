from __future__ import annotations

import os
from dataclasses import dataclass


def _parse_float(value: str | None, default: float) -> float:
    if value is None:
        return default
    try:
        return float(value)
    except ValueError:
        return default


@dataclass(frozen=True)
class FlaskCaptureSettings:
    reset_url: str
    reset_token: str
    reset_timeout_seconds: float
    reset_method: str

    @property
    def is_configured(self) -> bool:
        return bool(self.reset_url)


def load_capture_settings() -> FlaskCaptureSettings:
    return FlaskCaptureSettings(
        reset_url=os.getenv("CAPTURE_RESET_URL", "").strip(),
        reset_token=os.getenv("CAPTURE_RESET_TOKEN", "").strip(),
        reset_timeout_seconds=_parse_float(os.getenv("CAPTURE_RESET_TIMEOUT_SECONDS"), default=8.0),
        reset_method=os.getenv("CAPTURE_RESET_METHOD", "POST").strip().upper() or "POST",
    )

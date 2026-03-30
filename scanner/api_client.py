from __future__ import annotations

from dataclasses import dataclass

from io import BytesIO

import requests


@dataclass
class UploadResult:
    ok: bool
    status_code: int
    message: str
    response_preview: str


def upload_scan(
    image_path: str,
    upload_url: str,
    api_token: str | None = None,
    timeout_seconds: int = 15,
    field_name: str = "file",
) -> UploadResult:
    headers = {}
    if api_token:
        headers["Authorization"] = f"Bearer {api_token}"

    try:
        with open(image_path, "rb") as f:
            files = {field_name: (image_path.split("\\")[-1], f, "image/png")}
            resp = requests.post(upload_url, headers=headers, files=files, timeout=timeout_seconds)
        preview = (resp.text or "")[:280]
        return UploadResult(
            ok=resp.ok,
            status_code=resp.status_code,
            message="Upload success" if resp.ok else "Upload failed",
            response_preview=preview,
        )
    except Exception as exc:
        return UploadResult(
            ok=False,
            status_code=0,
            message=f"Upload error: {exc}",
            response_preview="",
        )

def upload_scan_bytes(
    image_bytes: bytes,
    filename: str,
    upload_url: str,
    api_token: str | None = None,
    timeout_seconds: int = 15,
    field_name: str = "file",
) -> UploadResult:
    headers = {}
    if api_token:
        headers["Authorization"] = f"Bearer {api_token}"

    try:
        bio = BytesIO(image_bytes)
        files = {field_name: (filename, bio, "image/png")}
        resp = requests.post(upload_url, headers=headers, files=files, timeout=timeout_seconds)
        preview = (resp.text or "")[:280]
        return UploadResult(
            ok=resp.ok,
            status_code=resp.status_code,
            message="Upload success" if resp.ok else "Upload failed",
            response_preview=preview,
        )
    except Exception as exc:
        return UploadResult(
            ok=False,
            status_code=0,
            message=f"Upload error: {exc}",
            response_preview="",
        )


def notify_unreadable_capture(
    notify_url: str,
    detector_confidence: float,
    readability_confidence: float,
    readability_tokens: int,
    reason: str,
    api_token: str | None = None,
    timeout_seconds: int = 10,
) -> UploadResult:
    headers = {"Content-Type": "application/json"}
    if api_token:
        headers["Authorization"] = f"Bearer {api_token}"

    payload = {
        "event": "scan_unreadable",
        "detector_confidence": float(detector_confidence),
        "readability_confidence": float(readability_confidence),
        "readability_tokens": int(readability_tokens),
        "reason": reason,
        "action": "recapture_requested",
    }

    try:
        resp = requests.post(
            notify_url,
            headers=headers,
            json=payload,
            timeout=timeout_seconds,
        )
        preview = (resp.text or "")[:280]
        return UploadResult(
            ok=resp.ok,
            status_code=resp.status_code,
            message="Unreadable capture notified" if resp.ok else "Unreadable notify failed",
            response_preview=preview,
        )
    except Exception as exc:
        return UploadResult(
            ok=False,
            status_code=0,
            message=f"Unreadable notify error: {exc}",
            response_preview="",
        )


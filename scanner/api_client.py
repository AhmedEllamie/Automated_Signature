from __future__ import annotations

from dataclasses import dataclass

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


from __future__ import annotations

import atexit
import os
import time
from typing import Any

import cv2
from flask import Flask, Response, jsonify, request

from scanner.calibration import FisheyeUndistorter
from scanner.camera import apply_camera_settings, open_video_capture
from scanner.config import ScannerConfig

from .models import STATUS_FAILED, STATUS_QUEUED, STATUS_RUNNING, STATUS_SUCCEEDED
from .worker import ScannerJobWorker


def _extract_auth_token() -> str:
    auth = (request.headers.get("Authorization") or "").strip()
    if auth.lower().startswith("bearer "):
        return auth[7:].strip()
    return (request.headers.get("X-Scanner-Token") or "").strip()


def create_app(
    cfg: ScannerConfig | None = None,
    *,
    worker: ScannerJobWorker | None = None,
    service_token: str | None = None,
) -> Flask:
    cfg = cfg or ScannerConfig()
    worker = worker or ScannerJobWorker(cfg)
    token = service_token if service_token is not None else os.getenv("SCANNER_SERVICE_TOKEN", "")

    app = Flask(__name__)
    app.config["SCANNER_WORKER"] = worker
    app.config["SCANNER_SERVICE_TOKEN"] = token

    worker.start()
    atexit.register(worker.stop)

    @app.before_request
    def _require_auth() -> Response | None:
        if request.path == "/health":
            return None
        expected = str(app.config.get("SCANNER_SERVICE_TOKEN") or "")
        if not expected:
            return None
        if _extract_auth_token() != expected:
            return jsonify({"ok": False, "error": "unauthorized"}), 401
        return None

    @app.get("/health")
    def health() -> Response:
        return jsonify({"ok": True, "status": "ready"})

    @app.get("/session/manual-config")
    def get_manual_config() -> Response:
        state = worker.get_manual_config()
        return jsonify({"ok": True, "manual_config": state})

    @app.post("/session/manual-config")
    def set_manual_config() -> Response:
        payload = request.get_json(silent=True) or {}
        try:
            state = worker.set_manual_config(payload)
            return jsonify({"ok": True, "manual_config": state})
        except ValueError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 400
        except RuntimeError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 503
        except Exception as exc:
            return jsonify({"ok": False, "error": f"unexpected_error: {exc}"}), 500

    @app.post("/session/focus-mode")
    def set_focus_mode() -> Response:
        payload = request.get_json(silent=True) or {}
        if "autofocus_enabled" not in payload:
            return jsonify({"ok": False, "error": "autofocus_enabled is required"}), 400
        try:
            state = worker.set_focus_mode(
                autofocus_enabled=bool(payload.get("autofocus_enabled")),
                manual_focus_value=payload.get("manual_focus_value"),
            )
            return jsonify({"ok": True, "manual_config": state})
        except ValueError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 400
        except RuntimeError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 503
        except Exception as exc:
            return jsonify({"ok": False, "error": f"unexpected_error: {exc}"}), 500

    @app.post("/session/focus-adjust")
    def adjust_focus() -> Response:
        payload = request.get_json(silent=True) or {}
        direction = payload.get("direction", "")
        try:
            state = worker.adjust_focus(
                direction=str(direction),
                step=payload.get("step"),
            )
            return jsonify({"ok": True, "manual_config": state})
        except ValueError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 400
        except RuntimeError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 503
        except Exception as exc:
            return jsonify({"ok": False, "error": f"unexpected_error: {exc}"}), 500

    @app.post("/session/quad-points")
    def set_quad_points() -> Response:
        payload = request.get_json(silent=True) or {}
        raw_points = payload.get("quad_points")
        if raw_points is None:
            return jsonify({"ok": False, "error": "quad_points is required"}), 400
        try:
            state = worker.set_quad_points(quad_points=raw_points)
            return jsonify({"ok": True, "manual_config": state})
        except ValueError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 400
        except RuntimeError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 503
        except Exception as exc:
            return jsonify({"ok": False, "error": f"unexpected_error: {exc}"}), 500

    @app.post("/jobs")
    def create_job() -> Response:
        payload = request.get_json(silent=True) or {}
        try:
            rec = worker.create_job(payload)
            return jsonify({"ok": True, "job": rec}), 202
        except ValueError as exc:
            msg = str(exc)
            status_code = 409 if "Manual config is not set" in msg else 400
            return jsonify({"ok": False, "error": msg}), status_code
        except RuntimeError as exc:
            return jsonify({"ok": False, "error": str(exc)}), 503
        except Exception as exc:
            return jsonify({"ok": False, "error": f"unexpected_error: {exc}"}), 500

    @app.get("/jobs/<job_id>")
    def get_job(job_id: str) -> Response:
        rec = worker.get_job(job_id)
        if rec is None:
            return jsonify({"ok": False, "error": "job_not_found"}), 404
        return jsonify({"ok": True, "job": rec})

    @app.get("/jobs/<job_id>/image")
    def get_job_image(job_id: str) -> Response:
        status, img = worker.get_job_image(job_id)
        if status == "missing":
            return jsonify({"ok": False, "error": "job_not_found"}), 404
        if status in (STATUS_QUEUED, STATUS_RUNNING):
            return jsonify({"ok": False, "error": "job_not_ready", "status": status}), 409
        if status == STATUS_FAILED:
            rec = worker.get_job(job_id) or {}
            payload: dict[str, Any] = {"ok": False, "error": "job_failed", "status": status}
            if rec:
                payload["job"] = rec
            return jsonify(payload), 409
        if status == STATUS_SUCCEEDED and img is not None:
            return Response(img, mimetype="image/png")
        return jsonify({"ok": False, "error": "job_image_missing", "status": status}), 500

    @app.get("/stream.mjpg")
    def stream_mjpeg() -> Response:
        fps_raw = request.args.get("fps", "10")
        width_raw = request.args.get("width", "0")
        fisheye_raw = request.args.get("fisheye", "1")
        try:
            fps = max(1.0, min(25.0, float(fps_raw)))
        except Exception:
            fps = 10.0
        try:
            target_width = max(0, int(width_raw))
        except Exception:
            target_width = 0
        fisheye_enabled = str(fisheye_raw).strip().lower() not in {"0", "false", "no", "off"}

        if not worker.acquire_camera(timeout_seconds=0.2):
            return jsonify({"ok": False, "error": "camera_busy", "detail": "capture job or stream already using camera"}), 409

        cap = open_video_capture(cfg)
        if cap is None or not cap.isOpened():
            worker.release_camera()
            return jsonify({"ok": False, "error": "camera_unavailable"}), 503

        apply_camera_settings(cap, cfg)
        undistorter = FisheyeUndistorter(cfg) if fisheye_enabled else None
        frame_delay = 1.0 / fps

        def _generate() -> Any:
            try:
                while True:
                    ok, frame = cap.read()
                    if not ok or frame is None:
                        break
                    if undistorter is not None:
                        frame = undistorter.apply(frame)
                    if target_width > 0 and frame.shape[1] > target_width:
                        h = int(frame.shape[0] * (target_width / float(frame.shape[1])))
                        frame = cv2.resize(frame, (target_width, max(1, h)), interpolation=cv2.INTER_AREA)
                    ok_jpg, buf = cv2.imencode(".jpg", frame, [int(cv2.IMWRITE_JPEG_QUALITY), 85])
                    if not ok_jpg or buf is None:
                        continue
                    payload = buf.tobytes()
                    yield (
                        b"--frame\r\n"
                        b"Content-Type: image/jpeg\r\n"
                        b"Cache-Control: no-store\r\n\r\n" + payload + b"\r\n"
                    )
                    time.sleep(frame_delay)
            finally:
                cap.release()
                worker.release_camera()

        return Response(
            _generate(),
            mimetype="multipart/x-mixed-replace; boundary=frame",
            headers={"Cache-Control": "no-store, no-cache, must-revalidate, max-age=0"},
        )

    return app


def main() -> int:
    cfg = ScannerConfig()
    host = os.getenv("SCANNER_SERVICE_HOST", "127.0.0.1")
    port = int(os.getenv("SCANNER_SERVICE_PORT", "8008"))
    app = create_app(cfg)
    app.run(host=host, port=port, threaded=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())


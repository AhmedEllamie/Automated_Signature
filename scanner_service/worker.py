from __future__ import annotations

from dataclasses import replace
import queue
import threading
import uuid
from typing import Any, Callable

from scanner.config import ScannerConfig

from .models import (
    JobRecord,
    JobRequest,
    ManualConfig,
    STATUS_FAILED,
    STATUS_QUEUED,
    STATUS_RUNNING,
    STATUS_SUCCEEDED,
    utc_now_iso,
)


CaptureExecutor = Callable[..., Any]
FrameSizeProvider = Callable[[ScannerConfig, bool, float | None], tuple[int, int]]
QuadNormalizer = Callable[[list[object]], Any]
QuadValidator = Callable[[Any, int, int, float], tuple[bool, str]]


class ScannerJobWorker:
    def __init__(
        self,
        cfg: ScannerConfig,
        *,
        capture_executor: CaptureExecutor | None = None,
        frame_size_provider: FrameSizeProvider | None = None,
        quad_normalizer: QuadNormalizer | None = None,
        quad_validator: QuadValidator | None = None,
    ) -> None:
        self._cfg = cfg
        if capture_executor is None or frame_size_provider is None or quad_normalizer is None or quad_validator is None:
            from scanner.capture import (
                capture_rectified_manual_png,
                normalize_quad_points,
                peek_frame_size,
                validate_quad_within_frame,
            )

            self._capture_executor = capture_executor or capture_rectified_manual_png
            self._frame_size_provider = frame_size_provider or peek_frame_size
            self._quad_normalizer = quad_normalizer or normalize_quad_points
            self._quad_validator = quad_validator or validate_quad_within_frame
        else:
            self._capture_executor = capture_executor
            self._frame_size_provider = frame_size_provider
            self._quad_normalizer = quad_normalizer
            self._quad_validator = quad_validator

        self._jobs: dict[str, JobRecord] = {}
        self._jobs_lock = threading.Lock()
        self._manual_lock = threading.Lock()
        self._camera_lock = threading.Lock()
        self._manual_config = ManualConfig()

        self._queue: queue.Queue[JobRequest] = queue.Queue()
        self._stop_event = threading.Event()
        self._thread = threading.Thread(
            target=self._run,
            name="scanner-capture-worker",
            daemon=True,
        )

    def start(self) -> None:
        if not self._thread.is_alive():
            self._thread.start()

    def stop(self, timeout_seconds: float = 3.0) -> None:
        self._stop_event.set()
        self._thread.join(timeout=max(0.1, timeout_seconds))

    def get_manual_config(self) -> dict[str, Any]:
        with self._manual_lock:
            return self._manual_config.to_dict()

    def set_manual_config(self, payload: dict[str, Any]) -> dict[str, Any]:
        if not isinstance(payload, dict):
            raise ValueError("JSON body must be an object")

        with self._manual_lock:
            current = replace(self._manual_config)
            current.quad_points = [list(p) for p in self._manual_config.quad_points]

        autofocus_enabled = bool(payload.get("autofocus_enabled", current.autofocus_enabled))

        if "manual_focus_value" in payload:
            raw_focus = payload.get("manual_focus_value")
            manual_focus_value = None if raw_focus is None else float(raw_focus)
        else:
            manual_focus_value = current.manual_focus_value

        raw_points = payload.get("quad_points", current.quad_points)
        if not raw_points:
            raise ValueError("quad_points is required and must contain 4 points")

        quad = self._quad_normalizer(raw_points)
        with self._camera_lock:
            frame_width, frame_height = self._frame_size_provider(
                self._cfg,
                autofocus_enabled,
                manual_focus_value,
            )
        valid, reason = self._quad_validator(quad, frame_width, frame_height, self._cfg.min_edge_px)
        if not valid:
            raise ValueError(reason)

        updated = ManualConfig(
            autofocus_enabled=autofocus_enabled,
            manual_focus_value=manual_focus_value,
            quad_points=self._quad_to_list(quad),
            frame_width=frame_width,
            frame_height=frame_height,
            valid=True,
            validation_message="ok",
            updated_at=utc_now_iso(),
        )
        with self._manual_lock:
            self._manual_config = updated
            return self._manual_config.to_dict()

    def set_focus_mode(
        self,
        *,
        autofocus_enabled: bool,
        manual_focus_value: float | None = None,
    ) -> dict[str, Any]:
        with self._manual_lock:
            current = replace(self._manual_config)
            current.quad_points = [list(p) for p in self._manual_config.quad_points]

        new_manual_focus = current.manual_focus_value if manual_focus_value is None else float(manual_focus_value)
        with self._camera_lock:
            frame_width, frame_height = self._frame_size_provider(
                self._cfg,
                bool(autofocus_enabled),
                new_manual_focus,
            )

        valid = bool(current.quad_points)
        validation_message = "quad_points are not set yet"
        if current.quad_points:
            quad = self._quad_normalizer(current.quad_points)
            q_ok, q_reason = self._quad_validator(quad, frame_width, frame_height, self._cfg.min_edge_px)
            valid = bool(q_ok)
            validation_message = "ok" if q_ok else q_reason
            current.quad_points = self._quad_to_list(quad)

        updated = ManualConfig(
            autofocus_enabled=bool(autofocus_enabled),
            manual_focus_value=new_manual_focus,
            quad_points=[list(p) for p in current.quad_points],
            frame_width=int(frame_width),
            frame_height=int(frame_height),
            valid=valid,
            validation_message=validation_message,
            updated_at=utc_now_iso(),
        )
        with self._manual_lock:
            self._manual_config = updated
            return self._manual_config.to_dict()

    def adjust_focus(self, *, direction: str, step: float | None = None) -> dict[str, Any]:
        d = str(direction or "").strip().lower()
        if d not in {"+", "-", "in", "out", "near", "far"}:
            raise ValueError("direction must be one of: '+', '-', 'in', 'out', 'near', 'far'")
        s = float(step) if step is not None else float(self._cfg.camera_focus_step)
        if s <= 0:
            raise ValueError("step must be > 0")

        with self._manual_lock:
            current = replace(self._manual_config)
            current.quad_points = [list(p) for p in self._manual_config.quad_points]

        base = current.manual_focus_value if current.manual_focus_value is not None else 0.0
        delta = -s if d in {"-", "in", "near"} else s
        new_focus = max(0.0, float(base) + delta)
        return self.set_focus_mode(autofocus_enabled=False, manual_focus_value=new_focus)

    def set_quad_points(self, *, quad_points: list[object]) -> dict[str, Any]:
        with self._manual_lock:
            current = replace(self._manual_config)
            current.quad_points = [list(p) for p in self._manual_config.quad_points]

        quad = self._quad_normalizer(quad_points)
        with self._camera_lock:
            frame_width, frame_height = self._frame_size_provider(
                self._cfg,
                current.autofocus_enabled,
                current.manual_focus_value,
            )
        valid, reason = self._quad_validator(quad, frame_width, frame_height, self._cfg.min_edge_px)
        if not valid:
            raise ValueError(reason)

        updated = ManualConfig(
            autofocus_enabled=current.autofocus_enabled,
            manual_focus_value=current.manual_focus_value,
            quad_points=self._quad_to_list(quad),
            frame_width=int(frame_width),
            frame_height=int(frame_height),
            valid=True,
            validation_message="ok",
            updated_at=utc_now_iso(),
        )
        with self._manual_lock:
            self._manual_config = updated
            return self._manual_config.to_dict()

    def create_job(self, payload: dict[str, Any]) -> dict[str, Any]:
        if not isinstance(payload, dict):
            payload = {}

        mode = str(payload.get("mode", "manual")).strip().lower()
        if mode != "manual":
            raise ValueError("Only mode='manual' is supported")

        readability_required_raw = payload.get("readability_required", None)
        readability_required = None
        if readability_required_raw is not None:
            readability_required = bool(readability_required_raw)

        timeout_seconds_raw = payload.get("timeout_seconds", self._cfg.upload_timeout_seconds)
        timeout_seconds = float(timeout_seconds_raw)
        if timeout_seconds <= 0:
            raise ValueError("timeout_seconds must be > 0")

        self._validate_manual_config_against_camera()

        job_id = str(uuid.uuid4())
        req = JobRequest(
            job_id=job_id,
            mode=mode,
            readability_required=readability_required,
            timeout_seconds=timeout_seconds,
        )
        rec = JobRecord(
            job_id=job_id,
            mode=mode,
            status=STATUS_QUEUED,
            created_at=req.created_at,
        )
        with self._jobs_lock:
            self._jobs[job_id] = rec
        self._queue.put(req)
        return rec.to_public_dict()

    def get_job(self, job_id: str) -> dict[str, Any] | None:
        with self._jobs_lock:
            rec = self._jobs.get(job_id)
            if rec is None:
                return None
            return rec.to_public_dict()

    def get_job_image(self, job_id: str) -> tuple[str, bytes | None]:
        with self._jobs_lock:
            rec = self._jobs.get(job_id)
            if rec is None:
                return "missing", None
            if rec.status != STATUS_SUCCEEDED or rec.image_bytes is None:
                return rec.status, None
            return rec.status, rec.image_bytes

    def acquire_camera(self, timeout_seconds: float = 1.0) -> bool:
        return self._camera_lock.acquire(timeout=max(0.0, float(timeout_seconds)))

    def release_camera(self) -> None:
        if self._camera_lock.locked():
            self._camera_lock.release()

    def _validate_manual_config_against_camera(self) -> None:
        with self._manual_lock:
            cfg = replace(self._manual_config)
            cfg.quad_points = [list(p) for p in self._manual_config.quad_points]

        if not cfg.quad_points:
            raise ValueError("Manual config is not set; call POST /session/manual-config first")

        quad = self._quad_normalizer(cfg.quad_points)
        with self._camera_lock:
            frame_width, frame_height = self._frame_size_provider(
                self._cfg,
                cfg.autofocus_enabled,
                cfg.manual_focus_value,
            )
        valid, reason = self._quad_validator(quad, frame_width, frame_height, self._cfg.min_edge_px)
        if not valid:
            raise ValueError(reason)

        with self._manual_lock:
            self._manual_config.frame_width = int(frame_width)
            self._manual_config.frame_height = int(frame_height)
            self._manual_config.valid = True
            self._manual_config.validation_message = "ok"
            self._manual_config.updated_at = utc_now_iso()
            self._manual_config.quad_points = self._quad_to_list(quad)

    def _run(self) -> None:
        while not self._stop_event.is_set():
            try:
                req = self._queue.get(timeout=0.2)
            except queue.Empty:
                continue

            self._mark_running(req.job_id)
            try:
                with self._manual_lock:
                    cfg = replace(self._manual_config)
                    cfg.quad_points = [list(p) for p in self._manual_config.quad_points]
                if not cfg.quad_points:
                    raise RuntimeError("Manual config is missing")

                with self._camera_lock:
                    result = self._capture_executor(
                        self._cfg,
                        cfg.quad_points,
                        autofocus_enabled=cfg.autofocus_enabled,
                        manual_focus_value=cfg.manual_focus_value,
                        readability_required=req.readability_required,
                        timeout_seconds=req.timeout_seconds,
                    )

                if result.ok and result.png_bytes is not None:
                    self._mark_succeeded(req.job_id, result)
                else:
                    self._mark_failed(
                        req.job_id,
                        error=result.status,
                        detail=result.message,
                        metadata=self._result_metadata(result),
                    )
            except Exception as exc:
                self._mark_failed(req.job_id, error="worker_error", detail=str(exc), metadata={})
            finally:
                self._queue.task_done()

    def _mark_running(self, job_id: str) -> None:
        with self._jobs_lock:
            rec = self._jobs[job_id]
            rec.status = STATUS_RUNNING
            rec.started_at = utc_now_iso()
            rec.error = None
            rec.detail = None

    def _mark_succeeded(self, job_id: str, result: Any) -> None:
        with self._jobs_lock:
            rec = self._jobs[job_id]
            rec.status = STATUS_SUCCEEDED
            rec.finished_at = utc_now_iso()
            rec.image_bytes = result.png_bytes
            rec.error = None
            rec.detail = result.message
            rec.metadata = self._result_metadata(result)

    def _mark_failed(
        self,
        job_id: str,
        *,
        error: str,
        detail: str,
        metadata: dict[str, Any],
    ) -> None:
        with self._jobs_lock:
            rec = self._jobs[job_id]
            rec.status = STATUS_FAILED
            rec.finished_at = utc_now_iso()
            rec.image_bytes = None
            rec.error = error
            rec.detail = detail
            rec.metadata = dict(metadata)

    @staticmethod
    def _result_metadata(result: Any) -> dict[str, Any]:
        metadata: dict[str, Any] = {
            "frame_width": result.frame_width,
            "frame_height": result.frame_height,
            "elapsed_ms": result.elapsed_ms,
        }
        if result.readability is not None:
            metadata["readability"] = {
                "readable": result.readability.readable,
                "mean_confidence": result.readability.mean_confidence,
                "token_count": result.readability.token_count,
                "message": result.readability.message,
            }
        return metadata

    @staticmethod
    def _quad_to_list(quad: Any) -> list[list[float]]:
        raw = quad.tolist() if hasattr(quad, "tolist") else quad
        if not isinstance(raw, list) or len(raw) != 4:
            raise ValueError("Quad must contain exactly 4 points")
        out: list[list[float]] = []
        for point in raw:
            if not isinstance(point, (list, tuple)) or len(point) != 2:
                raise ValueError("Each quad point must contain x and y values")
            out.append([float(point[0]), float(point[1])])
        return out


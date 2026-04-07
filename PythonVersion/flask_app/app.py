from __future__ import annotations

import asyncio
import base64
import io
import json
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen
from uuid import UUID

from flask import Flask, Response, request, send_file, send_from_directory

from PythonVersion.dependency_injection import ServiceProvider, get_service_provider
from PythonVersion.flask_app.config import FlaskCaptureSettings, load_capture_settings
from PythonVersion.flask_app.response import api_error, api_success
from PythonVersion.flask_app.state import RuntimeState
from PythonVersion.models.contracts import PrintRequest, get_paper_size_mm, parse_bool
from PythonVersion.services.printer.svg_converter import convert_to_gcode


def _run_async(coroutine: Any) -> Any:
    return asyncio.run(coroutine)


def _get_json_dict() -> dict[str, Any]:
    payload = request.get_json(silent=True)
    if isinstance(payload, dict):
        return payload
    return {}


def _parse_optional_int(value: Any) -> int | None:
    if value is None or value == "":
        return None
    if isinstance(value, int):
        return value
    return int(str(value))


def _ensure_connected(provider: ServiceProvider) -> None:
    if not provider.printer_service.is_open:
        raise RuntimeError("Printer is not connected. Call POST /api/connect first.")


def _ensure_not_busy(provider: ServiceProvider) -> None:
    if provider.printer_service.is_printing:
        raise RuntimeError("Printer is busy.")


def _build_print_request(payload: dict[str, Any] | None) -> PrintRequest:
    data = payload or {}
    print_request = PrintRequest.from_dict(data)
    if print_request.scale < 1:
        raise ValueError("Scale must be at least 1.")
    if print_request.rotation < 0 or print_request.rotation > 360:
        raise ValueError("Rotation must be between 0 and 360.")
    if print_request.paper is not None:
        paper_w, paper_h = get_paper_size_mm(print_request.paper)
        print_request.width = f"{paper_w}mm"
        print_request.height = f"{paper_h}mm"
    return print_request


def _extract_print_payload() -> dict[str, Any]:
    json_payload = _get_json_dict()
    if json_payload:
        nested = json_payload.get("printRequest")
        if isinstance(nested, dict):
            return nested
        return json_payload

    raw_json = request.form.get("printRequestJson")
    if raw_json:
        try:
            parsed = json.loads(raw_json)
        except json.JSONDecodeError as ex:
            raise ValueError(f"Invalid printRequestJson: {ex}") from ex
        if not isinstance(parsed, dict):
            raise ValueError("printRequestJson must be a JSON object.")
        return parsed

    if request.form:
        return request.form.to_dict(flat=True)

    return {}


def _convert_svg(svg_payload: bytes, print_request: PrintRequest) -> list[str]:
    if not svg_payload:
        raise ValueError("SVG payload is empty.")
    gcode = convert_to_gcode(io.BytesIO(svg_payload), print_request)
    if not gcode:
        raise ValueError("No drawable paths found. If SVG contains text, convert to paths first.")
    return gcode


def _to_iso8601_utc(value: datetime) -> str:
    if value.tzinfo is None:
        value = value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc).isoformat()


def _trigger_capture_reset(settings: FlaskCaptureSettings, payload: dict[str, Any]) -> dict[str, Any]:
    if not settings.is_configured:
        raise RuntimeError("Capture reset URL is not configured.")

    request_payload = {
        "action": "capture_reset",
        "requestedAt": datetime.now(timezone.utc).isoformat(),
    }
    request_payload.update(payload)
    body = json.dumps(request_payload).encode("utf-8")

    headers = {"Content-Type": "application/json"}
    if settings.reset_token:
        headers["Authorization"] = f"Bearer {settings.reset_token}"

    request_obj = Request(
        url=settings.reset_url,
        data=body if settings.reset_method != "GET" else None,
        method=settings.reset_method,
        headers=headers,
    )

    with urlopen(request_obj, timeout=settings.reset_timeout_seconds) as response:
        raw_body = response.read().decode("utf-8", errors="ignore")
        return {
            "statusCode": response.getcode(),
            "responseBody": raw_body[:4000],
        }


def _extract_capture_image_payload() -> tuple[str, str, bytes]:
    for key in ("photo", "image", "file", "capture"):
        upload = request.files.get(key)
        if upload is None:
            continue
        payload = upload.read()
        if payload:
            return (
                upload.filename or "capture.jpg",
                upload.mimetype or "image/jpeg",
                payload,
            )

    if request.files:
        upload = next(iter(request.files.values()))
        payload = upload.read()
        if payload:
            return (
                upload.filename or "capture.jpg",
                upload.mimetype or "image/jpeg",
                payload,
            )

    json_payload = _get_json_dict()
    image_base64 = json_payload.get("imageBase64") or request.form.get("imageBase64")
    if isinstance(image_base64, str) and image_base64:
        raw_data = image_base64.split(",", 1)[1] if "," in image_base64 else image_base64
        try:
            payload = base64.b64decode(raw_data)
        except Exception as ex:
            raise ValueError(f"Invalid imageBase64 payload: {ex}") from ex
        if payload:
            return (
                str(json_payload.get("fileName") or request.form.get("fileName") or "capture.jpg"),
                str(json_payload.get("contentType") or request.form.get("contentType") or "image/jpeg"),
                payload,
            )

    raw_binary = request.get_data(cache=False)
    content_type = request.content_type or ""
    if raw_binary and content_type.startswith("image/"):
        return ("capture.jpg", content_type, raw_binary)

    raise ValueError("No capture image payload found.")


def create_app(provider: ServiceProvider | None = None) -> Flask:
    provider = provider or get_service_provider()
    capture_settings = load_capture_settings()
    runtime_state = RuntimeState()

    app = Flask(__name__, static_folder="static", static_url_path="/static")

    @app.get("/")
    def home() -> Response:
        if not app.static_folder:
            return api_success(message="Diwan Signature Flask API")[0]
        return send_from_directory(app.static_folder, "index.html")

    @app.get("/api/health")
    def health() -> tuple[Response, int]:
        return api_success(
            message="Service is healthy.",
            data={
                "printerConnected": provider.printer_service.is_open,
                "printerBusy": provider.printer_service.is_printing,
                "captureResetConfigured": capture_settings.is_configured,
            },
        )

    @app.get("/api/config")
    def config() -> tuple[Response, int]:
        return api_success(
            message="Runtime config loaded.",
            data={
                "defaultComPort": provider.printer_service.default_com_port,
                "defaultBaudRate": provider.printer_service.default_baud_rate,
                "captureResetConfigured": capture_settings.is_configured,
                "captureResetMethod": capture_settings.reset_method,
            },
        )

    @app.post("/api/connect")
    def connect() -> tuple[Response, int]:
        if provider.printer_service.is_open:
            return api_error(
                message=f"Already connected to {provider.printer_service.port_name}. Disconnect first.",
                error_code="ALREADY_CONNECTED",
                status_code=409,
            )

        payload = _get_json_dict()
        com_port = (
            payload.get("comPort")
            or payload.get("com_port")
            or request.args.get("comPort")
            or request.args.get("com_port")
            or provider.printer_service.default_com_port
        )
        raw_baud = payload.get("baudRate") or payload.get("baud_rate") or request.args.get("baudRate")
        try:
            baud_rate = _parse_optional_int(raw_baud) or provider.printer_service.default_baud_rate
            provider.printer_service.open_port(str(com_port), baud_rate)
        except ValueError as ex:
            return api_error(str(ex), error_code="INVALID_BAUD_RATE", status_code=400)
        except Exception as ex:
            return api_error(f"Failed to connect: {ex}", error_code="CONNECT_FAILED", status_code=400)

        return api_success(
            message=f"Connected to {provider.printer_service.port_name}.",
            data=asdict(provider.printer_service.get_status()),
        )

    @app.post("/api/disconnect")
    def disconnect() -> tuple[Response, int]:
        if not provider.printer_service.is_open:
            return api_error("Printer is not connected.", error_code="NOT_CONNECTED", status_code=409)
        if provider.printer_service.is_printing:
            return api_error(
                "Cannot disconnect while printing.",
                error_code="PRINTER_BUSY",
                status_code=409,
            )

        provider.printer_service.close_port()
        return api_success("Disconnected from printer.", data=asdict(provider.printer_service.get_status()))

    @app.get("/api/status")
    def status() -> tuple[Response, int]:
        status_model = provider.printer_service.get_status()
        return api_success(message="Printer status loaded.", data=asdict(status_model))

    @app.post("/api/upload")
    def upload_svg() -> tuple[Response, int]:
        upload = request.files.get("svg") or request.files.get("file")
        if upload is None:
            return api_error("No SVG file uploaded.", error_code="SVG_REQUIRED", status_code=400)

        payload = upload.read()
        if not payload:
            return api_error("Uploaded SVG is empty.", error_code="EMPTY_SVG", status_code=400)

        model = runtime_state.set_uploaded_svg(upload.filename or "uploaded.svg", payload)
        return api_success(
            message="SVG uploaded successfully.",
            data={
                "fileName": model.file_name,
                "sizeBytes": len(model.content),
                "uploadedAt": _to_iso8601_utc(model.uploaded_at),
            },
            status_code=201,
        )

    @app.post("/api/print")
    def print_svg() -> tuple[Response, int]:
        try:
            _ensure_connected(provider)
            _ensure_not_busy(provider)
        except RuntimeError as ex:
            return api_error(str(ex), error_code="PRINTER_STATE_ERROR", status_code=409)

        upload = request.files.get("svg")
        if upload is not None:
            svg_payload = upload.read()
            svg_file_name = upload.filename or "uploaded.svg"
            if not svg_payload:
                return api_error("Uploaded SVG is empty.", error_code="EMPTY_SVG", status_code=400)
            runtime_state.set_uploaded_svg(svg_file_name, svg_payload)
        else:
            uploaded = runtime_state.get_uploaded_svg()
            if uploaded is None:
                return api_error(
                    "No uploaded SVG found. Call POST /api/upload first.",
                    error_code="SVG_NOT_UPLOADED",
                    status_code=400,
                )
            svg_payload = uploaded.content
            svg_file_name = uploaded.file_name

        try:
            print_request = _build_print_request(_extract_print_payload())
            gcode = _convert_svg(svg_payload, print_request)
            print_result = _run_async(provider.printer_service.print(gcode))
        except ValueError as ex:
            return api_error(str(ex), error_code="PRINT_VALIDATION_ERROR", status_code=400)
        except RuntimeError as ex:
            return api_error(str(ex), error_code="PRINT_RUNTIME_ERROR", status_code=400)
        except Exception as ex:
            return api_error(f"Print failed: {ex}", error_code="PRINT_FAILED", status_code=500)

        return api_success(
            message="Print completed.",
            data={
                "svgFileName": svg_file_name,
                "commandCount": len(gcode),
                "result": asdict(print_result),
                "status": asdict(provider.printer_service.get_status()),
            },
        )

    @app.post("/api/void")
    def void_print() -> tuple[Response, int]:
        try:
            _ensure_connected(provider)
            _ensure_not_busy(provider)
            result = _run_async(provider.printer_service.void_print())
        except RuntimeError as ex:
            return api_error(str(ex), error_code="VOID_RUNTIME_ERROR", status_code=409)
        except Exception as ex:
            return api_error(f"Void print failed: {ex}", error_code="VOID_FAILED", status_code=500)

        return api_success("Void print completed.", data=asdict(result))

    @app.post("/api/change-pen/start")
    def change_pen_start() -> tuple[Response, int]:
        try:
            _ensure_connected(provider)
            _ensure_not_busy(provider)
            result = _run_async(provider.printer_service.pen_change_start())
        except RuntimeError as ex:
            return api_error(str(ex), error_code="PEN_CHANGE_STATE_ERROR", status_code=409)
        except Exception as ex:
            return api_error(f"Pen change start failed: {ex}", error_code="PEN_CHANGE_START_FAILED", status_code=500)

        return api_success("Pen change start completed.", data=asdict(result))

    @app.post("/api/change-pen/finish")
    def change_pen_finish() -> tuple[Response, int]:
        try:
            _ensure_connected(provider)
            _ensure_not_busy(provider)
            result = _run_async(provider.printer_service.pen_change_finish())
        except RuntimeError as ex:
            return api_error(str(ex), error_code="PEN_CHANGE_STATE_ERROR", status_code=409)
        except Exception as ex:
            return api_error(
                f"Pen change finish failed: {ex}",
                error_code="PEN_CHANGE_FINISH_FAILED",
                status_code=500,
            )

        return api_success("Pen change finish completed.", data=asdict(result))

    @app.post("/api/change-pen")
    def change_pen() -> tuple[Response, int]:
        payload = _get_json_dict() or request.form.to_dict(flat=True)
        mode = str(payload.get("mode", "start")).strip().lower()
        if mode not in {"start", "finish"}:
            return api_error(
                "Change pen mode must be 'start' or 'finish'.",
                error_code="INVALID_PEN_MODE",
                status_code=400,
            )
        if mode == "start":
            return change_pen_start()
        return change_pen_finish()

    @app.post("/api/reset")
    def reset() -> tuple[Response, int]:
        if provider.printer_service.is_printing:
            return api_error(
                "Cannot reset while printer is busy.",
                error_code="PRINTER_BUSY",
                status_code=409,
            )

        payload = _get_json_dict()
        try:
            stats = provider.printer_service.reset_cumulative_distance()
            max_pen_distance = payload.get("maxPenDistanceM")
            if max_pen_distance is not None:
                stats = provider.printer_service.set_max_pen_distance_m(float(max_pen_distance))
        except ValueError as ex:
            return api_error(str(ex), error_code="RESET_VALIDATION_ERROR", status_code=400)
        except Exception as ex:
            return api_error(f"Reset failed: {ex}", error_code="RESET_FAILED", status_code=500)

        clear_uploaded_svg = parse_bool(payload.get("clearUploadedSvg"), default=False)
        if clear_uploaded_svg:
            runtime_state.clear_uploaded_svg()

        return api_success(
            message="Printer distance stats reset.",
            data={
                "stats": stats,
                "clearedUploadedSvg": clear_uploaded_svg,
            },
        )

    @app.post("/api/capture/request")
    def request_capture() -> tuple[Response, int]:
        payload = _get_json_dict()
        try:
            response_payload = _trigger_capture_reset(capture_settings, payload)
        except RuntimeError as ex:
            return api_error(str(ex), error_code="CAPTURE_NOT_CONFIGURED", status_code=400)
        except HTTPError as ex:
            body = ex.read().decode("utf-8", errors="ignore")
            return api_error(
                f"Capture reset endpoint responded with HTTP {ex.code}.",
                error_code="CAPTURE_TRIGGER_HTTP_ERROR",
                status_code=502,
                details={"statusCode": ex.code, "responseBody": body[:4000]},
            )
        except URLError as ex:
            return api_error(
                f"Failed to reach capture reset endpoint: {ex}",
                error_code="CAPTURE_TRIGGER_UNREACHABLE",
                status_code=502,
            )
        except Exception as ex:
            return api_error(f"Capture trigger failed: {ex}", error_code="CAPTURE_TRIGGER_FAILED", status_code=500)

        return api_success("Capture reset command sent.", data=response_payload)

    @app.post("/api/capture")
    def capture_upload() -> tuple[Response, int]:
        try:
            file_name, content_type, payload = _extract_capture_image_payload()
        except ValueError as ex:
            return api_error(str(ex), error_code="CAPTURE_PAYLOAD_INVALID", status_code=400)
        except Exception as ex:
            return api_error(f"Capture upload failed: {ex}", error_code="CAPTURE_UPLOAD_FAILED", status_code=500)

        model = runtime_state.set_captured_image(file_name, content_type, payload)
        return api_success(
            message="Captured image stored.",
            data={
                "fileName": model.file_name,
                "contentType": model.content_type,
                "sizeBytes": len(model.content),
                "capturedAt": _to_iso8601_utc(model.captured_at),
                "imageUrl": "/api/capture/latest/image",
            },
            status_code=201,
        )

    @app.get("/api/capture/latest")
    def capture_latest() -> tuple[Response, int]:
        model = runtime_state.get_captured_image()
        if model is None:
            return api_error("No captured image available.", error_code="CAPTURE_NOT_FOUND", status_code=404)

        include_data_uri = parse_bool(request.args.get("includeDataUri"), default=False)
        data: dict[str, Any] = {
            "fileName": model.file_name,
            "contentType": model.content_type,
            "sizeBytes": len(model.content),
            "capturedAt": _to_iso8601_utc(model.captured_at),
            "imageUrl": "/api/capture/latest/image",
        }
        if include_data_uri:
            encoded = base64.b64encode(model.content).decode("ascii")
            data["dataUri"] = f"data:{model.content_type};base64,{encoded}"

        return api_success(message="Latest captured image loaded.", data=data)

    @app.get("/api/capture/latest/image")
    def capture_latest_image() -> Response | tuple[Response, int]:
        model = runtime_state.get_captured_image()
        if model is None:
            return api_error("No captured image available.", error_code="CAPTURE_NOT_FOUND", status_code=404)

        return send_file(
            io.BytesIO(model.content),
            mimetype=model.content_type,
            as_attachment=False,
            download_name=model.file_name,
            max_age=0,
        )

    @app.get("/api/requests/<string:request_id>")
    def get_request(request_id: str) -> tuple[Response, int]:
        try:
            request_uuid = UUID(request_id)
        except ValueError:
            return api_error("Invalid request ID format.", error_code="INVALID_REQUEST_ID", status_code=400)

        log = _run_async(provider.print_approval_service.get_request_log_async(request_uuid))
        if log is None:
            return api_error(
                f"Request log with ID {request_id} not found.",
                error_code="REQUEST_NOT_FOUND",
                status_code=404,
            )
        return api_success(message="Request log loaded.", data=log.to_dict())

    @app.get("/api/requests")
    def list_requests() -> tuple[Response, int]:
        try:
            count = int(request.args.get("count", "10"))
            if count < 1:
                count = 1
            if count > 100:
                count = 100
        except ValueError:
            return api_error("count must be an integer.", error_code="INVALID_COUNT", status_code=400)

        logs = _run_async(provider.print_approval_service.get_recent_requests_async(count))
        return api_success(
            message="Recent request logs loaded.",
            data=[entry.to_dict() for entry in logs],
        )

    return app


app = create_app()

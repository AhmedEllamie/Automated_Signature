from __future__ import annotations

import argparse
import os
import time
from datetime import datetime

import cv2
import numpy as np

from scanner import (
    ManualSelector,
    ScannerConfig,
    a4_target_size,
    check_capture_reset_api,
    compute_warp_short_side,
    detect_document_quad,
    enhance_for_scan,
    notify_unreadable_capture,
    smooth_quad,
    upload_scan,
    upload_scan_bytes,
    verify_readability,
    warp_document,
)
from scanner.readability import ReadabilityResult


def put_status(
    frame: np.ndarray,
    mode: str,
    confidence: float,
    fps: float,
    saves: int,
    auto_status: str,
) -> None:
    lines = [
        f"Mode: {mode}",
        f"Confidence: {confidence:.2f}",
        f"FPS: {fps:.1f}",
        f"Saved: {saves}",
        auto_status,
        "Starts in AUTO | Keys: [a] auto  [m] manual  [r] reset  [q] quit",
    ]
    y = 28
    for line in lines:
        cv2.putText(frame, line, (10, y), cv2.FONT_HERSHEY_SIMPLEX, 0.62, (0, 255, 0), 2, cv2.LINE_AA)
        y += 28


def draw_quad(frame: np.ndarray, quad: np.ndarray, color: tuple[int, int, int]) -> None:
    pts = quad.astype(np.int32).reshape(-1, 1, 2)
    cv2.polylines(frame, [pts], True, color, 3, cv2.LINE_AA)


def save_scan(image: np.ndarray, save_dir: str) -> str:
    os.makedirs(save_dir, exist_ok=True)
    name = datetime.now().strftime("%Y%m%d_%H%M%S_%f") + ".png"
    path = os.path.join(save_dir, name)
    cv2.imwrite(path, image)
    return path


def encode_png_bytes(image: np.ndarray) -> bytes:
    ok, buf = cv2.imencode(".png", image)
    if not ok or buf is None:
        raise ValueError("Failed to encode rectified image as PNG")
    return buf.tobytes()


def _camera_api_preference(cfg: ScannerConfig) -> int:
    name = (cfg.camera_backend or "").strip().upper()
    if name in ("MSMF", "CAP_MSMF", "MEDIA_FOUNDATION"):
        return cv2.CAP_MSMF
    if name in ("DSHOW", "DIRECTSHOW", "CAP_DSHOW"):
        return cv2.CAP_DSHOW
    return int(cv2.CAP_ANY)


def apply_camera_settings(cap: cv2.VideoCapture, cfg: ScannerConfig) -> tuple[int, int]:
    fourcc = (cfg.camera_fourcc or "").strip().upper()
    if len(fourcc) == 4:
        try:
            code = cv2.VideoWriter_fourcc(*fourcc)
            cap.set(cv2.CAP_PROP_FOURCC, code)
        except Exception:
            pass
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, cfg.frame_width)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, cfg.frame_height)
    cap.read()
    aw = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    ah = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    print(
        f"Camera actual resolution: {aw}x{ah} (requested {cfg.frame_width}x{cfg.frame_height})"
    )
    if aw < cfg.frame_width * 0.95 or ah < cfg.frame_height * 0.95:
        print(
            "Hint: If Windows Camera shows higher resolution, try ScannerConfig.camera_backend "
            "'MSMF' or 'DSHOW', or set camera_fourcc to '' / 'MJPG' and retry."
        )
    return aw, ah


def fit_for_display(image: np.ndarray, max_width: int, max_height: int) -> np.ndarray:
    h, w = image.shape[:2]
    if w <= max_width and h <= max_height:
        return image
    scale = min(max_width / float(w), max_height / float(h))
    new_size = (max(1, int(w * scale)), max(1, int(h * scale)))
    return cv2.resize(image, new_size, interpolation=cv2.INTER_AREA)


def persist_capture(
    image: np.ndarray,
    cfg: ScannerConfig,
    readability_result: ReadabilityResult | None = None,
) -> None:
    # Memory mode keeps disk clean; local fallback is handled in run_post_processors on upload failure.
    if cfg.upload_enabled and cfg.upload_url and cfg.upload_from_memory:
        run_post_processors(image, image_path="", cfg=cfg, readability_result=readability_result)
        print("Rectified image uploaded from memory (local file kept only if upload fails).")
        return

    out_path = save_scan(image, cfg.save_dir)
    run_post_processors(image, out_path, cfg, readability_result=readability_result)
    if os.path.exists(out_path):
        print(f"Saved rectified image: {out_path}")
    else:
        print("Upload successful; local file deleted after upload.")


def notify_unreadable(
    cfg: ScannerConfig,
    detector_confidence: float,
    readability_result: ReadabilityResult | None,
) -> bool:
    if not cfg.unreadable_notify_enabled:
        return False
    if not cfg.unreadable_notify_url:
        print(
            "Unreadable capture detected; notify API skipped (set unreadable_notify_url to enable)."
        )
        return False

    reason = readability_result.message if readability_result is not None else "Low readability"
    readability_confidence = readability_result.mean_confidence if readability_result is not None else 0.0
    readability_tokens = readability_result.token_count if readability_result is not None else 0
    u = notify_unreadable_capture(
        notify_url=cfg.unreadable_notify_url,
        detector_confidence=detector_confidence,
        readability_confidence=readability_confidence,
        readability_tokens=readability_tokens,
        reason=reason,
        api_token=cfg.unreadable_notify_token or None,
        timeout_seconds=cfg.unreadable_notify_timeout_seconds,
    )
    print(f"Unreadable notify: {u.message} (status={u.status_code})")
    if u.response_preview:
        print(f"Unreadable notify response: {u.response_preview}")
    return u.ok


def clear_capture_lock_if_api_allows(
    cfg: ScannerConfig,
    now: float,
    capture_locked: bool,
    last_reset_poll_ts: float,
    last_reset_error_ts: float,
) -> tuple[bool, float, float]:
    if not capture_locked:
        return capture_locked, last_reset_poll_ts, last_reset_error_ts
    if not cfg.capture_reset_url:
        return capture_locked, last_reset_poll_ts, last_reset_error_ts
    if (now - last_reset_poll_ts) < max(0.05, cfg.capture_reset_poll_interval_seconds):
        return capture_locked, last_reset_poll_ts, last_reset_error_ts

    last_reset_poll_ts = now
    reset_state = check_capture_reset_api(
        reset_url=cfg.capture_reset_url,
        api_token=cfg.capture_reset_token or None,
        timeout_seconds=cfg.capture_reset_timeout_seconds,
    )
    if reset_state.ok and reset_state.allow_capture:
        print("Capture lock cleared by reset API. Ready for next photo.")
        return False, last_reset_poll_ts, last_reset_error_ts
    if not reset_state.ok and (now - last_reset_error_ts) >= 5.0:
        print(f"Reset API warning: {reset_state.message} (status={reset_state.status_code})")
        last_reset_error_ts = now
    return capture_locked, last_reset_poll_ts, last_reset_error_ts


def process_single_image(image_path: str, cfg: ScannerConfig, show_windows: bool = True) -> int:
    frame = cv2.imread(image_path)
    if frame is None:
        print(f"Error: Cannot read image: {image_path}")
        return 1

    fh, fw = frame.shape[:2]
    warp_short = compute_warp_short_side(fw, fh, cfg)
    dst_size = a4_target_size(warp_short, cfg.a4_ratio)
    quad, confidence, _debug = detect_document_quad(frame, cfg)
    vis = frame.copy()

    if quad is None or confidence < cfg.confidence_threshold:
        print("No confident document detection in this image.")
        print(f"Confidence: {confidence:.2f} (threshold: {cfg.confidence_threshold:.2f})")
        if show_windows:
            cv2.imshow(
                "A4 Scanner - Input",
                fit_for_display(vis, cfg.max_display_width, cfg.max_display_height),
            )
            cv2.waitKey(0)
            cv2.destroyAllWindows()
        return 2

    draw_quad(vis, quad, (0, 255, 0))
    warped = warp_document(frame, quad, dst_size, cfg=cfg)
    result = enhance_for_scan(warped) if cfg.apply_scan_enhancement else warped

    can_save, readability_result = can_save_by_readability(result, cfg)
    if not can_save:
        notify_unreadable(cfg, confidence, readability_result)
        if show_windows:
            cv2.imshow(
                "A4 Scanner - Input",
                fit_for_display(vis, cfg.max_display_width, cfg.max_display_height),
            )
            cv2.imshow(
                "A4 Scanner - Rectified",
                fit_for_display(result, cfg.max_display_width, cfg.max_display_height),
            )
            cv2.waitKey(0)
            cv2.destroyAllWindows()
        return 3

    print(f"Detected with confidence: {confidence:.2f}")
    persist_capture(result, cfg, readability_result=readability_result)

    if show_windows:
        cv2.imshow(
            "A4 Scanner - Input",
            fit_for_display(vis, cfg.max_display_width, cfg.max_display_height),
        )
        cv2.imshow(
            "A4 Scanner - Rectified",
            fit_for_display(result, cfg.max_display_width, cfg.max_display_height),
        )
        cv2.waitKey(0)
        cv2.destroyAllWindows()
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Adaptive A4 document scanner")
    parser.add_argument(
        "--image",
        type=str,
        default=None,
        help="Path to a saved photo to process once instead of webcam mode.",
    )
    parser.add_argument(
        "--verify-readable",
        action="store_true",
        help="Run OCR readability verification on the flattened image.",
    )
    parser.add_argument(
        "--upload-url",
        type=str,
        default="",
        help="If set, upload saved scans to this API endpoint.",
    )
    parser.add_argument(
        "--upload-token",
        type=str,
        default="",
        help="Optional bearer token for the upload API.",
    )
    parser.add_argument(
        "--tesseract-cmd",
        type=str,
        default="",
        help="Full path to tesseract executable (if not in PATH).",
    )
    parser.add_argument(
        "--no-gui",
        action="store_true",
        help="Disable OpenCV windows (useful on headless Ubuntu/Orange Pi).",
    )
    parser.add_argument(
        "--capture-reset-url",
        type=str,
        default="",
        help="API endpoint that unlocks next capture when it returns allow_capture=true.",
    )
    parser.add_argument(
        "--capture-reset-token",
        type=str,
        default="",
        help="Optional bearer token for capture reset API.",
    )
    parser.add_argument(
        "--unreadable-notify-url",
        type=str,
        default="",
        help="Optional API endpoint to call when readability gate rejects a capture.",
    )
    parser.add_argument(
        "--unreadable-notify-token",
        type=str,
        default="",
        help="Optional bearer token for unreadable-capture notify endpoint.",
    )
    return parser.parse_args()


def run_post_processors(
    image: np.ndarray,
    image_path: str,
    cfg: ScannerConfig,
    readability_result: ReadabilityResult | None = None,
) -> None:
    if cfg.enable_readability_check:
        r = readability_result
        if r is None:
            r = verify_readability(
                image,
                min_confidence=cfg.min_readability_confidence,
                tesseract_cmd=cfg.tesseract_cmd,
                mode=cfg.readability_mode,
            )
        print(
            "Readability:",
            f"{r.message} | readable={r.readable} | mean_conf={r.mean_confidence:.2f} | tokens={r.token_count}",
        )

    if cfg.upload_enabled and cfg.upload_url:
        # Memory upload: avoid writing files on successful uploads.
        if cfg.upload_from_memory:
            image_bytes = encode_png_bytes(image)
            filename = "scan.png"
            u = upload_scan_bytes(
                image_bytes=image_bytes,
                filename=filename,
                upload_url=cfg.upload_url,
                api_token=cfg.upload_token or None,
                timeout_seconds=cfg.upload_timeout_seconds,
                field_name=cfg.upload_field_name,
            )
            print(f"Upload: {u.message} (status={u.status_code})")
            if u.response_preview:
                print(f"Upload response: {u.response_preview}")

            if not u.ok and cfg.save_on_upload_fail:
                saved_path = save_scan(image, cfg.save_dir)
                print(f"Upload failed; kept local copy: {saved_path}")
            return

        # Disk upload: upload the saved file, then optionally delete it.
        u = upload_scan(
            image_path=image_path,
            upload_url=cfg.upload_url,
            api_token=cfg.upload_token or None,
            timeout_seconds=cfg.upload_timeout_seconds,
            field_name=cfg.upload_field_name,
        )
        print(f"Upload: {u.message} (status={u.status_code})")
        if u.response_preview:
            print(f"Upload response: {u.response_preview}")

        if u.ok and cfg.delete_after_upload_success:
            try:
                os.remove(image_path)
                print(f"Deleted local copy after successful upload: {image_path}")
            except Exception as exc:
                print(f"Warning: could not delete local copy: {exc}")


def can_save_by_readability(
    image: np.ndarray,
    cfg: ScannerConfig,
) -> tuple[bool, ReadabilityResult | None]:
    if not cfg.enable_readability_check or not cfg.require_readable_to_save:
        return True, None

    r = verify_readability(
        image,
        min_confidence=cfg.min_readability_confidence,
        tesseract_cmd=cfg.tesseract_cmd,
        mode=cfg.readability_mode,
    )
    print(
        "Readability gate:",
        f"{r.message} | readable={r.readable} | mean_conf={r.mean_confidence:.2f} | tokens={r.token_count}",
    )
    if not r.readable:
        print("Save skipped: flattened image is not readable by current OCR threshold.")
    return r.readable, r


def run_webcam(cfg: ScannerConfig) -> int:
    api = _camera_api_preference(cfg)
    cap = cv2.VideoCapture(cfg.camera_index, api)
    if not cap.isOpened():
        print("Error: Cannot open camera. Check camera index or webcam connection.")
        return 1
    aw, ah = apply_camera_settings(cap, cfg)
    warp_short = compute_warp_short_side(aw, ah, cfg)
    dst_size = a4_target_size(warp_short, cfg.a4_ratio)
    print(f"Rectified output size (approx A4): {dst_size[0]}x{dst_size[1]} (short side {warp_short})")

    cv2.namedWindow("A4 Scanner", cv2.WINDOW_NORMAL)
    cv2.namedWindow("Rectified", cv2.WINDOW_NORMAL)

    selector = ManualSelector("A4 Scanner")
    cv2.setMouseCallback("A4 Scanner", selector.on_mouse)

    mode = cfg.start_mode.strip().upper() if cfg.start_mode else "AUTO"
    if mode not in {"AUTO", "MANUAL"}:
        mode = "AUTO"
    if cfg.single_capture_until_api_reset and not cfg.capture_reset_url:
        print(
            "Warning: single_capture_until_api_reset is enabled without capture_reset_url. "
            "Scanner will stay locked after first capture."
        )
    prev_quad = None
    warped_preview = np.zeros((dst_size[1], dst_size[0], 3), dtype=np.uint8)

    frame_count = 0
    detect_hits = 0
    total_conf = 0.0
    save_count = 0
    stable_frames = 0
    capture_locked = False
    last_reset_poll_ts = 0.0
    last_reset_error_ts = 0.0

    t0 = time.time()
    prev_fps_t = time.time()
    fps = 0.0

    try:
        while True:
            ok, frame = cap.read()
            if not ok:
                print("Warning: Camera read failed. Exiting loop.")
                break

            frame_count += 1
            now = time.time()
            dt = now - prev_fps_t
            if dt > 0:
                fps = 1.0 / dt
            prev_fps_t = now

            vis = frame.copy()
            active_quad = None
            confidence = 0.0

            if mode == "AUTO":
                selector.enabled = False
                quad, confidence, _debug = detect_document_quad(frame, cfg)
                if quad is not None:
                    total_conf += confidence
                    if confidence >= cfg.confidence_threshold:
                        detect_hits += 1
                        active_quad = smooth_quad(quad, prev_quad, cfg.smoothing_alpha)
                        prev_quad = active_quad
            else:
                selector.enabled = True
                vis = selector.draw(vis)
                manual_quad = selector.get_quad()
                if manual_quad is not None:
                    active_quad = manual_quad
                    confidence = 1.0

            if active_quad is not None:
                draw_quad(vis, active_quad, (0, 255, 255) if mode == "MANUAL" else (0, 255, 0))
                warped = warp_document(frame, active_quad, dst_size, cfg=cfg)
                warped_preview = enhance_for_scan(warped) if cfg.apply_scan_enhancement else warped
            else:
                stable_frames = 0

            if cfg.single_capture_until_api_reset:
                capture_locked, last_reset_poll_ts, last_reset_error_ts = clear_capture_lock_if_api_allows(
                    cfg=cfg,
                    now=now,
                    capture_locked=capture_locked,
                    last_reset_poll_ts=last_reset_poll_ts,
                    last_reset_error_ts=last_reset_error_ts,
                )

            auto_active = cfg.auto_capture_enabled and mode == "AUTO"
            if auto_active and active_quad is not None and not capture_locked:
                stable_frames += 1
                if stable_frames >= max(1, cfg.auto_capture_stable_frames):
                    can_save, readability_result = can_save_by_readability(warped_preview, cfg)
                    if can_save:
                        persist_capture(warped_preview, cfg, readability_result=readability_result)
                        save_count += 1
                    else:
                        notify_unreadable(cfg, confidence, readability_result)
                    if cfg.single_capture_until_api_reset:
                        capture_locked = True
                        print("Capture lock raised: waiting for reset API before next photo.")
                    stable_frames = 0

            if auto_active:
                if capture_locked:
                    if cfg.capture_reset_url:
                        auto_status = "Auto capture: LOCKED | waiting reset API"
                    else:
                        auto_status = "Auto capture: LOCKED | set capture_reset_url"
                else:
                    auto_status = f"Auto capture: READY {stable_frames}/{max(1, cfg.auto_capture_stable_frames)}"
            else:
                auto_status = "Auto capture: OFF (switch to AUTO mode)"

            put_status(vis, mode, confidence, fps, save_count, auto_status)
            cv2.imshow(
                "A4 Scanner",
                fit_for_display(vis, cfg.max_display_width, cfg.max_display_height),
            )
            cv2.imshow(
                "Rectified",
                fit_for_display(warped_preview, cfg.max_display_width, cfg.max_display_height),
            )

            key = cv2.waitKey(1) & 0xFF
            if key == ord("q"):
                break
            if key == ord("a"):
                mode = "AUTO"
                selector.reset()
            elif key == ord("m"):
                mode = "MANUAL"
                prev_quad = None
            elif key == ord("r"):
                selector.reset()
    finally:
        cap.release()
        cv2.destroyAllWindows()
        elapsed = max(0.001, time.time() - t0)
        avg_fps = frame_count / elapsed
        hit_rate = (detect_hits / max(1, frame_count)) * 100.0
        avg_conf = total_conf / max(1, frame_count)
        print("\nRun Metrics")
        print(f"- Frames processed: {frame_count}")
        print(f"- Elapsed seconds: {elapsed:.2f}")
        print(f"- Average FPS: {avg_fps:.2f}")
        print(f"- Auto detect hit rate: {hit_rate:.2f}%")
        print(f"- Mean confidence: {avg_conf:.3f}")
    return 0


def main() -> int:
    args = parse_args()
    cfg = ScannerConfig()
    if args.verify_readable:
        cfg.enable_readability_check = True
    if args.upload_url:
        cfg.upload_enabled = True
        cfg.upload_url = args.upload_url
    if args.upload_token:
        cfg.upload_token = args.upload_token
    if args.capture_reset_url:
        cfg.capture_reset_url = args.capture_reset_url
    if args.capture_reset_token:
        cfg.capture_reset_token = args.capture_reset_token
    if args.unreadable_notify_url:
        cfg.unreadable_notify_enabled = True
        cfg.unreadable_notify_url = args.unreadable_notify_url
    if args.unreadable_notify_token:
        cfg.unreadable_notify_token = args.unreadable_notify_token
    if args.tesseract_cmd:
        cfg.tesseract_cmd = args.tesseract_cmd
    if args.image:
        return process_single_image(args.image, cfg, show_windows=not args.no_gui)
    return run_webcam(cfg)


if __name__ == "__main__":
    raise SystemExit(main())


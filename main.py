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
    detect_document_quad,
    enhance_for_scan,
    smooth_quad,
    upload_scan,
    upload_scan_bytes,
    verify_readability,
    warp_document,
)


def put_status(frame: np.ndarray, mode: str, confidence: float, fps: float, saves: int) -> None:
    lines = [
        f"Mode: {mode}",
        f"Confidence: {confidence:.2f}",
        f"FPS: {fps:.1f}",
        f"Saved: {saves}",
        "Starts in AUTO | Keys: [a] auto  [m] manual  [r] reset  [s] save  [q] quit",
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


def fit_for_display(image: np.ndarray, max_width: int, max_height: int) -> np.ndarray:
    h, w = image.shape[:2]
    if w <= max_width and h <= max_height:
        return image
    scale = min(max_width / float(w), max_height / float(h))
    new_size = (max(1, int(w * scale)), max(1, int(h * scale)))
    return cv2.resize(image, new_size, interpolation=cv2.INTER_AREA)


def process_single_image(image_path: str, cfg: ScannerConfig, show_windows: bool = True) -> int:
    frame = cv2.imread(image_path)
    if frame is None:
        print(f"Error: Cannot read image: {image_path}")
        return 1

    dst_size = a4_target_size(cfg.warp_short_side, cfg.a4_ratio)
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
    warped = warp_document(frame, quad, dst_size)
    result = enhance_for_scan(warped) if cfg.apply_scan_enhancement else warped

    if not can_save_by_readability(result, cfg):
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

    # If upload is enabled and configured for memory mode, avoid saving to disk.
    if cfg.upload_enabled and cfg.upload_url and cfg.upload_from_memory:
        run_post_processors(result, image_path="", cfg=cfg)
        print("Rectified image uploaded from memory (local file kept only if upload fails).")
    else:
        out_path = save_scan(result, cfg.save_dir)
        run_post_processors(result, out_path, cfg)
        if os.path.exists(out_path):
            print(f"Saved rectified image: {out_path}")
        else:
            print("Upload successful; local file deleted after upload.")

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
    return parser.parse_args()


def run_post_processors(image: np.ndarray, image_path: str, cfg: ScannerConfig) -> None:
    if cfg.enable_readability_check:
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


def can_save_by_readability(image: np.ndarray, cfg: ScannerConfig) -> bool:
    if not cfg.enable_readability_check or not cfg.require_readable_to_save:
        return True

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
    return r.readable


def run_webcam(cfg: ScannerConfig) -> int:
    dst_size = a4_target_size(cfg.warp_short_side, cfg.a4_ratio)

    cap = cv2.VideoCapture(cfg.camera_index)
    if not cap.isOpened():
        print("Error: Cannot open camera. Check camera index or webcam connection.")
        return 1
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, cfg.frame_width)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, cfg.frame_height)

    cv2.namedWindow("A4 Scanner", cv2.WINDOW_NORMAL)
    cv2.namedWindow("Rectified", cv2.WINDOW_NORMAL)

    selector = ManualSelector("A4 Scanner")
    cv2.setMouseCallback("A4 Scanner", selector.on_mouse)

    mode = cfg.start_mode.strip().upper() if cfg.start_mode else "AUTO"
    if mode not in {"AUTO", "MANUAL"}:
        mode = "AUTO"
    prev_quad = None
    warped_preview = np.zeros((dst_size[1], dst_size[0], 3), dtype=np.uint8)

    frame_count = 0
    detect_hits = 0
    total_conf = 0.0
    save_count = 0

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
                warped = warp_document(frame, active_quad, dst_size)
                warped_preview = enhance_for_scan(warped) if cfg.apply_scan_enhancement else warped

            put_status(vis, mode, confidence, fps, save_count)
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
            elif key == ord("s"):
                if can_save_by_readability(warped_preview, cfg):
                    # Memory mode: do not create output file unless upload fails.
                    if cfg.upload_enabled and cfg.upload_url and cfg.upload_from_memory:
                        run_post_processors(warped_preview, image_path="", cfg=cfg)
                    else:
                        out_path = save_scan(warped_preview, cfg.save_dir)
                        save_count += 1
                        print(f"Saved: {out_path}")
                        run_post_processors(warped_preview, out_path, cfg)
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
    if args.tesseract_cmd:
        cfg.tesseract_cmd = args.tesseract_cmd
    if args.image:
        return process_single_image(args.image, cfg, show_windows=not args.no_gui)
    return run_webcam(cfg)


if __name__ == "__main__":
    raise SystemExit(main())


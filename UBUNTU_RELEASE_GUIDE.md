# Ubuntu Release Guide (A4 Scanner)

This guide prepares the project for production-style deployment on Ubuntu using `systemd`.

## 1) Install OS packages

```bash
sudo apt update
sudo apt install -y python3 python3-venv python3-pip tesseract-ocr libgl1 libglib2.0-0 v4l-utils curl
```

## 2) Create runtime user and app directory

```bash
sudo mkdir -p /opt/a4-flating
sudo chown -R $USER:$USER /opt/a4-flating
```

Copy your repo into `/opt/a4-flating` (or clone directly there).

## 3) Python environment

```bash
cd /opt/a4-flating
python3 -m venv .venv
source .venv/bin/activate
pip install --upgrade pip
pip install -r requirements.txt
```

## 4) Recommended Ubuntu config

Good defaults for Ubuntu are now auto-selected by code:

- Camera index defaults to `0` on Linux.
- Camera backend defaults to empty string (`CAP_ANY`/V4L2 path on Linux).

For explicit production behavior, set these env vars:

```bash
SCAN_CAMERA_INDEX=0
SCAN_CAMERA_BACKEND=V4L2
SCAN_CAMERA_FOURCC=MJPG
```

If your calibration file does not exist, either provide it or disable fisheye correction in config.

## 5) Create service env file

```bash
sudo cp deploy/ubuntu/a4-scanner.env.example /etc/default/a4-scanner
sudo nano /etc/default/a4-scanner
```

Set at least:

- `SCANNER_SERVICE_HOST=0.0.0.0`
- `SCANNER_SERVICE_PORT=8008`
- `SCANNER_SERVICE_TOKEN=<strong-random-token>`

## 6) Install systemd service

```bash
sudo cp deploy/ubuntu/scanner-service.service /etc/systemd/system/a4-scanner.service
sudo nano /etc/systemd/system/a4-scanner.service
```

Update these values if needed:

- `User=ubuntu` (or your service user)
- `WorkingDirectory=/opt/a4-flating`
- `ExecStart=/opt/a4-flating/.venv/bin/python -m scanner_service`

If camera access fails, ensure service user is in `video` group:

```bash
sudo usermod -aG video ubuntu
```

Then reload and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now a4-scanner
```

## 7) Verify health

```bash
curl http://127.0.0.1:8008/health
```

With token-protected endpoints:

```bash
curl -H "Authorization: Bearer <token>" http://127.0.0.1:8008/session/manual-config
```

Check logs:

```bash
sudo journalctl -u a4-scanner -f
```

## 8) Useful operations

```bash
sudo systemctl restart a4-scanner
sudo systemctl status a4-scanner
sudo systemctl stop a4-scanner
```

## 9) Release checklist

- Ubuntu packages installed.
- `.venv` and Python deps installed.
- Service env file configured with real tokens/URLs.
- Service enabled and healthy after reboot.
- Camera device reachable (`v4l2-ctl --list-devices`).
- Upload/reset/notify endpoints tested (if enabled).

# Ubuntu Release Guide (Flask UI + API)

This guide prepares `PythonVersion` for production-style deployment on Ubuntu using `systemd`.

## 1) Install OS packages

```bash
sudo apt update
sudo apt install -y python3 python3-venv python3-pip git
```

If you use USB serial printer access:

```bash
sudo usermod -aG dialout $USER
newgrp dialout
```

## 2) Clone project and install Python dependencies

```bash
sudo mkdir -p /opt
cd /opt
sudo git clone <YOUR_REPO_URL> diwan-signature
sudo chown -R $USER:$USER /opt/diwan-signature
cd /opt/diwan-signature

python3 -m venv .venv
source .venv/bin/activate
pip install -r PythonVersion/requirements.txt
```

## 3) Create runtime environment file

```bash
sudo mkdir -p /etc/diwan-signature
sudo cp PythonVersion/deploy/ubuntu/diwan-signature.env.example /etc/diwan-signature/diwan-signature.env
sudo nano /etc/diwan-signature/diwan-signature.env
```

At minimum set:

- `CAPTURE_RESET_URL` to your reset endpoint.

If scanner integration is used, also set:

- `SCANNER_SERVICE_BASE_URL`
- `SCANNER_SERVICE_BEARER_TOKEN` (if required)

## 4) Install systemd service

```bash
sudo cp PythonVersion/deploy/ubuntu/diwan-signature-flask.service /etc/systemd/system/diwan-signature-flask.service
```

Edit service user/group/path if needed:

```bash
sudo nano /etc/systemd/system/diwan-signature-flask.service
```

Important fields:

- `User` should be your deployment user.
- `Group` should usually be `dialout` for serial access.
- `WorkingDirectory` should be your repo path.
- `ExecStart` should point to that repo `.venv` Python.

## 5) Start and enable service

```bash
sudo systemctl daemon-reload
sudo systemctl enable diwan-signature-flask
sudo systemctl start diwan-signature-flask
sudo systemctl status diwan-signature-flask
```

## 6) Verify application

Local health check:

```bash
curl http://127.0.0.1:5001/api/health
```

UI:

- `http://<SERVER_IP>:5001/`
- `http://<SERVER_IP>:5001/configuration`

## 7) Logs and troubleshooting

Follow logs:

```bash
sudo journalctl -u diwan-signature-flask -f
```

Common checks:

- Service does not start:
  - verify `WorkingDirectory` and `ExecStart` paths.
  - verify `.venv` exists and dependencies installed.
- Capture endpoints fail:
  - verify `CAPTURE_RESET_URL` and connectivity.
- Scanner endpoints fail:
  - verify scanner base URL/token in env file.
- Printer connect fails:
  - verify `/dev/ttyUSB0` or `/dev/ttyACM0`.
  - verify user/group has `dialout`.

## 8) Update deployment (new release)

```bash
cd /opt/diwan-signature
git pull
source .venv/bin/activate
pip install -r PythonVersion/requirements.txt
sudo systemctl restart diwan-signature-flask
```

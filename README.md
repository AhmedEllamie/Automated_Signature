# Automated Signature (meta repository)

Thin parent repo that wires together the **plotter** and **A4 scanner** services as **Git submodules**.

| Submodule path | GitHub repo | Role |
|----------------|-------------|------|
| `plotter-signature/` | [AhmedEllamie/Plotter](https://github.com/AhmedEllamie/Plotter) | Flask/FastAPI print & signature automation |
| `a4-flating/` | [AhmedEllamie/Scanner](https://github.com/AhmedEllamie/Scanner) | HTTP scanner service (capture, rectify, validate) |

## Clone

```bash
git clone --recurse-submodules https://github.com/AhmedEllamie/Automated_Signature.git
cd Automated_Signature
```

If you already cloned without submodules:

```bash
git submodule update --init --recursive
```

## Update submodules to latest remote commits

```bash
git submodule update --remote --merge
```

## Local development

**Plotter** (editable install):

```bash
cd plotter-signature
python -m venv .venv
.venv\Scripts\activate   # Windows
# source .venv/bin/activate   # Linux/macOS
pip install -r requirements.txt
pip install -e .
python -m plotter_signature serve-flask --host 0.0.0.0 --port 5001
```

**Scanner**:

```bash
cd a4-flating
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python -m scanner_service
```

## Ubuntu deployment

- Plotter: see `plotter-signature/docs/UBUNTU_RELEASE_GUIDE.md` and `plotter-signature/deploy/ubuntu/`.
- Scanner: see `a4-flating/UBUNTU_RELEASE_GUIDE.md` and `a4-flating/deploy/ubuntu/`.

Typical install paths (adjust if you mirror repos elsewhere):

- `/opt/plotter-signature` — checkout or submodule path `plotter-signature`
- `/opt/a4-flating` — checkout or submodule path `a4-flating` (repo root contains `scanner_service/`)

## Previous monorepo layout

The histories before submodules were introduced are preserved for reference as tag `archive/monorepo-main-before-submodules`.

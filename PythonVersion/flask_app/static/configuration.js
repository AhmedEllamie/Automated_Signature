const FIXED_BAUD_RATE = 250000;
const MAX_FOCUS_VALUE = 255;
const MIN_FOCUS_VALUE = 0;
const REQUIRED_QUAD_POINTS = 4;
const POINT_LABELS = ["TL", "TR", "BR", "BL"];

const uiState = {
  streamVisible: false,
  quadPoints: [],
};

function showConfigMessage(message, isError = false) {
  const node = document.getElementById("configMessage");
  if (!node) return;
  node.textContent = message;
  node.className = isError ? "message-error" : "message-ok";
}

function readConnectionForm() {
  return {
    comPort: document.getElementById("comPort").value.trim(),
    baudRate: FIXED_BAUD_RATE,
  };
}

function readPrintSettingsForm() {
  return {
    width: document.getElementById("width").value.trim(),
    height: document.getElementById("height").value.trim(),
    xPosition: document.getElementById("xPosition").value.trim(),
    yPosition: document.getElementById("yPosition").value.trim(),
    scale: Number(document.getElementById("scale").value || 1),
    rotation: Number(document.getElementById("rotation").value || 0),
    invertX: document.getElementById("invertX").checked,
    invertY: document.getElementById("invertY").checked,
    penMode: document.getElementById("penMode").value,
    penMaxDistanceM: document.getElementById("penMaxDistanceM").value.trim(),
  };
}

function readCaptureSettingsForm() {
  return {
    autofocusEnabled: document.getElementById("autofocusEnabled").checked,
    manualFocusValue: Number(document.getElementById("manualFocusValue").value || 35),
    quadPoints: uiState.quadPoints.map((point) => [point[0], point[1]]),
    streamFps: Number(document.getElementById("streamFps").value || 10),
    streamWidth: Number(document.getElementById("streamWidth").value || 0),
    streamFisheye: document.getElementById("streamFisheye").checked,
  };
}

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function renderFocusLabel() {
  const value = Number(document.getElementById("manualFocusValue").value || 35);
  document.getElementById("manualFocusValueLabel").textContent = String(value);
}

function renderQuadPoints() {
  const overlay = document.getElementById("streamPointsOverlay");
  overlay.innerHTML = "";
  uiState.quadPoints.forEach((point, index) => {
    const marker = document.createElement("div");
    marker.className = "stream-point";
    marker.style.left = `${point[0]}%`;
    marker.style.top = `${point[1]}%`;
    marker.textContent = POINT_LABELS[index] || String(index + 1);
    overlay.appendChild(marker);
  });

  const status = document.getElementById("quadPointsStatus");
  if (status) {
    status.textContent = `Points: ${uiState.quadPoints.length}/${REQUIRED_QUAD_POINTS} (click on stream: top-left, top-right, bottom-right, bottom-left)`;
  }
}

function hydrateConfiguration() {
  const connection = loadConnectionSettings();
  document.getElementById("comPort").value = connection.comPort || "";

  const print = loadPrintSettings();
  document.getElementById("width").value = print.width || "210mm";
  document.getElementById("height").value = print.height || "297mm";
  document.getElementById("xPosition").value = print.xPosition || "50mm";
  document.getElementById("yPosition").value = print.yPosition || "50mm";
  document.getElementById("scale").value = print.scale || 1;
  document.getElementById("rotation").value = print.rotation || 0;
  document.getElementById("invertX").checked = Boolean(print.invertX);
  document.getElementById("invertY").checked = Boolean(print.invertY);
  document.getElementById("penMode").value = print.penMode === "finish" ? "finish" : "start";
  document.getElementById("penMaxDistanceM").value = print.penMaxDistanceM || "";

  const capture = loadCaptureSettings();
  document.getElementById("autofocusEnabled").checked = Boolean(capture.autofocusEnabled);
  document.getElementById("manualFocusValue").value = clamp(Number(capture.manualFocusValue || 35), MIN_FOCUS_VALUE, MAX_FOCUS_VALUE);
  document.getElementById("streamFps").value = Number(capture.streamFps || 10);
  document.getElementById("streamWidth").value = Number(capture.streamWidth || 0);
  document.getElementById("streamFisheye").checked = Boolean(capture.streamFisheye);
  if (Array.isArray(capture.quadPoints)) {
    uiState.quadPoints = capture.quadPoints
      .filter((point) => Array.isArray(point) && point.length === 2)
      .map((point) => [Number(point[0]), Number(point[1])])
      .filter((point) => Number.isFinite(point[0]) && Number.isFinite(point[1]));
  }
  renderFocusLabel();
  renderQuadPoints();
}

function persistConnectionSettings() {
  saveConnectionSettings(readConnectionForm());
}

function persistPrintSettings() {
  savePrintSettings(readPrintSettingsForm());
}

function persistCaptureSettings() {
  saveCaptureSettings(readCaptureSettingsForm());
}

async function scanSerialPorts() {
  const btn = document.getElementById("scanPortsBtn");
  if (btn) btn.disabled = true;
  try {
    const data = await apiGet("/api/serial-ports");
    const list = document.getElementById("comPortList");
    list.innerHTML = "";
    for (const p of data.ports || []) {
      const opt = document.createElement("option");
      opt.value = p.device;
      const desc = [p.description, p.manufacturer].filter(Boolean).join(" — ");
      opt.textContent = desc || p.device;
      list.appendChild(opt);
    }
    const count = (data.ports || []).length;
    showConfigMessage(count ? `Found ${count} serial port(s). Pick from suggestions or type a port.` : "No serial ports found.");
  } catch (error) {
    showConfigMessage(`Scan failed: ${error.message}`, true);
  } finally {
    if (btn) btn.disabled = false;
  }
}

async function connectPrinter() {
  const comPort = document.getElementById("comPort").value.trim();
  const payload = { baudRate: FIXED_BAUD_RATE };
  if (comPort) {
    payload.comPort = comPort;
  }
  try {
    await apiPostJson("/api/connect", payload);
    persistConnectionSettings();
    showConfigMessage("Printer connected.");
  } catch (error) {
    showConfigMessage(`Connect error: ${error.message}`, true);
  }
}

async function disconnectPrinter() {
  try {
    await apiPostJson("/api/disconnect");
    showConfigMessage("Printer disconnected.");
  } catch (error) {
    showConfigMessage(`Disconnect error: ${error.message}`, true);
  }
}

async function runChangePen() {
  const mode = document.getElementById("penMode").value || "start";
  try {
    await apiPostJson(`/api/change-pen/${mode}`);
    persistPrintSettings();
    showConfigMessage(`ChangePen ${mode} completed.`);
  } catch (error) {
    showConfigMessage(`ChangePen error: ${error.message}`, true);
  }
}

async function runReset() {
  try {
    await apiPostJson("/api/reset", { clearUploadedSvg: false });
    showConfigMessage("Distance reset completed.");
  } catch (error) {
    showConfigMessage(`Reset error: ${error.message}`, true);
  }
}

async function setPenMaxDistance() {
  const rawValue = document.getElementById("penMaxDistanceM").value.trim();
  if (!rawValue) {
    showConfigMessage("Enter pen max distance in meters first.", true);
    return;
  }
  try {
    await apiPostJson("/api/pen-max-distance", { meters: Number(rawValue) });
    persistPrintSettings();
    showConfigMessage("Pen max distance updated.");
  } catch (error) {
    showConfigMessage(`Set pen max distance error: ${error.message}`, true);
  }
}

async function applyScannerManualConfig() {
  const capture = readCaptureSettingsForm();
  if (capture.quadPoints.length !== REQUIRED_QUAD_POINTS) {
    return;
  }

  const streamImage = document.getElementById("streamPreview");
  const naturalWidth = Number(streamImage.naturalWidth || 0);
  const naturalHeight = Number(streamImage.naturalHeight || 0);
  if (!naturalWidth || !naturalHeight) {
    return;
  }

  const quadPointsPx = capture.quadPoints.map((point) => [
    Math.round((point[0] / 100) * naturalWidth),
    Math.round((point[1] / 100) * naturalHeight),
  ]);

  const payload = {
    autofocus_enabled: Boolean(capture.autofocusEnabled),
    manual_focus_value: Number(capture.manualFocusValue || 35),
    quad_points: quadPointsPx,
  };
  await apiPostJson("/api/scanner/manual-config", payload);
}

function registerPersistenceListeners() {
  const connectionFields = ["comPort"];
  const printFields = [
    "width",
    "height",
    "xPosition",
    "yPosition",
    "scale",
    "rotation",
    "invertX",
    "invertY",
    "penMode",
    "penMaxDistanceM",
  ];
  const captureFields = [
    "autofocusEnabled",
    "manualFocusValue",
    "streamFps",
    "streamWidth",
    "streamFisheye",
  ];

  connectionFields.forEach((id) => {
    const node = document.getElementById(id);
    node.addEventListener("input", persistConnectionSettings);
    node.addEventListener("change", persistConnectionSettings);
  });

  printFields.forEach((id) => {
    const node = document.getElementById(id);
    node.addEventListener("input", persistPrintSettings);
    node.addEventListener("change", persistPrintSettings);
  });

  captureFields.forEach((id) => {
    const node = document.getElementById(id);
    node.addEventListener("input", persistCaptureSettings);
    node.addEventListener("change", persistCaptureSettings);
  });
}

function buildStreamUrl() {
  const capture = readCaptureSettingsForm();
  const params = new URLSearchParams();
  params.set("fps", String(Math.min(25, Math.max(1, Number(capture.streamFps || 10)))));
  params.set("width", String(Math.max(0, Number(capture.streamWidth || 0))));
  params.set("fisheye", capture.streamFisheye ? "1" : "0");
  return `/api/scanner/stream.mjpg?${params.toString()}`;
}

function showStreamInline() {
  persistCaptureSettings();
  const url = buildStreamUrl();
  const img = document.getElementById("streamPreview");
  img.src = `${url}&t=${Date.now()}`;
  uiState.streamVisible = true;
  showConfigMessage("Live stream started.");
}

function stopStreamInline() {
  const img = document.getElementById("streamPreview");
  img.src = "";
  uiState.streamVisible = false;
  showConfigMessage("Live stream stopped.");
}

function clearQuadPoints() {
  uiState.quadPoints = [];
  renderQuadPoints();
  persistCaptureSettings();
}

async function adjustManualFocus(delta) {
  const input = document.getElementById("manualFocusValue");
  const current = Number(input.value || 35);
  const next = clamp(current + delta, MIN_FOCUS_VALUE, MAX_FOCUS_VALUE);
  input.value = String(next);
  renderFocusLabel();
  persistCaptureSettings();
  try {
    await applyScannerManualConfig();
    showConfigMessage(`Manual focus set to ${next}.`);
  } catch (error) {
    showConfigMessage(`Manual focus update failed: ${error.message}`, true);
  }
}

function addQuadPointFromClick(event) {
  const img = document.getElementById("streamPreview");
  if (!uiState.streamVisible || !img.src) {
    showConfigMessage("Start stream first.", true);
    return;
  }
  const rect = img.getBoundingClientRect();
  if (!rect.width || !rect.height) {
    return;
  }
  const xPercent = ((event.clientX - rect.left) / rect.width) * 100;
  const yPercent = ((event.clientY - rect.top) / rect.height) * 100;
  if (!Number.isFinite(xPercent) || !Number.isFinite(yPercent)) {
    return;
  }

  if (uiState.quadPoints.length >= REQUIRED_QUAD_POINTS) {
    uiState.quadPoints = [];
  }
  uiState.quadPoints.push([
    clamp(Number(xPercent.toFixed(4)), 0, 100),
    clamp(Number(yPercent.toFixed(4)), 0, 100),
  ]);
  renderQuadPoints();
  persistCaptureSettings();
  if (uiState.quadPoints.length === REQUIRED_QUAD_POINTS) {
    void applyScannerManualConfig()
      .then(() => showConfigMessage("4 points applied to scanner manual config."))
      .catch((error) => showConfigMessage(`Apply points failed: ${error.message}`, true));
  }
}

function registerActions() {
  document.getElementById("scanPortsBtn").addEventListener("click", scanSerialPorts);
  document.getElementById("connectBtn").addEventListener("click", connectPrinter);
  document.getElementById("disconnectBtn").addEventListener("click", disconnectPrinter);
  document.getElementById("setPenMaxBtn").addEventListener("click", setPenMaxDistance);
  document.getElementById("changePenBtn").addEventListener("click", runChangePen);
  document.getElementById("resetBtn").addEventListener("click", runReset);
  document.getElementById("showStreamBtn").addEventListener("click", showStreamInline);
  document.getElementById("stopStreamBtn").addEventListener("click", stopStreamInline);
  document.getElementById("clearPointsBtn").addEventListener("click", clearQuadPoints);
  document.getElementById("focusDownBtn").addEventListener("click", () => {
    void adjustManualFocus(-1);
  });
  document.getElementById("focusUpBtn").addEventListener("click", () => {
    void adjustManualFocus(1);
  });
  document.getElementById("streamPreview").addEventListener("click", addQuadPointFromClick);
  document.getElementById("autofocusEnabled").addEventListener("change", () => {
    persistCaptureSettings();
    void applyScannerManualConfig().catch(() => {});
  });
}

function initConfigurationPage() {
  hydrateConfiguration();
  registerPersistenceListeners();
  registerActions();
  showConfigMessage("Settings are saved automatically in this browser.");
}

initConfigurationPage();

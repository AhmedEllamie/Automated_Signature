const FIXED_BAUD_RATE = 250000;

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
    quadPointsText: document.getElementById("quadPointsText").value.trim(),
    streamFps: Number(document.getElementById("streamFps").value || 10),
    streamWidth: Number(document.getElementById("streamWidth").value || 0),
    streamFisheye: document.getElementById("streamFisheye").checked,
  };
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
  document.getElementById("manualFocusValue").value = Number(capture.manualFocusValue || 35);
  document.getElementById("quadPointsText").value = capture.quadPointsText || "";
  document.getElementById("streamFps").value = Number(capture.streamFps || 10);
  document.getElementById("streamWidth").value = Number(capture.streamWidth || 0);
  document.getElementById("streamFisheye").checked = Boolean(capture.streamFisheye);
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
    "quadPointsText",
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

function showStreamWindow() {
  persistCaptureSettings();
  const url = buildStreamUrl();
  window.open(url, "_blank", "noopener,noreferrer");
  showConfigMessage("Live stream opened. Use it to pick exact 4 points.");
}

function registerActions() {
  document.getElementById("scanPortsBtn").addEventListener("click", scanSerialPorts);
  document.getElementById("connectBtn").addEventListener("click", connectPrinter);
  document.getElementById("disconnectBtn").addEventListener("click", disconnectPrinter);
  document.getElementById("setPenMaxBtn").addEventListener("click", setPenMaxDistance);
  document.getElementById("changePenBtn").addEventListener("click", runChangePen);
  document.getElementById("resetBtn").addEventListener("click", runReset);
  document.getElementById("showStreamBtn").addEventListener("click", showStreamWindow);
}

function initConfigurationPage() {
  hydrateConfiguration();
  registerPersistenceListeners();
  registerActions();
  showConfigMessage("Settings are saved automatically in this browser.");
}

initConfigurationPage();

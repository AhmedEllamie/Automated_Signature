const state = {
  uploadedSvgName: null,
  capturePollHandle: null,
};

function appendLog(message, isError = false) {
  const logBox = document.getElementById("logBox");
  if (!logBox) return;
  const line = document.createElement("div");
  line.className = `log-line${isError ? " error" : ""}`;
  line.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
  logBox.prepend(line);
}

function clampPercent(value) {
  const asNumber = Number(value);
  if (!Number.isFinite(asNumber)) return 0;
  return Math.max(0, Math.min(100, asNumber));
}

function buildPrintSettingsPayload() {
  const settings = loadPrintSettings();
  return {
    width: settings.width,
    height: settings.height,
    xPosition: settings.xPosition,
    yPosition: settings.yPosition,
    scale: Number(settings.scale || 1),
    rotation: Number(settings.rotation || 0),
    invertX: Boolean(settings.invertX),
    invertY: Boolean(settings.invertY),
  };
}

function setBadgeState(elementId, text, className) {
  const node = document.getElementById(elementId);
  if (!node) return;
  node.textContent = text;
  node.className = `badge ${className}`;
}

function formatDistanceMetersFromMm(mmValue) {
  const meters = Number(mmValue) / 1000;
  if (!Number.isFinite(meters)) return "0.000 m";
  return `${meters.toFixed(3)} m`;
}

function renderStatusGui(status) {
  const isConnected = Boolean(status.is_open);
  const isBusy = Boolean(status.is_printing);
  const executionPercent = clampPercent(status.current_execution_percent);
  const remainingPenPercent = clampPercent(status.remaining_pen_percent);
  const hasPenConfig = Number(status.max_pen_distance_m || 0) > 0;

  setBadgeState(
    "statusConnectionBadge",
    isConnected ? "Connected" : "Disconnected",
    isConnected ? "badge-ok" : "badge-neutral"
  );
  setBadgeState(
    "statusBusyBadge",
    isBusy ? "Busy" : "Idle",
    isBusy ? "badge-warn" : "badge-ok"
  );

  const portNode = document.getElementById("statusPort");
  if (portNode) {
    portNode.textContent = status.port_name || "N/A";
  }

  const cumulativeNode = document.getElementById("statusCumulativeDistance");
  if (cumulativeNode) {
    cumulativeNode.textContent = formatDistanceMetersFromMm(status.cumulative_distance_mm);
  }

  const executedNode = document.getElementById("statusExecutedDistance");
  if (executedNode) {
    executedNode.textContent = formatDistanceMetersFromMm(status.current_executed_distance_mm);
  }

  const executionPercentNode = document.getElementById("statusExecutionPercent");
  if (executionPercentNode) {
    executionPercentNode.textContent = `${executionPercent.toFixed(2)}%`;
  }

  const executionFillNode = document.getElementById("statusExecutionFill");
  if (executionFillNode) {
    executionFillNode.style.width = `${executionPercent}%`;
  }

  const penPercentNode = document.getElementById("statusPenRemaining");
  if (penPercentNode) {
    penPercentNode.textContent = hasPenConfig ? `${remainingPenPercent.toFixed(2)}%` : "N/A";
  }

  const penFillNode = document.getElementById("statusPenFill");
  if (penFillNode) {
    penFillNode.style.width = `${hasPenConfig ? remainingPenPercent : 0}%`;
  }
}

async function refreshStatus() {
  try {
    const status = await apiGet("/api/status");
    renderStatusGui(status);
    appendLog("Status refreshed.");
  } catch (error) {
    appendLog(`Status error: ${error.message}`, true);
  }
}

async function uploadSvgFromFile(file) {
  const formData = new FormData();
  formData.append("svg", file);
  try {
    const data = await apiPostForm("/api/upload", formData);
    state.uploadedSvgName = data.fileName;
    document.getElementById("uploadedSvgLabel").textContent = `Uploaded SVG: ${data.fileName}`;
    appendLog(`SVG uploaded (${data.fileName}).`);
  } catch (error) {
    appendLog(`Upload error: ${error.message}`, true);
  }
}

async function printUploadedSvg() {
  const payload = { printRequest: buildPrintSettingsPayload() };
  try {
    const data = await apiPostJson("/api/print", payload);
    appendLog(`Print completed. Commands sent: ${data.result.commands_sent}.`);
    await refreshStatus();
  } catch (error) {
    appendLog(`Print error: ${error.message}`, true);
  }
}

async function runVoid() {
  try {
    await apiPostJson("/api/void");
    appendLog("Void completed.");
    await refreshStatus();
  } catch (error) {
    appendLog(`Void error: ${error.message}`, true);
  }
}

async function loadLatestCapture() {
  const response = await fetch("/api/capture/latest");
  if (response.status === 404) {
    return false;
  }
  const payload = await response.json();
  if (!response.ok || payload.success === false) {
    throw new Error(payload?.message || `Capture load failed (${response.status})`);
  }

  const data = payload.data;
  const imageEl = document.getElementById("capturePreview");
  imageEl.src = `${data.imageUrl}?t=${Date.now()}`;
  imageEl.style.display = "block";
  return true;
}

function startCapturePolling(maxAttempts = 20, intervalMs = 2000) {
  if (state.capturePollHandle) {
    clearInterval(state.capturePollHandle);
    state.capturePollHandle = null;
  }

  let attempts = 0;
  state.capturePollHandle = setInterval(async () => {
    attempts += 1;
    try {
      const loaded = await loadLatestCapture();
      if (loaded) {
        appendLog("Captured photo received.");
        clearInterval(state.capturePollHandle);
        state.capturePollHandle = null;
      } else if (attempts >= maxAttempts) {
        appendLog("Capture polling timed out (no image yet).", true);
        clearInterval(state.capturePollHandle);
        state.capturePollHandle = null;
      }
    } catch (error) {
      appendLog(`Capture polling error: ${error.message}`, true);
      clearInterval(state.capturePollHandle);
      state.capturePollHandle = null;
    }
  }, intervalMs);
}

async function requestCapture() {
  try {
    await apiPostJson("/api/capture/request", {});
    appendLog("Capture reset command sent. Waiting for callback image...");
    startCapturePolling();
  } catch (error) {
    appendLog(`Capture request error: ${error.message}`, true);
  }
}

function registerEvents() {
  document.getElementById("captureBtn").addEventListener("click", requestCapture);
  document.getElementById("printBtn").addEventListener("click", printUploadedSvg);
  document.getElementById("uploadBtn").addEventListener("click", () => {
    document.getElementById("svgFileInput").click();
  });
  document.getElementById("voidBtn").addEventListener("click", runVoid);
  document.getElementById("statusBtn").addEventListener("click", refreshStatus);

  document.getElementById("svgFileInput").addEventListener("change", async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;
    await uploadSvgFromFile(file);
    event.target.value = "";
  });
}

async function initPage() {
  registerEvents();
  await refreshStatus();
  try {
    await loadLatestCapture();
  } catch (error) {
    appendLog(`Initial capture check: ${error.message}`, true);
  }
}

initPage();

const state = {
  uploadedSvgName: null,
  capturePollHandle: null,
};

function appendLog(message, isError = false) {
  const logBox = document.getElementById("logBox");
  const line = document.createElement("div");
  line.className = `log-line${isError ? " error" : ""}`;
  line.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
  logBox.prepend(line);
}

async function parseApiResponse(response) {
  let payload = null;
  try {
    payload = await response.json();
  } catch (error) {
    throw new Error(`Invalid API response (${response.status})`);
  }

  if (!response.ok || payload.success === false) {
    const msg = payload?.message || `Request failed (${response.status})`;
    throw new Error(msg);
  }
  return payload.data;
}

async function apiGet(url) {
  const response = await fetch(url, { method: "GET" });
  return parseApiResponse(response);
}

async function apiPostJson(url, body = {}) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  return parseApiResponse(response);
}

async function apiPostForm(url, formData) {
  const response = await fetch(url, {
    method: "POST",
    body: formData,
  });
  return parseApiResponse(response);
}

function collectPrintSettings() {
  return {
    width: document.getElementById("width").value.trim(),
    height: document.getElementById("height").value.trim(),
    xPosition: document.getElementById("xPosition").value.trim(),
    yPosition: document.getElementById("yPosition").value.trim(),
    scale: Number(document.getElementById("scale").value || 1),
    rotation: Number(document.getElementById("rotation").value || 0),
    invertX: document.getElementById("invertX").checked,
    invertY: document.getElementById("invertY").checked,
  };
}

function updateStatusBox(status) {
  const statusBox = document.getElementById("statusBox");
  statusBox.textContent = JSON.stringify(status, null, 2);
}

async function refreshStatus() {
  try {
    const status = await apiGet("/api/status");
    updateStatusBox(status);
    appendLog("Status refreshed.");
  } catch (error) {
    appendLog(`Status error: ${error.message}`, true);
  }
}

async function connectPrinter() {
  const comPort = document.getElementById("comPort").value.trim();
  const baudRateRaw = document.getElementById("baudRate").value.trim();
  const payload = {};
  if (comPort) payload.comPort = comPort;
  if (baudRateRaw) payload.baudRate = Number(baudRateRaw);

  try {
    await apiPostJson("/api/connect", payload);
    appendLog("Printer connected.");
    await refreshStatus();
  } catch (error) {
    appendLog(`Connect error: ${error.message}`, true);
  }
}

async function disconnectPrinter() {
  try {
    await apiPostJson("/api/disconnect");
    appendLog("Printer disconnected.");
    await refreshStatus();
  } catch (error) {
    appendLog(`Disconnect error: ${error.message}`, true);
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
  const payload = { printRequest: collectPrintSettings() };
  try {
    const data = await apiPostJson("/api/print", payload);
    appendLog(`Print completed. Commands sent: ${data.result.commands_sent}.`);
    await refreshStatus();
  } catch (error) {
    appendLog(`Print error: ${error.message}`, true);
  }
}

async function runChangePen() {
  const mode = document.getElementById("penMode").value;
  try {
    await apiPostJson(`/api/change-pen/${mode}`);
    appendLog(`ChangePen ${mode} completed.`);
    await refreshStatus();
  } catch (error) {
    appendLog(`ChangePen error: ${error.message}`, true);
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

async function runReset() {
  try {
    await apiPostJson("/api/reset", { clearUploadedSvg: false });
    appendLog("Reset completed.");
    await refreshStatus();
  } catch (error) {
    appendLog(`Reset error: ${error.message}`, true);
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
  document.getElementById("changePenBtn").addEventListener("click", runChangePen);
  document.getElementById("statusBtn").addEventListener("click", refreshStatus);

  document.getElementById("connectBtn").addEventListener("click", connectPrinter);
  document.getElementById("disconnectBtn").addEventListener("click", disconnectPrinter);
  document.getElementById("voidBtn").addEventListener("click", runVoid);
  document.getElementById("resetBtn").addEventListener("click", runReset);

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

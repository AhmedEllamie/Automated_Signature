function showConfigMessage(message, isError = false) {
  const node = document.getElementById("configMessage");
  if (!node) return;
  node.textContent = message;
  node.className = isError ? "message-error" : "message-ok";
}

function readConnectionForm() {
  return {
    comPort: document.getElementById("comPort").value.trim(),
    baudRate: Number(document.getElementById("baudRate").value || 250000),
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
  };
}

function hydrateConfiguration() {
  const connection = loadConnectionSettings();
  document.getElementById("comPort").value = connection.comPort || "";
  document.getElementById("baudRate").value = connection.baudRate || 250000;

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
}

function persistConnectionSettings() {
  saveConnectionSettings(readConnectionForm());
}

function persistPrintSettings() {
  savePrintSettings(readPrintSettingsForm());
}

async function connectPrinter() {
  const payload = readConnectionForm();
  if (!payload.comPort) {
    delete payload.comPort;
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
    showConfigMessage("Reset completed.");
  } catch (error) {
    showConfigMessage(`Reset error: ${error.message}`, true);
  }
}

function registerPersistenceListeners() {
  const connectionFields = ["comPort", "baudRate"];
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
}

function registerActions() {
  document.getElementById("connectBtn").addEventListener("click", connectPrinter);
  document.getElementById("disconnectBtn").addEventListener("click", disconnectPrinter);
  document.getElementById("changePenBtn").addEventListener("click", runChangePen);
  document.getElementById("resetBtn").addEventListener("click", runReset);
}

function initConfigurationPage() {
  hydrateConfiguration();
  registerPersistenceListeners();
  registerActions();
  showConfigMessage("Settings are saved automatically in this browser.");
}

initConfigurationPage();

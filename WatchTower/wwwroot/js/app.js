const AUTO_REFRESH_MS = 5000;
const OFFLINE_THRESHOLD_SECONDS = 120;

let autoRefreshInterval = null;
let currentDevicesController = null;
let currentHistoryController = null;
let autoRefreshRunning = false;

const state = {
    devices: [],
    visibleDevices: [],
    selectedDeviceId: null,
    isLoadingDevices: false,
    lastRefreshAt: null
};

const elements = {
    statusFilter: document.getElementById("statusFilter"),
    empresaFilter: document.getElementById("empresaFilter"),
    searchBox: document.getElementById("searchBox"),
    refreshButton: document.getElementById("refreshButton"),
    serviceHealthBadge: document.getElementById("serviceHealthBadge"),
    lastRefreshText: document.getElementById("lastRefreshText"),
    devicesMessage: document.getElementById("devicesMessage"),
    historyMessage: document.getElementById("historyMessage"),
    devicesTableBody: document.getElementById("devicesTableBody"),
    historyTableBody: document.getElementById("historyTableBody")
};

document.addEventListener("DOMContentLoaded", async () => {
    try {
        wireEvents();
        await refreshAll(false);
        startAutoRefresh();
    } catch (error) {
        console.error("Error inicializando WatchTower:", error);
        setServiceHealth("error", "Error UI");
    }
});

function wireEvents() {
    elements.refreshButton?.addEventListener("click", async () => {
        await refreshAll(false);
        restartAutoRefresh();
    });

    elements.statusFilter?.addEventListener("change", async () => {
        await refreshAll(false);
        restartAutoRefresh();
    });

    elements.empresaFilter?.addEventListener("change", async () => {
        applyLocalFiltersAndRender();
        await refreshSelectedPanels();
    });

    elements.searchBox?.addEventListener("input", async () => {
        applyLocalFiltersAndRender();
        await refreshSelectedPanels();
    });

    document.addEventListener("visibilitychange", async () => {
        if (document.hidden) {
            stopAutoRefresh();
        } else {
            await refreshAll(true);
            startAutoRefresh();
        }
    });
}

function startAutoRefresh() {
    stopAutoRefresh();

    autoRefreshInterval = setInterval(async () => {
        if (document.hidden || autoRefreshRunning) {
            return;
        }

        autoRefreshRunning = true;

        try {
            await refreshAll(true);
        } catch (error) {
            console.error("Error en auto refresh:", error);
        } finally {
            autoRefreshRunning = false;
        }
    }, AUTO_REFRESH_MS);
}

function stopAutoRefresh() {
    if (autoRefreshInterval) {
        clearInterval(autoRefreshInterval);
        autoRefreshInterval = null;
    }
}

function restartAutoRefresh() {
    startAutoRefresh();
}

async function refreshAll(isAutoRefresh = false) {
    await loadDevices(isAutoRefresh);
    await refreshSelectedPanels();
}

async function loadDevices(isAutoRefresh = false) {
    if (state.isLoadingDevices) {
        return;
    }

    state.isLoadingDevices = true;
    setServiceHealth("loading", "Actualizando...");

    if (currentDevicesController) {
        currentDevicesController.abort();
    }

    currentDevicesController = new AbortController();

    if (!isAutoRefresh) {
        setMessage(elements.devicesMessage, "Cargando dispositivos...");
        renderDevicesLoading();
    }

    const params = new URLSearchParams();

    if (elements.statusFilter?.value) {
        params.set("status", elements.statusFilter.value);
    }

    const url = params.toString()
        ? `/api/devices?${params.toString()}`
        : "/api/devices";

    try {
        const response = await fetch(url, {
            cache: "no-store",
            signal: currentDevicesController.signal
        });

        if (!response.ok) {
            throw new Error(`Error HTTP ${response.status}`);
        }

        const devices = await response.json();
        state.devices = Array.isArray(devices) ? devices : [];

        populateEmpresaFilter(state.devices);
        applyLocalFiltersAndRender();

        state.lastRefreshAt = new Date();
        updateLastRefreshText();
        setServiceHealth("ok", "Operativo");

        if (!state.visibleDevices.length) {
            state.selectedDeviceId = null;
            clearHistory("Sin datos históricos.");
            return;
        }

        if (!state.selectedDeviceId || !state.visibleDevices.some(d => d.deviceId === state.selectedDeviceId)) {
            state.selectedDeviceId = state.visibleDevices[0].deviceId;
        }

        highlightSelectedRow();
    } catch (error) {
        if (error.name === "AbortError") {
            return;
        }

        console.error("Error cargando dispositivos:", error);
        setMessage(elements.devicesMessage, "No fue posible cargar los dispositivos.", true);
        renderDevicesError();
        clearHistory("Sin datos históricos.");
        setServiceHealth("error", "Error");
        updateLastRefreshText("Error al actualizar");
    } finally {
        state.isLoadingDevices = false;
        currentDevicesController = null;
    }
}

async function refreshSelectedPanels() {
    if (!state.visibleDevices.length) {
        clearHistory("Sin datos históricos.");
        return;
    }

    if (!state.selectedDeviceId || !state.visibleDevices.some(d => d.deviceId === state.selectedDeviceId)) {
        state.selectedDeviceId = state.visibleDevices[0].deviceId;
    }

    highlightSelectedRow();
    await loadDeviceHistory(state.selectedDeviceId);
}

function applyLocalFiltersAndRender() {
    const empresa = elements.empresaFilter?.value || "";
    const search = (elements.searchBox?.value || "").trim().toLowerCase();

    let devices = [...state.devices];

    if (empresa) {
        devices = devices.filter(d => (d.empresaNombre || "Sin empresa") === empresa);
    }

    if (search) {
        devices = devices.filter(d => (d.deviceId || "").toLowerCase().includes(search));
    }

    devices.sort(compareDevicesByPriority);
    state.visibleDevices = devices;

    renderDevices(devices);

    if (!devices.length) {
        setMessage(elements.devicesMessage, "No hay dispositivos para el filtro seleccionado.");
        return;
    }

    setMessage(elements.devicesMessage, `${devices.length} dispositivo(s) encontrados.`);
}

function populateEmpresaFilter(devices) {
    if (!elements.empresaFilter) {
        return;
    }

    const currentValue = elements.empresaFilter.value;
    const empresas = [];
    const seen = new Set();

    devices.forEach(device => {
        const empresaNombre = device.empresaNombre || "Sin empresa";
        const key = empresaNombre.trim().toLowerCase();

        if (!seen.has(key)) {
            seen.add(key);
            empresas.push(empresaNombre);
        }
    });

    while (elements.empresaFilter.options.length > 1) {
        elements.empresaFilter.remove(1);
    }

    empresas.sort((a, b) => a.localeCompare(b));

    empresas.forEach(nombre => {
        const option = document.createElement("option");
        option.value = nombre;
        option.textContent = nombre;
        elements.empresaFilter.appendChild(option);
    });

    const exists = Array.from(elements.empresaFilter.options).some(o => o.value === currentValue);
    elements.empresaFilter.value = exists ? currentValue : "";
}

function compareDevicesByPriority(a, b) {
    const order = { Offline: 0, Degraded: 1, Delayed: 2, Online: 3 };

    const statusA = normalizeStatus(a);
    const statusB = normalizeStatus(b);

    const priorityDiff = (order[statusA] ?? 99) - (order[statusB] ?? 99);
    if (priorityDiff !== 0) {
        return priorityDiff;
    }

    const dateA = new Date(a.lastHeartbeatReceivedAtUtc || 0).getTime();
    const dateB = new Date(b.lastHeartbeatReceivedAtUtc || 0).getTime();

    return dateB - dateA;
}

function renderDevices(devices) {
    if (!devices.length) {
        elements.devicesTableBody.innerHTML = `
            <tr>
                <td colspan="10" class="empty-cell">No hay dispositivos para mostrar.</td>
            </tr>`;
        return;
    }

    elements.devicesTableBody.innerHTML = "";

    devices.forEach(device => {
        const tr = document.createElement("tr");
        tr.classList.add("clickable");

        if (device.deviceId === state.selectedDeviceId) {
            tr.classList.add("selected");
        }

        const effectiveStatus = normalizeStatus(device);

        tr.innerHTML = `
            <td class="cell-deviceid">${escapeHtml(device.deviceId)}</td>
            <td>${escapeHtml(device.empresaNombre || "Sin empresa")}</td>
            <td>${renderStatusBadge(effectiveStatus)}</td>
            <td>${formatDateTime(device.lastHeartbeatReceivedAtUtc)}</td>
            <td>${formatNumber(device.uptime)}</td>
            <td>${renderRssi(device.rssi)}</td>
            <td>${renderWs(device.wsConnected)}</td>
            <td>${renderQueue(device.eventQueueSize)}</td>
            <td>${formatNumber(device.freeHeap)}</td>
            <td>${renderIssuesSummary(device.issuesJson)}</td>
        `;

        tr.addEventListener("click", async () => {
            state.selectedDeviceId = device.deviceId;
            highlightSelectedRow();
            await refreshSelectedPanels();
        });

        elements.devicesTableBody.appendChild(tr);
    });
}

function highlightSelectedRow() {
    const rows = elements.devicesTableBody.querySelectorAll("tr");
    rows.forEach(row => row.classList.remove("selected"));

    const targetRow = Array.from(rows).find(row =>
        row.firstElementChild && row.firstElementChild.textContent.trim() === state.selectedDeviceId
    );

    if (targetRow) {
        targetRow.classList.add("selected");
    }
}

async function loadDeviceHistory(deviceId) {
    if (!deviceId) {
        clearHistory("Sin datos históricos.");
        return;
    }

    setMessage(elements.historyMessage, `Cargando histórico de ${deviceId}...`);

    if (currentHistoryController) {
        currentHistoryController.abort();
    }

    currentHistoryController = new AbortController();

    try {
        const response = await fetch(`/api/devices/${encodeURIComponent(deviceId)}/history?limit=20`, {
            cache: "no-store",
            signal: currentHistoryController.signal
        });

        if (!response.ok) {
            throw new Error(`Error HTTP ${response.status}`);
        }

        const history = await response.json();
        renderHistory(history);
        setMessage(elements.historyMessage, `${deviceId} · ${history.length} registro(s) históricos.`);
    } catch (error) {
        if (error.name === "AbortError") {
            return;
        }

        console.error("Error cargando histórico:", error);
        setMessage(elements.historyMessage, "No fue posible cargar el histórico.", true);
        clearHistory("Sin datos históricos.");
    } finally {
        currentHistoryController = null;
    }
}

function renderHistory(items) {
    if (!items.length) {
        clearHistory("Sin datos históricos.");
        return;
    }

    elements.historyTableBody.innerHTML = "";

    items.forEach(item => {
        const effectiveStatus = normalizeStatus(item);

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${formatDateTime(item.receivedAtUtc)}</td>
            <td>${renderStatusBadge(effectiveStatus)}</td>
            <td>${renderRssi(item.rssi)}</td>
            <td>${renderWs(item.wsConnected)}</td>
            <td>${renderQueue(item.eventQueueSize)}</td>
            <td>${formatNumber(item.freeHeap)}</td>
        `;
        elements.historyTableBody.appendChild(tr);
    });
}

function normalizeStatus(device) {
    const originalStatus = device.operationalStatus || "Offline";
    const rawDate = device.lastHeartbeatReceivedAtUtc || device.receivedAtUtc;

    if (!rawDate) {
        return originalStatus;
    }

    const lastHeartbeat = new Date(rawDate);

    if (Number.isNaN(lastHeartbeat.getTime())) {
        return originalStatus;
    }

    const diffSeconds = Math.floor((new Date() - lastHeartbeat) / 1000);

    if (diffSeconds > OFFLINE_THRESHOLD_SECONDS) {
        return "Offline";
    }

    return originalStatus;
}

function renderStatusBadge(status) {
    const normalized = (status || "").toLowerCase();
    return `<span class="badge ${normalized}">${escapeHtml(status || "-")}</span>`;
}

function renderRssi(rssi) {
    if (rssi === null || rssi === undefined) {
        return "-";
    }

    const cssClass = rssi <= -80
        ? "metric-danger"
        : rssi <= -70
            ? "metric-warning"
            : "";

    return `<span class="${cssClass}">${escapeHtml(String(rssi))}</span>`;
}

function renderWs(connected) {
    return connected
        ? `<span class="ws-pill ws-on">Conectado</span>`
        : `<span class="ws-pill ws-off">Desconectado</span>`;
}

function renderQueue(queueSize) {
    const value = queueSize ?? 0;

    const cssClass = value > 10
        ? "metric-danger"
        : value > 0
            ? "metric-warning"
            : "";

    return `<span class="${cssClass}">${value}</span>`;
}

function renderIssuesSummary(issuesJson) {
    if (!issuesJson) {
        return `<span class="issues-empty">Sin issues</span>`;
    }

    const text = typeof issuesJson === "string"
        ? issuesJson
        : JSON.stringify(issuesJson);

    const normalized = text.replace(/\s+/g, " ").trim();
    const shortText = normalized.length > 60
        ? `${normalized.substring(0, 60)}...`
        : normalized;

    return `<span class="issues-text" title="${escapeHtml(normalized)}">${escapeHtml(shortText)}</span>`;
}

function renderDevicesLoading() {
    elements.devicesTableBody.innerHTML = `
        <tr>
            <td colspan="10" class="empty-cell">Cargando dispositivos...</td>
        </tr>`;
}

function renderDevicesError() {
    elements.devicesTableBody.innerHTML = `
        <tr>
            <td colspan="10" class="empty-cell">No fue posible cargar los dispositivos.</td>
        </tr>`;
}

function clearHistory(message) {
    elements.historyTableBody.innerHTML = `
        <tr>
            <td colspan="6" class="empty-cell">${escapeHtml(message)}</td>
        </tr>`;
}

function setMessage(element, text, isError = false) {
    if (!element) {
        return;
    }

    element.textContent = text || "";
    element.classList.toggle("error", isError);
}

function setServiceHealth(stateName, text) {
    if (!elements.serviceHealthBadge) {
        return;
    }

    elements.serviceHealthBadge.textContent = text;
    elements.serviceHealthBadge.className = "health-badge";
    elements.serviceHealthBadge.classList.add(stateName);
}

function updateLastRefreshText(customText = null) {
    if (!elements.lastRefreshText) {
        return;
    }

    if (customText) {
        elements.lastRefreshText.textContent = customText;
        return;
    }

    if (!state.lastRefreshAt) {
        elements.lastRefreshText.textContent = "-";
        return;
    }

    elements.lastRefreshText.textContent =
        `Última actualización: ${state.lastRefreshAt.toLocaleTimeString("es-CL")}`;
}

function formatDateTime(value) {
    if (!value) {
        return "-";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return date.toLocaleString("es-CL");
}

function formatNumber(value) {
    if (value === null || value === undefined) {
        return "-";
    }

    return Number(value).toLocaleString("es-CL");
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
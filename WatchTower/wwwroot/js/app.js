const AUTO_REFRESH_MS = 5000;
const OFFLINE_THRESHOLD_SECONDS = 120;

const state = {
    devices: [],
    visibleDevices: [],
    selectedDeviceId: null,
    isLoading: false
};

const elements = {
    statusFilter: document.getElementById("statusFilter"),
    empresaFilter: document.getElementById("empresaFilter"),
    searchBox: document.getElementById("searchBox"),
    refreshButton: document.getElementById("refreshButton"),
    lastRefreshLabel: document.getElementById("lastRefreshLabel"),
    devicesMessage: document.getElementById("devicesMessage"),
    historyMessage: document.getElementById("historyMessage"),
    devicesTableBody: document.getElementById("devicesTableBody"),
    historyTableBody: document.getElementById("historyTableBody"),
    deviceDetail: document.getElementById("deviceDetail")
};

document.addEventListener("DOMContentLoaded", () => {
    wireEvents();
    loadDevices();
    setInterval(() => {
        loadDevices(true);
    }, AUTO_REFRESH_MS);
});

function wireEvents() {
    elements.refreshButton.addEventListener("click", () => loadDevices());

    elements.statusFilter.addEventListener("change", () => {
        loadDevices();
    });

    elements.empresaFilter.addEventListener("change", () => {
        applyLocalFiltersAndRender();
    });

    elements.searchBox.addEventListener("input", () => {
        applyLocalFiltersAndRender();
    });
}

async function loadDevices(isAutoRefresh = false) {
    if (state.isLoading) {
        return;
    }

    state.isLoading = true;

    if (!isAutoRefresh) {
        setMessage(elements.devicesMessage, "Cargando dispositivos...");
        renderDevicesLoading();
    }

    const params = new URLSearchParams();

    if (elements.statusFilter.value) {
        params.set("status", elements.statusFilter.value);
    }

    const url = params.toString()
        ? `/api/devices?${params.toString()}`
        : "/api/devices";

    try {
        const response = await fetch(url, { cache: "no-store" });

        if (!response.ok) {
            throw new Error(`Error HTTP ${response.status}`);
        }

        const devices = await response.json();
        state.devices = Array.isArray(devices) ? devices : [];

        populateEmpresaFilter(state.devices);
        applyLocalFiltersAndRender();

        updateLastRefreshLabel();

        if (!state.visibleDevices.length) {
            clearDetail();
            clearHistory("Sin datos históricos.");
            return;
        }

        if (!state.selectedDeviceId || !state.visibleDevices.some(d => d.deviceId === state.selectedDeviceId)) {
            state.selectedDeviceId = state.visibleDevices[0].deviceId;
        }

        await selectDevice(state.selectedDeviceId, true);
    } catch (error) {
        console.error(error);
        setMessage(elements.devicesMessage, "No fue posible cargar los dispositivos.", true);
        renderDevicesError();
        clearDetail();
        clearHistory("Sin datos históricos.");
    } finally {
        state.isLoading = false;
    }
}

function applyLocalFiltersAndRender() {
    const empresa = elements.empresaFilter.value;
    const search = (elements.searchBox.value || "").trim().toLowerCase();

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
                <td colspan="7" class="empty-cell">No hay dispositivos para mostrar.</td>
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
            <td>${escapeHtml(device.deviceId)}</td>
            <td>${renderStatusBadge(effectiveStatus)}</td>
            <td>${timeAgo(device.lastHeartbeatReceivedAtUtc)}</td>
            <td>${renderRssi(device.rssi)}</td>
            <td>${renderWs(device.wsConnected)}</td>
            <td>${renderQueue(device.eventQueueSize)}</td>
            <td>${formatNumber(device.freeHeap)}</td>
        `;

        tr.addEventListener("click", async () => {
            await selectDevice(device.deviceId);
        });

        elements.devicesTableBody.appendChild(tr);
    });
}

async function selectDevice(deviceId, skipRowRefresh = false) {
    state.selectedDeviceId = deviceId;

    if (!skipRowRefresh) {
        renderDevices(state.visibleDevices);
    } else {
        highlightSelectedRow();
    }

    await Promise.all([
        loadDeviceDetail(deviceId),
        loadDeviceHistory(deviceId)
    ]);
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

async function loadDeviceDetail(deviceId) {
    elements.deviceDetail.innerHTML = "Cargando detalle...";

    try {
        const response = await fetch(`/api/devices/${encodeURIComponent(deviceId)}`, { cache: "no-store" });

        if (!response.ok) {
            throw new Error(`Error HTTP ${response.status}`);
        }

        const device = await response.json();
        renderDeviceDetail(device);
    } catch (error) {
        console.error(error);
        elements.deviceDetail.innerHTML = `<div class="detail-empty">No fue posible cargar el detalle.</div>`;
    }
}

async function loadDeviceHistory(deviceId) {
    setMessage(elements.historyMessage, "Cargando histórico...");

    try {
        const response = await fetch(`/api/devices/${encodeURIComponent(deviceId)}/history?limit=20`, { cache: "no-store" });

        if (!response.ok) {
            throw new Error(`Error HTTP ${response.status}`);
        }

        const history = await response.json();
        renderHistory(history);
        setMessage(elements.historyMessage, `${history.length} registro(s) históricos.`);
    } catch (error) {
        console.error(error);
        setMessage(elements.historyMessage, "No fue posible cargar el histórico.", true);
        clearHistory("Sin datos históricos.");
    }
}

function renderDeviceDetail(device) {
    const effectiveStatus = normalizeStatus(device);

    const issues = device.issuesJson
        ? `<pre class="pre-issues">${escapeHtml(device.issuesJson)}</pre>`
        : "Sin issues";

    elements.deviceDetail.innerHTML = `
        <div class="detail-grid">
            <div class="detail-item">
                <span class="detail-label">DeviceId</span>
                <div class="detail-value">${escapeHtml(device.deviceId)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">Empresa</span>
                <div class="detail-value">${escapeHtml(device.empresaNombre || "Sin empresa")}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">Estado</span>
                <div class="detail-value">${renderStatusBadge(effectiveStatus)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">Último heartbeat</span>
                <div class="detail-value">${formatDateTime(device.lastHeartbeatReceivedAtUtc)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">Uptime</span>
                <div class="detail-value">${formatNumber(device.uptime)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">RSSI</span>
                <div class="detail-value">${renderRssi(device.rssi)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">WebSocket</span>
                <div class="detail-value">${renderWs(device.wsConnected)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">EventQueueSize</span>
                <div class="detail-value">${renderQueue(device.eventQueueSize)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">FreeHeap</span>
                <div class="detail-value">${formatNumber(device.freeHeap)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">IssuesJson</span>
                <div class="detail-value">${issues}</div>
            </div>
        </div>
    `;
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
        `;
        elements.historyTableBody.appendChild(tr);
    });
}

function normalizeStatus(device) {
    const originalStatus = device.operationalStatus || "Offline";

    if (!device.lastHeartbeatReceivedAtUtc) {
        return originalStatus;
    }

    const now = new Date();
    const lastHeartbeat = new Date(device.lastHeartbeatReceivedAtUtc || device.receivedAtUtc);

    if (Number.isNaN(lastHeartbeat.getTime())) {
        return originalStatus;
    }

    const diffSeconds = Math.floor((now - lastHeartbeat) / 1000);

    if (diffSeconds > OFFLINE_THRESHOLD_SECONDS) {
        return "Offline";
    }

    return originalStatus;
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

function renderQueue(queueSize) {
    const value = queueSize ?? 0;

    const cssClass = value > 10
        ? "metric-danger"
        : value > 0
            ? "metric-warning"
            : "";

    return `<span class="${cssClass}">${value}</span>`;
}

function renderDevicesLoading() {
    elements.devicesTableBody.innerHTML = `
        <tr>
            <td colspan="7" class="empty-cell">Cargando dispositivos...</td>
        </tr>`;
}

function renderDevicesError() {
    elements.devicesTableBody.innerHTML = `
        <tr>
            <td colspan="7" class="empty-cell">No fue posible cargar los dispositivos.</td>
        </tr>`;
}

function clearDetail() {
    elements.deviceDetail.innerHTML = `<div class="detail-empty">Selecciona un dispositivo para ver su detalle.</div>`;
}

function clearHistory(message) {
    elements.historyTableBody.innerHTML = `
        <tr>
            <td colspan="5" class="empty-cell">${escapeHtml(message)}</td>
        </tr>`;
}

function setMessage(element, text, isError = false) {
    element.textContent = text || "";
    element.classList.toggle("error", isError);
}

function renderStatusBadge(status) {
    const normalized = (status || "").toLowerCase();
    return `<span class="badge ${normalized}">${escapeHtml(status || "-")}</span>`;
}

function renderWs(connected) {
    return connected
        ? `<span class="ws-pill ws-on">Conectado</span>`
        : `<span class="ws-pill ws-off">Desconectado</span>`;
}

function timeAgo(value) {
    if (!value) {
        return "-";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
        return value;
    }

    const seconds = Math.floor((new Date() - date) / 1000);

    if (seconds < 0) {
        return formatDateTime(value);
    }

    if (seconds < 60) {
        return `hace ${seconds}s`;
    }

    if (seconds < 3600) {
        return `hace ${Math.floor(seconds / 60)} min`;
    }

    if (seconds < 86400) {
        return `hace ${Math.floor(seconds / 3600)} h`;
    }

    return formatDateTime(value);
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

function updateLastRefreshLabel() {
    const now = new Date();
    elements.lastRefreshLabel.textContent = `Última actualización: ${now.toLocaleTimeString("es-CL")}`;
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
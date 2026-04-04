const state = {
    devices: [],
    selectedDeviceId: null
};

const elements = {
    statusFilter: document.getElementById("statusFilter"),
    empresaFilter: document.getElementById("empresaFilter"),
    refreshButton: document.getElementById("refreshButton"),
    devicesMessage: document.getElementById("devicesMessage"),
    historyMessage: document.getElementById("historyMessage"),
    devicesTableBody: document.getElementById("devicesTableBody"),
    historyTableBody: document.getElementById("historyTableBody"),
    deviceDetail: document.getElementById("deviceDetail")
};

document.addEventListener("DOMContentLoaded", () => {
    wireEvents();
    loadDevices();
});

function wireEvents() {
    elements.refreshButton.addEventListener("click", () => loadDevices());
    elements.statusFilter.addEventListener("change", () => loadDevices());

    if (elements.empresaFilter) {
        elements.empresaFilter.addEventListener("change", () => {
            if (!elements.empresaFilter.value) {
                renderDevices(state.devices);
                autoSelectFirstVisible();
                return;
            }

            const empresaNombre = elements.empresaFilter.options[elements.empresaFilter.selectedIndex]?.text || "";
            const filtered = state.devices.filter(d => (d.empresaNombre || "Sin empresa") === empresaNombre);

            renderDevices(filtered);

            if (!filtered.length) {
                clearDetail();
                clearHistory("Sin datos históricos.");
                setMessage(elements.devicesMessage, "No hay dispositivos para la empresa seleccionada.");
                return;
            }

            setMessage(elements.devicesMessage, `${filtered.length} dispositivo(s) encontrados.`);
            selectDevice(filtered[0].deviceId);
        });
    }
}

async function loadDevices() {
    setMessage(elements.devicesMessage, "Cargando dispositivos...");
    renderDevicesLoading();

    const params = new URLSearchParams();

    if (elements.statusFilter.value) {
        params.set("status", elements.statusFilter.value);
    }

    const url = params.toString()
        ? `/api/devices?${params.toString()}`
        : "/api/devices";

    try {
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(`Error HTTP ${response.status}`);
        }

        const devices = await response.json();
        state.devices = Array.isArray(devices) ? devices : [];

        populateEmpresaFilter(state.devices);
        renderDevices(state.devices);

        if (state.devices.length === 0) {
            setMessage(elements.devicesMessage, "No hay dispositivos para el filtro seleccionado.");
            clearDetail();
            clearHistory("Sin datos históricos.");
            return;
        }

        setMessage(elements.devicesMessage, `${state.devices.length} dispositivo(s) encontrados.`);

        let targetId = state.selectedDeviceId;

        if (!targetId || !state.devices.some(d => d.deviceId === targetId)) {
            targetId = state.devices[0].deviceId;
        }

        await selectDevice(targetId);
    } catch (error) {
        console.error(error);
        setMessage(elements.devicesMessage, "No fue posible cargar los dispositivos.", true);
        renderDevicesError();
        clearDetail();
        clearHistory("Sin datos históricos.");
    }
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

function renderDevices(devices) {
    if (!devices.length) {
        elements.devicesTableBody.innerHTML = `
            <tr>
                <td colspan="8" class="empty-cell">No hay dispositivos para mostrar.</td>
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

        tr.innerHTML = `
            <td>${escapeHtml(device.deviceId)}</td>
            <td>${escapeHtml(device.empresaNombre || "Sin empresa")}</td>
            <td>${renderStatusBadge(device.operationalStatus)}</td>
            <td>${formatDateTime(device.lastHeartbeatReceivedAtUtc)}</td>
            <td>${device.rssi ?? "-"}</td>
            <td>${renderWs(device.wsConnected)}</td>
            <td>${device.eventQueueSize ?? 0}</td>
            <td>${formatNumber(device.freeHeap)}</td>
        `;

        tr.addEventListener("click", async () => {
            await selectDevice(device.deviceId);
        });

        elements.devicesTableBody.appendChild(tr);
    });
}

async function selectDevice(deviceId) {
    state.selectedDeviceId = deviceId;
    highlightSelectedRow();

    await Promise.all([
        loadDeviceDetail(deviceId),
        loadDeviceHistory(deviceId)
    ]);
}

function autoSelectFirstVisible() {
    const firstRow = elements.devicesTableBody.querySelector("tr.clickable");
    if (!firstRow) {
        return;
    }

    const firstCell = firstRow.querySelector("td");
    if (!firstCell) {
        return;
    }

    selectDevice(firstCell.textContent.trim());
}

function highlightSelectedRow() {
    const rows = elements.devicesTableBody.querySelectorAll("tr");
    rows.forEach(row => row.classList.remove("selected"));

    const targetRow = Array.from(rows).find(row => row.firstElementChild && row.firstElementChild.textContent === state.selectedDeviceId);
    if (targetRow) {
        targetRow.classList.add("selected");
    }
}

async function loadDeviceDetail(deviceId) {
    elements.deviceDetail.innerHTML = "Cargando detalle...";

    try {
        const response = await fetch(`/api/devices/${encodeURIComponent(deviceId)}`);

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
        const response = await fetch(`/api/devices/${encodeURIComponent(deviceId)}/history?limit=20`);

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
                <div class="detail-value">${renderStatusBadge(device.operationalStatus)}</div>
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
                <div class="detail-value">${device.rssi ?? "-"}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">WebSocket</span>
                <div class="detail-value">${renderWs(device.wsConnected)}</div>
            </div>
            <div class="detail-item">
                <span class="detail-label">EventQueueSize</span>
                <div class="detail-value">${device.eventQueueSize ?? 0}</div>
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
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${formatDateTime(item.receivedAtUtc)}</td>
            <td>${renderStatusBadge(item.operationalStatus)}</td>
            <td>${item.rssi ?? "-"}</td>
            <td>${renderWs(item.wsConnected)}</td>
            <td>${item.eventQueueSize ?? 0}</td>
        `;
        elements.historyTableBody.appendChild(tr);
    });
}

function renderDevicesLoading() {
    elements.devicesTableBody.innerHTML = `
        <tr>
            <td colspan="8" class="empty-cell">Cargando dispositivos...</td>
        </tr>`;
}

function renderDevicesError() {
    elements.devicesTableBody.innerHTML = `
        <tr>
            <td colspan="8" class="empty-cell">No fue posible cargar los dispositivos.</td>
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
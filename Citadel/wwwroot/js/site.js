(() => {
    'use strict';

    const state = {
        devices: [],
        companies: [],
        selectedDeviceIdForUnassign: null,
        tiposHardware: [],
        firmwares: []
    };

    const $ = id => document.getElementById(id);

    const els = {
        txtSearch: $('txtSearch'),
        btnRefresh: $('btnRefresh'),
        btnNewDevice: $('btnNewDevice'),
        btnManageCompanies: $('btnManageCompanies'),
        devicesBody: $('devicesBody'),
        deviceCount: $('deviceCount'),

        deviceModal: new bootstrap.Modal($('deviceModal')),
        assignModal: new bootstrap.Modal($('assignModal')),
        unassignModal: new bootstrap.Modal($('unassignModal')),
        companiesModal: new bootstrap.Modal($('companiesModal')),
        companyFormModal: new bootstrap.Modal($('companyFormModal')),

        appToast: $('appToast'),
        toastBody: $('toastBody'),
        toastTime: $('toastTime'),

        deviceModalTitle: $('deviceModalTitle'),
        deviceFormMode: $('deviceFormMode'),
        deviceId: $('deviceId'),
        tipoHardwareId: $('tipoHardwareId'),
        firmwareId: $('firmwareId'),
        firmwareVersion: $('firmwareVersion'),
        deviceEnabled: $('deviceEnabled'),
        btnSaveDevice: $('btnSaveDevice'),
        deviceEditNote: $('deviceEditNote'),

        assignDeviceId: $('assignDeviceId'),
        assignEmpresaId: $('assignEmpresaId'),
        assignNombreDispositivo: $('assignNombreDispositivo'),
        assignDescripcion: $('assignDescripcion'),
        btnSaveAssignment: $('btnSaveAssignment'),

        unassignDeviceIdText: $('unassignDeviceIdText'),
        unassignUser: $('unassignUser'),
        btnConfirmUnassign: $('btnConfirmUnassign'),

        companiesBody: $('companiesBody'),
        btnNewCompany: $('btnNewCompany'),
        companyEditNote: $('companyEditNote'),

        companyModalTitle: $('companyModalTitle'),
        companyFormMode: $('companyFormMode'),
        companyId: $('companyId'),
        companyCodigo: $('companyCodigo'),
        companyNombre: $('companyNombre'),
        companyEnabled: $('companyEnabled'),
        btnSaveCompany: $('btnSaveCompany')
    };

    const toast = new bootstrap.Toast(els.appToast, { delay: 3200 });

    document.addEventListener('DOMContentLoaded', () => {
        wireEvents();
        loadAll();
    });

    function wireEvents() {
        els.btnRefresh?.addEventListener('click', loadAll);
        els.txtSearch?.addEventListener('input', renderDevices);
        els.btnNewDevice?.addEventListener('click', openCreateDeviceModal);
        els.btnManageCompanies?.addEventListener('click', openCompaniesModal);
        els.btnSaveDevice?.addEventListener('click', saveDevice);
        els.btnNewCompany?.addEventListener('click', openCreateCompanyModal);
        els.btnSaveCompany?.addEventListener('click', saveCompany);
        els.btnSaveAssignment?.addEventListener('click', saveAssignment);
        els.btnConfirmUnassign?.addEventListener('click', confirmUnassign);
        els.devicesBody?.addEventListener('click', handleDeviceActions);
        els.companiesBody?.addEventListener('click', handleCompanyActions);

        els.deviceId?.addEventListener('input', () => {
            els.deviceId.value = els.deviceId.value.toUpperCase().replace(/[^0-9A-F]/g, '');
        });

        els.companyCodigo?.addEventListener('input', () => {
            els.companyCodigo.value = els.companyCodigo.value.toUpperCase();
        });

        els.tipoHardwareId?.addEventListener('change', async () => {
            await loadFirmwaresByTipo(Number(els.tipoHardwareId.value || 1));
        });

        els.firmwareId?.addEventListener('change', () => {
            const selectedFw = state.firmwares.find(x => Number(x.id) === Number(els.firmwareId.value));
            if (selectedFw) {
                els.firmwareVersion.value = selectedFw.version || selectedFw.nombre || '';
            }
        });
    }

    async function loadAll() {
        try {
            if (els.devicesBody) {
                els.devicesBody.innerHTML = '<tr><td colspan="7" class="text-center py-5 text-muted">Cargando...</td></tr>';
            }

            await Promise.all([
                loadCompanies(),
                loadCatalogs()
            ]);

            await loadDevices();
            renderDevices();
            renderCompaniesTable();
        } catch (error) {
            console.error(error);
            showError(error.message || 'No fue posible cargar los datos.');
        }
    }

    async function loadCatalogs() {
        try {
            state.tiposHardware = await apiGet('/api/tipohardware');
        } catch {
            state.tiposHardware = [
                { id: 1, codigo: 'ESP32-HW.01', nombre: 'ESP32 HW.01' }
            ];
        }

        populateTipoHardware();
        await loadFirmwaresByTipo(Number(els.tipoHardwareId?.value || 1));
    }

    async function loadFirmwaresByTipo(tipoHardwareId) {
        try {
            state.firmwares = await apiGet(`/api/firmwares?tipoHardwareId=${encodeURIComponent(tipoHardwareId)}`);
        } catch {
            state.firmwares = [
                { id: 1, tipoHardwareId: 1, version: 'V01.00.000', nombre: 'V01.00.000' }
            ];
        }

        populateFirmwares();
    }

    async function loadDevices() {
        const devices = await apiGet('/api/devices');
        const rows = [];

        for (const device of devices) {
            let empresa = null;

            try {
                empresa = await apiGet(`/api/devices/${encodeURIComponent(device.deviceId)}/empresa`);
            } catch {
                empresa = null;
            }

            rows.push({
                deviceId: device.deviceId,
                tipoHardwareId: device.tipoHardwareId,
                firmwareId: device.firmwareId,
                firmwareVersion: device.firmwareVersion,
                habilitado: device.habilitado,
                empresaId: empresa?.empresaId ?? null,
                empresaNombre: empresa?.nombre ?? null,
                nombreDispositivo: empresa?.nombreDispositivo ?? null,
                descripcion: empresa?.descripcion ?? null
            });
        }

        state.devices = rows;
    }

    async function loadCompanies() {
        state.companies = await apiGet('/api/empresas');
        populateCompanies();
    }

    function populateTipoHardware() {
        if (!els.tipoHardwareId) return;

        els.tipoHardwareId.innerHTML = state.tiposHardware
            .map(x => `<option value="${x.id}">${escapeHtml(x.codigo || x.nombre || ('Tipo ' + x.id))}</option>`)
            .join('');
    }

    function populateFirmwares() {
        if (!els.firmwareId) return;

        els.firmwareId.innerHTML = state.firmwares
            .map(x => `<option value="${x.id}">${escapeHtml(x.version || x.nombre || ('Firmware ' + x.id))}</option>`)
            .join('');

        const selected = state.firmwares.find(x => String(x.id) === String(els.firmwareId.value)) || state.firmwares[0];

        if (selected) {
            els.firmwareId.value = String(selected.id);
            if (els.firmwareVersion) {
                els.firmwareVersion.value = selected.version || selected.nombre || '';
            }
        } else if (els.firmwareVersion) {
            els.firmwareVersion.value = '';
        }
    }

    function populateCompanies() {
        if (!els.assignEmpresaId) return;

        els.assignEmpresaId.innerHTML =
            '<option value="">Selecciona una empresa...</option>' +
            state.companies.map(c =>
                `<option value="${c.id}">${escapeHtml(c.codigo)} - ${escapeHtml(c.nombre)}</option>`
            ).join('');
    }

    function getTipoHardwareDisplay(tipoHardwareId) {
        const item = state.tiposHardware.find(x => Number(x.id) === Number(tipoHardwareId));

        if (!item) {
            return String(tipoHardwareId ?? '');
        }

        return item.codigo || item.nombre || String(tipoHardwareId);
    }

    function renderDevices() {
        const q = normalize(els.txtSearch?.value);

        const rows = state.devices.filter(d => {
            const text = normalize([
                d.deviceId,
                d.firmwareVersion,
                d.empresaNombre,
                d.nombreDispositivo,
                getTipoHardwareDisplay(d.tipoHardwareId)
            ].join(' '));

            return !q || text.includes(q);
        });

        if (els.deviceCount) {
            els.deviceCount.textContent = String(rows.length);
        }

        if (!rows.length) {
            if (els.devicesBody) {
                els.devicesBody.innerHTML = '<tr><td colspan="7" class="text-center py-5 text-muted">No hay registros para mostrar.</td></tr>';
            }
            return;
        }

        if (!els.devicesBody) return;

        els.devicesBody.innerHTML = rows.map(d => `
        <tr>
            <td><span class="mono">${escapeHtml(d.deviceId)}</span></td>
            <td>${escapeHtml(getTipoHardwareDisplay(d.tipoHardwareId))}</td>
            <td>${escapeHtml(d.firmwareVersion ?? '-')}</td>
            <td>
                ${d.empresaNombre
                ? `<span class="badge-soft badge-soft-success"><i class="bi bi-building"></i>${escapeHtml(d.empresaNombre)}</span>`
                : `<span class="badge-soft badge-soft-secondary"><i class="bi bi-dash-circle"></i>Sin asignar</span>`}
            </td>
            <td>${escapeHtml(d.nombreDispositivo ?? '-')}</td>
            <td>
                ${d.habilitado
                ? `<span class="badge-soft badge-soft-primary"><i class="bi bi-check-circle"></i>Habilitado</span>`
                : `<span class="badge-soft badge-soft-danger"><i class="bi bi-x-circle"></i>Deshabilitado</span>`}
            </td>
            <td class="text-end">
                <div class="action-stack">
                    <button class="btn btn-sm btn-outline-light" data-action="edit-device" data-device-id="${escapeHtml(d.deviceId)}">
                        <i class="bi bi-pencil-square"></i>
                    </button>
                    <button class="btn btn-sm btn-success" data-action="assign" data-device-id="${escapeHtml(d.deviceId)}" ${d.empresaId ? 'disabled' : ''}>
                        <i class="bi bi-link-45deg"></i> Asignar
                    </button>
                    <button class="btn btn-sm btn-danger" data-action="unassign" data-device-id="${escapeHtml(d.deviceId)}" ${!d.empresaId ? 'disabled' : ''}>
                        <i class="bi bi-x-circle"></i> Desasignar
                    </button>
                </div>
            </td>
        </tr>
    `).join('');
    }

    function renderCompaniesTable() {
        if (!els.companiesBody) return;

        if (!state.companies.length) {
            els.companiesBody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">No hay empresas registradas.</td></tr>';
            return;
        }

        els.companiesBody.innerHTML = state.companies.map(c => `
        <tr>
            <td>${c.id}</td>
            <td>${escapeHtml(c.codigo)}</td>
            <td>${escapeHtml(c.nombre)}</td>
            <td>
                ${c.habilitado
                ? `<span class="badge-soft badge-soft-primary"><i class="bi bi-check-circle"></i>Habilitada</span>`
                : `<span class="badge-soft badge-soft-danger"><i class="bi bi-x-circle"></i>Deshabilitada</span>`}
            </td>
            <td class="text-end">
                <button class="btn btn-sm btn-outline-light" data-action="edit-company" data-company-id="${c.id}">
                    <i class="bi bi-pencil-square"></i> Editar
                </button>
            </td>
        </tr>
    `).join('');
    }

    function handleDeviceActions(event) {
        const button = event.target.closest('button[data-action]');
        if (!button) return;

        const row = state.devices.find(x => x.deviceId === button.dataset.deviceId);
        if (!row) return;

        if (button.dataset.action === 'assign') openAssignModal(row);
        if (button.dataset.action === 'unassign') openUnassignModal(row);
        if (button.dataset.action === 'edit-device') openEditDeviceModal(row);
    }

    function handleCompanyActions(event) {
        const button = event.target.closest('button[data-action="edit-company"]');
        if (!button) return;

        const row = state.companies.find(x => x.id === Number(button.dataset.companyId));
        if (!row) return;

        openEditCompanyModal(row);
    }

    function openCreateDeviceModal() {
        els.deviceModalTitle.textContent = 'Nuevo dispositivo';
        els.deviceFormMode.value = 'create';

        els.deviceId.disabled = false;
        els.deviceId.readOnly = false;
        els.deviceId.value = '';

        els.deviceEnabled.checked = true;

        if (state.tiposHardware.length) {
            els.tipoHardwareId.value = String(state.tiposHardware[0].id);
        }

        loadFirmwaresByTipo(Number(els.tipoHardwareId.value || 1));
        els.deviceModal.show();
    }

    async function openEditDeviceModal(device) {
        try {
            const fullDevice = await apiGet(`/api/devices/${encodeURIComponent(device.deviceId)}`);

            els.deviceModalTitle.textContent = `Editar dispositivo ${fullDevice.deviceId}`;
            els.deviceFormMode.value = 'edit';

            els.deviceId.disabled = false;
            els.deviceId.readOnly = true;
            els.deviceId.value = fullDevice.deviceId ?? '';

            els.deviceEnabled.checked = !!fullDevice.habilitado;
            els.tipoHardwareId.value = String(fullDevice.tipoHardwareId ?? '');

            await loadFirmwaresByTipo(Number(fullDevice.tipoHardwareId ?? 1));

            els.firmwareId.value = fullDevice.firmwareId != null
                ? String(fullDevice.firmwareId)
                : '';

            els.firmwareVersion.value = fullDevice.firmwareVersion ?? '';
            els.deviceModal.show();
        } catch (error) {
            showError(error.message || 'No fue posible cargar el dispositivo para edición.');
        }
    }

    function openAssignModal(device) {
        els.assignDeviceId.value = device.deviceId;
        els.assignEmpresaId.value = '';
        els.assignNombreDispositivo.value = device.nombreDispositivo ?? '';
        els.assignDescripcion.value = device.descripcion ?? '';
        els.assignModal.show();
    }

    function openUnassignModal(device) {
        state.selectedDeviceIdForUnassign = device.deviceId;
        els.unassignDeviceIdText.textContent = device.deviceId;
        els.unassignUser.value = 'web';
        els.unassignModal.show();
    }

    function openCompaniesModal() {
        renderCompaniesTable();
        els.companiesModal.show();
    }

    function openCreateCompanyModal() {
        els.companyModalTitle.textContent = 'Nueva empresa';
        els.companyFormMode.value = 'create';
        els.companyId.value = '';
        els.companyCodigo.value = '';
        els.companyNombre.value = '';
        els.companyEnabled.checked = true;
        els.companyFormModal.show();
    }

    function openEditCompanyModal(company) {
        els.companyModalTitle.textContent = `Editar empresa ${company.nombre}`;
        els.companyFormMode.value = 'edit';
        els.companyId.value = company.id;
        els.companyCodigo.value = company.codigo ?? '';
        els.companyNombre.value = company.nombre ?? '';
        els.companyEnabled.checked = !!company.habilitado;
        els.companyFormModal.show();
    }

    async function saveDevice() {
        const mode = els.deviceFormMode.value;
        const selectedFw = state.firmwares.find(x => Number(x.id) === Number(els.firmwareId.value));

        const payload = {
            deviceId: (els.deviceId.value || '').trim().toUpperCase(),
            tipoHardwareId: Number(els.tipoHardwareId.value),
            firmwareId: els.firmwareId.value ? Number(els.firmwareId.value) : null,
            firmwareVersion: (selectedFw?.version || els.firmwareVersion.value || '').trim(),
            habilitado: els.deviceEnabled.checked
        };

        const validation = validateDevice(payload);
        if (!validation.ok) return showWarn(validation.message);

        try {
            setBusy(els.btnSaveDevice, true);

            if (mode === 'create') {
                await apiJson('/api/devices', 'POST', payload);
                showToast(`Dispositivo ${payload.deviceId} creado correctamente.`);
            } else {
                await apiJson(`/api/devices/${encodeURIComponent(payload.deviceId)}`, 'PUT', payload);
                showToast(`Dispositivo ${payload.deviceId} actualizado correctamente.`);
            }

            els.deviceModal.hide();
            await loadDevices();
            renderDevices();
        } catch (error) {
            showError(error.message || 'No fue posible guardar el dispositivo.');
        } finally {
            setBusy(els.btnSaveDevice, false);
        }
    }

    async function saveCompany() {
        const mode = els.companyFormMode.value;

        const payload = {
            codigo: (els.companyCodigo.value || '').trim().toUpperCase(),
            nombre: (els.companyNombre.value || '').trim(),
            habilitado: els.companyEnabled.checked
        };

        const validation = validateCompany(payload);
        if (!validation.ok) return showWarn(validation.message);

        try {
            setBusy(els.btnSaveCompany, true);

            if (mode === 'create') {
                await apiJson('/api/empresas', 'POST', payload);
                showToast(`Empresa ${payload.nombre} creada correctamente.`);
            } else {
                await apiJson(`/api/empresas/${Number(els.companyId.value)}`, 'PUT', payload);
                showToast(`Empresa ${payload.nombre} actualizada correctamente.`);
            }

            els.companyFormModal.hide();
            await loadCompanies();
            renderCompaniesTable();
        } catch (error) {
            showError(error.message || 'No fue posible guardar la empresa.');
        } finally {
            setBusy(els.btnSaveCompany, false);
        }
    }

    async function saveAssignment() {
        const payload = {
            empresaId: Number(els.assignEmpresaId.value),
            deviceId: (els.assignDeviceId.value || '').trim().toUpperCase(),
            nombreDispositivo: (els.assignNombreDispositivo.value || '').trim(),
            descripcion: (els.assignDescripcion.value || '').trim()
        };

        const validation = validateAssignment(payload);
        if (!validation.ok) return showWarn(validation.message);

        try {
            setBusy(els.btnSaveAssignment, true);
            await apiJson('/api/asignaciones', 'POST', payload);
            els.assignModal.hide();
            await loadDevices();
            renderDevices();
            showToast(`Dispositivo ${payload.deviceId} asignado correctamente.`);
        } catch (error) {
            showError(error.message || 'No fue posible asignar el dispositivo.');
        } finally {
            setBusy(els.btnSaveAssignment, false);
        }
    }

    async function confirmUnassign() {
        const deviceId = state.selectedDeviceIdForUnassign;
        const usuario = (els.unassignUser.value || '').trim();

        if (!deviceId) return showWarn('No hay un dispositivo seleccionado para desasignar.');
        if (!usuario) return showWarn('Debes indicar el usuario que realiza la desasignación.');

        const result = await Swal.fire({
            title: '¿Desasignar dispositivo?',
            text: `Se desasignará ${deviceId}.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sí, desasignar',
            cancelButtonText: 'Cancelar',
            background: '#171c25',
            color: '#e6edf6',
            customClass: { popup: 'swal-dark' }
        });

        if (!result.isConfirmed) return;

        try {
            setBusy(els.btnConfirmUnassign, true);
            await apiJson(`/api/asignaciones/${encodeURIComponent(deviceId)}/desasignar?usuario=${encodeURIComponent(usuario)}`, 'POST');
            els.unassignModal.hide();
            await loadDevices();
            renderDevices();
            showToast(`Dispositivo ${deviceId} desasignado correctamente.`);
        } catch (error) {
            showError(error.message || 'No fue posible desasignar el dispositivo.');
        } finally {
            setBusy(els.btnConfirmUnassign, false);
        }
    }

    function validateDevice(payload) {
        if (!payload.deviceId) {
            return { ok: false, message: 'El DeviceId es obligatorio.' };
        }

        if (!/^[0-9A-F]{12}$/.test(payload.deviceId)) {
            return { ok: false, message: 'El DeviceId debe tener exactamente 12 caracteres hexadecimales.' };
        }

        if (!Number.isInteger(payload.tipoHardwareId) || payload.tipoHardwareId <= 0) {
            return { ok: false, message: 'TipoHardwareId debe ser mayor que cero.' };
        }

        if (payload.firmwareId !== null && (!Number.isInteger(payload.firmwareId) || payload.firmwareId <= 0)) {
            return { ok: false, message: 'FirmwareId debe ser mayor que cero cuando se informa.' };
        }

        if (!payload.firmwareVersion || payload.firmwareVersion.length > 50) {
            return { ok: false, message: 'FirmwareVersion es obligatoria y debe tener máximo 50 caracteres.' };
        }

        return { ok: true };
    }

    function validateCompany(payload) {
        if (!payload.codigo) {
            return { ok: false, message: 'El código de empresa es obligatorio.' };
        }

        if (payload.codigo.length > 50) {
            return { ok: false, message: 'El código de empresa no puede exceder 50 caracteres.' };
        }

        if (!payload.nombre) {
            return { ok: false, message: 'El nombre de empresa es obligatorio.' };
        }

        if (payload.nombre.length > 200) {
            return { ok: false, message: 'El nombre de empresa no puede exceder 200 caracteres.' };
        }

        return { ok: true };
    }

    function validateAssignment(payload) {
        if (!Number.isInteger(payload.empresaId) || payload.empresaId <= 0) {
            return { ok: false, message: 'Debes seleccionar una empresa válida.' };
        }

        if (!payload.deviceId) {
            return { ok: false, message: 'El DeviceId es obligatorio.' };
        }

        if ((payload.nombreDispositivo || '').length > 200) {
            return { ok: false, message: 'NombreDispositivo no puede exceder 200 caracteres.' };
        }

        if ((payload.descripcion || '').length > 500) {
            return { ok: false, message: 'Descripción no puede exceder 500 caracteres.' };
        }

        return { ok: true };
    }

    async function apiGet(url) {
        const response = await fetch(url, {
            headers: { 'Accept': 'application/json' }
        });

        return readResponse(response);
    }

    async function apiJson(url, method, body) {
        const response = await fetch(url, {
            method,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: body ? JSON.stringify(body) : undefined
        });

        return readResponse(response);
    }

    async function readResponse(response) {
        const contentType = response.headers.get('content-type') || '';
        const isJson = contentType.includes('application/json');
        const data = isJson ? await response.json() : { message: await response.text() };

        if (!response.ok) {
            throw new Error((data && (data.message || data.title)) || `Error HTTP ${response.status}.`);
        }

        return data;
    }

    function setBusy(button, busy) {
        if (!button) return;

        const label = button.querySelector('.label');
        button.disabled = busy;

        if (label) {
            label.textContent = busy ? 'Guardando...' : 'Guardar';
        }
    }

    function showToast(message) {
        if (!els.toastBody) return;
        els.toastBody.textContent = message;
        if (els.toastTime) {
            els.toastTime.textContent = 'ahora';
        }
        toast.show();
    }

    function showWarn(message) {
        return Swal.fire({
            title: 'Validación',
            text: message,
            icon: 'warning',
            confirmButtonText: 'Aceptar',
            background: '#171c25',
            color: '#e6edf6',
            customClass: { popup: 'swal-dark' }
        });
    }

    function showError(message) {
        return Swal.fire({
            title: 'Error',
            text: message,
            icon: 'error',
            confirmButtonText: 'Cerrar',
            background: '#171c25',
            color: '#e6edf6',
            customClass: { popup: 'swal-dark' }
        });
    }

    function normalize(value) {
        return (value || '').toString().trim().toLowerCase();
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }
})();
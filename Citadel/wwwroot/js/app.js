async function cargar(){
    const res = await fetch('/api/devices');
    const data = await res.json();
    const tbody = document.getElementById('tbl');
    tbody.innerHTML = '';

    for(const d of data){
        tbody.innerHTML += `<tr>
            <td>${d.deviceId}</td>
            <td>${d.firmwareVersion}</td>
            <td>-</td>
            <td>-</td>
            <td>
                <button class="btn btn-sm btn-success" onclick="asignar('${d.deviceId}')">Asignar</button>
            </td>
        </tr>`;
    }
}

function abrirModalDevice(){
    Swal.fire({
        title:'Nuevo Device',
        input:'text',
        inputLabel:'DeviceId',
        showCancelButton:true
    }).then(async r=>{
        if(r.isConfirmed){
            await fetch('/api/devices',{
                method:'POST',
                headers:{'Content-Type':'application/json'},
                body:JSON.stringify({deviceId:r.value, tipoHardwareId:1})
            });
            cargar();
        }
    });
}

function abrirModalEmpresa(){
    Swal.fire({
        title:'Nueva Empresa',
        input:'text',
        inputLabel:'Nombre',
        showCancelButton:true
    });
}

function asignar(id){
    Swal.fire({
        title:'Asignar',
        input:'text',
        inputLabel:'EmpresaId',
        showCancelButton:true
    }).then(async r=>{
        if(r.isConfirmed){
            await fetch('/api/asignaciones',{
                method:'POST',
                headers:{'Content-Type':'application/json'},
                body:JSON.stringify({
                    empresaId:r.value,
                    deviceId:id
                })
            });
            cargar();
        }
    });
}

cargar();

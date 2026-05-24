document.addEventListener('DOMContentLoaded', function() {
    const user = requireStaffRole('dentist');
    if (!user) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);
    document.getElementById('dentistName').textContent = 'Dr. ' + user.firstName + ' ' + user.lastName;

    const body = document.getElementById('scheduleBody');
    const medList = document.getElementById('medicationList');
    let selectedApptId = null;

    function addMedRow(value) {
        const row = document.createElement('div');
        row.className = 'input-group mb-2 med-row';
        row.innerHTML = '<input type="text" class="form-control med-name" placeholder="Medication name" value="' + (value || '') + '"><button type="button" class="btn btn-outline-danger remove-med">×</button>';
        medList.appendChild(row);
        row.querySelector('.remove-med').addEventListener('click', function() { row.remove(); });
    }

    document.getElementById('addMedBtn').addEventListener('click', function() { addMedRow(''); });

    function pick(obj, a, b) {
        if (!obj) return undefined;
        if (obj[a] !== undefined && obj[a] !== null) return obj[a];
        return obj[b];
    }

    function pickField(obj) {
        for (var i = 1; i < arguments.length; i++) {
            var key = arguments[i];
            var cap = key.charAt(0).toUpperCase() + key.slice(1);
            var v = pick(obj, key, cap);
            if (v !== undefined && v !== null) return v;
        }
        return undefined;
    }

    function load() {
        apiGet('/api/Appointments?dentistId=' + user.id).then(function(items) {
            body.innerHTML = items.map(function(a) {
                var when = pickField(a, 'displayDateTime', 'myScheduledStart', 'preferredDateTime');
                var treatment = pickField(a, 'myTreatmentName') || '';
                var dateCell = formatDateTime(when);
                if (treatment) {
                    dateCell += '<p class="small text-muted mb-0">' + treatment + '</p>';
                }
                let actions = '';
                var st = pickField(a, 'displayStatus', 'status') || a.status;
                if (st === 'InProgress') {
                    actions += '<button class="btn btn-sm btn-primary me-1" data-complete="' + a.id + '">Complete visit</button>';
                }
                if (st === 'Completed' || st === 'InProgress') {
                    actions += '<button class="btn btn-sm btn-outline-secondary" data-receipt="' + a.id + '">Receipt</button>';
                }
                actions += '<button class="btn btn-sm btn-outline-secondary ms-1" data-open="' + a.id + '">Treatment</button>';
                actions += '<button class="btn btn-sm btn-outline-info ms-1" data-info="' + a.id + '">Info</button>';
                var status = pickField(a, 'displayStatus', 'status') || a.status;
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + dateCell + '</td><td>' + statusBadge(status) + '</td><td>' + actions + '</td></tr>';
            }).join('');

            body.querySelectorAll('[data-complete]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    const id = btn.getAttribute('data-complete');
                    await apiPatch('/api/Appointments/' + id + '/complete-visit', { dentistUserId: user.id });
                    selectedApptId = id;
                    document.getElementById('treatmentApptId').value = id;
                    medList.innerHTML = '';
                    addMedRow('');
                    load();
                });
            });
            body.querySelectorAll('[data-receipt]').forEach(function(btn) {
                btn.addEventListener('click', function() {
                    selectedApptId = btn.getAttribute('data-receipt');
                    document.getElementById('treatmentApptId').value = selectedApptId;
                    medList.innerHTML = '';
                    addMedRow('');
                });
            });
            body.querySelectorAll('[data-open]').forEach(function(btn) {
                btn.addEventListener('click', function() {
                    document.getElementById('treatmentApptId').value = btn.getAttribute('data-open');
                });
            });
            body.querySelectorAll('[data-info]').forEach(function(btn) {
                btn.addEventListener('click', function() {
                    showAppointmentInfo(btn.getAttribute('data-info'), 'dentist', { dentistId: user.id });
                });
            });
        });
    }

    document.getElementById('saveTreatment').addEventListener('click', async function() {
        const id = document.getElementById('treatmentApptId').value.trim();
        if (!id) return;
        await apiPut('/api/Treatments/' + id, {
            diagnosis: document.getElementById('diagnosis').value,
            treatmentPerformed: document.getElementById('treatment').value,
            recommendations: document.getElementById('recommendations').value,
            medicationPrescribed: ''
        });
        alert('Treatment saved');
    });

    document.getElementById('submitReceipt').addEventListener('click', async function() {
        const id = document.getElementById('treatmentApptId').value.trim() || selectedApptId;
        if (!id) { alert('Select an appointment first'); return; }
        const meds = [];
        medList.querySelectorAll('.med-name').forEach(function(inp) {
            if (inp.value.trim()) meds.push(inp.value.trim());
        });
        if (!meds.length) { alert('Add at least one medication'); return; }
        await apiPost('/api/Receipts/medications', {
            appointmentId: Number(id),
            medications: meds,
            dentistUserId: user.id
        });
        alert('Receipt sent to manager for pricing.');
        medList.innerHTML = '';
    });

    addMedRow('');
    load();
});

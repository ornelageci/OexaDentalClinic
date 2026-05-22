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

    function load() {
        apiGet('/api/Appointments?dentistId=' + user.id).then(function(items) {
            body.innerHTML = items.map(function(a) {
                let actions = '';
                if (a.status === 'InProgress') {
                    actions += '<button class="btn btn-sm btn-primary me-1" data-complete="' + a.id + '">Complete visit</button>';
                }
                if (a.status === 'Completed' || a.status === 'InProgress') {
                    actions += '<button class="btn btn-sm btn-outline-secondary" data-receipt="' + a.id + '">Receipt</button>';
                }
                actions += '<button class="btn btn-sm btn-outline-secondary ms-1" data-open="' + a.id + '">Treatment</button>';
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + statusBadge(a.status) + '</td><td>' + actions + '</td></tr>';
            }).join('');

            body.querySelectorAll('[data-complete]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    const id = btn.getAttribute('data-complete');
                    await apiPatch('/api/Appointments/' + id + '/status', { status: 'Completed' });
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
        await apiPost('/api/Receipts/medications', { appointmentId: Number(id), medications: meds });
        alert('Receipt sent to manager for pricing.');
        medList.innerHTML = '';
    });

    addMedRow('');
    load();
});

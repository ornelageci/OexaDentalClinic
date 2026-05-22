document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('manager')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    let problems = {};

    apiGet('/api/Problems').then(function(items) {
        items.forEach(function(p) { problems[p.key] = p.name; });
        loadUnassigned();
        loadList();
    });

    function loadUnassigned() {
        apiGet('/api/Appointments/unassigned').then(function(items) {
            const body = document.getElementById('unassignedBody');
            if (!items.length) {
                body.innerHTML = '<tr><td colspan="5" class="text-muted">No patients waiting for dentist assignment.</td></tr>';
                return;
            }
            body.innerHTML = items.map(function(a) {
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + (a.problemName || a.problemKey) + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td><select class="form-select form-select-sm assign-select" data-appt="' + a.id + '" data-problem="' + a.problemKey + '"><option value="">Choose dentist</option></select> <button class="btn btn-sm btn-primary assign-btn" data-appt="' + a.id + '">Assign</button></td></tr>';
            }).join('');

            body.querySelectorAll('.assign-select').forEach(function(sel) {
                const problemKey = sel.getAttribute('data-problem');
                apiGet('/api/Users/dentists?problemKey=' + encodeURIComponent(problemKey)).then(function(dentists) {
                    sel.innerHTML = '<option value="">Choose dentist</option>' + dentists.map(function(d) {
                        return '<option value="' + d.id + '">Dr. ' + d.firstName + ' ' + d.lastName + '</option>';
                    }).join('');
                });
            });

            body.querySelectorAll('.assign-btn').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    const apptId = btn.getAttribute('data-appt');
                    const row = btn.closest('tr');
                    const sel = row.querySelector('.assign-select');
                    const dentistId = sel.value;
                    if (!dentistId) { alert('Select a dentist'); return; }
                    await apiPatch('/api/Appointments/' + apptId + '/assign', { dentistUserId: Number(dentistId) });
                    alert('Dentist assigned. Emails sent to patient, dentist, manager and admin.');
                    loadUnassigned();
                    loadList();
                });
            });
        });
    }

    document.getElementById('checkinBtn').addEventListener('click', async function() {
        const id = document.getElementById('checkinId').value.trim();
        if (!id) return;
        await apiPatch('/api/Appointments/' + id + '/status', { status: 'InProgress' });
        document.getElementById('checkinMsg').textContent = 'Checked in #' + id;
        loadList();
    });

    document.getElementById('loadReceiptBtn').addEventListener('click', async function() {
        const apptId = document.getElementById('receiptApptId').value.trim();
        const box = document.getElementById('receiptPricing');
        if (!apptId) return;
        try {
            const data = await apiGet('/api/Receipts/' + apptId);
            if (!data.medications || !data.medications.length) {
                box.innerHTML = '<p class="text-muted">No medications from dentist yet.</p>';
                return;
            }
            let html = '<table class="table table-sm"><thead><tr><th>Medication</th><th>Price (EUR)</th></tr></thead><tbody>';
            data.medications.forEach(function(m) {
                html += '<tr><td>' + m.name + '</td><td><input type="number" step="0.01" class="form-control form-control-sm med-price" data-id="' + m.id + '" value="' + (m.unitPrice || '') + '"></td></tr>';
            });
            html += '</tbody></table><button class="btn btn-primary" id="saveReceiptPrices">Finalize receipt</button>';
            box.innerHTML = html;
            document.getElementById('saveReceiptPrices').addEventListener('click', async function() {
                const lines = [];
                box.querySelectorAll('.med-price').forEach(function(inp) {
                    lines.push({ medicationId: Number(inp.getAttribute('data-id')), unitPrice: Number(inp.value) });
                });
                await apiPut('/api/Receipts/' + data.receipt.id + '/prices', { lines: lines });
                alert('Receipt finalized. Emails sent.');
                box.innerHTML = '<p class="text-success">Receipt saved.</p>';
            });
        } catch {
            box.innerHTML = '<p class="text-danger">No receipt found for this appointment.</p>';
        }
    });

    document.getElementById('rescheduleBtn').addEventListener('click', async function() {
        const id = document.getElementById('actionId').value.trim();
        const appt = await apiGet('/api/Appointments/' + id);
        await apiPut('/api/Appointments/' + id + '/reschedule', {
            firstName: appt.firstName, lastName: appt.lastName, email: appt.email, phoneNumber: appt.phoneNumber,
            preferredDate: document.getElementById('newDate').value,
            preferredTime: document.getElementById('newTime').value,
            serviceNeeded: appt.serviceNeeded
        });
        alert('Rescheduled');
        loadList();
    });

    document.getElementById('cancelBtn').addEventListener('click', async function() {
        const id = document.getElementById('actionId').value.trim();
        await apiPost('/api/Appointments/' + id + '/cancel', {});
        alert('Cancelled');
        loadList();
    });

    apiGet('/api/Reports/summary').then(function(s) {
        document.getElementById('financeSummary').innerHTML =
            '<p>Revenue: <strong>' + s.totalRevenue + '</strong></p>' +
            '<p>Receipts: ' + s.receiptsCount + '</p>' +
            '<p>Completed visits: ' + s.completed + '</p>';
    });

    function loadList() {
        apiGet('/api/Appointments').then(function(items) {
            document.getElementById('apptBody').innerHTML = items.map(function(a) {
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + (problems[a.serviceNeeded] || a.serviceNeeded) + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + statusBadge(a.status) + '</td></tr>';
            }).join('');
        });
    }
});

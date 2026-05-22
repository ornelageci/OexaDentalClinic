document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('manager')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    document.getElementById('checkinBtn').addEventListener('click', async function() {
        const id = document.getElementById('checkinId').value.trim();
        if (!id) return;
        await apiPatch('/api/Appointments/' + id + '/status', { status: 'InProgress' });
        document.getElementById('checkinMsg').textContent = 'Checked in #' + id;
        loadList();
    });

    document.getElementById('checkoutBtn').addEventListener('click', async function() {
        const id = document.getElementById('checkoutId').value.trim();
        const amount = Number(document.getElementById('amount').value || 0);
        if (!id) return;
        await apiPatch('/api/Appointments/' + id + '/status', { status: 'Completed' });
        const receipt = await apiPost('/api/Receipts', { appointmentId: Number(id), totalAmount: amount });
        document.getElementById('checkoutMsg').textContent = 'Receipt: ' + receipt.receiptNumber + ' | Total: ' + receipt.totalAmount;
        loadList();
    });

    document.getElementById('rescheduleBtn').addEventListener('click', async function() {
        const id = document.getElementById('actionId').value.trim();
        const date = document.getElementById('newDate').value;
        const time = document.getElementById('newTime').value;
        const appt = await apiGet('/api/Appointments/' + id);
        await apiPut('/api/Appointments/' + id + '/reschedule', {
            firstName: appt.firstName,
            lastName: appt.lastName,
            email: appt.email,
            phoneNumber: appt.phoneNumber,
            preferredDate: date,
            preferredTime: time,
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

    apiGet('/api/Users/dentists').then(function(dentists) {
        document.getElementById('assignDentist').innerHTML = dentists.map(function(d) {
            return '<option value="' + d.id + '">Dr. ' + d.firstName + ' ' + d.lastName + ' (' + d.dentistServiceKey + ')</option>';
        }).join('');
    });

    document.getElementById('assignBtn').addEventListener('click', async function() {
        const apptId = document.getElementById('assignApptId').value.trim();
        const dentistId = document.getElementById('assignDentist').value;
        if (!apptId || !dentistId) return;
        await apiPatch('/api/Appointments/' + apptId + '/assign', { dentistUserId: Number(dentistId) });
        alert('Assigned');
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
            const body = document.getElementById('apptBody');
            body.innerHTML = items.map(function(a) {
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + statusBadge(a.status) + '</td></tr>';
            }).join('');
        });
    }
    loadList();
});

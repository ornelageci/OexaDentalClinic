document.addEventListener('DOMContentLoaded', function() {
    const user = requireStaffRole('dentist');
    if (!user) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);
    document.getElementById('dentistName').textContent = 'Dr. ' + user.firstName + ' ' + user.lastName;

    const body = document.getElementById('scheduleBody');

    function load() {
        apiGet('/api/Appointments?dentistId=' + user.id).then(function(items) {
            body.innerHTML = items.map(function(a) {
                let actions = '';
                if (a.status === 'InProgress' || a.status === 'Booked') {
                    actions += '<button class="btn btn-sm btn-primary me-1" data-complete="' + a.id + '">Complete</button>';
                }
                actions += '<button class="btn btn-sm btn-outline-secondary" data-open="' + a.id + '">Open</button>';
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + statusBadge(a.status) + '</td><td>' + actions + '</td></tr>';
            }).join('');

            body.querySelectorAll('[data-complete]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    await apiPatch('/api/Appointments/' + btn.getAttribute('data-complete') + '/status', { status: 'Completed' });
                    load();
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
            medicationPrescribed: document.getElementById('medication').value
        });
        alert('Treatment saved');
    });

    load();
});

document.addEventListener('DOMContentLoaded', function() {
    const user = requirePatient();
    if (!user) return;

    const body = document.getElementById('myAppointmentsBody');
    if (!body) return;

    loadAppointments(user.email);
    loadReminders(user.email);

    async function loadReminders(email) {
        try {
            const items = await apiGet('/api/Notifications/upcoming?email=' + encodeURIComponent(email));
            const box = document.getElementById('remindersBox');
            if (!box || !items.length) return;
            box.classList.remove('d-none');
            box.innerHTML = '<strong>Upcoming reminders</strong><ul class="mb-0 mt-2">' +
                items.map(function(a) {
                    return '<li>' + formatDateTime(a.preferredDateTime) + ' — ' + a.serviceNeeded +
                        (a.reminderSent ? ' (email sent)' : ' (email pending)') + '</li>';
                }).join('') + '</ul>';
        } catch {}
    }

    async function loadAppointments(email) {
        try {
            const items = await apiGet('/api/Appointments?email=' + encodeURIComponent(email));
            if (!items.length) {
                body.innerHTML = '<tr><td colspan="4" class="text-center text-muted">No appointments yet.</td></tr>';
                return;
            }

            body.innerHTML = items.map(function(a) {
                let actions = '';
                if (a.status === 'Booked') {
                    actions += '<button class="btn btn-sm btn-outline-danger me-1" data-cancel="' + a.id + '">Cancel</button>';
                }
                if (a.status === 'Completed') {
                    actions += '<button class="btn btn-sm btn-primary" data-rate="' + a.id + '">Rate</button>';
                }
                return (
                    '<tr>' +
                        '<td>' + formatDateTime(a.preferredDateTime) + '</td>' +
                        '<td>' + a.serviceNeeded + (a.isSpecialAppointment ? ' (Special)' : '') + '</td>' +
                        '<td>' + statusBadge(a.status) + '</td>' +
                        '<td>' + actions + '</td>' +
                    '</tr>'
                );
            }).join('');

            body.querySelectorAll('[data-cancel]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    const id = btn.getAttribute('data-cancel');
                    if (!confirm('Cancel this appointment?')) return;
                    await apiPost('/api/Appointments/' + id + '/cancel', {});
                    loadAppointments(email);
                });
            });

            body.querySelectorAll('[data-rate]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    const id = btn.getAttribute('data-rate');
                    const rating = prompt('Rate dentist (1-5):', '5');
                    if (!rating) return;
                    const comment = prompt('Optional comment:') || '';
                    await apiPost('/api/Reviews', {
                        appointmentId: Number(id),
                        rating: Number(rating),
                        comment: comment
                    });
                    alert('Thank you for your feedback!');
                });
            });
        } catch {
            body.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Could not load appointments.</td></tr>';
        }
    }
});

document.addEventListener('DOMContentLoaded', function() {
    const user = requirePatient();
    if (!user) return;

    const body = document.getElementById('myAppointmentsBody');
    if (!body) return;

    const rateModalEl = document.getElementById('rateModal');
    const rateModal = rateModalEl ? new bootstrap.Modal(rateModalEl) : null;
    const rateSummary = document.getElementById('rateApptSummary');
    const rateValue = document.getElementById('rateValue');
    const rateComment = document.getElementById('rateComment');
    const rateSubmitBtn = document.getElementById('rateSubmitBtn');
    let appointmentsCache = [];
    let ratingApptId = null;

    function pick(obj, a, b) {
        if (!obj) return undefined;
        if (obj[a] !== undefined && obj[a] !== null) return obj[a];
        return obj[b];
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function starDisplay(n) {
        var num = Number(n) || 0;
        var filled = '★'.repeat(Math.min(5, Math.max(0, num)));
        var empty = '☆'.repeat(5 - filled.length);
        return '<span class="text-warning" title="' + num + '/5">' + filled + empty + '</span>';
    }

    function setStarRating(value) {
        var v = Math.min(5, Math.max(1, Number(value) || 5));
        rateValue.value = String(v);
        document.querySelectorAll('.rate-star').forEach(function(btn) {
            var n = Number(btn.getAttribute('data-value'));
            btn.classList.toggle('is-active', n <= v);
        });
    }

    document.querySelectorAll('.rate-star').forEach(function(btn) {
        btn.addEventListener('click', function() {
            setStarRating(btn.getAttribute('data-value'));
        });
    });

    if (rateSubmitBtn) {
        rateSubmitBtn.addEventListener('click', async function() {
            if (!ratingApptId) return;
            var rating = Number(rateValue.value);
            if (rating < 1 || rating > 5) {
                alert('Please select a rating from 1 to 5 stars.');
                return;
            }
            try {
                await apiPost('/api/Reviews', {
                    appointmentId: Number(ratingApptId),
                    rating: rating,
                    comment: rateComment.value.trim() || null
                });
                rateModal.hide();
                alert('Thank you for your feedback!');
                loadAppointments(user.email);
            } catch (e) {
                alert(e.message || 'Could not submit rating.');
            }
        });
    }

    function openRateModal(appt) {
        ratingApptId = pick(appt, 'id', 'Id');
        var dentist = pick(appt, 'dentistName', 'DentistName') || 'Not assigned';
        var treatments = pick(appt, 'serviceNames', 'ServiceNames') || pick(appt, 'serviceNeeded', 'ServiceNeeded');
        var notes = pick(appt, 'additionalNotes', 'AdditionalNotes');
        var special = pick(appt, 'isSpecialAppointment', 'IsSpecialAppointment');

        rateSummary.innerHTML =
            '<dl class="mb-0">' +
            '<dt>Appointment</dt><dd>#' + escapeHtml(ratingApptId) + '</dd>' +
            '<dt>Date &amp; time</dt><dd>' + escapeHtml(formatDateTime(pick(appt, 'preferredDateTime', 'PreferredDateTime'))) + '</dd>' +
            '<dt>Treatments</dt><dd>' + escapeHtml(treatments) + (special ? ' <span class="badge bg-info">Special visit</span>' : '') + '</dd>' +
            '<dt>Dentist</dt><dd>' + escapeHtml(dentist) + '</dd>' +
            (notes ? '<dt>Your message</dt><dd>' + escapeHtml(notes) + '</dd>' : '') +
            '</dl>';

        rateComment.value = '';
        setStarRating(5);
        rateModal.show();
    }

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
                    var svc = pick(a, 'serviceNames', 'ServiceNames') || pick(a, 'serviceNeeded', 'ServiceNeeded');
                    return '<li>' + formatDateTime(pick(a, 'preferredDateTime', 'PreferredDateTime')) + ' — ' + escapeHtml(svc) +
                        (pick(a, 'reminderSent', 'ReminderSent') ? ' (email sent)' : ' (email pending)') + '</li>';
                }).join('') + '</ul>';
        } catch { /* optional */ }
    }

    async function loadAppointments(email) {
        try {
            const items = await apiGet('/api/Appointments?email=' + encodeURIComponent(email));
            appointmentsCache = items;
            if (!items.length) {
                body.innerHTML = '<tr><td colspan="5" class="text-center text-muted">No appointments yet.</td></tr>';
                return;
            }

            body.innerHTML = items.map(function(a) {
                const status = pick(a, 'status', 'Status');
                const hasReview = pick(a, 'hasReview', 'HasReview');
                const reviewRating = pick(a, 'reviewRating', 'ReviewRating');
                const treatments = pick(a, 'serviceNames', 'ServiceNames') || pick(a, 'serviceNeeded', 'ServiceNeeded');
                const dentist = pick(a, 'dentistName', 'DentistName') || '—';
                const special = pick(a, 'isSpecialAppointment', 'IsSpecialAppointment');

                let actions = '';
                if (status === 'Booked') {
                    actions += '<button type="button" class="btn btn-sm btn-outline-danger me-1" data-cancel="' + pick(a, 'id', 'Id') + '">Cancel</button>';
                }
                if (status === 'Completed') {
                    if (hasReview) {
                        actions += '<button type="button" class="btn btn-sm btn-success btn-rated" disabled>Rated ' + starDisplay(reviewRating) + '</button>';
                    } else {
                        actions += '<button type="button" class="btn btn-sm btn-primary" data-rate="' + pick(a, 'id', 'Id') + '">Rate</button>';
                    }
                }

                return (
                    '<tr>' +
                        '<td>' + formatDateTime(pick(a, 'preferredDateTime', 'PreferredDateTime')) + '</td>' +
                        '<td>' + escapeHtml(treatments) + (special ? ' <span class="badge bg-light text-dark border">Special</span>' : '') + '</td>' +
                        '<td>' + escapeHtml(dentist) + '</td>' +
                        '<td>' + statusBadge(status) + '</td>' +
                        '<td>' + actions + '</td>' +
                    '</tr>'
                );
            }).join('');

            body.querySelectorAll('[data-cancel]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    const id = btn.getAttribute('data-cancel');
                    if (!confirm('Cancel this appointment?')) return;
                    try {
                        await apiPost('/api/Appointments/' + id + '/cancel', {});
                        loadAppointments(email);
                    } catch (e) {
                        alert(e.message || 'Could not cancel.');
                    }
                });
            });

            body.querySelectorAll('[data-rate]').forEach(function(btn) {
                btn.addEventListener('click', function() {
                    const id = Number(btn.getAttribute('data-rate'));
                    const appt = appointmentsCache.find(function(a) { return pick(a, 'id', 'Id') === id; });
                    if (appt) openRateModal(appt);
                });
            });
        } catch {
            body.innerHTML = '<tr><td colspan="5" class="text-center text-danger">Could not load appointments.</td></tr>';
        }
    }
});

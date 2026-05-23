(function() {
    function pick(obj, a, b) {
        if (!obj) return undefined;
        if (obj[a] !== undefined && obj[a] !== null) return obj[a];
        return obj[b];
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function row(label, value) {
        return '<dt class="col-sm-4 text-muted">' + escapeHtml(label) + '</dt>' +
            '<dd class="col-sm-8">' + (value || '<span class="text-muted">—</span>') + '</dd>';
    }

    function ensureInfoModal() {
        if (document.getElementById('apptInfoModal')) return;
        var wrap = document.createElement('div');
        wrap.innerHTML =
            '<div class="modal fade" id="apptInfoModal" tabindex="-1" aria-labelledby="apptInfoModalLabel" aria-hidden="true">' +
            '  <div class="modal-dialog modal-dialog-centered modal-lg modal-dialog-scrollable">' +
            '    <div class="modal-content">' +
            '      <div class="modal-header">' +
            '        <h5 class="modal-title" id="apptInfoModalLabel">Appointment details</h5>' +
            '        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
            '      </div>' +
            '      <div class="modal-body" id="apptInfoModalBody"></div>' +
            '      <div class="modal-footer">' +
            '        <button type="button" class="btn btn-primary" data-bs-dismiss="modal">OK</button>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(wrap.firstElementChild);
    }

    function formatTreatmentsList(treatments) {
        if (!treatments || !treatments.length) return '—';
        return treatments.map(function(t) {
            return escapeHtml(pick(t, 'name', 'Name') || pick(t, 'key', 'Key'));
        }).join(', ');
    }

    function renderDentistBody(d) {
        var appt = d.appointment || d.Appointment || {};
        var notes = pick(appt, 'additionalNotes', 'AdditionalNotes');
        return '<dl class="row mb-0">' +
            row('Date & time', escapeHtml(formatDateTime(pick(appt, 'preferredDateTime', 'PreferredDateTime')))) +
            row('Treatments', formatTreatmentsList(d.treatments || d.Treatments)) +
            row('Message', notes ? escapeHtml(notes) : null) +
            '</dl>';
    }

    function renderTreatmentPricing(treatments) {
        if (!treatments || !treatments.length) {
            return '<p class="text-muted mb-0">No treatments listed.</p>';
        }
        var html = '<ul class="list-group list-group-flush mb-0">';
        treatments.forEach(function(t) {
            var name = pick(t, 'name', 'Name');
            var base = pick(t, 'basePrice', 'BasePrice');
            var discount = pick(t, 'discountPercent', 'DiscountPercent');
            var after = pick(t, 'priceAfterDiscount', 'PriceAfterDiscount');
            var display = pick(t, 'displayPrice', 'DisplayPrice');
            var priceLine = base != null
                ? (discount
                    ? '<span class="text-decoration-line-through text-muted me-1">' + base + ' EUR</span>' +
                      '<strong class="text-success">' + (after != null ? after : display) + ' EUR</strong>' +
                      ' <span class="badge bg-success">-' + discount + '%</span>'
                    : '<strong>' + base + ' EUR</strong>')
                : '—';
            html += '<li class="list-group-item px-0 d-flex justify-content-between align-items-start">' +
                '<span>' + escapeHtml(name) + '</span><span class="text-end">' + priceLine + '</span></li>';
        });
        html += '</ul>';
        return html;
    }

    function renderReceiptSection(receipt) {
        var html = '<h6 class="text-primary mt-3 mb-2">Receipt</h6>';
        if (!receipt) {
            return html + '<p class="text-muted mb-0">Receipt not done yet.</p>';
        }
        var status = pick(receipt, 'status', 'Status');
        var finalized = pick(receipt, 'isFinalized', 'IsFinalized') || status === 'Finalized';
        var number = pick(receipt, 'receiptNumber', 'ReceiptNumber');
        var meds = receipt.medications || receipt.Medications || [];

        if (!finalized) {
            html += '<p class="text-warning mb-2"><strong>Receipt not done yet</strong> — waiting for manager to add prices and finalize.</p>';
            html += '<p class="small text-muted mb-1">Receipt #' + escapeHtml(number) + ' · Status: ' + escapeHtml(status) + '</p>';
            if (meds.length) {
                html += '<ul class="mb-0">';
                meds.forEach(function(m) {
                    html += '<li>' + escapeHtml(pick(m, 'name', 'Name')) + '</li>';
                });
                html += '</ul>';
            } else {
                html += '<p class="text-muted small mb-0">No medications on receipt yet.</p>';
            }
            return html;
        }

        var total = pick(receipt, 'totalAmount', 'TotalAmount');
        html += '<p class="mb-2"><strong>Receipt #' + escapeHtml(number) + '</strong> · <span class="badge bg-success">Finalized</span></p>';
        if (meds.length) {
            html += '<table class="table table-sm mb-2"><thead><tr><th>Medication</th><th class="text-end">Price (EUR)</th></tr></thead><tbody>';
            meds.forEach(function(m) {
                var price = pick(m, 'unitPrice', 'UnitPrice');
                html += '<tr><td>' + escapeHtml(pick(m, 'name', 'Name')) + '</td>' +
                    '<td class="text-end">' + (price != null ? price : '—') + '</td></tr>';
            });
            html += '</tbody></table>';
        }
        html += '<p class="mb-0"><strong>Total: ' + (total != null ? total + ' EUR' : '—') + '</strong></p>';
        return html;
    }

    function renderAdminBody(d) {
        var appt = d.appointment || d.Appointment || {};
        var pricing = d.pricing || d.Pricing || {};
        var dentist = d.assignedDentist || d.AssignedDentist;
        var record = d.treatmentRecord || d.TreatmentRecord;
        var dentistName = dentist
            ? 'Dr. ' + escapeHtml(pick(dentist, 'firstName', 'FirstName') + ' ' + pick(dentist, 'lastName', 'LastName'))
            : null;

        var html = '<dl class="row mb-3">' +
            row('Appointment ID', '#' + escapeHtml(pick(appt, 'id', 'Id'))) +
            row('Patient', escapeHtml(pick(appt, 'firstName', 'FirstName') + ' ' + pick(appt, 'lastName', 'LastName'))) +
            row('Email', escapeHtml(pick(appt, 'email', 'Email'))) +
            row('Phone', escapeHtml(pick(appt, 'phoneNumber', 'PhoneNumber'))) +
            row('Date & time', escapeHtml(formatDateTime(pick(appt, 'preferredDateTime', 'PreferredDateTime')))) +
            row('Status', statusBadge(pick(appt, 'status', 'Status'))) +
            row('Assigned dentist', dentistName) +
            row('Special appointment', pick(appt, 'isSpecialAppointment', 'IsSpecialAppointment') ? 'Yes' : 'No') +
            row('Message', pick(appt, 'additionalNotes', 'AdditionalNotes') ? escapeHtml(pick(appt, 'additionalNotes', 'AdditionalNotes')) : null) +
            '</dl>';

        html += '<h6 class="text-primary mb-2">Treatments &amp; pricing</h6>';
        html += renderTreatmentPricing(d.treatments || d.Treatments);
        html += '<p class="mt-2 mb-0">' +
            '<span class="me-3">Subtotal (list): <strong>' + (pick(pricing, 'estimatedBaseTotal', 'EstimatedBaseTotal') ?? '—') + ' EUR</strong></span>' +
            '<span class="me-3 text-success">You save: <strong>' + (pick(pricing, 'estimatedSavings', 'EstimatedSavings') ?? '0') + ' EUR</strong></span>' +
            '<span>Estimated visit: <strong>' + (pick(pricing, 'estimatedTotal', 'EstimatedTotal') ?? '—') + ' EUR</strong></span>' +
            '</p>';

        html += '<h6 class="text-primary mt-3 mb-2">Treatment record</h6>';
        if (!record) {
            html += '<p class="text-muted">No treatment record saved yet.</p>';
        } else {
            html += '<dl class="row mb-0">' +
                row('Diagnosis', pick(record, 'diagnosis', 'Diagnosis') ? escapeHtml(pick(record, 'diagnosis', 'Diagnosis')) : null) +
                row('Treatment performed', pick(record, 'treatmentPerformed', 'TreatmentPerformed') ? escapeHtml(pick(record, 'treatmentPerformed', 'TreatmentPerformed')) : null) +
                row('Recommendations', pick(record, 'recommendations', 'Recommendations') ? escapeHtml(pick(record, 'recommendations', 'Recommendations')) : null) +
                '</dl>';
        }

        html += renderReceiptSection(d.receipt || d.Receipt);
        return html;
    }

    window.showAppointmentInfo = async function(appointmentId, role) {
        ensureInfoModal();
        var modalEl = document.getElementById('apptInfoModal');
        var bodyEl = document.getElementById('apptInfoModalBody');
        var titleEl = document.getElementById('apptInfoModalLabel');
        titleEl.textContent = 'Appointment #' + appointmentId;
        bodyEl.innerHTML = '<p class="text-muted mb-0">Loading...</p>';

        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();

        try {
            var d = await apiGet('/api/Appointments/' + appointmentId + '/details', { loading: true });
            if (role === 'dentist') {
                titleEl.textContent = 'Visit info #' + appointmentId;
                bodyEl.innerHTML = renderDentistBody(d);
            } else {
                titleEl.textContent = 'Appointment #' + appointmentId + ' — full details';
                bodyEl.innerHTML = renderAdminBody(d);
            }
        } catch (e) {
            bodyEl.innerHTML = '<p class="text-danger">' + escapeHtml(e.message || 'Could not load details') + '</p>';
        }
    };
})();

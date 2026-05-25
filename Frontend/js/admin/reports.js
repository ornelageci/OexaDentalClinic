document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;

    var content = document.getElementById('reportsContent');
    var yearInput = document.getElementById('revYear');
    var monthInput = document.getElementById('revMonth');

    var now = new Date();
    yearInput.value = String(now.getFullYear());
    monthInput.value = String(now.getMonth() + 1).padStart(2, '0');

    monthInput.addEventListener('blur', function() {
        var m = monthInput.value.replace(/\D/g, '');
        if (m.length === 1) monthInput.value = '0' + m;
    });

    function eur(n) {
        var x = Number(n);
        if (isNaN(x)) return '—';
        return x.toFixed(2) + ' EUR';
    }

    function pick(obj, a, b) {
        if (!obj) return undefined;
        if (obj[a] !== undefined && obj[a] !== null) return obj[a];
        return obj[b];
    }

    function getPeriodParams() {
        var y = parseInt(yearInput.value.replace(/\D/g, ''), 10);
        var mRaw = monthInput.value.replace(/\D/g, '');
        if (mRaw.length === 1) mRaw = '0' + mRaw;
        if (mRaw.length !== 2) return null;
        var m = parseInt(mRaw, 10);
        if (isNaN(y) || y < 2000 || y > 2100) return null;
        if (isNaN(m) || m < 0 || m > 12) return null;
        return { year: y, month: m, code: y + '-' + mRaw };
    }

    function loadRevenue() {
        var p = getPeriodParams();
        if (!p) {
            alert('Periudha: viti (4 shifra) + muaji (00–12).\n\n06 = muaji · 00 = viti i plotë');
            return;
        }
        var url = '/api/Reports/revenue?year=' + encodeURIComponent(p.year) +
            '&month=' + encodeURIComponent(p.month) + '&_=' + Date.now();

        content.innerHTML = '<p class="text-muted">Loading revenue...</p>';

        apiGet(url, { loading: true }).then(function(data) {
            var s = data.summary || data.Summary || {};
            var byDentist = data.byDentist || data.ByDentist || [];
            var receipts = data.receipts || data.Receipts || [];
            var code = pick(data, 'periodCode', 'PeriodCode') || p.code;
            var kind = pick(data, 'periodKind', 'PeriodKind') || (code.endsWith('-00') ? 'year' : 'month');
            var label = pick(data, 'periodLabel', 'PeriodLabel') ||
                pick(data, 'monthLabel', 'MonthLabel') || code;
            var vatPct = pick(data, 'vatRatePercent', 'VatRatePercent') || 20;
            var periodTitle = kind === 'year' ? 'Viti' : 'Muaji';
            var spanWord = kind === 'year' ? 'vitit' : 'muajit';

            var html = '<p class="mb-3"><strong>' + periodTitle + ':</strong> ' + label +
                ' <span class="text-muted">(' + code + ')</span> · <strong>TVSH:</strong> ' + vatPct + '%</p>';

            html += '<div class="row g-3 mb-4">';
            html += statCard('Subtotal (para TVSH)', eur(pick(s, 'subtotalBeforeVat', 'SubtotalBeforeVat')));
            html += statCard('TVSH (' + vatPct + '%)', eur(pick(s, 'vatAmount', 'VatAmount')));
            html += statCard('Total (pas TVSH)', eur(pick(s, 'totalAfterVat', 'TotalAfterVat')), true);
            html += statCard('Finalized receipts', pick(s, 'receiptCount', 'ReceiptCount'));
            html += '</div>';

            html += '<div class="row g-3 mb-4">';
            html += '<div class="col-md-6"><div class="card portal-card"><div class="card-body">' +
                '<div class="small text-muted">Treatments total</div><div class="h5 mb-0">' + eur(pick(s, 'treatmentsTotal', 'TreatmentsTotal')) + '</div></div></div></div>';
            html += '<div class="col-md-6"><div class="card portal-card"><div class="card-body">' +
                '<div class="small text-muted">Medications total</div><div class="h5 mb-0">' + eur(pick(s, 'medicationsTotal', 'MedicationsTotal')) + '</div></div></div></div>';
            html += '</div>';

            html += '<h4 class="mb-3">Revenue by dentist</h4>';
            if (!byDentist.length) {
                html += '<p class="text-muted">No dentist revenue for this ' + spanWord + '.</p>';
            } else {
                html += '<div class="table-responsive mb-4"><table class="table table-sm table-striped">' +
                    '<thead><tr><th>Dentist</th><th class="text-end">Treatments</th><th class="text-end">Medications</th>' +
                    '<th class="text-end">Para TVSH</th><th class="text-end">TVSH</th><th class="text-end">Pas TVSH</th></tr></thead><tbody>';
                byDentist.forEach(function(d) {
                    html += '<tr><td>' + pick(d, 'dentistName', 'DentistName') + '</td>' +
                        '<td class="text-end">' + eur(pick(d, 'treatmentsTotalEur', 'TreatmentsTotalEur')) + '</td>' +
                        '<td class="text-end">' + eur(pick(d, 'medicationsTotalEur', 'MedicationsTotalEur')) + '</td>' +
                        '<td class="text-end">' + eur(pick(d, 'subtotalBeforeVat', 'SubtotalBeforeVat')) + '</td>' +
                        '<td class="text-end">' + eur(pick(d, 'vatAmount', 'VatAmount')) + '</td>' +
                        '<td class="text-end"><strong>' + eur(pick(d, 'totalAfterVat', 'TotalAfterVat')) + '</strong></td></tr>';
                });
                html += '</tbody></table></div>';
            }

            html += '<h4 class="mb-3">Receipts (completed visits)</h4>';
            if (!receipts.length) {
                html += '<p class="text-muted">No finalized receipts for this ' + spanWord + '.</p>';
            } else {
                receipts.forEach(function(r, idx) {
                    var rid = 'receipt-' + idx;
                    html += '<div class="card portal-card mb-2">' +
                        '<div class="card-body py-3">' +
                        '<div class="d-flex flex-wrap justify-content-between align-items-start gap-2">' +
                        '<div><strong>' + pick(r, 'receiptNumber', 'ReceiptNumber') + '</strong> · ' +
                        pick(r, 'patientName', 'PatientName') + '<br>' +
                        '<span class="small text-muted">Appointment #' + pick(r, 'appointmentId', 'AppointmentId') + ' · ' +
                        formatDateTime(pick(r, 'visitDate', 'VisitDate')) + '</span></div>' +
                        '<div class="text-end"><div class="small text-muted">Total (pas TVSH)</div>' +
                        '<strong>' + eur(pick(r, 'totalAfterVat', 'TotalAfterVat')) + '</strong></div></div>' +
                        '<button class="btn btn-sm btn-outline-primary mt-2" type="button" data-bs-toggle="collapse" data-bs-target="#' + rid + '">Details</button>' +
                        '<div class="collapse mt-3" id="' + rid + '">' +
                        lineTable('Treatments', r.treatments || r.Treatments) +
                        lineTable('Medications', r.medications || r.Medications, true) +
                        totalsBlock(r) +
                        '</div></div></div>';
                });
            }

            content.innerHTML = html;
        }).catch(function(err) {
            content.innerHTML = '<p class="text-danger">Failed to load revenue: ' + (err.message || err) + '</p>';
        });
    }

    function statCard(label, value, highlight) {
        var cls = highlight ? 'border-primary' : '';
        return '<div class="col-md-6 col-lg-3"><div class="card portal-card ' + cls + '"><div class="card-body">' +
            '<div class="small text-muted">' + label + '</div><div class="h4 mb-0">' + value + '</div></div></div></div>';
    }

    function lineTable(title, lines, groupMedsByDentist) {
        lines = lines || [];
        if (!lines.length) return '<p class="small text-muted mb-2">No ' + title.toLowerCase() + '.</p>';

        if (groupMedsByDentist && title === 'Medications') {
            var groups = {};
            var order = [];
            lines.forEach(function(l) {
                var key = pick(l, 'dentistName', 'DentistName') || '—';
                if (!groups[key]) {
                    groups[key] = [];
                    order.push(key);
                }
                groups[key].push(l);
            });
            var h = '<h6 class="mt-2 mb-1">' + title + '</h6>';
            order.forEach(function(dentist) {
                h += '<p class="small fw-semibold mb-1">' + dentist + '</p>';
                h += '<table class="table table-sm mb-2"><thead><tr><th>Item</th><th class="text-end">EUR</th></tr></thead><tbody>';
                groups[dentist].forEach(function(l) {
                    h += '<tr><td>' + pick(l, 'name', 'Name') + '</td><td class="text-end">' + eur(pick(l, 'amountEur', 'AmountEur')) + '</td></tr>';
                });
                h += '</tbody></table>';
            });
            return h;
        }

        var h = '<h6 class="mt-2 mb-1">' + title + '</h6><table class="table table-sm mb-2"><thead><tr><th>Item</th><th>Dentist</th><th class="text-end">EUR</th></tr></thead><tbody>';
        lines.forEach(function(l) {
            h += '<tr><td>' + pick(l, 'name', 'Name') + '</td><td class="small">' + (pick(l, 'dentistName', 'DentistName') || '—') + '</td>' +
                '<td class="text-end">' + eur(pick(l, 'amountEur', 'AmountEur')) + '</td></tr>';
        });
        return h + '</tbody></table>';
    }

    function totalsBlock(r) {
        return '<div class="bg-light rounded p-3 small">' +
            '<div class="d-flex justify-content-between"><span>Treatments</span><span>' + eur(pick(r, 'treatmentsTotalEur', 'TreatmentsTotalEur')) + '</span></div>' +
            '<div class="d-flex justify-content-between"><span>Medications</span><span>' + eur(pick(r, 'medicationsTotalEur', 'MedicationsTotalEur')) + '</span></div>' +
            '<hr class="my-2">' +
            '<div class="d-flex justify-content-between"><span>Subtotal (para TVSH)</span><span>' + eur(pick(r, 'subtotalBeforeVat', 'SubtotalBeforeVat')) + '</span></div>' +
            '<div class="d-flex justify-content-between"><span>TVSH (' + (pick(r, 'vatRatePercent', 'VatRatePercent') || 20) + '%)</span><span>' + eur(pick(r, 'vatAmount', 'VatAmount')) + '</span></div>' +
            '<div class="d-flex justify-content-between fw-bold"><span>Total (pas TVSH)</span><span>' + eur(pick(r, 'totalAfterVat', 'TotalAfterVat')) + '</span></div>' +
            '</div>';
    }

    document.getElementById('loadRevenueBtn').addEventListener('click', loadRevenue);
    loadRevenue();
});

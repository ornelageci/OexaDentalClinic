document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('manager')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    let problems = {};

    apiGet('/api/Problems').then(function(items) {
        items.forEach(function(p) { problems[p.key] = p.name; });
        loadUnassigned();
        loadList();
    }).catch(function(e) {
        alert('Could not load treatments: ' + (e.message || e));
    });

    function pick(obj) {
        for (var i = 1; i < arguments.length; i++) {
            var key = arguments[i];
            if (obj && obj[key] !== undefined) return obj[key];
        }
        return undefined;
    }

    function formatTimeRange(startIso, durationMinutes) {
        if (!startIso) return '—';
        var start = new Date(startIso);
        if (isNaN(start.getTime())) return formatDateTime(startIso);
        var end = new Date(start.getTime() + (durationMinutes || 60) * 60000);
        return start.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) +
            ' – ' + end.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    function loadUnassigned() {
        apiGet('/api/Appointments/unassigned').then(function(items) {
            const body = document.getElementById('unassignedBody');
            if (!items.length) {
                body.innerHTML = '<tr><td colspan="5" class="text-muted">No treatments waiting for dentist assignment.</td></tr>';
                return;
            }
            body.innerHTML = items.map(function(row) {
                var apptId = row.appointmentId || row.id;
                var problemKey = row.problemKey || '';
                var when = row.scheduledStart || row.preferredDateTime;
                var duration = row.durationMinutes || 60;
                var windowLabel = '';
                if (row.windowStart && row.windowEnd) {
                    windowLabel = '<div class="small text-muted">Window: ' +
                        formatDateTime(row.windowStart) + ' – ' +
                        new Date(row.windowEnd).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) +
                        '</div>';
                }
                return '<tr data-appt="' + apptId + '" data-problem="' + escapeAttr(problemKey) + '" data-line="' + (row.treatmentLineId || '') + '" data-duration="' + duration + '">' +
                    '<td>' + apptId + '</td>' +
                    '<td>' + row.firstName + ' ' + row.lastName + '</td>' +
                    '<td>' + (row.problemName || problems[problemKey] || problemKey) + '</td>' +
                    '<td>' + formatTimeRange(when, duration) + windowLabel + '</td>' +
                    '<td class="assign-cell">' +
                    '<select class="form-select form-select-sm assign-select"><option value="">Loading dentists...</option></select>' +
                    '<button type="button" class="btn btn-sm btn-primary assign-btn mt-1">Assign</button>' +
                    '<button type="button" class="btn btn-sm btn-outline-secondary reschedule-row-btn mt-1 ms-1">Reschedule</button>' +
                    '<div class="small text-danger mt-1 assign-error" style="display:none"></div>' +
                    '</td></tr>';
            }).join('');

            body.querySelectorAll('tr[data-problem]').forEach(function(tr) {
                const problemKey = tr.getAttribute('data-problem');
                const sel = tr.querySelector('.assign-select');
                apiGet('/api/Users/dentists?problemKey=' + encodeURIComponent(problemKey)).then(function(dentists) {
                    if (!dentists.length) {
                        sel.innerHTML = '<option value="">No dentist for this treatment</option>';
                        sel.disabled = true;
                        return;
                    }
                    sel.disabled = false;
                    sel.innerHTML = '<option value="">Choose dentist</option>' + dentists.map(function(d) {
                        return '<option value="' + d.id + '">Dr. ' + d.firstName + ' ' + d.lastName + '</option>';
                    }).join('');
                }).catch(function() {
                    sel.innerHTML = '<option value="">Could not load dentists</option>';
                });

                tr.querySelector('.assign-btn').addEventListener('click', function() {
                    assignTreatment(tr);
                });
                tr.querySelector('.reschedule-row-btn').addEventListener('click', function() {
                    openTreatmentReschedule(tr);
                });
            });
        }).catch(function(e) {
            document.getElementById('unassignedBody').innerHTML =
                '<tr><td colspan="5" class="text-danger">Failed to load: ' + (e.message || e) + '</td></tr>';
        });
    }

    async function assignTreatment(tr) {
        const apptId = tr.getAttribute('data-appt');
        const problemKey = tr.getAttribute('data-problem');
        const sel = tr.querySelector('.assign-select');
        const errBox = tr.querySelector('.assign-error');
        const btn = tr.querySelector('.assign-btn');
        const dentistId = sel && sel.value;

        errBox.style.display = 'none';
        errBox.textContent = '';

        if (!dentistId) {
            alert('Please select a dentist for this treatment.');
            return;
        }

        const payload = {
            problemKey: problemKey,
            dentistUserId: Number(dentistId)
        };

        btn.disabled = true;
        const prevText = btn.textContent;
        btn.textContent = 'Assigning...';

        try {
            await apiPatch('/api/Appointments/' + apptId + '/assign-treatment', payload);
            alert('Treatment assigned. Confirmation emails were sent.');
            loadUnassigned();
            loadList();
        } catch (e) {
            var msg = e.message || 'Assignment failed.';
            var body = e.body || {};
            errBox.textContent = msg;
            errBox.style.display = 'block';
            if (body.needsReschedule) {
                openTreatmentReschedule(tr, body);
                alert(msg + ' Use the Reschedule section below to pick an available time, then click Reschedule.');
            } else {
                alert(msg);
            }
        } finally {
            btn.disabled = false;
            btn.textContent = prevText;
        }
    }

    function ensureTimeSelect() {
        var el = document.getElementById('newTime');
        if (!el || el.tagName === 'SELECT') return;
        el.outerHTML = '<select class="form-select" id="newTime" disabled><option value="">Select date and dentist first</option></select>';
    }

    function ensureTimeInput() {
        var el = document.getElementById('newTime');
        if (!el || el.tagName === 'INPUT') return;
        el.outerHTML = '<input class="form-control" id="newTime" placeholder="HH:mm">';
    }

    function openTreatmentReschedule(tr, extra) {
        extra = extra || {};
        var apptId = tr.getAttribute('data-appt');
        var problemKey = tr.getAttribute('data-problem');
        var lineId = tr.getAttribute('data-line');
        var duration = tr.getAttribute('data-duration') || extra.durationMinutes || 60;
        var sel = tr.querySelector('.assign-select');
        var dentistId = extra.dentistUserId || (sel && sel.value);
        if (!dentistId) {
            alert('Select a dentist for this treatment first.');
            return;
        }

        ensureTimeSelect();
        document.getElementById('rescheduleMode').value = 'treatment';
        document.getElementById('actionId').value = apptId;
        document.getElementById('rescheduleProblemKey').value = problemKey;
        document.getElementById('rescheduleTreatmentLineId').value = lineId || extra.treatmentLineId || '';
        document.getElementById('rescheduleDentistId').value = dentistId || '';
        document.getElementById('rescheduleDuration').value = duration;

        var treatmentName = problems[problemKey] || problemKey;
        document.getElementById('rescheduleContext').textContent =
            'Rescheduling treatment: ' + treatmentName + ' (appointment #' + apptId + '). Busy times are disabled.';

        var dateVal = extra.suggestedDate || '';
        if (!dateVal && extra.windowStart) {
            dateVal = String(extra.windowStart).substring(0, 10);
        }
        document.getElementById('newDate').value = dateVal;

        var section = document.getElementById('rescheduleSection');
        section.scrollIntoView({ behavior: 'smooth', block: 'start' });

        if (dentistId && dateVal) {
            loadDentistSlots();
        } else {
            resetSlotSelect('Select a dentist in the row above, then pick a date.');
        }
    }

    function resetSlotSelect(message) {
        var newTime = document.getElementById('newTime');
        newTime.innerHTML = '<option value="">' + (message || 'Select date and dentist first') + '</option>';
        newTime.disabled = true;
        document.getElementById('slotHint').textContent = '';
    }

    async function loadDentistSlots() {
        var mode = document.getElementById('rescheduleMode').value;
        var newTime = document.getElementById('newTime');
        var slotHint = document.getElementById('slotHint');
        var date = document.getElementById('newDate').value;

        if (mode === 'treatment') {
            var dentistId = document.getElementById('rescheduleDentistId').value;
            var apptId = document.getElementById('actionId').value;
            var duration = document.getElementById('rescheduleDuration').value || 60;
            var lineId = document.getElementById('rescheduleTreatmentLineId').value;

            if (!dentistId || !date || !apptId) {
                resetSlotSelect('Select dentist and date first');
                return;
            }

            newTime.innerHTML = '<option value="">Loading times...</option>';
            newTime.disabled = true;

            try {
                var url = '/api/Appointments/dentist-slots?dentistId=' + encodeURIComponent(dentistId) +
                    '&date=' + encodeURIComponent(date) +
                    '&durationMinutes=' + encodeURIComponent(duration) +
                    '&appointmentId=' + encodeURIComponent(apptId);
                if (lineId) url += '&treatmentLineId=' + encodeURIComponent(lineId);

                var slots = await apiGet(url, { loading: true });
                if (!slots || !slots.length) {
                    resetSlotSelect('No slots in patient window');
                    slotHint.textContent = 'Try another date or use whole-appointment reschedule.';
                    return;
                }

                newTime.innerHTML = '<option value="">Select a time</option>' + slots.map(function(s) {
                    var time = pick(s, 'time', 'Time');
                    var label = pick(s, 'label', 'Label');
                    var available = pick(s, 'available', 'Available');
                    if (!available) label += ' (busy)';
                    return '<option value="' + time + '" ' + (available ? '' : 'disabled') + '>' + label + '</option>';
                }).join('');
                newTime.disabled = false;
                var availableCount = slots.filter(function(s) { return pick(s, 'available', 'Available'); }).length;
                slotHint.textContent = availableCount + ' of ' + slots.length + ' times available for this dentist.';
            } catch (e) {
                resetSlotSelect('Could not load slots');
                slotHint.textContent = e.message || '';
            }
            return;
        }

        resetSlotSelect('Whole-appointment reschedule: enter time as HH:mm');
        ensureTimeInput();
    }

    document.getElementById('newDate').addEventListener('change', function() {
        if (document.getElementById('rescheduleMode').value === 'treatment') {
            loadDentistSlots();
        }
    });

    document.getElementById('actionId').addEventListener('change', function() {
        document.getElementById('rescheduleMode').value = 'appointment';
        document.getElementById('rescheduleProblemKey').value = '';
        document.getElementById('rescheduleTreatmentLineId').value = '';
        document.getElementById('rescheduleDentistId').value = '';
        document.getElementById('rescheduleDuration').value = '';
        document.getElementById('rescheduleContext').textContent =
            'Reschedule a whole appointment, or use Reschedule on a treatment row to adjust one dentist\'s time.';
        ensureTimeInput();
        document.getElementById('slotHint').textContent = 'Enter a new start time as HH:mm for the whole visit.';
    });

    function escapeAttr(s) {
        return String(s).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;');
    }

    document.getElementById('checkinBtn').addEventListener('click', async function() {
        const id = document.getElementById('checkinId').value.trim();
        if (!id) return;
        try {
            await apiPatch('/api/Appointments/' + id + '/status', { status: 'InProgress' });
            document.getElementById('checkinMsg').textContent = 'Checked in #' + id + ' — emails sent.';
            loadList();
        } catch (e) {
            alert(e.message || 'Check-in failed');
        }
    });

    document.getElementById('loadReceiptBtn').addEventListener('click', async function() {
        const apptId = document.getElementById('receiptApptId').value.trim();
        const box = document.getElementById('receiptPricing');
        if (!apptId) return;
        try {
            const data = await apiGet('/api/Receipts/' + apptId, { loading: true });
            if (!data.medications || !data.medications.length) {
                box.innerHTML = '<p class="text-muted">No medications from dentist yet.</p>';
                return;
            }
            let html = '<table class="table table-sm"><thead><tr><th>Medication</th><th>Price (EUR)</th></tr></thead><tbody>';
            data.medications.forEach(function(m) {
                html += '<tr><td>' + m.name + '</td><td><input type="number" step="0.01" class="form-control form-control-sm med-price" data-id="' + m.id + '" value="' + (m.unitPrice || '') + '"></td></tr>';
            });
            html += '</tbody></table><button type="button" class="btn btn-primary" id="saveReceiptPrices">Finalize receipt</button>';
            box.innerHTML = html;
            document.getElementById('saveReceiptPrices').addEventListener('click', async function() {
                const lines = [];
                box.querySelectorAll('.med-price').forEach(function(inp) {
                    lines.push({ medicationId: Number(inp.getAttribute('data-id')), unitPrice: Number(inp.value) });
                });
                try {
                    await apiPut('/api/Receipts/' + data.receipt.id + '/prices', { lines: lines });
                    alert('Receipt finalized. Emails sent.');
                    box.innerHTML = '<p class="text-success">Receipt saved.</p>';
                } catch (e) {
                    alert(e.message || 'Could not save receipt');
                }
            });
        } catch {
            box.innerHTML = '<p class="text-danger">No receipt found for this appointment.</p>';
        }
    });

    document.getElementById('rescheduleBtn').addEventListener('click', async function() {
        const id = document.getElementById('actionId').value.trim();
        if (!id) return;

        var mode = document.getElementById('rescheduleMode').value;
        var dateVal = document.getElementById('newDate').value;
        var timeEl = document.getElementById('newTime');
        var timeVal = timeEl ? timeEl.value : '';

        if (!dateVal || !timeVal) {
            alert('Select date and time.');
            return;
        }

        try {
            if (mode === 'treatment') {
                var problemKey = document.getElementById('rescheduleProblemKey').value;
                var dentistId = document.getElementById('rescheduleDentistId').value;
                var payload = {
                    problemKey: problemKey,
                    preferredDate: dateVal,
                    preferredTime: timeVal
                };
                if (dentistId) payload.dentistUserId = Number(dentistId);

                await apiPatch('/api/Appointments/' + id + '/reschedule-treatment', payload);
                alert('Treatment rescheduled and assigned. Confirmation emails sent.');
            } else {
                const appt = await apiGet('/api/Appointments/' + id, { loading: true });
                var dateFormatted = dateVal.split('-').reverse().join('.');
                await apiPut('/api/Appointments/' + id + '/reschedule', {
                    firstName: appt.firstName, lastName: appt.lastName, email: appt.email, phoneNumber: appt.phoneNumber,
                    preferredDate: dateFormatted,
                    preferredTime: timeVal,
                    serviceNeeded: appt.serviceNeeded
                });
                alert('Rescheduled. Confirmation emails sent.');
            }
            loadList();
            loadUnassigned();
        } catch (e) {
            alert(e.message || 'Reschedule failed');
        }
    });

    document.getElementById('cancelBtn').addEventListener('click', async function() {
        const id = document.getElementById('actionId').value.trim();
        if (!id) return;
        try {
            await apiPost('/api/Appointments/' + id + '/cancel', {});
            alert('Cancelled');
            loadList();
            loadUnassigned();
        } catch (e) {
            alert(e.message || 'Cancel failed');
        }
    });

    apiGet('/api/Reports/summary').then(function(s) {
        document.getElementById('financeSummary').innerHTML =
            '<p>Revenue: <strong>' + s.totalRevenue + '</strong></p>' +
            '<p>Receipts: ' + s.receiptsCount + '</p>' +
            '<p>Completed visits: ' + s.completed + '</p>';
    }).catch(function() {
        document.getElementById('financeSummary').textContent = 'Could not load summary.';
    });

    function loadList() {
        apiGet('/api/Appointments').then(function(items) {
            document.getElementById('apptBody').innerHTML = items.map(function(a) {
                var svc = a.serviceNeeded || '';
                var label = problems[svc] || svc;
                if (!problems[svc] && svc.indexOf(',') >= 0) {
                    label = svc.split(',').map(function(k) { return problems[k.trim()] || k.trim(); }).join(', ');
                }
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + label + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + statusBadge(a.status) + '</td></tr>';
            }).join('');
        });
    }
});

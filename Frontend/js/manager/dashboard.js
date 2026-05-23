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
                return '<tr data-appt="' + apptId + '" data-problem="' + escapeAttr(problemKey) + '">' +
                    '<td>' + apptId + '</td>' +
                    '<td>' + row.firstName + ' ' + row.lastName + '</td>' +
                    '<td>' + (row.problemName || problems[problemKey] || problemKey) + '</td>' +
                    '<td>' + formatDateTime(when) + '</td>' +
                    '<td class="assign-cell">' +
                    '<select class="form-select form-select-sm assign-select"><option value="">Loading dentists...</option></select>' +
                    '<div class="reschedule-fields mt-1" style="display:none">' +
                    '<label class="form-label small mb-0">New time (this treatment only)</label>' +
                    '<div class="input-group input-group-sm">' +
                    '<input type="date" class="form-control assign-date">' +
                    '<input type="text" class="form-control assign-time" placeholder="HH:mm">' +
                    '</div></div>' +
                    '<button type="button" class="btn btn-sm btn-primary assign-btn mt-1">Assign</button>' +
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
        const rescheduleBox = tr.querySelector('.reschedule-fields');
        const dateInp = tr.querySelector('.assign-date');
        const timeInp = tr.querySelector('.assign-time');
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
        if (dateInp && dateInp.value && timeInp && timeInp.value.trim()) {
            payload.preferredDate = dateInp.value;
            payload.preferredTime = timeInp.value.trim();
        }

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
            var needsReschedule = e.body && e.body.needsReschedule;
            errBox.textContent = msg;
            errBox.style.display = 'block';
            if (needsReschedule && rescheduleBox) {
                rescheduleBox.style.display = 'block';
                alert(msg + ' Enter a new date and time below, then click Assign again.');
            } else {
                alert(msg);
            }
        } finally {
            btn.disabled = false;
            btn.textContent = prevText;
        }
    }

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
        try {
            const appt = await apiGet('/api/Appointments/' + id, { loading: true });
            await apiPut('/api/Appointments/' + id + '/reschedule', {
                firstName: appt.firstName, lastName: appt.lastName, email: appt.email, phoneNumber: appt.phoneNumber,
                preferredDate: document.getElementById('newDate').value,
                preferredTime: document.getElementById('newTime').value,
                serviceNeeded: appt.serviceNeeded
            });
            alert('Rescheduled. Confirmation emails sent.');
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

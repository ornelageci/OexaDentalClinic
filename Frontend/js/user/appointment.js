document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('appointmentForm');
    if (!form) return;

    const user = typeof getSession === 'function' ? getSession() : null;
    if (user && user.role === 'Patient') {
        const fn = document.getElementById('firstName');
        const ln = document.getElementById('lastName');
        const em = document.getElementById('email');
        const ph = document.getElementById('phone');
        if (fn) fn.value = user.firstName || '';
        if (ln) ln.value = user.lastName || '';
        if (em) { em.value = user.email || ''; em.readOnly = true; }
        if (ph) ph.value = user.phoneNumber || '';
    }

    let problems = [];
    const treatmentList = document.getElementById('treatmentList');
    const priceSummary = document.getElementById('priceSummary');
    const preferredDate = document.getElementById('preferredDate');
    const preferredTime = document.getElementById('preferredTime');
    const slotHint = document.getElementById('slotHint');

    const today = new Date();
    preferredDate.min = today.toISOString().split('T')[0];

    function formatPrice(p) {
        var price = p.hasPromotion ? p.priceAfterDiscount : p.basePrice;
        var html = '<span class="treatment-price">' + price + ' EUR</span>';
        if (p.hasPromotion) {
            html += ' <span class="treatment-was">' + p.basePrice + ' EUR</span>';
            html += ' <span class="treatment-badge">-' + p.discountPercent + '%</span>';
        }
        return html;
    }

    function renderTreatments() {
        if (!problems.length) {
            treatmentList.innerHTML = '<p class="text-danger">Could not load treatments.</p>';
            return;
        }
        treatmentList.innerHTML = problems.map(function(p) {
            return (
                '<label class="treatment-option">' +
                    '<input type="checkbox" class="treatment-check" value="' + p.key + '">' +
                    '<span class="treatment-info">' +
                        '<span class="treatment-name">' + p.name + '</span>' +
                        '<span class="treatment-meta">' + (p.durationMinutes || 60) + ' min · ' + formatPrice(p) + '</span>' +
                    '</span>' +
                '</label>'
            );
        }).join('');

        document.querySelectorAll('.treatment-check').forEach(function(cb) {
            cb.addEventListener('change', onSelectionChange);
        });
    }

    function getSelectedKeys() {
        return Array.from(document.querySelectorAll('.treatment-check:checked')).map(function(el) {
            return el.value;
        });
    }

    function updatePriceSummary() {
        var keys = getSelectedKeys();
        if (!keys.length) {
            priceSummary.style.display = 'none';
            priceSummary.innerHTML = '';
            return;
        }

        var lines = [];
        var total = 0;
        var totalWas = 0;
        keys.forEach(function(key) {
            var p = problems.find(function(x) { return x.key === key; });
            if (!p) return;
            var price = p.hasPromotion ? p.priceAfterDiscount : p.basePrice;
            total += price;
            totalWas += p.basePrice;
            lines.push('<div class="price-line"><span>' + p.name + '</span><span>' + price + ' EUR</span></div>');
        });

        var duration = keys.reduce(function(sum, key) {
            var p = problems.find(function(x) { return x.key === key; });
            return sum + (p ? (p.durationMinutes || 60) : 0);
        }, 0);

        var html = lines.join('');
        html += '<div class="price-line price-total"><span>Estimated visit (' + duration + ' min)</span><strong>' + total.toFixed(2) + ' EUR</strong></div>';
        if (totalWas > total) {
            html += '<div class="small text-success">You save ' + (totalWas - total).toFixed(2) + ' EUR with current promotions</div>';
        }
        priceSummary.innerHTML = html;
        priceSummary.style.display = 'block';
    }

    function onSelectionChange() {
        updatePriceSummary();
        loadTimeSlots();
    }

    async function loadTimeSlots() {
        var keys = getSelectedKeys();
        var date = preferredDate.value;

        preferredTime.innerHTML = '<option value="">Loading times...</option>';
        preferredTime.disabled = true;
        slotHint.textContent = '';

        if (!keys.length || !date) {
            preferredTime.innerHTML = '<option value="">Select treatments and date first</option>';
            return;
        }

        try {
            var services = keys.join(',');
            var slots = await apiGet('/api/Appointments/time-slots?date=' + encodeURIComponent(date) + '&services=' + encodeURIComponent(services));

            if (!slots.length) {
                preferredTime.innerHTML = '<option value="">No slots (clinic may be closed)</option>';
                slotHint.textContent = 'Choose another date.';
                return;
            }

            preferredTime.innerHTML = '<option value="">Select a time</option>' +
                slots.map(function(s) {
                    var label = s.available ? s.label : s.label + ' (unavailable)';
                    return '<option value="' + s.time + '" ' + (s.available ? '' : 'disabled class="slot-unavailable"') + '>' + label + '</option>';
                }).join('');

            preferredTime.disabled = false;
            var availableCount = slots.filter(function(s) { return s.available; }).length;
            slotHint.textContent = availableCount + ' of ' + slots.length + ' slots available for your selected treatments.';
        } catch (e) {
            preferredTime.innerHTML = '<option value="">Could not load slots</option>';
            slotHint.textContent = 'Make sure the backend is running.';
        }
    }

    apiGet('/api/Problems').then(function(items) {
        problems = items;
        renderTreatments();
    }).catch(function() {
        treatmentList.innerHTML = '<p class="text-danger">Could not load treatments. Is the API running?</p>';
    });

    preferredDate.addEventListener('change', loadTimeSlots);

    form.addEventListener('submit', async function(event) {
        event.preventDefault();

        var keys = getSelectedKeys();
        var firstName = document.getElementById('firstName').value.trim();
        var lastName = document.getElementById('lastName').value.trim();
        var email = document.getElementById('email').value.trim();
        var phoneNumber = document.getElementById('phone').value.trim();
        var date = preferredDate.value;
        var time = preferredTime.value;
        var additionalNotes = document.getElementById('message').value.trim();
        var isSpecial = document.getElementById('isSpecial') ? document.getElementById('isSpecial').checked : false;

        if (!firstName || !lastName || !email || !phoneNumber || !date || !time || !keys.length) {
            alert('Please fill in all required fields and select at least one treatment.');
            return;
        }

        var dateFormatted = date.split('-').reverse().join('.');

        try {
            var check = await apiGet('/api/Appointments/availability?service=' + encodeURIComponent(keys.join(',')) + '&date=' + encodeURIComponent(dateFormatted) + '&time=' + time);
            if (!check.available) {
                alert('This time slot is no longer available. Please choose another time.');
                loadTimeSlots();
                return;
            }
        } catch {
            alert('Could not check availability. Make sure the backend is running.');
            return;
        }

        try {
            var result = await apiPost('/api/Appointments', {
                firstName: firstName,
                lastName: lastName,
                email: email,
                phoneNumber: phoneNumber,
                preferredDate: dateFormatted,
                preferredTime: time,
                serviceNeeded: keys.join(','),
                additionalNotes: additionalNotes || null,
                isSpecialAppointment: isSpecial,
                patientUserId: user ? user.id : null
            });
            alert('Appointment booked successfully! Reception will assign your dentist.\nStatus: ' + result.status);
            form.reset();
            priceSummary.style.display = 'none';
            renderTreatments();
            preferredTime.innerHTML = '<option value="">Select treatments and date first</option>';
            preferredTime.disabled = true;
        } catch (err) {
            alert('Booking failed. ' + (err.message || ''));
        }
    });
});

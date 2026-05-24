document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('appointmentForm');
    if (!form) return;

    const user = typeof getSession === 'function' ? getSession() : null;
    const isPatient = user && (user.role === 'Patient' || user.Role === 'Patient');

    if (!isPatient) {
        form.querySelectorAll('input, select, textarea, button').forEach(function(el) {
            el.disabled = true;
        });
        var modalEl = document.getElementById('loginRequiredModal');
        if (modalEl) {
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
        } else {
            alert('You have to log in in order to book an appointment.');
            window.location.href = 'login.html';
        }
        return;
    }

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

    function pick(obj, a, b) {
        return obj[a] !== undefined && obj[a] !== null ? obj[a] : obj[b];
    }

    function isSunday(dateStr) {
        if (!dateStr) return false;
        var d = new Date(dateStr + 'T12:00:00');
        return d.getDay() === 0;
    }

    function formatPrice(p) {
        var hasPromo = pick(p, 'hasPromotion', 'HasPromotion');
        var base = pick(p, 'basePrice', 'BasePrice');
        var after = pick(p, 'priceAfterDiscount', 'PriceAfterDiscount');
        var discount = pick(p, 'discountPercent', 'DiscountPercent');
        var price = hasPromo ? after : base;
        var html = '<span class="treatment-price">' + price + ' EUR</span>';
        if (hasPromo) {
            html += ' <span class="treatment-was">' + base + ' EUR</span>';
            html += ' <span class="treatment-badge">-' + discount + '%</span>';
        }
        return html;
    }

    function renderTreatments() {
        if (!problems.length) {
            treatmentList.innerHTML = '<p class="text-danger">Could not load treatments.</p>';
            return;
        }
        treatmentList.innerHTML = problems.map(function(p) {
            var key = pick(p, 'key', 'Key');
            var name = pick(p, 'name', 'Name');
            var mins = pick(p, 'durationMinutes', 'DurationMinutes') || 60;
            return (
                '<label class="treatment-option">' +
                    '<input type="checkbox" class="treatment-check" value="' + key + '">' +
                    '<span class="treatment-info">' +
                        '<span class="treatment-name">' + name + '</span>' +
                        '<span class="treatment-meta">' + mins + ' min · ' + formatPrice(p) + '</span>' +
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
            var p = problems.find(function(x) { return (pick(x, 'key', 'Key')) === key; });
            if (!p) return;
            var hasPromo = pick(p, 'hasPromotion', 'HasPromotion');
            var base = pick(p, 'basePrice', 'BasePrice');
            var after = pick(p, 'priceAfterDiscount', 'PriceAfterDiscount');
            var price = hasPromo ? after : base;
            total += Number(price);
            totalWas += Number(base);
            lines.push('<div class="price-line"><span>' + pick(p, 'name', 'Name') + '</span><span>' + price + ' EUR</span></div>');
        });

        var duration = keys.reduce(function(sum, key) {
            var p = problems.find(function(x) { return (pick(x, 'key', 'Key')) === key; });
            return sum + (p ? (pick(p, 'durationMinutes', 'DurationMinutes') || 60) : 0);
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

    function onDateChange() {
        if (isSunday(preferredDate.value)) {
            alert('OEXA Dental Clinic is closed on Sundays. Please choose Monday–Saturday.');
            preferredDate.value = '';
            preferredTime.innerHTML = '<option value="">Select treatments and date first</option>';
            preferredTime.disabled = true;
            slotHint.textContent = 'Clinic closed on Sundays.';
            return;
        }
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

        if (isSunday(date)) {
            preferredTime.innerHTML = '<option value="">Closed on Sundays</option>';
            slotHint.textContent = 'Please pick a weekday or Saturday.';
            return;
        }

        try {
            var services = keys.join(',');
            var url = '/api/Appointments/time-slots?date=' + encodeURIComponent(date) + '&services=' + encodeURIComponent(services);
            var slots = await apiGet(url, { loading: true });

            if (!slots || !slots.length) {
                preferredTime.innerHTML = '<option value="">No slots available</option>';
                slotHint.textContent = 'Try another date or fewer treatments.';
                return;
            }

            preferredTime.innerHTML = '<option value="">Select a time</option>' +
                slots.map(function(s) {
                    var time = pick(s, 'time', 'Time');
                    var label = pick(s, 'label', 'Label');
                    var available = pick(s, 'available', 'Available');
                    if (!available) label += ' (unavailable)';
                    return '<option value="' + time + '" ' + (available ? '' : 'disabled') + '>' + label + '</option>';
                }).join('');

            preferredTime.disabled = false;
            var availableCount = slots.filter(function(s) { return pick(s, 'available', 'Available'); }).length;
            slotHint.textContent = availableCount + ' of ' + slots.length + ' slots available.';
        } catch (e) {
            preferredTime.innerHTML = '<option value="">Could not load slots</option>';
            var msg = (e && e.message) ? e.message : '';
            if (msg.indexOf('error') >= 0 || msg.indexOf('{') === 0) {
                try {
                    var parsed = JSON.parse(msg);
                    msg = parsed.error || parsed.detail || msg;
                } catch (_) { /* keep raw */ }
            }
            slotHint.textContent = msg || 'Make sure the backend is running at http://localhost:5095';
        }
    }

    apiGet('/api/Problems').then(function(items) {
        problems = items;
        renderTreatments();
    }).catch(function() {
        treatmentList.innerHTML = '<p class="text-danger">Could not load treatments. Run the API with dotnet run.</p>';
    });

    preferredDate.addEventListener('change', onDateChange);
    preferredDate.addEventListener('input', onDateChange);

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

        if (isSunday(date)) {
            alert('Cannot book on Sundays — clinic is closed.');
            return;
        }

        if (!firstName || !lastName || !email || !phoneNumber || !date || !time || !keys.length) {
            alert('Please fill in all required fields and select at least one treatment.');
            return;
        }

        var dateFormatted = date.split('-').reverse().join('.');

        try {
            var check = await apiGet('/api/Appointments/availability?service=' + encodeURIComponent(keys.join(',')) + '&date=' + encodeURIComponent(dateFormatted) + '&time=' + time, { loading: true });
            var available = pick(check, 'available', 'Available');
            if (!available) {
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
            alert('Appointment booked successfully! Reception will assign your dentist.\nStatus: ' + (result.status || result.Status));
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

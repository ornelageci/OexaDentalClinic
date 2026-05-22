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

    apiGet('/api/Problems').then(function(items) {
        problems = items;
        const sel = document.getElementById('service');
        sel.innerHTML = '<option value="">Select your problem</option>' +
            items.map(function(p) {
                return '<option value="' + p.key + '">' + p.name + '</option>';
            }).join('');
    }).catch(function() {
        document.getElementById('service').innerHTML = '<option value="">Could not load problems</option>';
    });

    document.getElementById('service').addEventListener('change', function() {
        const p = problems.find(function(x) { return x.key === this.value; }.bind(this));
        const box = document.getElementById('pricePreview');
        if (!p || !box) return;
        if (p.hasPromotion) {
            box.innerHTML = '<span style="text-decoration:line-through">' + p.basePrice + ' EUR</span> ' +
                '<span class="text-danger">-' + p.discountPercent + '%</span> ' +
                '<strong>' + p.priceAfterDiscount + ' EUR</strong>';
        } else {
            box.innerHTML = '<strong>' + p.basePrice + ' EUR</strong>';
        }
    });

    form.addEventListener('submit', async function(event) {
        event.preventDefault();

        const firstName = document.getElementById('firstName').value.trim();
        const lastName = document.getElementById('lastName').value.trim();
        const email = document.getElementById('email').value.trim();
        const phoneNumber = document.getElementById('phone').value.trim();
        const preferredDate = document.getElementById('preferredDate').value;
        const preferredTime = document.getElementById('preferredTime').value;
        const serviceNeeded = document.getElementById('service').value;
        const additionalNotes = document.getElementById('message').value.trim();
        const isSpecial = document.getElementById('isSpecial') ? document.getElementById('isSpecial').checked : false;

        if (!firstName || !lastName || !email || !phoneNumber || !preferredDate || !preferredTime || !serviceNeeded) {
            alert('Please fill in all required fields.');
            return;
        }

        try {
            const check = await apiGet('/api/Appointments/availability?service=' + encodeURIComponent(serviceNeeded) + '&date=' + preferredDate + '&time=' + preferredTime);
            if (!check.available) {
                alert('This time slot is not available. Please choose another time.');
                return;
            }
        } catch {
            alert('Could not check availability. Make sure the backend is running.');
            return;
        }

        try {
            const result = await apiPost('/api/Appointments', {
                firstName: firstName,
                lastName: lastName,
                email: email,
                phoneNumber: phoneNumber,
                preferredDate: preferredDate,
                preferredTime: preferredTime,
                serviceNeeded: serviceNeeded,
                additionalNotes: additionalNotes || null,
                isSpecialAppointment: isSpecial,
                patientUserId: user ? user.id : null
            });
            alert('Appointment booked! Reception will assign a dentist.\nStatus: ' + result.status);
            form.reset();
            document.getElementById('pricePreview').innerHTML = '';
        } catch (err) {
            alert('Booking failed. ' + (err.message || ''));
        }
    });
});

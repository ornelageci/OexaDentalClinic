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

        const appointmentData = {
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
        };

        try {
            const result = await apiPost('/api/Appointments', appointmentData);
            alert('Appointment booked successfully!\nStatus: ' + result.status);
            form.reset();
        } catch (err) {
            alert('Booking failed. ' + (err.message || ''));
        }
    });
});

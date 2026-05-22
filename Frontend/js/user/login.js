document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('patientLoginForm');
    if (!form) return;

    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        try {
            const user = await apiPost('/api/Auth/login', {
                email: document.getElementById('email').value.trim(),
                password: document.getElementById('password').value
            });
            if ((user.role || '') !== 'Patient') {
                alert('Use Staff Login for staff accounts.');
                return;
            }
            setSession(user);
            window.location.href = 'index.html';
        } catch {
            alert('Login failed.');
        }
    });
});

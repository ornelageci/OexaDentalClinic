document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('registerForm');
    if (!form) return;

    form.addEventListener('submit', async function(e) {
        e.preventDefault();
        try {
            const user = await apiPost('/api/Auth/register', {
                firstName: document.getElementById('firstName').value.trim(),
                lastName: document.getElementById('lastName').value.trim(),
                email: document.getElementById('email').value.trim(),
                phoneNumber: document.getElementById('phone').value.trim(),
                password: document.getElementById('password').value
            });
            setSession(user);
            alert('Account created successfully!');
            window.location.href = 'book-appointment.html';
        } catch {
            alert('Registration failed. Email may already exist.');
        }
    });
});

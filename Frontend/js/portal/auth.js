document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('staffLoginForm');
    if (!form) return;

    form.addEventListener('submit', async function(e) {
        e.preventDefault();

        const email = document.getElementById('email').value.trim();
        const password = document.getElementById('password').value;

        try {
            const user = await apiPost('/api/Auth/login', { email: email, password: password });
            const role = (user.role || '').toLowerCase();
            setSession(user);

            if (role === 'admin') window.location.href = '../admin/dashboard.html';
            else if (role === 'manager') window.location.href = '../manager/dashboard.html';
            else if (role === 'marketer') window.location.href = '../marketer/dashboard.html';
            else if (role === 'dentist') window.location.href = '../dentist/dashboard.html';
            else alert('This login is for staff only. Patients use the main website login.');
        } catch (err) {
            alert('Login failed. Check email and password.');
        }
    });
});

function staffLogout() {
    clearSession();
    window.location.href = '../portal/login.html';
}

function requireStaffRole(role) {
    const user = getSession();
    const userRole = (user && (user.role || user.Role) || '').toLowerCase();
    if (userRole !== role) {
        window.location.href = '../portal/login.html';
        return null;
    }
    return user;
}

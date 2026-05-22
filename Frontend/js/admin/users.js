document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);
    const body = document.getElementById('usersBody');

    function load() {
        const role = document.getElementById('roleFilter').value;
        const url = role ? '/api/Users?role=' + encodeURIComponent(role) : '/api/Users';
        apiGet(url).then(function(users) {
            body.innerHTML = users.map(function(u) {
                return '<tr><td>' + u.id + '</td><td>' + u.firstName + ' ' + u.lastName + '</td><td>' + u.email + '</td><td>' + u.role + '</td><td><button class="btn btn-sm btn-outline-danger" data-del="' + u.id + '">Delete</button></td></tr>';
            }).join('');
            body.querySelectorAll('[data-del]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    if (!confirm('Delete user?')) return;
                    await fetch(API_BASE_URL + '/api/Users/' + btn.getAttribute('data-del'), { method: 'DELETE' });
                    load();
                });
            });
        });
    }

    document.getElementById('roleFilter').addEventListener('change', load);

    document.getElementById('addUserBtn').addEventListener('click', async function() {
        try {
            await apiPost('/api/Users', {
                firstName: document.getElementById('newFirst').value.trim(),
                lastName: document.getElementById('newLast').value.trim(),
                email: document.getElementById('newEmail').value.trim(),
                password: document.getElementById('newPass').value,
                role: document.getElementById('newRole').value,
                dentistServiceKey: document.getElementById('newDentistKey').value.trim() || null
            });
            alert('User created');
            load();
        } catch {
            alert('Could not create user');
        }
    });

    load();
});

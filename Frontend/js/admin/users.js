document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);
    const body = document.getElementById('usersBody');

    function load() {
        apiGet('/api/Users').then(function(users) {
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
    load();
});

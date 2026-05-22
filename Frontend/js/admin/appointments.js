document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);
    const body = document.getElementById('tableBody');

    function load() {
        apiGet('/api/Appointments').then(function(items) {
            body.innerHTML = items.map(function(a) {
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + a.serviceNeeded + '</td><td>' + statusBadge(a.status) + '</td></tr>';
            }).join('');
        });
    }
    document.getElementById('refreshBtn').addEventListener('click', load);
    load();
});

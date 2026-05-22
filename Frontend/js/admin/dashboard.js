document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    apiGet('/api/Reports/summary').then(function(s) {
        document.getElementById('statsRow').innerHTML =
            '<div class="col-md-3"><div class="portal-stat"><div class="text-muted">Total</div><h3>' + s.totalAppointments + '</h3></div></div>' +
            '<div class="col-md-3"><div class="portal-stat"><div class="text-muted">Completed</div><h3>' + s.completed + '</h3></div></div>' +
            '<div class="col-md-3"><div class="portal-stat"><div class="text-muted">Cancelled</div><h3>' + s.cancelled + '</h3></div></div>' +
            '<div class="col-md-3"><div class="portal-stat"><div class="text-muted">Revenue</div><h3>' + s.totalRevenue + '</h3></div></div>';
    }).catch(function() {});
});

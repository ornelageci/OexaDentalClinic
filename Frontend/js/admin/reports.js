document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    apiGet('/api/Reports/summary').then(function(s) {
        let html = '<div class="row g-3 mb-4">';
        html += '<div class="col-md-4"><div class="portal-stat"><h3>' + s.totalAppointments + '</h3><div>Appointments</div></div></div>';
        html += '<div class="col-md-4"><div class="portal-stat"><h3>' + s.totalRevenue + '</h3><div>Revenue</div></div></div>';
        html += '<div class="col-md-4"><div class="portal-stat"><h3>' + s.receiptsCount + '</h3><div>Receipts</div></div></div>';
        html += '</div><h4>By service</h4><ul class="list-group">';
        (s.appointmentsByService || []).forEach(function(x) {
            html += '<li class="list-group-item d-flex justify-content-between"><span>' + x.service + '</span><span>' + x.count + '</span></li>';
        });
        html += '</ul>';
        document.getElementById('reportsContent').innerHTML = html;
    });
});

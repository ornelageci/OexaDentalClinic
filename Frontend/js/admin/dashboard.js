document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;

    var statsRow = document.getElementById('statsRow');

    apiGet('/api/Reports/dashboard', { loading: true }).then(function(s) {
        var monthLabel = s.monthLabel || s.MonthLabel || 'This month';
        var revenue = s.monthRevenue != null ? s.monthRevenue : s.MonthRevenue;
        statsRow.innerHTML =
            '<div class="col-md-6 col-lg-4 col-xl-2"><div class="portal-stat"><div class="text-muted small">Total appointments</div><h3>' + (s.totalAppointments ?? 0) + '</h3></div></div>' +
            '<div class="col-md-6 col-lg-4 col-xl-2"><div class="portal-stat"><div class="text-muted small">Completed</div><h3>' + (s.completed ?? 0) + '</h3></div></div>' +
            '<div class="col-md-6 col-lg-4 col-xl-2"><div class="portal-stat"><div class="text-muted small">In progress</div><h3>' + (s.inProgress ?? 0) + '</h3></div></div>' +
            '<div class="col-md-6 col-lg-4 col-xl-2"><div class="portal-stat"><div class="text-muted small">Booked</div><h3>' + (s.booked ?? 0) + '</h3></div></div>' +
            '<div class="col-md-6 col-lg-4 col-xl-2"><div class="portal-stat"><div class="text-muted small">Cancelled</div><h3>' + (s.cancelled ?? 0) + '</h3></div></div>' +
            '<div class="col-md-6 col-lg-4 col-xl-2"><div class="portal-stat"><div class="text-muted small">Revenue (' + monthLabel + ')</div><h3>' + Number(revenue || 0).toFixed(2) + ' EUR</h3><div class="small text-muted">' + (s.receiptCountThisMonth ?? 0) + ' receipts</div></div></div>';
    }).catch(function(err) {
        statsRow.innerHTML = '<div class="col-12"><p class="text-danger">Could not load dashboard stats: ' + (err.message || err) + '</p></div>';
    });
});

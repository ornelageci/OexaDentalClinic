document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    const body = document.getElementById('tableBody');
    let problemNames = {};

    apiGet('/api/Problems/manage').then(function(items) {
        items.forEach(function(p) {
            problemNames[p.key] = p.name;
        });
        load();
    }).catch(function() {
        load();
    });

    function serviceLabel(serviceNeeded) {
        if (!serviceNeeded) return '—';
        if (problemNames[serviceNeeded]) return problemNames[serviceNeeded];
        if (serviceNeeded.indexOf(',') >= 0) {
            return serviceNeeded.split(',').map(function(k) {
                return problemNames[k.trim()] || k.trim();
            }).join(', ');
        }
        return serviceNeeded;
    }

    function load() {
        apiGet('/api/Appointments').then(function(items) {
            body.innerHTML = items.map(function(a) {
                return '<tr><td>' + a.id + '</td><td>' + a.firstName + ' ' + a.lastName + '</td><td>' + formatDateTime(a.preferredDateTime) + '</td><td>' + serviceLabel(a.serviceNeeded) + '</td><td>' + statusBadge(a.status) + '</td><td><button type="button" class="btn btn-sm btn-outline-info" data-info="' + a.id + '">Info</button></td></tr>';
            }).join('');

            body.querySelectorAll('[data-info]').forEach(function(btn) {
                btn.addEventListener('click', function() {
                    showAppointmentInfo(btn.getAttribute('data-info'), 'admin');
                });
            });
        });
    }

    document.getElementById('refreshBtn').addEventListener('click', load);
});

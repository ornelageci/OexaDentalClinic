/** Shared admin navbar — set data-admin-page on <nav id="adminNav"> */
(function() {
    function renderAdminNav() {
        var nav = document.getElementById('adminNav');
        if (!nav) return;

        var page = nav.getAttribute('data-admin-page') || '';

        function link(href, label, key) {
            var active = page === key;
            var cls = 'btn btn-sm admin-nav-link' + (active ? ' admin-nav-link--active' : '');
            var aria = active ? ' aria-current="page"' : '';
            return '<a href="' + href + '" class="' + cls + '"' + aria + '>' + label + '</a>';
        }

        nav.className = 'navbar portal-nav py-3';
        nav.innerHTML =
            '<div class="container-fluid px-3 px-lg-4 d-flex flex-wrap align-items-center justify-content-between gap-2">' +
            '<a href="dashboard.html" class="navbar-brand mb-0 h1 text-decoration-none text-primary">Admin Panel</a>' +
            '<div class="d-flex flex-wrap gap-2 align-items-center admin-nav-links">' +
            link('dashboard.html', 'Dashboard', 'dashboard') +
            link('users.html', 'Users', 'users') +
            link('treatments.html', 'Treatments', 'treatments') +
            link('appointments.html', 'Appointments', 'appointments') +
            link('reports.html', 'Revenue', 'reports') +
            '<button type="button" class="btn btn-outline-secondary btn-sm" id="logoutBtn">Logout</button>' +
            '</div></div>';

        var logout = document.getElementById('logoutBtn');
        if (logout && typeof staffLogout === 'function') {
            logout.addEventListener('click', staffLogout);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', renderAdminNav);
    } else {
        renderAdminNav();
    }
})();

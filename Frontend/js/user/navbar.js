document.addEventListener('DOMContentLoaded', function() {
    const navbar = document.getElementById('navbar');
    if (!navbar) return;

    const currentPage = window.location.pathname.split('/').pop() || 'index.html';
    const user = typeof getSession === 'function' ? getSession() : null;
    const isPatient = user && (user.role || user.Role) === 'Patient';

    function active(file) {
        return currentPage === file ? 'active' : '';
    }

    let menuExtras = '';
    if (isPatient) {
        menuExtras = `
            <li><a class="dropdown-item ${active('my-appointments.html')}" href="my-appointments.html">My Appointments</a></li>
            <li><a class="dropdown-item" href="#" id="logoutPatient">Logout</a></li>
        `;
    } else {
        menuExtras = `
            <li><a class="dropdown-item ${active('login.html')}" href="login.html">Login</a></li>
            <li><a class="dropdown-item ${active('register.html')}" href="register.html">Register</a></li>
        `;
    }
    if (!isPatient) {
        menuExtras += '<li><hr class="dropdown-divider"></li><li><a class="dropdown-item" href="../portal/login.html">Staff Login</a></li>';
    }

    navbar.className = 'navbar navbar-expand-lg fixed-top oexa-navbar';
    navbar.innerHTML = `
        <div class="container-fluid oexa-nav-inner">
            <a class="navbar-brand oexa-brand" href="index.html" aria-label="OEXA Dental Clinic home">
                <img src="../../assets/images/oexaLogo.JPG" alt="OEXA Dental Clinic" class="oexa-logo-img">
            </a>

            <ul class="navbar-nav oexa-nav-pages ms-auto d-none d-lg-flex">
                <li class="nav-item"><a class="nav-link ${active('index.html')}" href="index.html">Home</a></li>
                <li class="nav-item"><a class="nav-link ${active('about.html')}" href="about.html">About Us</a></li>
                <li class="nav-item"><a class="nav-link ${active('services.html')}" href="services.html">Services</a></li>
                <li class="nav-item"><a class="nav-link ${active('staff.html')}" href="staff.html">Our Staff</a></li>
                <li class="nav-item"><a class="nav-link ${active('book-appointment.html')}" href="book-appointment.html">Book Appointment</a></li>
                <li class="nav-item"><a class="nav-link ${active('offers.html')}" href="offers.html">Offers</a></li>
                <li class="nav-item"><a class="nav-link ${active('contact.html')}" href="contact.html">Contact Us</a></li>
            </ul>

            <div class="dropdown oexa-burger-wrap">
                <button class="btn oexa-burger-btn" type="button" data-bs-toggle="dropdown" aria-expanded="false" aria-label="Menu">
                    <span></span><span></span><span></span>
                </button>
                <ul class="dropdown-menu dropdown-menu-end oexa-nav-dropdown">
                    <li><a class="dropdown-item ${active('index.html')}" href="index.html">Home</a></li>
                    <li><a class="dropdown-item ${active('about.html')}" href="about.html">About Us</a></li>
                    <li><a class="dropdown-item ${active('services.html')}" href="services.html">Services</a></li>
                    <li><a class="dropdown-item ${active('staff.html')}" href="staff.html">Our Staff</a></li>
                    <li><a class="dropdown-item ${active('book-appointment.html')}" href="book-appointment.html">Book Appointment</a></li>
                    <li><a class="dropdown-item ${active('offers.html')}" href="offers.html">Offers</a></li>
                    <li><a class="dropdown-item ${active('contact.html')}" href="contact.html">Contact Us</a></li>
                    <li><hr class="dropdown-divider"></li>
                    ${menuExtras}
                </ul>
            </div>
        </div>
    `;

    const logoutBtn = document.getElementById('logoutPatient');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', function(e) {
            e.preventDefault();
            clearSession();
            window.location.href = 'index.html';
        });
    }
});

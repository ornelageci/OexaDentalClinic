document.addEventListener('DOMContentLoaded', function() {
    const navbar = document.getElementById('navbar');
    if (!navbar) return;

    const currentPage = window.location.pathname.split('/').pop() || 'index.html';
    const user = typeof getSession === 'function' ? getSession() : null;
    const isPatient = user && (user.role || user.Role) === 'Patient';

    function active(file) {
        return currentPage === file ? 'active' : '';
    }

    let authLinks = '';
    if (isPatient) {
        authLinks = `
            <li class="nav-item"><a class="nav-link ${active('my-appointments.html')}" href="my-appointments.html">My Appointments</a></li>
            <li class="nav-item"><a class="nav-link" href="#" id="logoutPatient">Logout</a></li>
        `;
    } else {
        authLinks = `
            <li class="nav-item"><a class="nav-link ${active('login.html')}" href="login.html">Login</a></li>
            <li class="nav-item"><a class="nav-link ${active('register.html')}" href="register.html">Register</a></li>
        `;
    }

    navbar.innerHTML = `
        <div class="container">
            <a class="navbar-brand" href="index.html">OEXA Dental Clinic</a>
            <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                <span class="navbar-toggler-icon"></span>
            </button>
            <div class="collapse navbar-collapse" id="navbarNav">
                <ul class="navbar-nav ms-auto">
                    <li class="nav-item"><a class="nav-link ${active('index.html')}" href="index.html">Home</a></li>
                    <li class="nav-item"><a class="nav-link ${active('about.html')}" href="about.html">About Us</a></li>
                    <li class="nav-item"><a class="nav-link ${active('services.html')}" href="services.html">Services</a></li>
                    <li class="nav-item"><a class="nav-link ${active('staff.html')}" href="staff.html">Our Staff</a></li>
                    <li class="nav-item"><a class="nav-link ${active('book-appointment.html')}" href="book-appointment.html">Book Appointment</a></li>
                    <li class="nav-item"><a class="nav-link ${active('offers.html')}" href="offers.html">Offers</a></li>
                    <li class="nav-item"><a class="nav-link ${active('contact.html')}" href="contact.html">Contact Us</a></li>
                    ${authLinks}
                    <li class="nav-item"><a class="nav-link" href="../portal/login.html">Staff Login</a></li>
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

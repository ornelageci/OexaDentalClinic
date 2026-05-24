document.addEventListener('DOMContentLoaded', function() {
    const footer = document.getElementById('footer');
    if (!footer) return;

    const user = typeof getSession === 'function' ? getSession() : null;
    const isPatient = user && (user.role === 'Patient' || user.Role === 'Patient');
    const staffLink = isPatient ? '' : '<li><a href="../portal/login.html">Staff Login</a></li>';

    footer.innerHTML = `
        <div class="container">
            <div class="row">
                <div class="col-md-4 mb-3">
                    <h5>Oexa Dental Clinic</h5>
                    <p class="footer-text">Your trusted partner in dental health. We are committed to exceptional, gentle care for the whole family.</p>
                </div>
                <div class="col-md-4 mb-3">
                    <h5>Quick Links</h5>
                    <ul class="list-unstyled footer-links">
                        <li><a href="index.html">Home</a></li>
                        <li><a href="about.html">About Us</a></li>
                        <li><a href="services.html">Services</a></li>
                        <li><a href="staff.html">Our Staff</a></li>
                        <li><a href="book-appointment.html">Book Appointment</a></li>
                        <li><a href="offers.html">Offers</a></li>
                        <li><a href="login.html">Login</a></li>
                        <li><a href="register.html">Register</a></li>
                        <li><a href="contact.html">Contact Us</a></li>
                        ${staffLink}
                    </ul>
                </div>
                <div class="col-md-4 mb-3">
                    <h5>Contact Info</h5>
                    <p class="footer-text mb-1">📍 Tirane Delijorgji</p>
                    <p class="footer-text mb-1">📱 <a href="https://wa.me/355696851089" target="_blank" rel="noopener">+355 69 685 1089</a></p>
                    <p class="footer-text mb-1">📧 <a href="mailto:info@oexadentalclinic.com">info@oexadentalclinic.com</a></p>
                    <p class="footer-text mb-0">📸 @oexadentalclinic</p>
                </div>
            </div>
            <hr class="my-4 footer-divider">
            <div class="text-center">
                <p class="mb-0 small">&copy; ${new Date().getFullYear()} Oexa Dental Clinic. All rights reserved.</p>
            </div>
        </div>
    `;
});

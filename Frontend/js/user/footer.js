document.addEventListener('DOMContentLoaded', function() {
    const footer = document.getElementById('footer');
    if (!footer) return;

    footer.innerHTML = `
        <div class="container">
            <div class="row">
                <div class="col-md-4 mb-3">
                    <h5>Oexa Dental Clinic</h5>
                    <p>Your trusted partner in dental health and wellness. We are committed to providing exceptional dental care.</p>
                </div>
                <div class="col-md-4 mb-3">
                    <h5>Quick Links</h5>
                    <ul class="list-unstyled">
                        <li><a href="index.html">Home</a></li>
                        <li><a href="about.html">About Us</a></li>
                        <li><a href="services.html">Services</a></li>
                        <li><a href="staff.html">Our Staff</a></li>
                        <li><a href="book-appointment.html">Book Appointment</a></li>
                        <li><a href="offers.html">Offers</a></li>
                        <li><a href="login.html">Login</a></li>
                        <li><a href="register.html">Register</a></li>
                        <li><a href="contact.html">Contact Us</a></li>
                        <li><a href="../portal/login.html">Staff Login</a></li>
                    </ul>
                </div>
                <div class="col-md-4 mb-3">
                    <h5>Contact Info</h5>
                    <p> Tirane Delijorgji<br> WhatsApp: +355 69 68 67 665<br> Instagram: @oexadentalclinic<br> Email: info@oexadental.com</p>
                </div>
            </div>
            <hr class="my-4">
            <div class="text-center">
                <p>&copy; ${new Date().getFullYear()} Oexa Dental Clinic. All rights reserved.</p>
            </div>
        </div>
    `;
});

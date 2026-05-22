document.addEventListener('DOMContentLoaded', function() {
    const list = document.getElementById('offersList');
    if (!list) return;

    apiGet('/api/Promotions/active')
        .then(function(items) {
            if (!items.length) {
                list.innerHTML = '<div class="col-12"><div class="empty-offers fade-in-up"><i class="bi bi-gift"></i><p>No active offers right now. Check back soon!</p></div></div>';
                return;
            }
            list.innerHTML = items.map(function(p, i) {
                var hasPrice = p.basePrice != null && p.priceAfterDiscount != null;
                var priceBlock = hasPrice
                    ? '<div class="offer-prices">' +
                        '<span class="offer-was">' + p.basePrice + ' EUR</span>' +
                        '<span class="offer-now">' + p.priceAfterDiscount + ' EUR</span>' +
                      '</div>'
                    : '';

                return (
                    '<div class="col-md-6 col-lg-4 mb-4">' +
                        '<article class="offer-card fade-in-up" style="animation-delay:' + (i * 0.08) + 's">' +
                            '<div class="offer-badge">-' + p.discountPercent + '%</div>' +
                            '<h3>' + p.title + '</h3>' +
                            '<p class="offer-desc">' + (p.description || '') + '</p>' +
                            (p.treatmentName ? '<p class="offer-treatment"><i class="bi bi-heart-pulse"></i> ' + p.treatmentName + '</p>' : '') +
                            priceBlock +
                            '<p class="offer-until"><i class="bi bi-calendar-event"></i> Valid until ' + new Date(p.endDate).toLocaleDateString() + '</p>' +
                            '<a href="book-appointment.html" class="btn btn-primary btn-sm mt-2">Book with offer</a>' +
                        '</article>' +
                    '</div>'
                );
            }).join('');
        })
        .catch(function() {
            list.innerHTML = '<div class="col-12 text-center text-danger">Could not load offers. Is the API running?</div>';
        });
});

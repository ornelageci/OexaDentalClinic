document.addEventListener('DOMContentLoaded', function() {
    const list = document.getElementById('offersList');
    if (!list) return;

    function pick(obj, a, b) {
        return obj[a] !== undefined && obj[a] !== null ? obj[a] : obj[b];
    }

    function renderOffers(items) {
        if (!items.length) {
            list.innerHTML = '<div class="col-12"><div class="empty-offers is-visible"><i class="bi bi-gift"></i><p>No active offers right now. Check back soon!</p></div></div>';
            return;
        }

        list.innerHTML = items.map(function(p, i) {
            var title = pick(p, 'title', 'Title');
            var desc = pick(p, 'description', 'Description') || '';
            var discount = pick(p, 'discountPercent', 'DiscountPercent');
            var endDate = pick(p, 'endDate', 'EndDate');
            var treatmentName = pick(p, 'treatmentName', 'TreatmentName');
            var basePrice = pick(p, 'basePrice', 'BasePrice');
            var priceAfter = pick(p, 'priceAfterDiscount', 'PriceAfterDiscount');

            var hasPrice = basePrice != null && priceAfter != null;
            var priceBlock = hasPrice
                ? '<div class="offer-prices"><span class="offer-was">' + basePrice + ' EUR</span><span class="offer-now">' + priceAfter + ' EUR</span></div>'
                : '';

            return (
                '<div class="col-md-6 col-lg-4 mb-4">' +
                    '<article class="offer-card is-visible">' +
                        '<div class="offer-badge">-' + discount + '%</div>' +
                        '<h3>' + title + '</h3>' +
                        '<p class="offer-desc">' + desc + '</p>' +
                        (treatmentName ? '<p class="offer-treatment"><i class="bi bi-heart-pulse"></i> ' + treatmentName + '</p>' : '') +
                        priceBlock +
                        '<p class="offer-until"><i class="bi bi-calendar-event"></i> Valid until ' + new Date(endDate).toLocaleDateString() + '</p>' +
                        '<a href="book-appointment.html" class="btn btn-primary btn-sm mt-2">Book with offer</a>' +
                    '</article>' +
                '</div>'
            );
        }).join('');
    }

    function loadFromProblems() {
        return apiGet('/api/Problems').then(function(problems) {
            var promos = problems.filter(function(p) {
                return pick(p, 'hasPromotion', 'HasPromotion');
            }).map(function(p) {
                return {
                    title: pick(p, 'promotionTitle', 'PromotionTitle') || (pick(p, 'name', 'Name') + ' offer'),
                    description: 'Book this treatment and save automatically at checkout.',
                    discountPercent: pick(p, 'discountPercent', 'DiscountPercent'),
                    endDate: new Date(Date.now() + 60 * 24 * 60 * 60 * 1000).toISOString(),
                    treatmentName: pick(p, 'name', 'Name'),
                    basePrice: pick(p, 'basePrice', 'BasePrice'),
                    priceAfterDiscount: pick(p, 'priceAfterDiscount', 'PriceAfterDiscount')
                };
            });
            renderOffers(promos);
        });
    }

    apiGet('/api/Promotions/active')
        .then(function(items) {
            if (items && items.length) {
                renderOffers(items);
            } else {
                return loadFromProblems();
            }
        })
        .catch(function() {
            loadFromProblems().catch(function() {
                list.innerHTML = '<div class="col-12 text-center text-danger is-visible">Could not load offers. Start the API with <code>dotnet run</code> in Backend/OexaDentalClinic.Api.</div>';
            });
        });
});

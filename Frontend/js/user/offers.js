document.addEventListener('DOMContentLoaded', function() {
    const list = document.getElementById('offersList');
    if (!list) return;

    apiGet('/api/Promotions/active')
        .then(function(items) {
            if (!items.length) {
                list.innerHTML = '<div class="col-12 text-center text-muted">No active offers right now.</div>';
                return;
            }
            list.innerHTML = items.map(function(p) {
                return (
                    '<div class="col-md-6 mb-4">' +
                        '<div class="card h-100">' +
                            '<div class="card-body">' +
                                '<h5 class="card-title">' + p.title + '</h5>' +
                                '<p class="text-muted">' + (p.description || '') + '</p>' +
                                '<p><strong>' + p.discountPercent + '% off</strong></p>' +
                                '<p class="small mb-0">Until: ' + new Date(p.endDate).toLocaleDateString() + '</p>' +
                            '</div>' +
                        '</div>' +
                    '</div>'
                );
            }).join('');
        })
        .catch(function() {
            list.innerHTML = '<div class="col-12 text-center text-danger">Could not load offers.</div>';
        });
});

document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('marketer')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    const list = document.getElementById('promoList');
    const problemSelect = document.getElementById('problemKey');
    const preview = document.getElementById('treatmentPreview');
    let catalog = [];

    function pick(obj, a, b) {
        return obj[a] !== undefined && obj[a] !== null ? obj[a] : obj[b];
    }

    function loadCatalog() {
        return apiGet('/api/Problems/catalog').then(function(items) {
            catalog = items;
            problemSelect.innerHTML = '<option value="">— Select treatment —</option>' +
                items.map(function(p) {
                    var key = pick(p, 'key', 'Key');
                    var name = pick(p, 'name', 'Name');
                    var price = pick(p, 'basePrice', 'BasePrice');
                    return '<option value="' + key + '">' + name + ' (' + price + ' EUR)</option>';
                }).join('');
        }).catch(function() {
            problemSelect.innerHTML = '<option value="">Could not load treatments</option>';
        });
    }

    function updatePreview() {
        var key = problemSelect.value;
        var item = catalog.find(function(p) { return pick(p, 'key', 'Key') === key; });
        if (!item) {
            preview.textContent = '';
            return;
        }
        var mins = pick(item, 'durationMinutes', 'DurationMinutes');
        var price = pick(item, 'basePrice', 'BasePrice');
        var cat = pick(item, 'dentistCategoryKey', 'DentistCategoryKey');
        preview.innerHTML = '<strong>Base price:</strong> ' + price + ' EUR · <strong>Duration:</strong> ' + mins + ' min · <strong>Category:</strong> ' + cat;
    }

    problemSelect.addEventListener('change', updatePreview);

    function loadPromos() {
        apiGet('/api/Promotions').then(function(items) {
            if (!items.length) {
                list.innerHTML = '<li class="list-group-item text-muted">No promotions yet.</li>';
                return;
            }
            list.innerHTML = items.map(function(p) {
                var title = pick(p, 'title', 'Title');
                var discount = pick(p, 'discountPercent', 'DiscountPercent');
                var treatment = pick(p, 'treatmentName', 'TreatmentName') || pick(p, 'problemKey', 'ProblemKey') || '—';
                var start = new Date(pick(p, 'startDate', 'StartDate')).toLocaleDateString();
                var end = new Date(pick(p, 'endDate', 'EndDate')).toLocaleDateString();
                var audience = pick(p, 'targetAudience', 'TargetAudience') || '';
                return '<li class="list-group-item d-flex justify-content-between align-items-start gap-2">' +
                    '<div><strong>' + title + '</strong> <span class="badge bg-success">-' + discount + '%</span><br>' +
                    '<span class="small text-primary">Treatment: ' + treatment + '</span><br>' +
                    '<small class="text-muted">' + start + ' → ' + end + (audience ? ' · ' + audience : '') + '</small></div>' +
                    '<button class="btn btn-sm btn-outline-danger flex-shrink-0" data-del="' + pick(p, 'id', 'Id') + '">Delete</button></li>';
            }).join('');
            list.querySelectorAll('[data-del]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    if (!confirm('Delete this promotion?')) return;
                    try {
                        await apiDelete('/api/Promotions/' + btn.getAttribute('data-del'));
                        loadPromos();
                    } catch (e) {
                        alert(e.message || 'Delete failed');
                    }
                });
            });
        });
    }

    document.getElementById('addPromo').addEventListener('click', async function() {
        var problemKey = problemSelect.value;
        if (!problemKey) {
            alert('Please select a treatment for this promotion.');
            return;
        }
        var discount = Number(document.getElementById('discount').value);
        if (!discount || discount < 1 || discount > 90) {
            alert('Enter a discount between 1 and 90 percent.');
            return;
        }
        var startDate = document.getElementById('startDate').value;
        var endDate = document.getElementById('endDate').value;
        if (!startDate || !endDate) {
            alert('Please set start and end dates.');
            return;
        }
        if (endDate < startDate) {
            alert('End date must be on or after start date.');
            return;
        }

        try {
            await apiPost('/api/Promotions', {
                title: document.getElementById('title').value.trim(),
                description: document.getElementById('description').value.trim(),
                discountPercent: discount,
                startDate: startDate,
                endDate: endDate,
                targetAudience: document.getElementById('audience').value.trim() || 'All patients',
                problemKey: problemKey
            });
            alert('Promotion created. Patients will see the discount on this treatment when booking.');
            document.getElementById('title').value = '';
            document.getElementById('description').value = '';
            document.getElementById('discount').value = '';
            loadPromos();
        } catch (e) {
            alert(e.message || 'Could not create promotion');
        }
    });

    var today = new Date().toISOString().split('T')[0];
    var in30 = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
    document.getElementById('startDate').value = today;
    document.getElementById('endDate').value = in30;
    document.getElementById('startDate').min = today;
    document.getElementById('endDate').min = today;

    loadCatalog().then(loadPromos);
});

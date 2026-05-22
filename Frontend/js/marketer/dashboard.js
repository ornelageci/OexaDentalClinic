document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('marketer')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);
    const list = document.getElementById('promoList');

    function load() {
        apiGet('/api/Promotions').then(function(items) {
            list.innerHTML = items.map(function(p) {
                return '<li class="list-group-item d-flex justify-content-between align-items-center">' +
                    '<div><strong>' + p.title + '</strong> (' + p.discountPercent + '%)<br><small>' + p.targetAudience + '</small></div>' +
                    '<button class="btn btn-sm btn-outline-danger" data-del="' + p.id + '">Delete</button></li>';
            }).join('');
            list.querySelectorAll('[data-del]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    await fetch(API_BASE_URL + '/api/Promotions/' + btn.getAttribute('data-del'), { method: 'DELETE' });
                    load();
                });
            });
        });
    }

    document.getElementById('addPromo').addEventListener('click', async function() {
        await apiPost('/api/Promotions', {
            title: document.getElementById('title').value,
            description: document.getElementById('description').value,
            discountPercent: Number(document.getElementById('discount').value),
            startDate: document.getElementById('startDate').value,
            endDate: document.getElementById('endDate').value,
            targetAudience: document.getElementById('audience').value
        });
        load();
    });

    load();
});

document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    const body = document.getElementById('treatmentsBody');
    const editId = document.getElementById('editId');
    const tKey = document.getElementById('tKey');
    const formTitle = document.getElementById('formTitle');
    const cancelBtn = document.getElementById('cancelEdit');

    function pick(obj, a, b) {
        return obj[a] !== undefined && obj[a] !== null ? obj[a] : obj[b];
    }

    var categories = [];

    function loadCategories() {
        return apiGet('/api/DentistCategories').then(function(items) {
            categories = items;
            var sel = document.getElementById('tCategory');
            sel.innerHTML = items.map(function(c) {
                var key = pick(c, 'key', 'Key');
                var name = pick(c, 'displayName', 'DisplayName');
                return '<option value="' + key + '">' + name + '</option>';
            }).join('');
        });
    }

    function resetForm() {
        editId.value = '';
        tKey.value = '';
        tKey.disabled = false;
        document.getElementById('tName').value = '';
        document.getElementById('tDesc').value = '';
        document.getElementById('tPrice').value = '';
        document.getElementById('tDuration').value = '60';
        if (categories.length) {
            document.getElementById('tCategory').value = pick(categories[0], 'key', 'Key');
        }
        formTitle.textContent = 'Add new treatment';
        cancelBtn.classList.add('d-none');
    }

    function load() {
        apiGet('/api/Problems/manage').then(function(items) {
            body.innerHTML = items.map(function(p) {
                var id = pick(p, 'id', 'Id');
                return '<tr>' +
                    '<td><strong>' + pick(p, 'name', 'Name') + '</strong></td>' +
                    '<td><code>' + pick(p, 'key', 'Key') + '</code></td>' +
                    '<td>' + pick(p, 'basePrice', 'BasePrice') + ' EUR</td>' +
                    '<td>' + pick(p, 'durationMinutes', 'DurationMinutes') + ' min</td>' +
                    '<td>' + pick(p, 'dentistCategoryKey', 'DentistCategoryKey') + '</td>' +
                    '<td class="text-nowrap">' +
                        '<button class="btn btn-sm btn-outline-primary me-1" data-edit="' + id + '">Edit</button>' +
                        '<button class="btn btn-sm btn-outline-danger" data-del="' + id + '">Delete</button>' +
                    '</td></tr>';
            }).join('');

            body.querySelectorAll('[data-edit]').forEach(function(btn) {
                btn.addEventListener('click', function() {
                    var id = Number(btn.getAttribute('data-edit'));
                    var p = items.find(function(x) { return pick(x, 'id', 'Id') === id; });
                    if (!p) return;
                    editId.value = id;
                    tKey.value = pick(p, 'key', 'Key');
                    tKey.disabled = true;
                    document.getElementById('tName').value = pick(p, 'name', 'Name');
                    document.getElementById('tDesc').value = pick(p, 'description', 'Description') || '';
                    document.getElementById('tPrice').value = pick(p, 'basePrice', 'BasePrice');
                    document.getElementById('tDuration').value = pick(p, 'durationMinutes', 'DurationMinutes');
                    document.getElementById('tCategory').value = pick(p, 'dentistCategoryKey', 'DentistCategoryKey');
                    formTitle.textContent = 'Edit treatment';
                    cancelBtn.classList.remove('d-none');
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                });
            });

            body.querySelectorAll('[data-del]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    if (!confirm('Delete this treatment?')) return;
                    try {
                        await apiDelete('/api/Problems/' + btn.getAttribute('data-del'));
                        load();
                        if (editId.value === btn.getAttribute('data-del')) resetForm();
                    } catch (e) {
                        alert(e.message || 'Delete failed');
                    }
                });
            });
        });
    }

    cancelBtn.addEventListener('click', resetForm);

    document.getElementById('saveBtn').addEventListener('click', async function() {
        var name = document.getElementById('tName').value.trim();
        var price = Number(document.getElementById('tPrice').value);
        var duration = Number(document.getElementById('tDuration').value);
        var category = document.getElementById('tCategory').value;
        var desc = document.getElementById('tDesc').value.trim();

        if (!name || !price || !duration) {
            alert('Name, price, and duration are required.');
            return;
        }

        try {
            if (editId.value) {
                await apiPut('/api/Problems/' + editId.value, {
                    name: name,
                    description: desc || null,
                    basePrice: price,
                    durationMinutes: duration,
                    dentistCategoryKey: category
                });
                alert('Treatment updated.');
            } else {
                var key = tKey.value.trim();
                if (!key) {
                    alert('Treatment key is required (e.g. teeth-whitening).');
                    return;
                }
                await apiPost('/api/Problems', {
                    key: key,
                    name: name,
                    description: desc || null,
                    basePrice: price,
                    durationMinutes: duration,
                    dentistCategoryKey: category
                });
                alert('Treatment created.');
            }
            resetForm();
            load();
        } catch (e) {
            alert(e.message || 'Save failed');
        }
    });

    loadCategories().then(function() {
        resetForm();
        load();
    });
});

document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;
    document.getElementById('logoutBtn').addEventListener('click', staffLogout);

    const body = document.getElementById('usersBody');
    const newRole = document.getElementById('newRole');
    const roleFilter = document.getElementById('roleFilter');
    const newDentistKey = document.getElementById('newDentistKey');
    const dentistTypeWrap = document.getElementById('dentistTypeWrap');

    let roles = [];
    let categories = [];

    function pick(obj, a, b) {
        if (!obj) return undefined;
        if (obj[a] !== undefined && obj[a] !== null) return obj[a];
        return obj[b];
    }

    function categoryLabel(key) {
        if (!key) return '—';
        var c = categories.find(function(x) { return pick(x, 'key', 'Key') === key; });
        return c ? pick(c, 'displayName', 'DisplayName') : key;
    }

    function fillSelect(select, items, valueKey, labelKey, placeholder) {
        select.innerHTML = '<option value="">' + (placeholder || 'Select...') + '</option>' +
            items.map(function(item) {
                var val = pick(item, valueKey, valueKey.charAt(0).toUpperCase() + valueKey.slice(1));
                var label = pick(item, labelKey, labelKey.charAt(0).toUpperCase() + labelKey.slice(1));
                return '<option value="' + val + '">' + label + '</option>';
            }).join('');
    }

    function toggleDentistType() {
        var isDentist = newRole.value === 'Dentist';
        dentistTypeWrap.style.display = isDentist ? '' : 'none';
        newDentistKey.required = isDentist;
    }

    async function loadRoles() {
        roles = await apiGet('/api/UserRoles');
        fillSelect(newRole, roles, 'key', 'displayName', 'Select role');
        var filterHtml = '<option value="">All roles</option>' +
            roles.map(function(r) {
                return '<option value="' + pick(r, 'key', 'Key') + '">' + pick(r, 'displayName', 'DisplayName') + '</option>';
            }).join('');
        roleFilter.innerHTML = filterHtml;
        renderRolesList();
        toggleDentistType();
    }

    async function loadCategories() {
        categories = await apiGet('/api/DentistCategories');
        fillSelect(newDentistKey, categories, 'key', 'displayName', 'Select dentist type');
        renderCategoriesList();
    }

    function renderRolesList() {
        var list = document.getElementById('rolesList');
        list.innerHTML = roles.map(function(r) {
            var id = pick(r, 'id', 'Id');
            var key = pick(r, 'key', 'Key');
            var name = pick(r, 'displayName', 'DisplayName');
            var system = pick(r, 'isSystem', 'IsSystem');
            return '<li class="list-group-item d-flex justify-content-between align-items-center px-0">' +
                '<span><strong>' + name + '</strong> <code class="small">' + key + '</code></span>' +
                (system ? '<span class="badge bg-secondary">System</span>' :
                    '<button type="button" class="btn btn-sm btn-outline-danger" data-del-role="' + id + '">Remove</button>') +
                '</li>';
        }).join('');
        list.querySelectorAll('[data-del-role]').forEach(function(btn) {
            btn.addEventListener('click', async function() {
                if (!confirm('Remove this role?')) return;
                try {
                    await apiDelete('/api/UserRoles/' + btn.getAttribute('data-del-role'));
                    await loadRoles();
                } catch (e) {
                    alert(e.message || 'Could not remove role');
                }
            });
        });
    }

    function renderCategoriesList() {
        var list = document.getElementById('categoriesList');
        list.innerHTML = categories.map(function(c) {
            var id = pick(c, 'id', 'Id');
            var key = pick(c, 'key', 'Key');
            var name = pick(c, 'displayName', 'DisplayName');
            return '<li class="list-group-item d-flex justify-content-between align-items-center px-0">' +
                '<span><strong>' + name + '</strong> <code class="small">' + key + '</code></span>' +
                '<button type="button" class="btn btn-sm btn-outline-danger" data-del-cat="' + id + '">Remove</button>' +
                '</li>';
        }).join('');
        list.querySelectorAll('[data-del-cat]').forEach(function(btn) {
            btn.addEventListener('click', async function() {
                if (!confirm('Remove this dentist type?')) return;
                try {
                    await apiDelete('/api/DentistCategories/' + btn.getAttribute('data-del-cat'));
                    await loadCategories();
                } catch (e) {
                    alert(e.message || 'Could not remove type');
                }
            });
        });
    }

    function load() {
        const role = roleFilter.value;
        const url = role ? '/api/Users?role=' + encodeURIComponent(role) : '/api/Users';
        apiGet(url).then(function(users) {
            body.innerHTML = users.map(function(u) {
                var typeCol = u.role === 'Dentist' ? categoryLabel(u.dentistServiceKey) : '—';
                return '<tr><td>' + u.id + '</td><td>' + u.firstName + ' ' + u.lastName + '</td><td>' + u.email + '</td><td>' + u.role + '</td><td>' + typeCol + '</td><td><button class="btn btn-sm btn-outline-danger" data-del="' + u.id + '">Delete</button></td></tr>';
            }).join('');
            body.querySelectorAll('[data-del]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    if (!confirm('Delete user?')) return;
                    try {
                        await apiDelete('/api/Users/' + btn.getAttribute('data-del'));
                        load();
                    } catch (e) {
                        alert(e.message || 'Delete failed');
                    }
                });
            });
        });
    }

    newRole.addEventListener('change', toggleDentistType);
    roleFilter.addEventListener('change', load);

    document.getElementById('addRoleBtn').addEventListener('click', async function() {
        var name = document.getElementById('newRoleName').value.trim();
        if (!name) return;
        try {
            await apiPost('/api/UserRoles', { displayName: name });
            document.getElementById('newRoleName').value = '';
            await loadRoles();
        } catch (e) {
            alert(e.message || 'Could not add role');
        }
    });

    document.getElementById('addCategoryBtn').addEventListener('click', async function() {
        var name = document.getElementById('newCategoryName').value.trim();
        if (!name) return;
        try {
            await apiPost('/api/DentistCategories', { displayName: name });
            document.getElementById('newCategoryName').value = '';
            await loadCategories();
        } catch (e) {
            alert(e.message || 'Could not add dentist type');
        }
    });

    document.getElementById('addUserBtn').addEventListener('click', async function() {
        try {
            var payload = {
                firstName: document.getElementById('newFirst').value.trim(),
                lastName: document.getElementById('newLast').value.trim(),
                email: document.getElementById('newEmail').value.trim(),
                password: document.getElementById('newPass').value,
                role: newRole.value,
                dentistServiceKey: newRole.value === 'Dentist' ? newDentistKey.value : null
            };
            if (!payload.firstName || !payload.lastName || !payload.email || !payload.password || !payload.role) {
                alert('Fill in all required fields.');
                return;
            }
            if (payload.role === 'Dentist' && !payload.dentistServiceKey) {
                alert('Select a dentist type.');
                return;
            }
            await apiPost('/api/Users', payload);
            alert('User created');
            load();
        } catch (e) {
            alert(e.message || 'Could not create user');
        }
    });

    loadRoles().then(loadCategories).then(load);
});

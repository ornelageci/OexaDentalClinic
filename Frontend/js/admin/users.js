document.addEventListener('DOMContentLoaded', function() {
    if (!requireStaffRole('admin')) return;

    const body = document.getElementById('usersBody');
    const newRole = document.getElementById('newRole');
    const roleFilter = document.getElementById('roleFilter');
    const newDentistKey = document.getElementById('newDentistKey');
    const dentistTypeWrap = document.getElementById('dentistTypeWrap');

    var DEFAULT_ROLES = [
        { key: 'Patient', displayName: 'Patient', isSystem: true },
        { key: 'Dentist', displayName: 'Dentist', isSystem: true },
        { key: 'Manager', displayName: 'Manager', isSystem: true },
        { key: 'Marketer', displayName: 'Marketer', isSystem: true },
        { key: 'Admin', displayName: 'Admin', isSystem: true }
    ];

    var DEFAULT_CATEGORIES = [
        { key: 'general', displayName: 'General dentistry' },
        { key: 'orthodontics', displayName: 'Orthodontics' },
        { key: 'cosmetic', displayName: 'Cosmetic dentistry' },
        { key: 'pediatric', displayName: 'Pediatric dentistry' },
        { key: 'oral-surgery', displayName: 'Oral surgery' }
    ];

    let roles = [];
    let categories = [];

    function pick(obj, a, b) {
        if (!obj) return undefined;
        if (obj[a] !== undefined && obj[a] !== null) return obj[a];
        return obj[b];
    }

    function userField(u, camel, pascal) {
        return pick(u, camel, pascal);
    }

    function categoryLabel(key) {
        if (!key) return '—';
        var c = categories.find(function(x) { return pick(x, 'key', 'Key') === key; });
        return c ? pick(c, 'displayName', 'DisplayName') : key;
    }

    function fillSelect(select, items, valueKey, labelKey, placeholder, allowEmpty) {
        if (!items || !items.length) {
            select.innerHTML = '<option value="">' + (placeholder || 'No options') + '</option>';
            return;
        }
        var html = '';
        if (allowEmpty !== false) {
            html += '<option value="">' + (placeholder || 'Select...') + '</option>';
        }
        html += items.map(function(item) {
            var val = pick(item, valueKey, valueKey.charAt(0).toUpperCase() + valueKey.slice(1));
            var label = pick(item, labelKey, labelKey.charAt(0).toUpperCase() + labelKey.slice(1));
            return '<option value="' + val + '">' + label + '</option>';
        }).join('');
        select.innerHTML = html;
    }

    function toggleDentistType() {
        var isDentist = newRole.value === 'Dentist';
        dentistTypeWrap.style.display = isDentist ? '' : 'none';
        newDentistKey.required = isDentist;
    }

    async function loadRoles() {
        var listEl = document.getElementById('rolesList');
        try {
            roles = await apiGet('/api/UserRoles');
        } catch (e) {
            roles = DEFAULT_ROLES.slice();
            if (listEl) {
                listEl.innerHTML = '<li class="list-group-item text-warning small px-0">Could not load roles from server (' +
                    (e.message || 'error') + '). Showing defaults — restart the API after migrations.</li>';
            }
        }

        fillSelect(newRole, roles, 'key', 'displayName', 'Select role');
        roleFilter.innerHTML = '<option value="">All roles</option>' +
            roles.map(function(r) {
                return '<option value="' + pick(r, 'key', 'Key') + '">' + pick(r, 'displayName', 'DisplayName') + '</option>';
            }).join('');
        renderRolesList();
        toggleDentistType();
    }

    async function loadCategories() {
        var listEl = document.getElementById('categoriesList');
        try {
            categories = await apiGet('/api/DentistCategories');
        } catch (e) {
            categories = DEFAULT_CATEGORIES.slice();
            if (listEl) {
                listEl.innerHTML = '<li class="list-group-item text-warning small px-0">Could not load dentist types (' +
                    (e.message || 'error') + '). Showing defaults.</li>';
            }
        }

        fillSelect(newDentistKey, categories, 'key', 'displayName', 'Select dentist type');
        renderCategoriesList();
    }

    function renderRolesList() {
        var list = document.getElementById('rolesList');
        if (!list) return;
        if (!roles.length) {
            list.innerHTML = '<li class="list-group-item text-muted small px-0">No roles yet.</li>';
            return;
        }
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
        if (!list) return;
        if (!categories.length) {
            list.innerHTML = '<li class="list-group-item text-muted small px-0">No dentist types yet.</li>';
            return;
        }
        list.innerHTML = categories.map(function(c) {
            var id = pick(c, 'id', 'Id');
            var key = pick(c, 'key', 'Key');
            var name = pick(c, 'displayName', 'DisplayName');
            return '<li class="list-group-item d-flex justify-content-between align-items-center px-0">' +
                '<span><strong>' + name + '</strong> <code class="small">' + key + '</code></span>' +
                (id ? '<button type="button" class="btn btn-sm btn-outline-danger" data-del-cat="' + id + '">Remove</button>' : '') +
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

    function loadUsers() {
        var role = roleFilter.value;
        var url = role ? '/api/Users?role=' + encodeURIComponent(role) : '/api/Users';

        body.innerHTML = '<tr><td colspan="6" class="text-muted">Loading users...</td></tr>';

        apiGet(url).then(function(users) {
            if (!Array.isArray(users)) {
                body.innerHTML = '<tr><td colspan="6" class="text-danger">Unexpected response from server.</td></tr>';
                return;
            }
            if (!users.length) {
                body.innerHTML = '<tr><td colspan="6" class="text-muted">No users found' +
                    (role ? ' for role “' + role + '”' : '') + '.</td></tr>';
                return;
            }
            body.innerHTML = users.map(function(u) {
                var id = userField(u, 'id', 'Id');
                var fn = userField(u, 'firstName', 'FirstName');
                var ln = userField(u, 'lastName', 'LastName');
                var email = userField(u, 'email', 'Email');
                var roleName = userField(u, 'role', 'Role');
                var dKey = userField(u, 'dentistServiceKey', 'DentistServiceKey');
                var typeCol = roleName === 'Dentist' ? categoryLabel(dKey) : '—';
                return '<tr><td>' + id + '</td><td>' + fn + ' ' + ln + '</td><td>' + email + '</td><td>' + roleName + '</td><td>' + typeCol + '</td>' +
                    '<td><button class="btn btn-sm btn-outline-danger" data-del="' + id + '">Delete</button></td></tr>';
            }).join('');

            body.querySelectorAll('[data-del]').forEach(function(btn) {
                btn.addEventListener('click', async function() {
                    if (!confirm('Delete user?')) return;
                    try {
                        await apiDelete('/api/Users/' + btn.getAttribute('data-del'));
                        loadUsers();
                    } catch (e) {
                        alert(e.message || 'Delete failed');
                    }
                });
            });
        }).catch(function(e) {
            body.innerHTML = '<tr><td colspan="6" class="text-danger">Failed to load users: ' + (e.message || e) +
                '. Is the API running at ' + (typeof API_BASE_URL !== 'undefined' ? API_BASE_URL : '') + '?</td></tr>';
        });
    }

    newRole.addEventListener('change', toggleDentistType);
    roleFilter.addEventListener('change', loadUsers);

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
            loadUsers();
        } catch (e) {
            alert(e.message || 'Could not create user');
        }
    });

    (async function init() {
        await Promise.allSettled([loadRoles(), loadCategories()]);
        loadUsers();
    })();
});

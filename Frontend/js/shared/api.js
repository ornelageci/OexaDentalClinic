function resolveApiUrl() {
    if (typeof window !== 'undefined' && window.OEXA_API_URL) return window.OEXA_API_URL;
    if (typeof document !== 'undefined') {
        const meta = document.querySelector('meta[name="oexa-api-url"]');
        if (meta && meta.getAttribute('content')) return meta.getAttribute('content');
    }
    return localStorage.getItem('oexa_api_url') || 'http://localhost:5095';
}

const API_BASE_URL = resolveApiUrl();

function getSession() {
    const raw = localStorage.getItem('oexa_user');
    if (!raw) return null;
    try { return JSON.parse(raw); } catch { return null; }
}

function setSession(user) {
    localStorage.setItem('oexa_user', JSON.stringify(user));
    localStorage.setItem('oexa_role', (user.role || user.Role || '').toLowerCase());
}

function clearSession() {
    localStorage.removeItem('oexa_user');
    localStorage.removeItem('oexa_role');
}

function requirePatient() {
    const user = getSession();
    if (!user || (user.role || user.Role) !== 'Patient') {
        window.location.href = 'login.html';
        return null;
    }
    return user;
}

async function apiGet(path) {
    const res = await fetch(API_BASE_URL + path);
    if (!res.ok) {
        const text = await res.text();
        try {
            const j = JSON.parse(text);
            throw new Error(j.error || j.detail || j.title || text);
        } catch (e) {
            if (e instanceof SyntaxError) throw new Error(text || res.statusText);
            throw e;
        }
    }
    return res.json();
}

async function apiPost(path, body) {
    const res = await fetch(API_BASE_URL + path, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
}

async function apiPatch(path, body) {
    const res = await fetch(API_BASE_URL + path, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
}

async function apiPut(path, body) {
    const res = await fetch(API_BASE_URL + path, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
}

function formatDateTime(value) {
    if (!value) return '';
    const d = new Date(value);
    if (Number.isNaN(d.getTime())) return value;
    return d.toLocaleString();
}

function statusBadge(status) {
    const map = {
        Booked: 'primary',
        InProgress: 'warning',
        Completed: 'success',
        Cancelled: 'secondary'
    };
    const cls = map[status] || 'light';
    return '<span class="badge bg-' + cls + '">' + status + '</span>';
}

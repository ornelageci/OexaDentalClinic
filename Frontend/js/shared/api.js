function resolveApiUrl() {
    if (typeof window !== 'undefined' && window.OEXA_API_URL) return window.OEXA_API_URL;
    if (typeof document !== 'undefined') {
        const meta = document.querySelector('meta[name="oexa-api-url"]');
        if (meta && meta.getAttribute('content')) return meta.getAttribute('content');
    }
    return localStorage.getItem('oexa_api_url') || 'http://localhost:5095';
}

const API_BASE_URL = resolveApiUrl();

let _loadingDepth = 0;

function resolveLoadingCssUrl() {
    var scripts = document.getElementsByTagName('script');
    for (var i = scripts.length - 1; i >= 0; i--) {
        var src = scripts[i].src;
        if (src && src.indexOf('api.js') !== -1) {
            return src.replace(/\/js\/shared\/api\.js.*$/, '/css/loading-overlay.css');
        }
    }
    return '../../css/loading-overlay.css';
}

function ensureLoadingStyles() {
    if (document.getElementById('oexa-loading-styles')) return;
    var link = document.createElement('link');
    link.id = 'oexa-loading-styles';
    link.rel = 'stylesheet';
    link.href = resolveLoadingCssUrl();
    document.head.appendChild(link);
}

function ensureLoadingOverlay() {
    ensureLoadingStyles();
    if (document.getElementById('oexa-loading-overlay')) return;
    var overlay = document.createElement('div');
    overlay.id = 'oexa-loading-overlay';
    overlay.setAttribute('aria-hidden', 'true');
    overlay.innerHTML = '<div class="oexa-loading-spinner" role="status" aria-label="Loading"></div>';
    document.body.appendChild(overlay);
}

function showLoading() {
    if (typeof document === 'undefined') return;
    ensureLoadingOverlay();
    _loadingDepth++;
    if (_loadingDepth === 1) {
        document.getElementById('oexa-loading-overlay').classList.add('is-active');
        document.body.classList.add('oexa-loading-active');
    }
}

function hideLoading() {
    if (typeof document === 'undefined') return;
    _loadingDepth = Math.max(0, _loadingDepth - 1);
    if (_loadingDepth === 0) {
        var el = document.getElementById('oexa-loading-overlay');
        if (el) el.classList.remove('is-active');
        document.body.classList.remove('oexa-loading-active');
    }
}

async function withLoading(fn) {
    showLoading();
    try {
        return await fn();
    } finally {
        hideLoading();
    }
}

function apiLoadingEnabled(options) {
    return options && options.loading === true;
}

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

async function apiGet(path, options) {
    const run = async function() {
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
    };
    return apiLoadingEnabled(options) ? withLoading(run) : run();
}

async function parseApiError(res) {
    const text = await res.text();
    try {
        const j = JSON.parse(text);
        const err = new Error(j.error || j.detail || j.title || text);
        err.body = j;
        throw err;
    } catch (e) {
        if (e instanceof SyntaxError) throw new Error(text || res.statusText);
        throw e;
    }
}

async function apiPost(path, body) {
    return withLoading(async function() {
        const res = await fetch(API_BASE_URL + path, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        if (!res.ok) await parseApiError(res);
        return res.json();
    });
}

async function apiPatch(path, body) {
    return withLoading(async function() {
        const res = await fetch(API_BASE_URL + path, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        if (!res.ok) await parseApiError(res);
        return res.json();
    });
}

async function apiPut(path, body) {
    return withLoading(async function() {
        const res = await fetch(API_BASE_URL + path, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        if (!res.ok) await parseApiError(res);
        return res.json();
    });
}

async function apiDelete(path) {
    return withLoading(async function() {
        const res = await fetch(API_BASE_URL + path, { method: 'DELETE' });
        if (!res.ok) await parseApiError(res);
        if (res.status === 204) return null;
        const text = await res.text();
        return text ? JSON.parse(text) : null;
    });
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

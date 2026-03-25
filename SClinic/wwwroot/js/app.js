/**
 * app.js — Global S-Clinic client scripts
 * Handles: JWT cookie management, logout, login modal mount point
 */

const TOKEN_KEY = 'sc_token';

// ── Auth helpers ────────────────────────────────────────────────────────────
const Auth = {
    getToken: () => localStorage.getItem(TOKEN_KEY),

    saveToken(token, expiresAt) {
        localStorage.setItem(TOKEN_KEY, token);
        // Also set cookie so Razor/JWT middleware can read it server-side
        const expires = new Date(expiresAt).toUTCString();
        document.cookie = `sc_token=${token}; path=/; expires=${expires}; SameSite=Lax`;
    },

    clearToken() {
        localStorage.removeItem(TOKEN_KEY);
        document.cookie = 'sc_token=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT';
    },

    async login(email, password) {
        const res = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Đăng nhập thất bại.');
        }
        const data = await res.json();
        Auth.saveToken(data.token, data.expiresAt);
        return data;
    },

    logout() {
        Auth.clearToken();
        window.location.href = '/';
    }
};

// ── Attach global event listeners ────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    // Logout button
    document.getElementById('btn-logout')?.addEventListener('click', () => Auth.logout());

    // Login modal trigger (Vue mounts the actual modal)
    document.getElementById('btn-login-modal')?.dispatchEvent(new CustomEvent('open-login'));
});

// Expose globally for Vue components
window.SClinicAuth = Auth;

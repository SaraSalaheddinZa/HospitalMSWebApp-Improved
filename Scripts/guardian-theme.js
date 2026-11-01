/* Public pages only. Run before CSS to resolve the preference before first paint. */
(function () {
    'use strict';
    var root = document.documentElement;
    if (!root.classList.contains('gh-cinematic')) return;
    var key = 'guardian-public-theme';
    var media = window.matchMedia('(prefers-color-scheme: dark)');
    var chosen = null;
    try { chosen = localStorage.getItem(key); } catch (_) { /* Storage may be unavailable. */ }
    if (chosen !== 'dark' && chosen !== 'light') chosen = null;
    function apply() {
        var value = chosen || (media.matches ? 'dark' : 'light');
        root.setAttribute('data-theme', value);
        document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
            var next = value === 'dark' ? 'light' : 'dark';
            button.setAttribute('aria-label', 'Switch to ' + next + ' mode');
            button.querySelector('[data-theme-label]').textContent = next === 'dark' ? 'Dark' : 'Light';
            button.hidden = false;
        });
    }
    apply();
    if (media.addEventListener) media.addEventListener('change', apply);
    window.addEventListener('storage', function (event) {
        if (event.key !== key) return;
        chosen = event.newValue === 'dark' || event.newValue === 'light' ? event.newValue : null;
        apply();
    });
    document.addEventListener('DOMContentLoaded', function () {
        apply();
        document.querySelectorAll('[data-theme-toggle]').forEach(function (button) {
            button.addEventListener('click', function () {
                chosen = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
                try { localStorage.setItem(key, chosen); } catch (_) { /* This page still switches. */ }
                apply();
            });
        });
    });
})();

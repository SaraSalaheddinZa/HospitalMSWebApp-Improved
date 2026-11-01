/* Public-only pre-paint preference; the two page controllers own their controls. */
(function () {
    'use strict';
    var root = document.documentElement, theme = null;
    try { theme = localStorage.getItem('guardian-public-theme'); } catch (_) { }
    if (theme !== 'light' && theme !== 'dark') {
        theme = matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    root.dataset.theme = theme;
    if (matchMedia('(prefers-reduced-motion: reduce)').matches || new URLSearchParams(location.search).get('motion') === 'reduce') {
        root.dataset.static = 'true';
        root.dataset.motion = 'static';
    }
})();

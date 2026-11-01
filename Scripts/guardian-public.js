(function () {
    'use strict';
    document.body.classList.add('public-enhanced');
    var menu = document.querySelector('.public-menu');
    var nav = document.getElementById('public-navigation');
    var header = document.querySelector('.public-header');
    var motionButton = document.querySelector('.public-motion-toggle');
    var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    var desktop = window.matchMedia('(min-width: 1001px)');
    var motionPaused = false;

    function closeMenu(restoreFocus) {
        if (!nav || !menu) return;
        nav.classList.remove('is-open');
        menu.setAttribute('aria-expanded', 'false');
        if (restoreFocus) menu.focus();
    }
    if (menu && nav) {
        menu.addEventListener('click', function () {
            var open = nav.classList.toggle('is-open');
            menu.setAttribute('aria-expanded', String(open));
        });
        nav.querySelectorAll('a').forEach(function (link) {
            link.addEventListener('click', function () { closeMenu(false); });
        });
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && nav.classList.contains('is-open')) closeMenu(true);
        });
        document.addEventListener('click', function (event) {
            if (!header.contains(event.target)) closeMenu(false);
        });
        document.addEventListener('focusin', function (event) {
            if (!header.contains(event.target)) closeMenu(false);
        });
        desktop.addEventListener('change', function () { closeMenu(false); });
    }

    function updateHeader() {
        if (header) header.classList.toggle('is-scrolled', window.scrollY > 12);
    }
    updateHeader();
    window.addEventListener('scroll', updateHeader, { passive: true });

    function applyMotionPreference() {
        document.body.classList.toggle('public-motion-paused', motionPaused || reducedMotion.matches);
        if (!motionButton) return;
        motionButton.hidden = reducedMotion.matches;
        motionButton.setAttribute('aria-pressed', String(motionPaused));
        motionButton.innerHTML = motionPaused ? '<span aria-hidden="true">▷</span> Resume motion' : '<span aria-hidden="true">Ⅱ</span> Pause motion';
    }
    if (motionButton) {
        motionButton.addEventListener('click', function () {
            motionPaused = !motionPaused;
            applyMotionPreference();
        });
        applyMotionPreference();
        reducedMotion.addEventListener('change', applyMotionPreference);
    }

    // Progressive enhancement: content is visible without JS and never waits for an observer.
    if ('IntersectionObserver' in window) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                if (!motionPaused && !reducedMotion.matches) entry.target.classList.add('public-reveal');
                observer.unobserve(entry.target);
            });
        }, { threshold: .12 });
        document.querySelectorAll('[data-reveal]').forEach(function (section) { observer.observe(section); });
    }

    // Native details retain keyboard and no-JS support. Keep the service choices uncluttered.
    var serviceDetails = document.querySelectorAll('.public-service-list details');
    serviceDetails.forEach(function (detail) {
        detail.addEventListener('toggle', function () {
            if (!detail.open) return;
            serviceDetails.forEach(function (other) { if (other !== detail) other.open = false; });
        });
    });
})();

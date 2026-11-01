/* Progressive enhancement: links, content and sign-in work without this script. */
(function () {
    'use strict';
    if (!document.documentElement.classList.contains('gh-cinematic')) return;
    var root = document.documentElement;
    var reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
    var fine = window.matchMedia('(hover: hover) and (pointer: fine)');
    var paused = false;
    var motion = document.querySelector('[data-motion-toggle]');
    var scene = document.querySelector('[data-scene]');
    var frame = 0;
    function resetScene() {
        if (frame) cancelAnimationFrame(frame);
        frame = 0;
        if (scene) { scene.style.removeProperty('--pointer-x'); scene.style.removeProperty('--pointer-y'); }
    }
    function updateMotion() {
        root.classList.toggle('motion-paused', paused || reduced.matches || document.hidden);
        resetScene();
        if (motion) {
            motion.hidden = reduced.matches;
            motion.setAttribute('aria-pressed', String(paused));
            motion.textContent = paused ? 'Resume motion' : 'Pause motion';
        }
    }
    if (motion) motion.addEventListener('click', function () { paused = !paused; updateMotion(); });
    if (reduced.addEventListener) reduced.addEventListener('change', updateMotion);
    document.addEventListener('visibilitychange', updateMotion);
    updateMotion();
    if (scene) {
        scene.addEventListener('pointermove', function (event) {
            if (!fine.matches || reduced.matches || paused || document.hidden || frame) return;
            // One layout read per pointer frame, never a perpetual animation loop.
            frame = requestAnimationFrame(function () {
                var box = scene.getBoundingClientRect();
                scene.style.setProperty('--pointer-x', ((event.clientX - box.left) / box.width - .5) * 12 + 'px');
                scene.style.setProperty('--pointer-y', ((event.clientY - box.top) / box.height - .5) * 10 + 'px');
                frame = 0;
            });
        });
        scene.addEventListener('pointerleave', resetScene);
    }
    if ('IntersectionObserver' in window) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                // Animate on entry, never hide content while waiting for JavaScript.
                entry.target.classList.add('has-entered');
                observer.unobserve(entry.target);
            });
        }, { threshold: .08 });
        document.querySelectorAll('[data-reveal]').forEach(function (section) { observer.observe(section); });
    }
    var menu = document.querySelector('[data-menu-toggle]');
    var nav = document.getElementById('public-navigation');
    var header = document.querySelector('.cg-header');
    function closeMenu(focus) {
        if (!menu || !nav) return;
        menu.setAttribute('aria-expanded', 'false');
        nav.classList.remove('is-open');
        if (focus) menu.focus();
    }
    if (menu && nav) {
        root.classList.add('menu-enhanced');
        menu.hidden = false;
        menu.addEventListener('click', function () {
            var open = menu.getAttribute('aria-expanded') !== 'true';
            menu.setAttribute('aria-expanded', String(open));
            nav.classList.toggle('is-open', open);
            // On narrow screens navigation precedes the toggle in DOM order.
            // Opening it moves keyboard users to the first newly available link.
            if (open) nav.querySelector('a').focus();
        });
        nav.querySelectorAll('a').forEach(function (link) { link.addEventListener('click', function () { closeMenu(false); }); });
        document.addEventListener('keydown', function (event) { if (event.key === 'Escape' && nav.classList.contains('is-open')) closeMenu(true); });
        document.addEventListener('click', function (event) { if (!header.contains(event.target)) closeMenu(false); });
        document.addEventListener('focusin', function (event) { if (!header.contains(event.target)) closeMenu(false); });
        window.matchMedia('(min-width: 1101px)').addEventListener('change', function () { closeMenu(false); });
    }
    var password = document.getElementById('passwordTb');
    var visibility = document.querySelector('[data-password-toggle]');
    if (password && visibility) {
        visibility.hidden = false;
        visibility.addEventListener('click', function () {
            var show = password.type === 'password';
            password.type = show ? 'text' : 'password';
            visibility.textContent = show ? 'Hide' : 'Show';
            visibility.setAttribute('aria-pressed', String(show));
            visibility.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        });
    }
    var form = document.querySelector('[data-sign-in]');
    var pending = document.getElementById('sign-in-progress');
    function resetSubmission() {
        if (form) { form.removeAttribute('aria-busy'); form.classList.remove('is-submitting'); }
        if (pending) pending.hidden = true;
        if (password && visibility) {
            password.type = 'password'; visibility.textContent = 'Show';
            visibility.setAttribute('aria-pressed', 'false'); visibility.setAttribute('aria-label', 'Show password');
        }
    }
    if (form) form.addEventListener('submit', function (event) {
        if (form.getAttribute('aria-busy') === 'true') { event.preventDefault(); return; }
        form.setAttribute('aria-busy', 'true'); form.classList.add('is-submitting');
        if (pending) pending.hidden = false;
        // Keep the Web Forms submit control enabled: its name/value dispatches the server event.
    });
    window.addEventListener('pageshow', function () {
        resetSubmission();
        var error = document.getElementById('ErrMsg');
        if (error && error.textContent.trim()) {
            error.setAttribute('tabindex', '-1');
            error.focus({ preventScroll: true });
            error.scrollIntoView({ block: 'center', behavior: 'instant' });
        }
    });
})();

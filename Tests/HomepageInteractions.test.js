// Small DOM harness for landing-page interaction branches that complement visual browser QA.
// No browser automation, external packages, network requests or database writes.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../Scripts/guardian-public.js'), 'utf8');

function harness(reduced = false) {
    let focused = null;
    function element() {
        const classes = new Set();
        const events = {};
        const attributes = {};
        return {
            classList: {
                add: value => classes.add(value),
                remove: value => classes.delete(value),
                contains: value => classes.has(value),
                toggle(value, force) {
                    const present = force === undefined ? !classes.has(value) : force;
                    if (present) classes.add(value); else classes.delete(value);
                    return present;
                }
            },
            addEventListener: (name, handler) => { (events[name] ||= []).push(handler); },
            emit: (name, event = {}) => (events[name] || []).forEach(handler => handler(event)),
            setAttribute: (name, value) => { attributes[name] = value; },
            getAttribute: name => attributes[name],
            focus() { focused = this; },
            open: false,
            hidden: true,
            innerHTML: ''
        };
    }
    const body = element(), menu = element(), nav = element(), header = element(), motion = element();
    const links = [element(), element()], details = [element(), element()], reveal = element();
    nav.querySelectorAll = () => links;
    header.contains = target => [menu, nav, header, ...links].includes(target);
    const preferences = element(), desktop = element();
    preferences.matches = reduced;
    desktop.matches = false;
    const document = element();
    document.body = body;
    document.querySelector = selector => ({ '.public-menu': menu, '.public-header': header, '.public-motion-toggle': motion })[selector];
    document.getElementById = () => nav;
    document.querySelectorAll = selector => selector === '[data-reveal]' ? [reveal] : details;
    let observerCallback;
    const observed = new Set();
    class Observer {
        constructor(callback) { observerCallback = callback; }
        observe(target) { observed.add(target); }
        unobserve(target) { observed.delete(target); }
    }
    const window = element();
    window.scrollY = 0;
    window.matchMedia = query => query.includes('reduced-motion') ? preferences : desktop;
    window.IntersectionObserver = Observer;
    vm.runInNewContext(source, { document, window, IntersectionObserver: Observer });
    return { body, menu, nav, header, motion, links, details, preferences, desktop, document, window, reveal,
        focused: () => focused, revealSection: () => observerCallback([{ target: reveal, isIntersecting: true }]), observed };
}

test('progressive enhancement enables navigation and the motion control', () => {
    const app = harness();
    assert.equal(app.body.classList.contains('public-enhanced'), true);
    assert.equal(app.motion.hidden, false);
    assert.equal(app.motion.getAttribute('aria-pressed'), 'false');
});
test('manual pause and resume apply consistent state and accessible feedback', () => {
    const app = harness();
    app.motion.emit('click');
    assert.equal(app.body.classList.contains('public-motion-paused'), true);
    assert.equal(app.motion.getAttribute('aria-pressed'), 'true');
    assert.match(app.motion.innerHTML, /Resume motion/);
    app.motion.emit('click');
    assert.equal(app.body.classList.contains('public-motion-paused'), false);
    assert.match(app.motion.innerHTML, /Pause motion/);
});
test('reduced-motion preference disables motion and hides the unneeded toggle', () => {
    const app = harness(true);
    assert.equal(app.body.classList.contains('public-motion-paused'), true);
    assert.equal(app.motion.hidden, true);
    app.revealSection();
    assert.equal(app.reveal.classList.contains('public-reveal'), false);
});
test('live preference changes respect an existing manual pause', () => {
    const app = harness();
    app.motion.emit('click');
    app.preferences.matches = true;
    app.preferences.emit('change');
    assert.equal(app.motion.hidden, true);
    app.preferences.matches = false;
    app.preferences.emit('change');
    assert.equal(app.motion.hidden, false);
    assert.equal(app.body.classList.contains('public-motion-paused'), true);
});
test('Escape closes an open menu and returns focus without stealing focus otherwise', () => {
    const app = harness();
    app.document.emit('keydown', { key: 'Escape' });
    assert.equal(app.focused(), null);
    app.menu.emit('click');
    assert.equal(app.menu.getAttribute('aria-expanded'), 'true');
    app.document.emit('keydown', { key: 'Escape' });
    assert.equal(app.menu.getAttribute('aria-expanded'), 'false');
    assert.equal(app.nav.classList.contains('is-open'), false);
    assert.equal(app.focused(), app.menu);
});
test('navigation, outside interaction and resizing all close the mobile menu', () => {
    const app = harness();
    for (const close of [() => app.links[0].emit('click'), () => app.document.emit('click', { target: {} }),
        () => app.document.emit('focusin', { target: {} }), () => app.desktop.emit('change')]) {
        app.menu.emit('click');
        close();
        assert.equal(app.menu.getAttribute('aria-expanded'), 'false');
    }
});
test('reveal runs once and observation ends after content enters view', () => {
    const app = harness();
    app.revealSection();
    assert.equal(app.reveal.classList.contains('public-reveal'), true);
    assert.equal(app.observed.has(app.reveal), false);
});
test('native service details keep only the selected section expanded', () => {
    const app = harness();
    app.details[0].open = true;
    app.details[1].open = true;
    app.details[1].emit('toggle');
    assert.equal(app.details[0].open, false);
    assert.equal(app.details[1].open, true);
});

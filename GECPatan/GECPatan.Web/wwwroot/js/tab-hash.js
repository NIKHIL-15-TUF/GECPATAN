/*
 * tab-hash.js  --  URL-hash driven tabs for every page that uses openTab().
 *
 *   /Department/5              -> default tab (first tab / #defaultOpen)
 *   /Department/5?tab=Labs     -> opens the "Labs" tab
 *   click a tab                -> tab opens AND the URL's ?tab= changes
 *   reload / share link        -> the tab named in ?tab= opens
 *   old links with #Labs       -> still work (read as a fallback)
 *
 * The value is simply the id of the .tabcontent div (About, Labs,
 * Sec_Research ...), matched case-insensitively.
 *
 * Must be loaded AFTER custom.js so it replaces the old openTab().
 */
(function () {
    'use strict';

    var hashApplied = false;          // true once a valid hash tab has been opened

    // Only pages whose buttons call openTab() use this script
    // (Achievements has its own year tabs and is skipped here).
    function usesOpenTab() { return !!document.querySelector('.tablinks[onclick*="openTab"]'); }

    function allPanes() { return document.querySelectorAll('.tabcontent'); }

    // Find the pane whose id matches `name` (case-insensitive). Null if none.
    function findPane(name) {
        if (!name) return null;
        var lower = String(name).toLowerCase();
        var panes = allPanes();
        for (var i = 0; i < panes.length; i++) {
            if (panes[i].id && panes[i].id.toLowerCase() === lower) return panes[i];
        }
        return null;
    }

    // Tab name from ?tab=..., else from a legacy #hash.
    function hashName() {
        try {
            var q = new URLSearchParams(window.location.search).get('tab');
            if (q) return q;
        } catch (e) { /* very old browser: fall through to hash */ }
        var h = window.location.hash || '';
        if (h.charAt(0) === '#') h = h.slice(1);
        try { return decodeURIComponent(h); } catch (e) { return h; }
    }

    // Put the tab in the URL as ?tab=Name (other query values are kept, #hash dropped).
    function writeName(name) {
        try {
            var u = new URL(window.location.href);
            u.searchParams.set('tab', name);
            u.hash = '';
            if (window.history && history.replaceState) {
                history.replaceState(null, '', u.pathname + u.search);
                return;
            }
            window.location.href = u.pathname + u.search;
        } catch (e) { window.location.hash = name; }
    }

    // Which tab does this button open?  Reads openTab(event,'Name') from onclick.
    function buttonTarget(btn) {
        var m = /openTab\s*\(\s*[^,]+,\s*['"]([^'"]+)['"]/.exec(btn.getAttribute('onclick') || '');
        return m ? m[1] : null;
    }

    function showTab(pane) {
        var panes = allPanes();
        for (var i = 0; i < panes.length; i++) panes[i].style.display = 'none';
        pane.style.display = 'block';

        // Highlight the matching button in BOTH the desktop and the mobile bar.
        var links = document.querySelectorAll('.tablinks');
        for (var j = 0; j < links.length; j++) {
            var t = buttonTarget(links[j]);
            var on = t && t.toLowerCase() === pane.id.toLowerCase();
            links[j].classList.toggle('active', !!on);
        }
    }

    function openDefault() {
        var def = document.getElementById('defaultOpen');
        var name = def && buttonTarget(def);
        var pane = findPane(name) || allPanes()[0];
        if (pane) showTab(pane);
    }

    function applyHash() {
        var pane = findPane(hashName());
        if (pane) { hashApplied = true; showTab(pane); }
        else { hashApplied = false; openDefault(); }
    }

    // Replaces the old global openTab(evt, name) used by all the onclick="" attributes.
    window.openTab = function (evt, name) {
        var pane = findPane(name);
        if (!pane) return;

        var byUser = !!(evt && evt.isTrusted);

        // Other scripts (old custom.js) may do defaultOpen.click() on load.
        // That is not a real click -- don't let it override the tab in the URL.
        if (!byUser && hashApplied) return;

        showTab(pane);

        if (byUser) {
            hashApplied = true;
            // replaceState: changes the URL without reloading the page
            // or piling up history entries.
            writeName(pane.id);
        }
    };

    // Expose helpers so other pages (e.g. Achievements) can reuse them.
    window.TabHash = { getName: hashName, set: writeName };

    window.addEventListener('hashchange', function () {
        if (usesOpenTab()) applyHash();
    });

    function init() { if (usesOpenTab()) applyHash(); }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();

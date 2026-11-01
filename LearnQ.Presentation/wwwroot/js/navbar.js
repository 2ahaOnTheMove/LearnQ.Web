/**
 * Navbar helpers — closes the offcanvas menu on link clicks and after Blazor enhanced navigation.
 * Loaded after bootstrap.bundle.min.js and blazor.web.js.
 */
(function () {
    'use strict';

    // Close offcanvas when clicking a link inside it (without preventing default so navigation proceeds).
    document.addEventListener('click', function (e) {
        var link = e.target.closest('.offcanvas a[href]');
        if (!link) return;
        // Skip links that toggle something (e.g., dropdown toggles) or are inside a form (logout).
        if (link.hasAttribute('data-bs-toggle') || link.closest('form')) return;
        var offcanvas = document.querySelector('.offcanvas.show');
        if (offcanvas) {
            var instance = bootstrap.Offcanvas.getInstance(offcanvas);
            if (instance) instance.hide();
        }
    });

    // Close offcanvas and dropdowns after Blazor enhanced navigation (back/forward, NavigateTo, form redirects).
    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancednavigationend', function () {
            document.querySelectorAll('.offcanvas.show').forEach(function (el) {
                var inst = bootstrap.Offcanvas.getInstance(el);
                if (inst) inst.hide();
            });
            // Close any open dropdowns.
            document.querySelectorAll('.dropdown.show').forEach(function (el) {
                el.classList.remove('show');
            });
        });
    }
})();

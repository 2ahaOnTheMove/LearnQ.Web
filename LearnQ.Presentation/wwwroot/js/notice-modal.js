/**
 * Notice modal helpers — shows the Bootstrap rate-limit modal on initial load
 * and after Blazor enhanced navigation.
 * Loaded after bootstrap.bundle.min.js and blazor.web.js.
 */
(function () {
    'use strict';

    /**
     * Find the notice modal in the DOM and show it using Bootstrap JS.
     * Idempotent: safe to call multiple times; will not show an already-visible modal.
     */
    function showNoticeModalIfPresent() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Modal) {
            // Bootstrap not loaded yet — retry briefly
            setTimeout(showNoticeModalIfPresent, 100);
            return;
        }

        var modalEl = document.getElementById('noticeModal');
        if (!modalEl) return;

        // Destroy any stale instance first (DOM may have been replaced by enhanced nav)
        var existing = bootstrap.Modal.getInstance(modalEl);
        if (existing) {
            existing.dispose();
        }

        // Determine options from data attributes
        var options = {
            backdrop: modalEl.getAttribute('data-bs-backdrop') === 'static' ? 'static' : true,
            keyboard: modalEl.getAttribute('data-bs-keyboard') !== 'false'
        };

        var instance = new bootstrap.Modal(modalEl, options);
        instance.show();
    }

    // Run on initial page load (static SSR render)
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', showNoticeModalIfPresent);
    } else {
        showNoticeModalIfPresent();
    }

    // Run after Blazor enhanced navigation (page navigation without full reload)
    if (window.Blazor && window.Blazor.addEventListener) {
        window.Blazor.addEventListener('enhancednavigationend', function () {
            // Small delay to let Blazor finish DOM updates
            setTimeout(showNoticeModalIfPresent, 50);
        });
    }
})();

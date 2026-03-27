/* ============================================================
   DAIRY MANAGEMENT SYSTEM
   Admin Layout JS
   ============================================================ */

// ── Toggle Sidebar ─────────────────────────────────────────
function toggleSidebar() {
    if (window.innerWidth <= 768) {
        document.body.classList.toggle('mobile-open');
    } else {
        document.body.classList.toggle('sidebar-collapsed');
    }
}

// ── Full Screen ────────────────────────────────────────────
function toggleFullScreen() {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen();
    } else {
        document.exitFullscreen();
    }
}

// ── Auto Close Mobile Sidebar On Nav Click ─────────────────
document.querySelectorAll('.nav-item-link').forEach(link => {
    link.addEventListener('click', () => {
        if (window.innerWidth <= 768) {
            document.body.classList.remove('mobile-open');
        }
    });
});
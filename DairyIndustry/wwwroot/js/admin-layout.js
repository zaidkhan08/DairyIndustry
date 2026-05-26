/* ============================================================
   DAIRY MANAGEMENT SYSTEM — Admin Layout JS
   ============================================================ */

// ── Toggle Sidebar ──────────────────────────────────────────
function toggleSidebar() {
    if (window.innerWidth <= 900) {
        document.body.classList.toggle('mobile-open');
    } else {
        const collapsed = document.body.classList.toggle('sidebar-collapsed');
        localStorage.setItem('dms_sidebar_collapsed', collapsed ? '1' : '0');
    }
}

// ── Fullscreen ──────────────────────────────────────────────
function toggleFullScreen() {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen?.();
    } else {
        document.exitFullscreen?.();
    }
}

// ── Restore sidebar state ───────────────────────────────────
(function restoreSidebarState() {
    if (window.innerWidth > 900) {
        const saved = localStorage.getItem('dms_sidebar_collapsed');
        if (saved === '1') document.body.classList.add('sidebar-collapsed');
    }
})();

// ── Set data-label on nav links for collapsed tooltips ───────
document.querySelectorAll('.nav-item-link').forEach(link => {
    const span = link.querySelector('span');
    if (span) {
        link.setAttribute('data-label', span.textContent.trim());
    }
});

// ── Auto close mobile sidebar on nav click ──────────────────
document.querySelectorAll('.nav-item-link').forEach(link => {
    link.addEventListener('click', () => {
        if (window.innerWidth <= 900) {
            document.body.classList.remove('mobile-open');
        }
    });
});

// ── Close mobile sidebar when clicking backdrop ─────────────
document.addEventListener('click', (e) => {
    if (window.innerWidth <= 900 && document.body.classList.contains('mobile-open')) {
        const sidebar = document.getElementById('sidebar');
        if (sidebar && !sidebar.contains(e.target)) {
            document.body.classList.remove('mobile-open');
        }
    }
});

// ── Toast helper ────────────────────────────────────────────
window.showToast = function (message, type = 'info', duration = 3500) {
    let container = document.querySelector('.dms-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'dms-toast-container';
        document.body.appendChild(container);
    }

    const icons = {
        success: 'bi-check-circle-fill',
        warning: 'bi-exclamation-triangle-fill',
        danger: 'bi-x-circle-fill',
        info: 'bi-info-circle-fill'
    };

    const toast = document.createElement('div');
    toast.className = `dms-toast ${type}`;
    toast.innerHTML = `<i class="bi ${icons[type] || icons.info}"></i><span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(12px)';
        toast.style.transition = 'opacity 240ms ease, transform 240ms ease';
        setTimeout(() => toast.remove(), 260);
    }, duration);
};

// ── Active link highlight from URL ──────────────────────────
(function setActiveFromUrl() {
    const path = window.location.pathname.toLowerCase();
    document.querySelectorAll('.nav-item-link[href]').forEach(link => {
        const href = link.getAttribute('href')?.toLowerCase();
        if (href && href !== '/' && path.startsWith(href)) {
            link.classList.add('active');
            // Open parent collapse if inside one
            const parentCollapse = link.closest('.collapse');
            if (parentCollapse) {
                parentCollapse.classList.add('show');
                const trigger = document.querySelector(`[data-bs-target="#${parentCollapse.id}"]`);
                if (trigger) trigger.setAttribute('aria-expanded', 'true');
            }
        }
    });
})();
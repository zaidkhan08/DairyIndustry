// admin-notifications.js
(function () {
    let allNotifs = [];
    let activeSev = 'all';
    let loaded = false;
    const sevOrder = { danger: 1, warning: 2, info: 3 };

    function relTime(dateStr) {
        const diff = Math.floor((new Date() - new Date(dateStr)) / 60000); // minutes
        if (diff < 1) return 'Just now';
        if (diff < 60) return diff + 'm ago';
        const hrs = Math.floor(diff / 60);
        if (hrs < 24) return hrs + 'h ago';
        const days = Math.floor(hrs / 24);
        if (days === 1) return 'Yesterday';
        return days + 'd ago';
    }

    function updateBadge(count) {
        const bell = document.getElementById('notif-badge');
        const header = document.getElementById('notif-badge-header');
        if (bell) {
            bell.textContent = count > 99 ? '99+' : count;
            bell.style.display = count > 0 ? '' : 'none';
        }
        if (header) {
            header.textContent = count > 99 ? '99+' : count;
            header.style.display = count > 0 ? '' : 'none';
        }
    }

    function renderList() {
        const list = document.getElementById('notif-list');
        if (!list) return;

        const items = allNotifs
            .filter(n => activeSev === 'all' || n.severity === activeSev)
            .sort((a, b) => (sevOrder[a.severity] || 9) - (sevOrder[b.severity] || 9));

        if (items.length === 0) {
            list.innerHTML = '<div class="notif-empty"><i class="bi bi-bell-slash" style="font-size:22px;display:block;margin-bottom:6px;"></i>No notifications</div>';
            return;
        }

        list.innerHTML = items.map(n =>
            `<a class="notif-item unread" href="${n.actionUrl || '#'}"
                data-id="${n.notificationId}"
                onclick="markRead(event, ${n.notificationId}, '${n.actionUrl || '#'}')">
                <div class="notif-dot ${n.severity}"></div>
                <div style="flex:1;min-width:0;">
                    <div class="notif-item-title">${n.title}</div>
                    <div class="notif-item-msg" title="${n.message}">${n.message}</div>
                    <div class="notif-item-time">${relTime(n.createdAt)}</div>
                </div>
            </a>`
        ).join('');
    }

    function fetchNotifications() {
        const list = document.getElementById('notif-list');
        if (list) list.innerHTML = '<div class="notif-loading"><div class="spinner-border spinner-border-sm mb-2" role="status"></div><div>Loading...</div></div>';

        fetch('/Admin/GetNotifications')
            .then(r => r.json())
            .then(data => {
                allNotifs = data;
                updateBadge(data.length);
                renderList();
            })
            .catch(() => {
                if (list) list.innerHTML = '<div class="notif-empty">Could not load notifications.</div>';
            });
    }

    function pollCount() {
        fetch('/Admin/GetNotificationCount')
            .then(r => r.json())
            .then(data => {
                updateBadge(data.count);
                // if dropdown is open and new notifs arrived, refresh list
                if (loaded && data.count !== allNotifs.length) {
                    fetchNotifications();
                }
            })
            .catch(() => { });
    }

    // Mark single as read then navigate
    window.markRead = function (e, notificationId, actionUrl) {
        e.preventDefault();
        fetch('/Admin/MarkNotificationRead', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: 'notificationId=' + notificationId
        })
            .then(() => {
                // remove from local array
                allNotifs = allNotifs.filter(n => n.notificationId !== notificationId);
                updateBadge(allNotifs.length);
                renderList();
                // navigate after short delay so render is visible
                if (actionUrl && actionUrl !== '#') {
                    setTimeout(() => { window.location.href = actionUrl; }, 150);
                }
            })
            .catch(() => {
                if (actionUrl && actionUrl !== '#') window.location.href = actionUrl;
            });
    };

    // Mark all as read
    window.markAllRead = function () {
        fetch('/Admin/MarkAllNotificationsRead', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
        })
            .then(() => {
                allNotifs = [];
                updateBadge(0);
                renderList();
            })
            .catch(() => { });
    };

    // Tab clicks
    document.querySelectorAll('.notif-tab').forEach(btn => {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.notif-tab').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            activeSev = btn.dataset.sev;
            renderList();
        });
    });

    // Lazy load on first bell open
    const bell = document.getElementById('notifBell');
    if (bell) {
        bell.addEventListener('click', function () {
            if (!loaded) {
                loaded = true;
                fetchNotifications();
            }
        });
    }

    // Poll every 60 seconds + immediate on load
    pollCount();
    setInterval(pollCount, 60000);

})();
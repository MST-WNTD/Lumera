let notificationCheckInterval;

// Load and display notifications
async function loadNotifications() {
    try {
        const response = await fetch('/client/notifications');
        const data = await response.json();

        if (data.success && data.notifications) {
            updateNotificationUI(data.notifications);
        }
    } catch (error) {
        console.error('Error loading notifications:', error);
    }
}

// Update notification count
async function updateNotificationCount() {
    try {
        const response = await fetch('/client/notifications/unread-count');
        const data = await response.json();

        if (data.success) {
            const badge = document.getElementById('notificationBadge');
            if (data.count > 0) {
                badge.textContent = data.count > 99 ? '99+' : data.count;
                badge.classList.remove('hidden');
            } else {
                badge.classList.add('hidden');
            }
        }
    } catch (error) {
        console.error('Error updating notification count:', error);
    }
}

// Update notification UI
function updateNotificationUI(notifications) {
    const notificationList = document.getElementById('notificationList');

    if (notifications.length === 0) {
        notificationList.innerHTML = `
            <div class="p-8 text-center text-stone-500 dark:text-stone-400">
                <span class="material-symbols-outlined text-4xl mb-2 block">notifications_off</span>
                <p>No notifications</p>
            </div>
        `;
        return;
    }

    notificationList.innerHTML = notifications.map(notif => {
        const icon = notif.type === 'Booking' ? 'book_online' :
                    notif.type === 'Message' ? 'mail' : 'notifications';
        const bgColor = notif.isRead ? '' : 'bg-primary/5 dark:bg-primary/10';

        return `
            <div class="notification-item ${bgColor} p-4 hover:bg-stone-50 dark:hover:bg-stone-800/50 border-b border-stone-200 dark:border-stone-800 last:border-b-0 relative group flex items-start gap-3"
                 onclick="handleNotificationClick(${notif.id}, '${notif.redirectUrl}')">
                <!-- Icon -->
                <div class="flex-shrink-0">
                    <div class="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center">
                        <span class="material-symbols-outlined text-primary text-sm">${icon}</span>
                    </div>
                </div>

                <!-- Content with fixed width -->
                <div class="flex-1 min-w-0 cursor-pointer">
                    <p class="font-semibold text-stone-900 dark:text-white text-sm truncate">${notif.title}</p>
                    <p class="text-sm text-stone-600 dark:text-stone-300 truncate">${notif.message}</p>
                    <p class="text-xs text-stone-400 dark:text-stone-500 mt-1">${notif.timeAgo}</p>
                </div>

                <!-- Unread indicator -->
                ${!notif.isRead ? '<div class="flex-shrink-0 self-start pt-1"><div class="w-2 h-2 bg-primary rounded-full"></div></div>' : ''}

                <!-- Delete button -->
                <button class="opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0 self-start p-1 rounded hover:bg-red-100 dark:hover:bg-red-900/30"
                        onclick="event.stopPropagation(); deleteNotification(${notif.id})"
                        title="Delete notification"
                        type="button">
                    <span class="material-symbols-outlined text-red-500 text-sm">close</span>
                </button>
            </div>
        `;
    }).join('');
}

// Delete notification
async function deleteNotification(notificationId) {
    try {
        const response = await fetch(`/client/notifications/delete/${notificationId}`, {
            method: 'DELETE'
        });

        const data = await response.json();

        if (data.success) {
            updateNotificationCount();
            loadNotifications();
        } else {
            console.error('Error deleting notification:', data.message);
        }
    } catch (error) {
        console.error('Error deleting notification:', error);
    }
}

// Handle notification click
async function handleNotificationClick(notificationId, redirectUrl) {
    try {
        await fetch(`/client/notifications/mark-read/${notificationId}`, {
            method: 'POST'
        });

        if (redirectUrl) {
            window.location.href = redirectUrl;
        }

        updateNotificationCount();
        loadNotifications();
    } catch (error) {
        console.error('Error handling notification click:', error);
    }
}

// Mark all as read
async function markAllAsRead() {
    try {
        const response = await fetch('/client/notifications/mark-all-read', {
            method: 'POST'
        });

        const data = await response.json();
        if (data.success) {
            updateNotificationCount();
            loadNotifications();
        }
    } catch (error) {
        console.error('Error marking all as read:', error);
    }
}

// Toggle notification dropdown
function toggleNotificationDropdown() {
    const dropdown = document.getElementById('notificationDropdown');
    dropdown.classList.toggle('hidden');

    if (!dropdown.classList.contains('hidden')) {
        loadNotifications();
    }
}

// Initialize notification system
function initializeNotifications() {
    // Notification bell click
    const notificationBtn = document.getElementById('notificationBtn');
    if (notificationBtn) {
        notificationBtn.addEventListener('click', toggleNotificationDropdown);
    }

    // Mark all read button
    const markAllReadBtn = document.getElementById('markAllReadBtn');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', markAllAsRead);
    }

    // Close dropdown when clicking outside
    document.addEventListener('click', function(event) {
        const container = document.getElementById('notificationContainer');
        const dropdown = document.getElementById('notificationDropdown');

        if (container && dropdown && !container.contains(event.target) && !dropdown.classList.contains('hidden')) {
            dropdown.classList.add('hidden');
        }
    });

    // Initial load
    updateNotificationCount();

    // Check for new notifications every 30 seconds
    notificationCheckInterval = setInterval(updateNotificationCount, 30000);
}

// Clean up interval when page unloads
window.addEventListener('beforeunload', function() {
    if (notificationCheckInterval) {
        clearInterval(notificationCheckInterval);
    }
});

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initializeNotifications);
"use client";

import { useEffect, useState } from "react";
import { X, Bell, Check, Trash2, CheckCheck } from "lucide-react";
import { Notification, SessionExpiredError } from "../lib/api";
import { apiClient } from "../lib/api";
import toast from "react-hot-toast";

interface NotificationsModalProps {
  isOpen: boolean;
  onClose: () => void;
  notifications: Notification[];
  onNotificationsUpdate: () => void;
  loading: boolean;
}

export default function NotificationsModal({
  isOpen,
  onClose,
  notifications,
  onNotificationsUpdate,
  loading,
}: NotificationsModalProps) {
  const [selectedNotifications, setSelectedNotifications] = useState<Set<number>>(
    new Set()
  );

  useEffect(() => {
    if (isOpen) {
      setSelectedNotifications(new Set());
    }
  }, [isOpen]);

  const handleToggleSelect = (id: number) => {
    setSelectedNotifications((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  const handleMarkSelectedAsRead = async () => {
    if (selectedNotifications.size === 0) return;

    try {
      const promises = Array.from(selectedNotifications).map((id) =>
        apiClient.markNotificationAsRead(id)
      );
      await Promise.all(promises);
      toast.success(`${selectedNotifications.size} notification(s) marked as read`);
      setSelectedNotifications(new Set());
      onNotificationsUpdate();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to mark notifications as read");
    }
  };

  const handleDeleteSelected = async () => {
    if (selectedNotifications.size === 0) return;

    try {
      const promises = Array.from(selectedNotifications).map((id) =>
        apiClient.deleteNotification(id)
      );
      await Promise.all(promises);
      toast.success(`${selectedNotifications.size} notification(s) deleted`);
      setSelectedNotifications(new Set());
      onNotificationsUpdate();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to delete notifications");
    }
  };

  const handleMarkAllRead = async () => {
    const unreadNotifications = notifications.filter((n) => !n.isRead);
    if (unreadNotifications.length === 0) return;

    try {
      const promises = unreadNotifications.map((n) =>
        apiClient.markNotificationAsRead(n.id)
      );
      await Promise.all(promises);
      toast.success("All notifications marked as read");
      onNotificationsUpdate();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to mark all as read");
    }
  };

  const handleDeleteAllRead = async () => {
    try {
      await apiClient.deleteAllReadNotifications();
      toast.success("All read notifications deleted");
      onNotificationsUpdate();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to delete read notifications");
    }
  };

  const unreadCount = notifications.filter((n) => !n.isRead).length;
  const readCount = notifications.filter((n) => n.isRead).length;

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-slate-800 rounded-2xl p-6 max-w-2xl w-full border border-slate-700/50 shadow-2xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-3">
            <Bell className="w-6 h-6 text-indigo-400" />
            <h2 className="text-2xl font-bold text-slate-100">Notifications</h2>
            {unreadCount > 0 && (
              <span className="px-2 py-1 bg-red-500 text-white text-xs font-bold rounded-full">
                {unreadCount} unread
              </span>
            )}
          </div>
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-slate-200 hover:bg-slate-700/50 rounded-lg transition-colors cursor-pointer"
            aria-label="Close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Actions Bar */}
        {notifications.length > 0 && (
          <div className="flex flex-wrap items-center gap-2 mb-4 pb-4 border-b border-slate-700/50">
            {selectedNotifications.size > 0 ? (
              <>
                <button
                  onClick={handleMarkSelectedAsRead}
                  className="px-3 py-1.5 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center gap-2"
                >
                  <Check className="w-4 h-4" />
                  Mark Selected Read ({selectedNotifications.size})
                </button>
                <button
                  onClick={handleDeleteSelected}
                  className="px-3 py-1.5 bg-red-600 text-white text-sm font-medium rounded-lg hover:bg-red-700 transition-colors cursor-pointer flex items-center gap-2"
                >
                  <Trash2 className="w-4 h-4" />
                  Delete Selected ({selectedNotifications.size})
                </button>
                <button
                  onClick={() => setSelectedNotifications(new Set())}
                  className="px-3 py-1.5 bg-slate-700 text-slate-200 text-sm font-medium rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
                >
                  Clear Selection
                </button>
              </>
            ) : (
              <>
                {unreadCount > 0 && (
                  <button
                    onClick={handleMarkAllRead}
                    className="px-3 py-1.5 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center gap-2"
                  >
                    <CheckCheck className="w-4 h-4" />
                    Mark All Read
                  </button>
                )}
                {readCount > 0 && (
                  <button
                    onClick={handleDeleteAllRead}
                    className="px-3 py-1.5 bg-slate-700 text-slate-200 text-sm font-medium rounded-lg hover:bg-slate-600 transition-colors cursor-pointer flex items-center gap-2"
                  >
                    <Trash2 className="w-4 h-4" />
                    Delete All Read
                  </button>
                )}
              </>
            )}
          </div>
        )}

        {/* Notifications List */}
        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <div className="text-center py-12">
              <div className="animate-spin h-8 w-8 text-indigo-400 mx-auto"></div>
              <p className="text-slate-400 mt-4">Loading notifications...</p>
            </div>
          ) : notifications.length === 0 ? (
            <div className="text-center py-12">
              <Bell className="w-12 h-12 text-slate-500 mx-auto mb-4" />
              <p className="text-slate-400">No notifications</p>
            </div>
          ) : (
            <div className="space-y-2">
              {notifications.map((notification) => (
                <div
                  key={notification.id}
                  className={`bg-slate-900/50 rounded-lg p-4 border ${
                    notification.isRead
                      ? "border-slate-700/50"
                      : "border-indigo-500/50 bg-indigo-500/5"
                  } transition-all`}
                >
                  <div className="flex items-start gap-3">
                    <input
                      type="checkbox"
                      checked={selectedNotifications.has(notification.id)}
                      onChange={() => handleToggleSelect(notification.id)}
                      className="mt-1 w-4 h-4 text-indigo-600 bg-slate-800 border-slate-600 rounded focus:ring-indigo-500 focus:ring-2 cursor-pointer"
                    />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <div className="flex-1 min-w-0">
                          <h3
                            className={`font-semibold text-sm sm:text-base ${
                              notification.isRead
                                ? "text-slate-300"
                                : "text-slate-100"
                            }`}
                          >
                            {notification.title}
                          </h3>
                          <p className="text-slate-400 text-xs sm:text-sm mt-1 break-words">
                            {notification.description}
                          </p>
                          <p className="text-slate-500 text-xs mt-2">
                            {new Date(notification.createdAt).toLocaleString()}
                          </p>
                        </div>
                        {!notification.isRead && (
                          <span className="flex-shrink-0 w-2 h-2 bg-indigo-500 rounded-full mt-1.5"></span>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}


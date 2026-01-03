"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";
import {
  apiClient,
  UserDevice,
  AccountLog,
  SessionExpiredError,
} from "../lib/api";
import toast from "react-hot-toast";
import {
  User,
  Mail,
  Smartphone,
  Activity,
  Trash2,
  X,
  Edit2,
  Check,
  AlertTriangle,
  ArrowLeft,
  Eye,
  EyeOff,
  ChevronDown,
} from "lucide-react";

type Tab = "profile" | "devices" | "logs" | "delete";

export default function AccountPage() {
  const { isLoading, isAuthenticated, user, logout, setUser } = useAuth();
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<Tab>("profile");
  const [devices, setDevices] = useState<UserDevice[]>([]);
  const [logs, setLogs] = useState<AccountLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [devicesLoading, setDevicesLoading] = useState(false);
  const [logsLoading, setLogsLoading] = useState(false);
  const [editingField, setEditingField] = useState<"username" | "email" | null>(
    null
  );
  const [editValues, setEditValues] = useState({
    username: "",
    email: "",
  });
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleteConfirmText, setDeleteConfirmText] = useState("");
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [showRevokeModal, setShowRevokeModal] = useState(false);
  const [deviceToRevoke, setDeviceToRevoke] = useState<UserDevice | null>(null);
  const [isTabDropdownOpen, setIsTabDropdownOpen] = useState(false);

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    if (isAuthenticated && user) {
      setEditValues({
        username: user.username || "",
        email: user.email || "",
      });
      loadInitialData();
    }
  }, [isAuthenticated, user]);

  useEffect(() => {
    if (activeTab === "devices" && devices.length === 0) {
      loadDevices();
    }
    if (activeTab === "logs" && logs.length === 0) {
      loadLogs();
    }
  }, [activeTab]);

  const loadInitialData = async () => {
    try {
      setLoading(true);
      await Promise.all([loadDevices(), loadLogs()]);
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to load account");
    } finally {
      setLoading(false);
    }
  };

  const loadDevices = async () => {
    try {
      setDevicesLoading(true);
      const data = await apiClient.getUserDevices();
      setDevices(data.filter((d) => d.isVerified));
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to load devices");
    } finally {
      setDevicesLoading(false);
    }
  };

  const loadLogs = async () => {
    try {
      setLogsLoading(true);
      const data = await apiClient.getAccountLogs();
      setLogs(data);
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to load activity logs");
    } finally {
      setLogsLoading(false);
    }
  };

  const handleUpdateProfile = async (field: "username" | "email") => {
    try {
      const updateData: { username?: string; email?: string } = {};
      if (field === "username") {
        updateData.username = editValues.username;
      } else {
        updateData.email = editValues.email;
      }

      await apiClient.updateProfile(updateData);
      toast.success(
        `${field === "username" ? "Username" : "Email"} updated`
      );
      setEditingField(null);

      // Refresh user data
      const updatedUser = await apiClient.getCurrentUser();
      setUser({
        username: updatedUser.username,
        email: updatedUser.email,
      });
      setEditValues({
        username: updatedUser.username,
        email: updatedUser.email,
      });
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to update profile";
      toast.error(errorMessage);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      toast.error("Passwords do not match");
      return;
    }

    if (passwordForm.newPassword.length < 8) {
      toast.error("Password must be 8+ characters");
      return;
    }

    try {
      await apiClient.changePassword(
        passwordForm.currentPassword,
        passwordForm.newPassword
      );
      toast.success("Password changed");
      setShowChangePassword(false);
      setPasswordForm({
        currentPassword: "",
        newPassword: "",
        confirmPassword: "",
      });
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to change password";
      toast.error(errorMessage);
    }
  };

  const handleRevokeDeviceClick = (device: UserDevice) => {
    setDeviceToRevoke(device);
    setShowRevokeModal(true);
  };

  const handleRevokeDevice = async () => {
    if (!deviceToRevoke) return;

    try {
      await apiClient.revokeDevice(deviceToRevoke.id);
      toast.success("Device revoked");
      setShowRevokeModal(false);
      setDeviceToRevoke(null);
      loadDevices();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to revoke device");
    }
  };

  const handleDeleteAccount = async () => {
    if (deleteConfirmText !== "DELETE") {
      toast.error('Please type "DELETE" to confirm');
      return;
    }

    try {
      await apiClient.deleteAccount();
      toast.success("Account deleted");
      await logout();
      router.push("/login");
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error("Failed to delete account");
    }
  };

  const formatAction = (action: string, context?: string) => {
    // If context exists and is user-friendly, use it as the main message
    if (context) {
      return context;
    }

    // Otherwise, format the action name
    return action
      .replace(/([A-Z])/g, " $1")
      .trim()
      .replace(/^./, (str) => str.toUpperCase());
  };

  if (isLoading || loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 flex items-center justify-center">
        <div className="text-center">
          <svg
            className="animate-spin h-12 w-12 text-indigo-400 mx-auto"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
            ></circle>
            <path
              className="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            ></path>
          </svg>
          <p className="mt-4 text-slate-400">Loading account</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50">
      {/* Navigation */}
      <nav className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10 border-b border-slate-800/50">
        <Link href="/dashboard" className="group">
          <span className="text-xl md:text-2xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>

        <div className="flex items-center space-x-2 sm:space-x-4">
          <Link
            href="/dashboard"
            className="px-2 sm:px-3 py-1.5 sm:py-2 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm flex items-center gap-1.5 sm:gap-2"
          >
            <ArrowLeft className="w-4 h-4 sm:w-5 sm:h-5" />
            <span className="hidden sm:inline">Back</span>
          </Link>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container mx-auto px-6 py-12">
        <div className="max-w-4xl mx-auto">
          <h1 className="text-2xl md:text-4xl font-bold text-slate-100 mb-8">
            Account Settings
          </h1>

          {/* Tabs */}
          <div className="mb-6 border-b border-slate-700/50">
            {/* Desktop Tabs */}
            <div className="hidden md:flex space-x-2">
              <button
                onClick={() => setActiveTab("profile")}
                className={`px-6 py-3 font-semibold transition-colors whitespace-nowrap cursor-pointer border-b-2 ${
                  activeTab === "profile"
                    ? "text-indigo-400 border-indigo-400"
                    : "text-slate-400 hover:text-slate-300 border-transparent"
                }`}
              >
                <div className="flex items-center gap-2">
                  <User className="w-4 h-4" />
                  Profile
                </div>
              </button>
              <button
                onClick={() => setActiveTab("devices")}
                className={`px-6 py-3 font-semibold transition-colors whitespace-nowrap cursor-pointer border-b-2 ${
                  activeTab === "devices"
                    ? "text-indigo-400 border-indigo-400"
                    : "text-slate-400 hover:text-slate-300 border-transparent"
                }`}
              >
                <div className="flex items-center gap-2">
                  <Smartphone className="w-4 h-4" />
                  Devices
                </div>
              </button>
              <button
                onClick={() => setActiveTab("logs")}
                className={`px-6 py-3 font-semibold transition-colors whitespace-nowrap cursor-pointer border-b-2 ${
                  activeTab === "logs"
                    ? "text-indigo-400 border-indigo-400"
                    : "text-slate-400 hover:text-slate-300 border-transparent"
                }`}
              >
                <div className="flex items-center gap-2">
                  <Activity className="w-4 h-4" />
                  Activity Logs
                </div>
              </button>
              <button
                onClick={() => setActiveTab("delete")}
                className={`px-6 py-3 font-semibold transition-colors whitespace-nowrap cursor-pointer border-b-2 ${
                  activeTab === "delete"
                    ? "text-red-400 border-red-400"
                    : "text-slate-400 hover:text-slate-300 border-transparent"
                }`}
              >
                <div className="flex items-center gap-2">
                  <Trash2 className="w-4 h-4" />
                  Delete Account
                </div>
              </button>
            </div>

            {/* Mobile Tab Dropdown */}
            <div className="md:hidden relative">
              <button
                onClick={() => setIsTabDropdownOpen(!isTabDropdownOpen)}
                className="w-full px-4 py-3 font-semibold text-slate-300 hover:text-indigo-400 transition-colors rounded-lg hover:bg-slate-700/50 flex items-center justify-between cursor-pointer"
              >
                <span className="flex items-center gap-2">
                  {activeTab === "profile" && (
                    <>
                      <User className="w-4 h-4" />
                      Profile
                    </>
                  )}
                  {activeTab === "devices" && (
                    <>
                      <Smartphone className="w-4 h-4" />
                      Devices
                    </>
                  )}
                  {activeTab === "logs" && (
                    <>
                      <Activity className="w-4 h-4" />
                      Activity Logs
                    </>
                  )}
                  {activeTab === "delete" && (
                    <>
                      <Trash2 className="w-4 h-4" />
                      Delete Account
                    </>
                  )}
                </span>
                <ChevronDown
                  className={`w-5 h-5 transition-transform ${
                    isTabDropdownOpen ? "rotate-180" : ""
                  }`}
                />
              </button>

              {isTabDropdownOpen && (
                <div className="absolute top-full left-0 right-0 mt-2 bg-slate-800/95 backdrop-blur-md rounded-xl border border-slate-700/50 shadow-2xl z-50">
                  <div className="flex flex-col p-2">
                    {(["profile", "devices", "logs", "delete"] as Tab[]).map(
                      (tab) => (
                        <button
                          key={tab}
                          onClick={() => {
                            setActiveTab(tab);
                            setIsTabDropdownOpen(false);
                          }}
                          className={`px-4 py-3 font-semibold transition-colors rounded-lg cursor-pointer text-left flex items-center gap-2 ${
                            activeTab === tab
                              ? tab === "delete"
                                ? "bg-red-600/20 text-red-400"
                                : "bg-indigo-600/20 text-indigo-400"
                              : "text-slate-300 hover:bg-slate-700/50 hover:text-slate-100"
                          }`}
                        >
                          {tab === "profile" && <User className="w-4 h-4" />}
                          {tab === "devices" && (
                            <Smartphone className="w-4 h-4" />
                          )}
                          {tab === "logs" && <Activity className="w-4 h-4" />}
                          {tab === "delete" && <Trash2 className="w-4 h-4" />}
                          {tab === "profile"
                            ? "Profile"
                            : tab === "devices"
                            ? "Devices"
                            : tab === "logs"
                            ? "Activity Logs"
                            : "Delete Account"}
                        </button>
                      )
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Tab Content */}
          <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-4 sm:p-6 md:p-8 border border-slate-700/50 shadow-2xl">
            {/* Profile Tab */}
            {activeTab === "profile" && (
              <div className="space-y-4 sm:space-y-6">
                <div>
                  <label className="block text-xs sm:text-sm font-semibold text-slate-400 mb-2">
                    Username
                  </label>
                  <div className="flex items-center gap-2 sm:gap-3">
                    {editingField === "username" ? (
                      <>
                        <input
                          type="text"
                          autoComplete="off"
                          value={editValues.username}
                          onChange={(e) =>
                            setEditValues({
                              ...editValues,
                              username: e.target.value,
                            })
                          }
                          className="flex-1 px-3 sm:px-4 py-2 sm:py-3 text-sm sm:text-base bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                        />
                        <button
                          onClick={() => handleUpdateProfile("username")}
                          className="p-2 sm:p-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex-shrink-0"
                        >
                          <Check className="w-4 h-4 sm:w-5 sm:h-5" />
                        </button>
                        <button
                          onClick={() => {
                            setEditingField(null);
                            setEditValues({
                              username: user.username || "",
                              email: user.email || "",
                            });
                          }}
                          className="p-2 sm:p-3 bg-slate-700 text-slate-300 rounded-lg hover:bg-slate-600 transition-colors cursor-pointer flex-shrink-0"
                        >
                          <X className="w-4 h-4 sm:w-5 sm:h-5" />
                        </button>
                      </>
                    ) : (
                      <>
                        <p className="flex-1 text-base sm:text-lg text-slate-100 font-medium truncate">
                          {user.username}
                        </p>
                        <button
                          onClick={() => setEditingField("username")}
                          className="p-2 sm:p-3 bg-slate-700 text-slate-300 rounded-lg hover:bg-slate-600 transition-colors cursor-pointer flex-shrink-0"
                        >
                          <Edit2 className="w-4 h-4 sm:w-5 sm:h-5" />
                        </button>
                      </>
                    )}
                  </div>
                </div>

                <div>
                  <label className="block text-xs sm:text-sm font-semibold text-slate-400 mb-2">
                    Email Address
                  </label>
                  <div className="flex items-center gap-2 sm:gap-3">
                    {editingField === "email" ? (
                      <>
                        <input
                          type="email"
                          autoComplete="off"
                          value={editValues.email}
                          onChange={(e) =>
                            setEditValues({
                              ...editValues,
                              email: e.target.value,
                            })
                          }
                          className="flex-1 px-3 sm:px-4 py-2 sm:py-3 text-sm sm:text-base bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                        />
                        <button
                          onClick={() => handleUpdateProfile("email")}
                          className="p-2 sm:p-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex-shrink-0"
                        >
                          <Check className="w-4 h-4 sm:w-5 sm:h-5" />
                        </button>
                        <button
                          onClick={() => {
                            setEditingField(null);
                            setEditValues({
                              username: user.username || "",
                              email: user.email || "",
                            });
                          }}
                          className="p-2 sm:p-3 bg-slate-700 text-slate-300 rounded-lg hover:bg-slate-600 transition-colors cursor-pointer flex-shrink-0"
                        >
                          <X className="w-4 h-4 sm:w-5 sm:h-5" />
                        </button>
                      </>
                    ) : (
                      <>
                        <p className="flex-1 text-base sm:text-lg text-slate-100 font-medium truncate">
                          {user.email}
                        </p>
                        <button
                          onClick={() => setEditingField("email")}
                          className="p-2 sm:p-3 bg-slate-700 text-slate-300 rounded-lg hover:bg-slate-600 transition-colors cursor-pointer flex-shrink-0"
                        >
                          <Edit2 className="w-4 h-4 sm:w-5 sm:h-5" />
                        </button>
                      </>
                    )}
                  </div>
                </div>

                <div className="pt-4 sm:pt-6 border-t border-slate-700/50">
                  <label className="block text-xs sm:text-sm font-semibold text-slate-400 mb-3 sm:mb-4">
                    Password
                  </label>
                  {!showChangePassword ? (
                    <button
                      onClick={() => setShowChangePassword(true)}
                      className="w-full sm:w-auto px-4 sm:px-6 py-2.5 sm:py-3 text-sm sm:text-base bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
                    >
                      Change Password
                    </button>
                  ) : (
                    <form
                      onSubmit={handleChangePassword}
                      className="space-y-3 sm:space-y-4"
                    >
                      <div>
                        <label className="block text-xs sm:text-sm font-semibold text-slate-300 mb-2">
                          Current Password
                        </label>
                        <div className="relative">
                          <input
                            type={showCurrentPassword ? "text" : "password"}
                            autoComplete="current-password"
                            required
                            value={passwordForm.currentPassword}
                            onChange={(e) =>
                              setPasswordForm({
                                ...passwordForm,
                                currentPassword: e.target.value,
                              })
                            }
                            className="w-full px-3 sm:px-4 py-2.5 sm:py-3 pr-10 sm:pr-12 text-sm sm:text-base bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                          />
                          <button
                            type="button"
                            onClick={() =>
                              setShowCurrentPassword(!showCurrentPassword)
                            }
                            className="absolute right-2 sm:right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-300 transition-colors cursor-pointer"
                          >
                            {showCurrentPassword ? (
                              <EyeOff className="w-4 h-4 sm:w-5 sm:h-5" />
                            ) : (
                              <Eye className="w-4 h-4 sm:w-5 sm:h-5" />
                            )}
                          </button>
                        </div>
                      </div>
                      <div>
                        <label className="block text-xs sm:text-sm font-semibold text-slate-300 mb-2">
                          New Password
                        </label>
                        <div className="relative">
                          <input
                            type={showNewPassword ? "text" : "password"}
                            autoComplete="new-password"
                            required
                            value={passwordForm.newPassword}
                            onChange={(e) =>
                              setPasswordForm({
                                ...passwordForm,
                                newPassword: e.target.value,
                              })
                            }
                            className="w-full px-3 sm:px-4 py-2.5 sm:py-3 pr-10 sm:pr-12 text-sm sm:text-base bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                          />
                          <button
                            type="button"
                            onClick={() => setShowNewPassword(!showNewPassword)}
                            className="absolute right-2 sm:right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-300 transition-colors cursor-pointer"
                          >
                            {showNewPassword ? (
                              <EyeOff className="w-4 h-4 sm:w-5 sm:h-5" />
                            ) : (
                              <Eye className="w-4 h-4 sm:w-5 sm:h-5" />
                            )}
                          </button>
                        </div>
                      </div>
                      <div>
                        <label className="block text-xs sm:text-sm font-semibold text-slate-300 mb-2">
                          Confirm New Password
                        </label>
                        <div className="relative">
                          <input
                            type={showConfirmPassword ? "text" : "password"}
                            autoComplete="new-password"
                            required
                            value={passwordForm.confirmPassword}
                            onChange={(e) =>
                              setPasswordForm({
                                ...passwordForm,
                                confirmPassword: e.target.value,
                              })
                            }
                            className="w-full px-3 sm:px-4 py-2.5 sm:py-3 pr-10 sm:pr-12 text-sm sm:text-base bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                          />
                          <button
                            type="button"
                            onClick={() =>
                              setShowConfirmPassword(!showConfirmPassword)
                            }
                            className="absolute right-2 sm:right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-300 transition-colors cursor-pointer"
                          >
                            {showConfirmPassword ? (
                              <EyeOff className="w-4 h-4 sm:w-5 sm:h-5" />
                            ) : (
                              <Eye className="w-4 h-4 sm:w-5 sm:h-5" />
                            )}
                          </button>
                        </div>
                      </div>
                      <div className="flex flex-col sm:flex-row gap-2 sm:gap-3 sm:space-x-0">
                        <button
                          type="button"
                          onClick={() => {
                            setShowChangePassword(false);
                            setPasswordForm({
                              currentPassword: "",
                              newPassword: "",
                              confirmPassword: "",
                            });
                          }}
                          className="flex-1 px-4 py-2.5 sm:py-3 text-sm sm:text-base bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
                        >
                          Cancel
                        </button>
                        <button
                          type="submit"
                          className="flex-1 px-4 py-2.5 sm:py-3 text-sm sm:text-base bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
                        >
                          Change Password
                        </button>
                      </div>
                    </form>
                  )}
                </div>
              </div>
            )}

            {/* Devices Tab */}
            {activeTab === "devices" && (
              <div>
                {devicesLoading ? (
                  <div className="text-center py-8 sm:py-12">
                    <svg
                      className="animate-spin h-6 w-6 sm:h-8 sm:w-8 text-indigo-400 mx-auto"
                      fill="none"
                      viewBox="0 0 24 24"
                    >
                      <circle
                        className="opacity-25"
                        cx="12"
                        cy="12"
                        r="10"
                        stroke="currentColor"
                        strokeWidth="4"
                      ></circle>
                      <path
                        className="opacity-75"
                        fill="currentColor"
                        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                      ></path>
                    </svg>
                    <p className="mt-4 text-sm sm:text-base text-slate-400">
                      Loading devices
                    </p>
                  </div>
                ) : devices.length === 0 ? (
                  <div className="text-center py-8 sm:py-12">
                    <Smartphone className="w-10 h-10 sm:w-12 sm:h-12 text-slate-500 mx-auto mb-4" />
                    <p className="text-sm sm:text-base text-slate-400">
                      No verified devices found
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3 sm:space-y-4">
                    {devices.map((device) => (
                      <div
                        key={device.id}
                        className="bg-slate-900/50 rounded-lg p-3 sm:p-4 border border-slate-700/50 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3"
                      >
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-2 sm:gap-3 mb-2">
                            <Smartphone className="w-4 h-4 sm:w-5 sm:h-5 text-indigo-400 flex-shrink-0" />
                            <p className="text-sm sm:text-base text-slate-100 font-medium truncate">
                              {device.deviceIdentifier}
                            </p>
                          </div>
                          <div className="text-xs sm:text-sm text-slate-400 space-y-1">
                            <p className="break-words">
                              Verified:{" "}
                              {device.verifiedAt
                                ? new Date(device.verifiedAt).toLocaleString()
                                : "N/A"}
                            </p>
                            <p>Active sessions: {device.activeTokens}</p>
                            <p>
                              Added:{" "}
                              {new Date(device.createdAt).toLocaleDateString()}
                            </p>
                          </div>
                        </div>
                        <button
                          onClick={() => handleRevokeDeviceClick(device)}
                          className="w-full sm:w-auto px-3 sm:px-4 py-2 text-xs sm:text-sm bg-red-600/20 text-red-400 rounded-lg hover:bg-red-600/30 transition-colors border border-red-600/30 cursor-pointer flex-shrink-0"
                        >
                          Revoke
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Activity Logs Tab */}
            {activeTab === "logs" && (
              <div>
                {logsLoading ? (
                  <div className="text-center py-8 sm:py-12">
                    <svg
                      className="animate-spin h-6 w-6 sm:h-8 sm:w-8 text-indigo-400 mx-auto"
                      fill="none"
                      viewBox="0 0 24 24"
                    >
                      <circle
                        className="opacity-25"
                        cx="12"
                        cy="12"
                        r="10"
                        stroke="currentColor"
                        strokeWidth="4"
                      ></circle>
                      <path
                        className="opacity-75"
                        fill="currentColor"
                        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                      ></path>
                    </svg>
                    <p className="mt-4 text-sm sm:text-base text-slate-400">
                      Loading logs
                    </p>
                  </div>
                ) : logs.length === 0 ? (
                  <div className="text-center py-8 sm:py-12">
                    <Activity className="w-10 h-10 sm:w-12 sm:h-12 text-slate-500 mx-auto mb-4" />
                    <p className="text-sm sm:text-base text-slate-400">
                      No activity logs found
                    </p>
                  </div>
                ) : (
                  <div className="space-y-2 sm:space-y-3 max-h-[400px] sm:max-h-[600px] overflow-y-auto">
                    {logs.map((log) => (
                      <div
                        key={log.id}
                        className="bg-slate-900/50 rounded-lg p-3 sm:p-4 border border-slate-700/50"
                      >
                        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 sm:gap-0">
                          <div className="flex items-start gap-2 sm:gap-3 min-w-0 flex-1">
                            <Activity className="w-4 h-4 sm:w-5 sm:h-5 text-indigo-400 flex-shrink-0 mt-0.5" />
                            <p className="text-xs sm:text-sm text-slate-100 font-medium break-words">
                              {formatAction(log.action, log.additionalContext)}
                            </p>
                          </div>
                          <p className="text-xs sm:text-sm text-slate-400 flex-shrink-0 sm:ml-4">
                            {new Date(log.timestamp).toLocaleString()}
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Delete Account Tab */}
            {activeTab === "delete" && (
              <div className="space-y-4 sm:space-y-6">
                <div className="bg-red-500/10 border border-red-500/30 rounded-lg p-4 sm:p-6">
                  <div className="flex items-start gap-3 sm:gap-4">
                    <AlertTriangle className="w-5 h-5 sm:w-6 sm:h-6 text-red-400 flex-shrink-0 mt-0.5 sm:mt-1" />
                    <div className="min-w-0 flex-1">
                      <h3 className="text-base sm:text-lg font-bold text-red-400 mb-2">
                        Warning: This action cannot be undone
                      </h3>
                      <p className="text-sm sm:text-base text-slate-300 mb-3 sm:mb-4">
                        Deleting your account will permanently:
                      </p>
                      <ul className="list-disc list-inside text-xs sm:text-sm text-slate-300 space-y-1.5 sm:space-y-2 ml-2 sm:ml-4">
                        <li>Delete all vaults you own and all their items</li>
                        <li>
                          Remove you from all vaults you&apos;re a member of
                        </li>
                        <li>Transfer ownership of items to the vault owner</li>
                        <li>Delete all your account data and activity logs</li>
                        <li>Revoke access from all your devices</li>
                      </ul>
                    </div>
                  </div>
                </div>

                {!showDeleteConfirm ? (
                  <button
                    onClick={() => setShowDeleteConfirm(true)}
                    className="w-full px-4 sm:px-6 py-2.5 sm:py-3 text-sm sm:text-base bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 transition-colors cursor-pointer"
                  >
                    Delete My Account
                  </button>
                ) : (
                  <div className="space-y-3 sm:space-y-4">
                    <div>
                      <label className="block text-xs sm:text-sm font-semibold text-slate-300 mb-2">
                        Type &quot;DELETE&quot; to confirm:
                      </label>
                      <input
                        type="text"
                        autoComplete="off"
                        value={deleteConfirmText}
                        onChange={(e) => setDeleteConfirmText(e.target.value)}
                        className="w-full px-3 sm:px-4 py-2.5 sm:py-3 text-sm sm:text-base bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-transparent"
                        placeholder="DELETE"
                      />
                    </div>
                    <div className="flex flex-col sm:flex-row gap-2 sm:gap-3 sm:space-x-0">
                      <button
                        onClick={() => {
                          setShowDeleteConfirm(false);
                          setDeleteConfirmText("");
                        }}
                        className="flex-1 px-4 sm:px-6 py-2.5 sm:py-3 text-sm sm:text-base bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
                      >
                        Cancel
                      </button>
                      <button
                        onClick={handleDeleteAccount}
                        disabled={deleteConfirmText !== "DELETE"}
                        className="flex-1 px-4 sm:px-6 py-2.5 sm:py-3 text-sm sm:text-base bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
                      >
                        Confirm Deletion
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Revoke Device Confirmation Modal */}
      {showRevokeModal && deviceToRevoke && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
            <div className="mb-6">
              <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-500/20 mb-4">
                <AlertTriangle className="h-6 w-6 text-red-400" />
              </div>
              <h3 className="text-xl font-bold text-slate-100 text-center mb-2">
                Revoke Device Access?
              </h3>
              <p className="text-slate-400 text-center mb-4">
                Are you sure you want to revoke access for this device?
              </p>
              <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                <div className="flex items-center gap-3 mb-2">
                  <Smartphone className="w-5 h-5 text-indigo-400" />
                  <p className="text-slate-100 font-medium">
                    {deviceToRevoke.deviceIdentifier}
                  </p>
                </div>
                <div className="text-sm text-slate-400 space-y-1">
                  <p>
                    Verified:{" "}
                    {deviceToRevoke.verifiedAt
                      ? new Date(deviceToRevoke.verifiedAt).toLocaleString()
                      : "N/A"}
                  </p>
                  <p>Active sessions: {deviceToRevoke.activeTokens}</p>
                </div>
              </div>
              <p className="text-red-400 text-sm text-center mt-4">
                This will sign out all active sessions on this device.
              </p>
            </div>
            <div className="flex gap-3">
              <button
                onClick={() => {
                  setShowRevokeModal(false);
                  setDeviceToRevoke(null);
                }}
                className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
              >
                Cancel
              </button>
              <button
                onClick={handleRevokeDevice}
                className="flex-1 px-4 py-3 bg-red-500 text-white font-semibold rounded-lg hover:bg-red-600 transition-colors cursor-pointer"
              >
                Revoke Access
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

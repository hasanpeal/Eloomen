"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";
import { apiClient, Vault, CreateVaultRequest } from "../lib/api";
import toast from "react-hot-toast";

export default function DashboardPage() {
  const { isLoading, isAuthenticated, user, logout } = useAuth();
  const router = useRouter();
  const [vaults, setVaults] = useState<Vault[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showAccountModal, setShowAccountModal] = useState(false);
  const [showAccessDeniedModal, setShowAccessDeniedModal] = useState(false);
  const [accessDeniedVault, setAccessDeniedVault] = useState<Vault | null>(
    null
  );
  const [createForm, setCreateForm] = useState<CreateVaultRequest>({
    name: "",
    description: "",
    policyType: "Immediate",
  });

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    if (isAuthenticated) {
      loadVaults();
    }
  }, [isAuthenticated]);

  const loadVaults = async () => {
    try {
      setLoading(true);
      const data = await apiClient.getVaults();
      setVaults(data);
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : "Failed to load vaults";
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateVault = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiClient.createVault(createForm);
      toast.success("Vault created successfully!");
      setShowCreateModal(false);
      setCreateForm({
        name: "",
        description: "",
        policyType: "Immediate",
      });
      loadVaults();
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : "Failed to create vault";
      toast.error(errorMessage);
    }
  };

  const handleLogout = async () => {
    await logout();
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
          <p className="mt-4 text-slate-400">Loading vaults...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  const getPrivilegeBadge = (privilege?: string) => {
    switch (privilege) {
      case "Owner":
        return (
          <span className="px-3 py-1 bg-yellow-500/20 text-yellow-400 rounded-full text-xs font-semibold border border-yellow-500/30">
            👑 Owner
          </span>
        );
      case "Admin":
        return (
          <span className="px-3 py-1 bg-blue-500/20 text-blue-400 rounded-full text-xs font-semibold border border-blue-500/30">
            🛠 Admin
          </span>
        );
      case "Member":
        return (
          <span className="px-3 py-1 bg-green-500/20 text-green-400 rounded-full text-xs font-semibold border border-green-500/30">
            👀 Member
          </span>
        );
      default:
        return null;
    }
  };

  // Helper function to check if vault is accessible and get access message
  // This matches the backend IsVaultAccessible logic
  const getVaultAccessInfo = (vault: Vault) => {
    // Owner always has access
    if (vault.userPrivilege === "Owner") {
      return { hasAccess: true, message: null };
    }

    // If no policy, vault is accessible
    if (!vault.policy) {
      return { hasAccess: true, message: null };
    }

    const policy = vault.policy;
    const now = new Date();

    // Check if expired or revoked (backend checks this first)
    if (policy.releaseStatus === "Expired") {
      return {
        hasAccess: false,
        message: "This vault has expired.",
      };
    }

    if (policy.releaseStatus === "Revoked") {
      return {
        hasAccess: false,
        message: "This vault has been revoked.",
      };
    }

    // Check if released (backend checks this next)
    if (policy.releaseStatus === "Released") {
      // Check if expired (for ExpiryBased) - backend checks this
      if (policy.policyType === "ExpiryBased" && policy.expiresAt) {
        const expiresAt = new Date(policy.expiresAt);
        if (now > expiresAt) {
          return {
            hasAccess: false,
            message: `Expired on ${expiresAt.toLocaleDateString()}.`,
          };
        }
        return {
          hasAccess: true,
          message: `Expires ${expiresAt.toLocaleDateString()}.`,
        };
      }

      // For other Released policies (Immediate, ManualRelease after release), access is granted
      return { hasAccess: true, message: null };
    }

    // Check if pending (backend checks TimeBased here)
    if (policy.releaseStatus === "Pending") {
      // TimeBased: Check if release date has been reached
      if (policy.policyType === "TimeBased" && policy.releaseDate) {
        const releaseDate = new Date(policy.releaseDate);
        if (now >= releaseDate) {
          // Should be released by backend, but check anyway
          return {
            hasAccess: true,
            message: "Vault is now accessible.",
          };
        }
        return {
          hasAccess: false,
          message: `Available ${releaseDate.toLocaleDateString()} at ${releaseDate.toLocaleTimeString()}.`,
        };
      }

      // ManualRelease: Pending until owner releases
      if (policy.policyType === "ManualRelease") {
        return {
          hasAccess: false,
          message: "Awaiting manual release by owner.",
        };
      }
    }

    // Default: not accessible
    return { hasAccess: false, message: "Vault is not accessible." };
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50">
      {/* Navigation */}
      <nav className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10 border-b border-slate-800/50">
        <Link href="/" className="group">
          <span className="text-2xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>
        <div className="flex items-center space-x-4">
          <button
            onClick={() => setShowAccountModal(true)}
            className="px-5 py-2.5 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm cursor-pointer"
          >
            Account
          </button>
          <button
            onClick={handleLogout}
            className="px-5 py-2.5 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm cursor-pointer"
          >
            Logout
          </button>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container mx-auto px-6 py-12">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center justify-between mb-8">
            <div>
              <h1 className="text-4xl font-bold text-slate-100 mb-2">
                Your Vaults
              </h1>
              <p className="text-slate-400">
                Manage your secure vaults and collaborate with others
              </p>
            </div>
            <button
              onClick={() => setShowCreateModal(true)}
              className="px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all duration-200 shadow-lg shadow-indigo-500/20 cursor-pointer"
            >
              + Create Vault
            </button>
          </div>

          {vaults.length === 0 ? (
            <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-12 border border-slate-700/50 shadow-2xl text-center">
              <p className="text-slate-400 text-lg mb-6">
                You don&apos;t have any vaults yet.
              </p>
              <button
                onClick={() => setShowCreateModal(true)}
                className="px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all duration-200 cursor-pointer"
              >
                Create Your First Vault
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {vaults.map((vault) => {
                const accessInfo = getVaultAccessInfo(vault);
                const isOwner = vault.userPrivilege === "Owner";
                const canAccess = isOwner || accessInfo.hasAccess;

                // For non-accessible vaults, wrap in a div that prevents all interactions
                if (!canAccess) {
                  return (
                    <div
                      key={vault.id}
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        setAccessDeniedVault(vault);
                        setShowAccessDeniedModal(true);
                      }}
                      onMouseDown={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                      }}
                      onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                          e.preventDefault();
                          e.stopPropagation();
                          setAccessDeniedVault(vault);
                          setShowAccessDeniedModal(true);
                        }
                      }}
                      role="button"
                      tabIndex={0}
                      aria-disabled={true}
                      className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 shadow-xl transition-all duration-200 group cursor-not-allowed opacity-75"
                    >
                      <div className="flex items-start justify-between mb-4">
                        <h3 className="text-xl font-bold text-slate-100">
                          {vault.name}
                        </h3>
                        {getPrivilegeBadge(vault.userPrivilege)}
                      </div>
                      {vault.description && (
                        <p className="text-slate-400 text-sm mb-4 line-clamp-2">
                          {vault.description}
                        </p>
                      )}
                      {accessInfo.message && (
                        <div className="mb-4 p-3 bg-yellow-500/10 border border-yellow-500/30 rounded-lg">
                          <p className="text-yellow-400 text-xs font-semibold">
                            🔒 {accessInfo.message}
                          </p>
                        </div>
                      )}
                      <div className="flex items-center justify-between text-xs text-slate-500">
                        <span>
                          Created{" "}
                          {new Date(vault.createdAt).toLocaleDateString()}
                        </span>
                        {vault.status === "Deleted" && (
                          <span className="text-red-400">Deleted</span>
                        )}
                      </div>
                    </div>
                  );
                }

                // For accessible vaults, allow navigation
                return (
                  <div
                    key={vault.id}
                    onClick={() => {
                      router.push(`/vaults/${vault.id}`);
                    }}
                    className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 shadow-xl hover:border-indigo-500/50 transition-all duration-200 hover:shadow-2xl hover:shadow-indigo-500/10 group cursor-pointer"
                  >
                    <div className="flex items-start justify-between mb-4">
                      <h3 className="text-xl font-bold text-slate-100 group-hover:text-indigo-400 transition-colors">
                        {vault.name}
                      </h3>
                      {getPrivilegeBadge(vault.userPrivilege)}
                    </div>
                    {vault.description && (
                      <p className="text-slate-400 text-sm mb-4 line-clamp-2">
                        {vault.description}
                      </p>
                    )}
                    {accessInfo.message && (
                      <div className="mb-4 p-3 bg-blue-500/10 border border-blue-500/30 rounded-lg">
                        <p className="text-blue-400 text-xs">
                          ℹ️ {accessInfo.message}
                        </p>
                      </div>
                    )}
                    <div className="flex items-center justify-between text-xs text-slate-500">
                      <span>
                        Created {new Date(vault.createdAt).toLocaleDateString()}
                      </span>
                      {vault.status === "Deleted" && (
                        <span className="text-red-400">Deleted</span>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </main>

      {/* Create Vault Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl max-h-[90vh] overflow-y-auto">
            <h2 className="text-2xl font-bold text-slate-100 mb-6">
              Create New Vault
            </h2>
            <form onSubmit={handleCreateVault}>
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Vault Name *
                </label>
                <input
                  type="text"
                  required
                  value={createForm.name}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, name: e.target.value })
                  }
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="e.g., Family Vault"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Description
                </label>
                <textarea
                  value={createForm.description}
                  onChange={(e) =>
                    setCreateForm({
                      ...createForm,
                      description: e.target.value,
                    })
                  }
                  rows={3}
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
                  placeholder="Optional description..."
                />
              </div>

              {/* Policy Section */}
              <div className="mb-4 pt-4 border-t border-slate-700/50">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Release Policy *
                </label>
                <select
                  value={createForm.policyType || "Immediate"}
                  onChange={(e) =>
                    setCreateForm({
                      ...createForm,
                      policyType: e.target
                        .value as CreateVaultRequest["policyType"],
                      releaseDate: undefined,
                      expiresAt: undefined,
                    })
                  }
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                >
                  <option value="Immediate">Immediate Release</option>
                  <option value="TimeBased">Time-Based Release</option>
                  <option value="ExpiryBased">Expiry-Based</option>
                  <option value="ManualRelease">Manual Release</option>
                </select>
              </div>

              {/* Time-Based Policy Configuration */}
              {createForm.policyType === "TimeBased" && (
                <div className="mb-4">
                  <label className="block text-sm font-semibold text-slate-300 mb-2">
                    Release Date *
                  </label>
                  <input
                    type="date"
                    required
                    value={
                      createForm.releaseDate
                        ? new Date(createForm.releaseDate)
                            .toISOString()
                            .slice(0, 10)
                        : ""
                    }
                    onChange={(e) =>
                      setCreateForm({
                        ...createForm,
                        releaseDate: e.target.value
                          ? new Date(
                              e.target.value + "T00:00:00Z"
                            ).toISOString()
                          : undefined,
                      })
                    }
                    min={new Date(Date.now() + 24 * 60 * 60 * 1000)
                      .toISOString()
                      .slice(0, 10)}
                    className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  />
                </div>
              )}

              {/* Expiry-Based Policy Configuration */}
              {createForm.policyType === "ExpiryBased" && (
                <div className="mb-4">
                  <label className="block text-sm font-semibold text-slate-300 mb-2">
                    Expires At *
                  </label>
                  <input
                    type="date"
                    required
                    value={
                      createForm.expiresAt
                        ? new Date(createForm.expiresAt)
                            .toISOString()
                            .slice(0, 10)
                        : ""
                    }
                    onChange={(e) =>
                      setCreateForm({
                        ...createForm,
                        expiresAt: e.target.value
                          ? new Date(
                              e.target.value + "T00:00:00Z"
                            ).toISOString()
                          : undefined,
                      })
                    }
                    min={new Date(Date.now() + 24 * 60 * 60 * 1000)
                      .toISOString()
                      .slice(0, 10)}
                    className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  />
                </div>
              )}

              <div className="mb-6"></div>
              <div className="flex space-x-3">
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateModal(false);
                    setCreateForm({
                      name: "",
                      description: "",
                      policyType: "Immediate",
                    });
                  }}
                  className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 px-4 py-3 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all duration-200 cursor-pointer"
                >
                  Create
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Access Denied Modal */}
      {showAccessDeniedModal && accessDeniedVault && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
            <div className="mb-6">
              <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-yellow-500/20 mb-4">
                <svg
                  className="h-8 w-8 text-yellow-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
              </div>
              <h2 className="text-2xl font-bold text-slate-100 mb-2 text-center">
                🔒 Vault Not Accessible
              </h2>
              <h3 className="text-xl font-semibold text-slate-300 mb-4 text-center">
                {accessDeniedVault.name}
              </h3>
              <div className="bg-yellow-500/10 border border-yellow-500/30 rounded-lg p-4">
                <p className="text-yellow-400 text-center">
                  {getVaultAccessInfo(accessDeniedVault).message ||
                    "This vault is not yet accessible due to its release policy."}
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => {
                setShowAccessDeniedModal(false);
                setAccessDeniedVault(null);
              }}
              className="w-full px-4 py-3 bg-indigo-500 text-white font-semibold rounded-lg hover:bg-indigo-600 transition-colors cursor-pointer"
            >
              Close
            </button>
          </div>
        </div>
      )}

      {/* Account Information Modal */}
      {showAccountModal && user && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
            <h2 className="text-2xl font-bold text-slate-100 mb-6">
              Account Information
            </h2>
            <div className="space-y-6">
              <div>
                <label className="block text-sm font-semibold text-slate-400 mb-2">
                  Username
                </label>
                <p className="text-lg text-slate-100 font-medium">
                  {user.username}
                </p>
              </div>
              <div>
                <label className="block text-sm font-semibold text-slate-400 mb-2">
                  Email Address
                </label>
                <p className="text-lg text-slate-100 font-medium">
                  {user.email}
                </p>
              </div>
            </div>
            <div className="mt-6">
              <button
                type="button"
                onClick={() => setShowAccountModal(false)}
                className="w-full px-4 py-3 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all duration-200 cursor-pointer"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

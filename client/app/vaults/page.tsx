"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";
import { apiClient, Vault } from "../lib/api";
import toast from "react-hot-toast";

export default function VaultsPage() {
  const { isLoading, isAuthenticated } = useAuth();
  const router = useRouter();
  const [vaults, setVaults] = useState<Vault[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState({ name: "", description: "" });

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
    } catch (error: any) {
      toast.error(error.message || "Failed to load vaults");
    } finally {
      setLoading(false);
    }
  };

  const handleCreateVault = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const vault = await apiClient.createVault(createForm);
      toast.success("Vault created successfully!");
      setShowCreateModal(false);
      setCreateForm({ name: "", description: "" });
      loadVaults();
    } catch (error: any) {
      toast.error(error.message || "Failed to create vault");
    }
  };

  const handleDeleteVault = async (id: number) => {
    if (!confirm("Are you sure you want to delete this vault? It can be restored within 30 days.")) {
      return;
    }
    try {
      await apiClient.deleteVault(id);
      toast.success("Vault deleted successfully");
      loadVaults();
    } catch (error: any) {
      toast.error(error.message || "Failed to delete vault");
    }
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
          <Link
            href="/dashboard"
            className="px-5 py-2.5 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm"
          >
            Dashboard
          </Link>
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
                You don't have any vaults yet.
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
              {vaults.map((vault) => (
                <Link
                  key={vault.id}
                  href={`/vaults/${vault.id}`}
                  className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 shadow-xl hover:border-indigo-500/50 transition-all duration-200 hover:shadow-2xl hover:shadow-indigo-500/10 group"
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
                  <div className="flex items-center justify-between text-xs text-slate-500">
                    <span>
                      Created {new Date(vault.createdAt).toLocaleDateString()}
                    </span>
                    {vault.status === "Deleted" && (
                      <span className="text-red-400">Deleted</span>
                    )}
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </main>

      {/* Create Vault Modal */}
      {showCreateModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
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
              <div className="mb-6">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Description
                </label>
                <textarea
                  value={createForm.description}
                  onChange={(e) =>
                    setCreateForm({ ...createForm, description: e.target.value })
                  }
                  rows={3}
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
                  placeholder="Optional description..."
                />
              </div>
              <div className="flex space-x-3">
                <button
                  type="button"
                  onClick={() => {
                    setShowCreateModal(false);
                    setCreateForm({ name: "", description: "" });
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
    </div>
  );
}


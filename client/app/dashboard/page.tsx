"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";

export default function DashboardPage() {
  const { user, isLoading, isAuthenticated, logout } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  const handleLogout = async () => {
    await logout();
  };

  if (isLoading) {
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
          <p className="mt-4 text-slate-400">Loading</p>
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
        <Link href="/" className="group">
          <span className="text-2xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>
        <div className="flex items-center space-x-4">
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
        <div className="max-w-4xl mx-auto">
          <div className="mb-8">
            <h1 className="text-4xl font-bold text-slate-100 mb-2">
              Welcome to your Dashboard
            </h1>
            <p className="text-slate-400">
              Manage your secure vault and settings
            </p>
          </div>

          {/* User Info Card */}
          <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-8 border border-slate-700/50 shadow-2xl mb-6">
            <h2 className="text-2xl font-bold text-slate-100 mb-6">
              Account Information
            </h2>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-slate-400 mb-1">
                  Username
                </label>
                <p className="text-lg text-slate-100 font-medium">
                  {user.username}
                </p>
              </div>
              <div>
                <label className="block text-sm font-semibold text-slate-400 mb-1">
                  Email Address
                </label>
                <p className="text-lg text-slate-100 font-medium">
                  {user.email}
                </p>
              </div>
            </div>
          </div>

          {/* Placeholder for future features */}
          <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-8 border border-slate-700/50 shadow-2xl">
            <h2 className="text-2xl font-bold text-slate-100 mb-4">
              Your Vaults
            </h2>
            <p className="text-slate-400">
              Vault management features will be available soon.
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}

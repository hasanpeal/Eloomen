"use client";

import { useState, useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { apiClient } from "../lib/api";
import { useAuth } from "../contexts/AuthContext";
import toast from "react-hot-toast";

function VerifyDeviceContent() {
  const [code, setCode] = useState("");
  const [usernameOrEmail, setUsernameOrEmail] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setUser } = useAuth();

  useEffect(() => {
    const usernameOrEmailParam = searchParams.get("usernameOrEmail");
    if (usernameOrEmailParam) {
      setUsernameOrEmail(usernameOrEmailParam);
    }
  }, [searchParams]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    if (!usernameOrEmail) {
      setError("Username or email is required");
      setIsLoading(false);
      return;
    }

    try {
      const response = await apiClient.verifyDevice(usernameOrEmail, code);

      if (response.token && response.userName && response.email) {
        // Update auth context with user info directly from response
        // The token is already stored by apiClient
        setUser({
          username: response.userName,
          email: response.email,
        });
        setSuccess(true);
        toast.success("Device verified successfully.");
        router.push("/dashboard");
      } else {
        setError(response.message || "Verification failed. Please try again.");
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : "Verification failed. Please try again.";
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 flex items-center justify-center px-4 py-12 relative overflow-hidden">
        <div className="w-full max-w-md relative z-10">
          <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-8 border border-slate-700/50 shadow-2xl text-center">
            <div className="w-16 h-16 bg-green-500/20 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg
                className="w-8 h-8 text-green-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-slate-100 mb-2">
              Device Verified!
            </h2>
            <p className="text-slate-400">
              Your device has been successfully verified.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 flex items-center justify-center px-4 py-12 relative overflow-hidden">
      {/* Animated background elements */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-indigo-500/10 rounded-full blur-3xl animate-pulse" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-500/10 rounded-full blur-3xl animate-pulse delay-1000" />
      </div>

      <div className="w-full max-w-md relative z-10">
        {/* Logo and Title */}
        <div className="text-center mb-8">
          <Link href="/" className="inline-block mb-6 group">
            <span className="text-2xl md:text-4xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
              Eloomen
            </span>
          </Link>
          <h1 className="text-2xl md:text-4xl font-bold text-slate-100 mb-2">
            Verify Your Device
          </h1>
          <p className="text-slate-400 text-lg">
            Enter the verification code sent to your email
          </p>
        </div>

        {/* Verification Form */}
        <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-8 border border-slate-700/50 shadow-2xl">
          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="bg-red-900/20 border border-red-800 rounded-xl p-4">
                <p className="text-sm text-red-400 font-medium">{error}</p>
              </div>
            )}

            {usernameOrEmail && (
              <div className="mb-4 p-3 bg-slate-900/50 rounded-lg border border-slate-700/50">
                <p className="text-xs text-slate-400 mb-1">
                  Verifying device for:
                </p>
                <p className="text-sm font-medium text-slate-200">
                  {usernameOrEmail}
                </p>
              </div>
            )}

            <div>
              <label
                htmlFor="code"
                className="block text-sm font-semibold text-slate-300 mb-2"
              >
                Verification Code
              </label>
              <input
                type="text"
                id="code"
                value={code}
                onChange={(e) =>
                  setCode(e.target.value.replace(/\D/g, "").slice(0, 6))
                }
                required
                maxLength={6}
                className="w-full px-4 py-3.5 bg-slate-900/80 backdrop-blur-sm border border-slate-600 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all text-slate-100 font-medium placeholder:text-slate-500 text-center text-2xl tracking-widest"
                placeholder="000000"
              />
              <p className="mt-2 text-xs text-slate-500 text-center">
                Enter the 6-digit code from your email
              </p>
            </div>

            {!usernameOrEmail && (
              <div className="bg-yellow-900/20 border border-yellow-800 rounded-xl p-4">
                <p className="text-sm text-yellow-400 font-medium">
                  Please go back to login and try again.
                </p>
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading || code.length !== 6 || !usernameOrEmail}
              className="w-full py-3.5 px-4 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-xl font-bold text-lg hover:shadow-2xl transition-all shadow-xl hover:shadow-indigo-500/50 transform hover:-translate-y-0.5 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none cursor-pointer"
            >
              {isLoading ? (
                <span className="flex items-center justify-center">
                  <svg
                    className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
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
                  Verifying
                </span>
              ) : (
                "Verify Device"
              )}
            </button>
          </form>

          <div className="mt-6 text-center">
            <Link
              href="/login"
              className="text-sm text-slate-400 hover:text-slate-300 transition-colors"
            >
              Back to login
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function VerifyDevicePage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 flex items-center justify-center px-4 py-12 relative overflow-hidden">
          {/* Animated background elements */}
          <div className="absolute inset-0 overflow-hidden pointer-events-none">
            <div className="absolute -top-40 -right-40 w-80 h-80 bg-indigo-500/10 rounded-full blur-3xl animate-pulse" />
            <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-500/10 rounded-full blur-3xl animate-pulse delay-1000" />
          </div>
          <div className="text-center relative z-10">
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
          </div>
        </div>
      }
    >
      <VerifyDeviceContent />
    </Suspense>
  );
}

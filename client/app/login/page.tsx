"use client";

import { useState, useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";
import toast from "react-hot-toast";

function LoginContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { isLoading: authLoading, isAuthenticated, login } = useAuth();
  const inviteEmail = searchParams.get("email");
  const inviteToken = searchParams.get("token");
  const [formData, setFormData] = useState({
    usernameOrEmail: inviteEmail || "",
    password: "",
  });
  const [rememberMe, setRememberMe] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  // Redirect to dashboard if already authenticated (unless there's an invite token)
  useEffect(() => {
    if (!authLoading && isAuthenticated && !inviteToken) {
      router.push("/dashboard");
    }
  }, [authLoading, isAuthenticated, inviteToken, router]);

  // Show loading state while checking auth
  if (authLoading) {
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
        </div>
      </div>
    );
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    setError("");
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");

    try {
      const result = await login(
        formData.usernameOrEmail,
        formData.password,
        rememberMe,
        inviteToken || undefined
      );

      if (result.success) {
        toast.success("Login successful");
        // If invite was accepted, go to vaults, otherwise dashboard
        router.push("/dashboard");
      } else if (result.requiresVerification) {
        // Use verificationType from backend response
        if (result.verificationType === "Device") {
          const redirectUrl = `/verify-device?usernameOrEmail=${encodeURIComponent(
            formData.usernameOrEmail
          )}`;
          if (inviteToken) {
            router.push(`${redirectUrl}&token=${encodeURIComponent(inviteToken)}`);
          } else {
            router.push(redirectUrl);
          }
        } else if (result.verificationType === "Email") {
          const redirectUrl = `/verify-email?email=${encodeURIComponent(
            formData.usernameOrEmail
          )}`;
          if (inviteToken) {
            router.push(`${redirectUrl}&token=${encodeURIComponent(inviteToken)}`);
          } else {
            router.push(redirectUrl);
          }
        } else {
          setError(result.message || "Verification required");
        }
      } else {
        setError(result.message || "Login failed");
      }
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : "An error occurred";
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

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
            Welcome back
          </h1>
          <p className="text-slate-400 text-lg">
            Sign in to access your secure vault
          </p>
        </div>

        {/* Login Form */}
        <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-8 border border-slate-700/50 shadow-2xl">
          <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
              <div className="bg-red-900/20 border border-red-800 rounded-xl p-4">
                <p className="text-sm text-red-400 font-medium">{error}</p>
              </div>
            )}

            <div>
              <label
                htmlFor="usernameOrEmail"
                className="block text-sm font-semibold text-slate-300 mb-2"
              >
                Username or Email
                {inviteEmail && (
                  <span className="ml-2 text-xs text-indigo-400">(from invite)</span>
                )}
              </label>
              <input
                type="text"
                id="usernameOrEmail"
                name="usernameOrEmail"
                autoComplete="username"
                value={formData.usernameOrEmail}
                onChange={handleChange}
                required
                disabled={!!inviteEmail}
                className="w-full px-4 py-3.5 bg-slate-900/80 backdrop-blur-sm border border-slate-600 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all text-slate-100 font-medium placeholder:text-slate-500 disabled:opacity-60 disabled:cursor-not-allowed"
                placeholder="Enter your username or email"
              />
              {inviteEmail && (
                <p className="mt-1 text-xs text-slate-400">
                  This email is from your vault invitation and cannot be changed.
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="password"
                className="block text-sm font-semibold text-slate-300 mb-2"
              >
                Password
              </label>
              <div className="relative">
                <input
                  type={showPassword ? "text" : "password"}
                  id="password"
                  name="password"
                  autoComplete="current-password"
                  value={formData.password}
                  onChange={handleChange}
                  required
                  className="w-full px-4 py-3.5 bg-slate-900/80 backdrop-blur-sm border border-slate-600 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all text-slate-100 pr-12 font-medium placeholder:text-slate-500"
                  placeholder="Enter your password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-200 transition-colors cursor-pointer"
                >
                  {showPassword ? (
                    <svg
                      className="w-5 h-5"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"
                      />
                    </svg>
                  ) : (
                    <svg
                      className="w-5 h-5"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      />
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                      />
                    </svg>
                  )}
                </button>
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <input
                  id="remember-me"
                  name="remember-me"
                  type="checkbox"
                  checked={rememberMe}
                  onChange={(e) => setRememberMe(e.target.checked)}
                  className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-slate-600 rounded"
                />
                <label
                  htmlFor="remember-me"
                  className="ml-2 block text-sm text-slate-300 font-medium"
                >
                  Remember me
                </label>
              </div>
              <Link
                href="/forgot-password"
                className="text-sm font-semibold text-indigo-400 hover:text-indigo-300 transition-colors"
              >
                Forgot password?
              </Link>
            </div>

            <button
              type="submit"
              disabled={isLoading}
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
                  Signing in
                </span>
              ) : (
                "Sign In"
              )}
            </button>
          </form>

          <div className="mt-6 text-center">
            <p className="text-sm text-slate-400">
              Don&apos;t have an account?{" "}
              <Link
                href="/signup"
                className="font-semibold text-indigo-400 hover:text-indigo-300 transition-colors"
              >
                Sign up
              </Link>
            </p>
          </div>
        </div>

        {/* Security Notice */}
        <div className="mt-6 text-center">
          <p className="text-xs text-slate-500 flex items-center justify-center space-x-1">
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
              />
            </svg>
            <span>Your data is encrypted and secure</span>
          </p>
        </div>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense
      fallback={
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
      }
    >
      <LoginContent />
    </Suspense>
  );
}

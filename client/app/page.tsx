"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "./contexts/AuthContext";
import ContactModal from "./components/ContactModal";

// SEO: This is a client component, but metadata is handled in layout.tsx
// The content below is optimized for search engines with semantic HTML

export default function Home() {
  const { isLoading, isAuthenticated } = useAuth();
  const router = useRouter();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [showContactModal, setShowContactModal] = useState(false);

  // Redirect to dashboard if already authenticated
  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      router.push("/dashboard");
    }
  }, [isLoading, isAuthenticated, router]);

  // Show loading state while checking auth
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
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 relative overflow-hidden">
      {/* Animated background elements */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-indigo-500/10 rounded-full blur-3xl animate-pulse" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-500/10 rounded-full blur-3xl animate-pulse delay-1000" />
      </div>

      {/* Navigation */}
      <nav
        className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10"
        role="navigation"
        aria-label="Main navigation"
      >
        <Link href="/" className="group" aria-label="Eloomen Home">
          <span className="text-xl md:text-3xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>

        {/* Desktop Navigation */}
        <div className="hidden md:flex items-center space-x-4">
          <Link
            href="/about"
            className="px-5 py-2.5 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm cursor-pointer"
            aria-label="Learn about Eloomen"
          >
            About Us
          </Link>
          <button
            onClick={() => setShowContactModal(true)}
            className="px-6 py-2.5 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-lg font-semibold hover:shadow-xl transition-all shadow-lg hover:shadow-indigo-500/50 transform hover:-translate-y-0.5 cursor-pointer"
            aria-label="Contact us"
          >
            Contact
          </button>
        </div>

        {/* Mobile Menu Button */}
        <button
          onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          className="md:hidden p-2 text-slate-300 hover:text-indigo-400 transition-colors cursor-pointer"
          aria-label="Toggle menu"
          aria-expanded={isMobileMenuOpen}
        >
          {isMobileMenuOpen ? (
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          ) : (
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 6h16M4 12h16M4 18h16"
              />
            </svg>
          )}
        </button>

        {/* Mobile Menu */}
        {isMobileMenuOpen && (
          <div className="absolute top-full left-0 right-0 mt-2 mx-6 bg-slate-800/95 backdrop-blur-md rounded-xl border border-slate-700/50 shadow-2xl md:hidden z-50">
            <div className="flex flex-col p-4 space-y-2">
              <Link
                href="/about"
                onClick={() => setIsMobileMenuOpen(false)}
                className="px-5 py-3 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-700/50 cursor-pointer text-center"
                aria-label="Learn about Eloomen"
              >
                About Us
              </Link>
              <button
                onClick={() => {
                  setShowContactModal(true);
                  setIsMobileMenuOpen(false);
                }}
                className="px-6 py-3 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-lg font-semibold hover:shadow-xl transition-all shadow-lg hover:shadow-indigo-500/50 cursor-pointer text-center"
                aria-label="Contact us"
              >
                Contact
              </button>
            </div>
          </div>
        )}
      </nav>

      {/* Hero Section */}
      <main className="relative container mx-auto px-6 py-20 z-0">
        <div className="max-w-5xl mx-auto text-center">
          <div className="inline-block mb-6">
            <span className="px-4 py-2 bg-indigo-900/30 text-indigo-300 rounded-full text-sm font-semibold border border-indigo-800">
              ✨ Secure • Private • Trusted
            </span>
          </div>
          <h1 className="text-3xl md:text-6xl lg:text-7xl font-bold mb-8 bg-gradient-to-r from-slate-100 via-indigo-100 to-purple-100 bg-clip-text text-transparent leading-tight tracking-tight">
            Secure your digital life.
            <br />
            <span className="bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent">
              Share it on your terms.
            </span>
          </h1>
          <p className="text-md md:text-xl text-slate-400 mb-12 max-w-3xl mx-auto leading-relaxed font-light">
            A secure, relationship-based digital vault platform that allows you
            to store, organize, and share sensitive data with specific people or
            groups immediately, conditionally, or at a future time.
          </p>

          <div className="flex flex-row gap-4 justify-center mb-20">
            <Link
              href="/signup"
              className="px-8 py-4 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-xl font-semibold text-lg hover:shadow-2xl transition-all shadow-xl hover:shadow-indigo-500/50 transform hover:-translate-y-1 cursor-pointer"
              aria-label="Sign up for Eloomen"
            >
              Sign Up
            </Link>
            <Link
              href="/login"
              className="px-8 py-4 bg-slate-800/80 backdrop-blur-sm text-slate-100 rounded-xl font-semibold text-lg border-2 border-slate-700 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl cursor-pointer"
              aria-label="Sign in to Eloomen"
            >
              Sign In
            </Link>
          </div>

          {/* Features Grid */}
          <section
            className="grid md:grid-cols-2 lg:grid-cols-4 gap-6 mt-20"
            aria-label="Key Features"
          >
            <article className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                  />
                </svg>
              </div>
              <h2 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Secure Vaults
              </h2>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Organize your data into custom vaults like Family, Medical,
                Legal, and more.
              </p>
            </article>

            <article className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                  />
                </svg>
              </div>
              <h2 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Dynamic Groups
              </h2>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Create fully configurable relationship groups for flexible
                sharing.
              </p>
            </article>

            <article className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                  />
                </svg>
              </div>
              <h2 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Time-Based Access
              </h2>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Set policies for delayed releases, inactivity triggers, and
                expiry dates.
              </p>
            </article>

            <article className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
              </div>
              <h2 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Any Data Type
              </h2>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Store documents, passwords, crypto wallets, notes, links, and
                more.
              </p>
            </article>
          </section>

          {/* Use Cases Section */}
          <section className="mt-24 mb-16" aria-label="Use Cases">
            <h2 className="text-2xl md:text-4xl font-bold mb-12 text-slate-100">
              Built for Real-World Scenarios
            </h2>
            <div className="grid md:grid-cols-3 gap-6">
              <article className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl">
                <h2 className="font-bold text-xl mb-3 text-slate-100">
                  For Families
                </h2>
                <p className="text-slate-400 leading-relaxed text-sm">
                  Parents can securely share important documents and information
                  with children.
                </p>
              </article>
              <article className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl">
                <h2 className="font-bold text-xl mb-3 text-slate-100">
                  For Partners
                </h2>
                <p className="text-slate-400 leading-relaxed text-sm">
                  Spouses can share subscriptions, estate information, and
                  personal data with conditional access.
                </p>
              </article>
              <article className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl">
                <h2 className="font-bold text-xl mb-3 text-slate-100">
                  For Professionals
                </h2>
                <p className="text-slate-400 leading-relaxed text-sm">
                  Patients, lawyers, and caregivers can securely access critical
                  information when needed.
                </p>
              </article>
            </div>
          </section>
        </div>
      </main>

      {/* Footer */}
      <footer
        className="relative border-t border-slate-800/50 mt-20 backdrop-blur-sm bg-slate-900/30"
        role="contentinfo"
      >
        <div className="container mx-auto px-6 py-8">
          <div className="flex flex-row justify-center">
            <p className="text-slate-400 text-sm">
              © 2026 Eloomen. All rights reserved.
            </p>
          </div>
        </div>
      </footer>

      {/* Contact Modal */}
      <ContactModal
        isOpen={showContactModal}
        onClose={() => setShowContactModal(false)}
        isPublic={true}
      />
    </div>
  );
}

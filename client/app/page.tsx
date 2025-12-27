import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 relative overflow-hidden">
      {/* Animated background elements */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-indigo-500/10 rounded-full blur-3xl animate-pulse" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-500/10 rounded-full blur-3xl animate-pulse delay-1000" />
      </div>

      {/* Navigation */}
      <nav className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10">
        <Link href="/" className="group">
          <span className="text-3xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>
        <div className="flex items-center space-x-4">
          <Link
            href="/login"
            className="px-5 py-2.5 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm"
          >
            Sign In
          </Link>
          <Link
            href="/signup"
            className="px-6 py-2.5 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-lg font-semibold hover:shadow-xl transition-all shadow-lg hover:shadow-indigo-500/50 transform hover:-translate-y-0.5"
          >
            Get Started
          </Link>
        </div>
      </nav>

      {/* Hero Section */}
      <main className="relative container mx-auto px-6 py-20 z-10">
        <div className="max-w-5xl mx-auto text-center">
          <div className="inline-block mb-6">
            <span className="px-4 py-2 bg-indigo-900/30 text-indigo-300 rounded-full text-sm font-semibold border border-indigo-800">
              ✨ Secure • Private • Trusted
            </span>
          </div>
          <h1 className="text-5xl md:text-6xl lg:text-7xl font-bold mb-8 bg-gradient-to-r from-slate-100 via-indigo-100 to-purple-100 bg-clip-text text-transparent leading-tight tracking-tight">
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

          <div className="flex flex-col sm:flex-row gap-4 justify-center mb-20">
            <Link
              href="/signup"
              className="px-8 py-4 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-xl font-semibold text-lg hover:shadow-2xl transition-all shadow-xl hover:shadow-indigo-500/50 transform hover:-translate-y-1"
            >
              Sign Up
            </Link>
            <Link
              href="/login"
              className="px-8 py-4 bg-slate-800/80 backdrop-blur-sm text-slate-100 rounded-xl font-semibold text-lg border-2 border-slate-700 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl"
            >
              Sign In
            </Link>
          </div>

          {/* Features Grid */}
          <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-6 mt-20">
            <div className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
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
              </div>
              <h3 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Secure Vaults
              </h3>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Organize your data into custom vaults like Family, Medical,
                Legal, and more.
              </p>
            </div>

            <div className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                  />
                </svg>
              </div>
              <h3 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Dynamic Groups
              </h3>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Create fully configurable relationship groups for flexible
                sharing.
              </p>
            </div>

            <div className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                  />
                </svg>
              </div>
              <h3 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Time-Based Access
              </h3>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Set policies for delayed releases, inactivity triggers, and
                expiry dates.
              </p>
            </div>

            <div className="group bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-2xl hover:-translate-y-1">
              <div className="w-14 h-14 bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 rounded-xl flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg">
                <svg
                  className="w-7 h-7 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
              </div>
              <h3 className="text-lg font-bold mb-2 text-slate-100 text-left">
                Any Data Type
              </h3>
              <p className="text-slate-400 text-sm leading-relaxed text-left">
                Store documents, passwords, crypto wallets, notes, links, and
                more.
              </p>
            </div>
          </div>

          {/* Use Cases Section */}
          <div className="mt-24 mb-16">
            <h2 className="text-4xl md:text-5xl font-bold mb-12 text-slate-100">
              Built for Real-World Scenarios
            </h2>
            <div className="grid md:grid-cols-3 gap-6">
              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl">
                <h3 className="font-bold text-xl mb-3 text-slate-100">
                  For Families
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  Parents can securely share important documents and information
                  with children, accessible at the right time.
                </p>
              </div>
              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl">
                <h3 className="font-bold text-xl mb-3 text-slate-100">
                  For Partners
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  Spouses can share subscriptions, estate information, and
                  personal data with conditional access.
                </p>
              </div>
              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl">
                <h3 className="font-bold text-xl mb-3 text-slate-100">
                  For Professionals
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  Patients, lawyers, and caregivers can securely access critical
                  information when needed.
                </p>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="relative border-t border-slate-800/50 mt-20 backdrop-blur-sm bg-slate-900/30">
        <div className="container mx-auto px-6 py-8">
          <div className="flex flex-col md:flex-row justify-between items-center">
            <div className="mb-4 md:mb-0">
              <span className="text-xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent">
                Eloomen
              </span>
            </div>
            <p className="text-slate-400 text-sm">
              © 2024 Eloomen. Secure your digital life. Share it on your terms.
            </p>
          </div>
        </div>
      </footer>
    </div>
  );
}

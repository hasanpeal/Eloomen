"use client";

import Link from "next/link";
import { Lock, Shield, Users, Clock, FileText, Key, Eye, Bell } from "lucide-react";

export default function AboutPage() {
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

        <div className="flex items-center space-x-4">
          <Link
            href="/login"
            className="px-5 py-2.5 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm cursor-pointer"
            aria-label="Sign in to your account"
          >
            Sign In
          </Link>
          <Link
            href="/signup"
            className="px-6 py-2.5 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-lg font-semibold hover:shadow-xl transition-all shadow-lg hover:shadow-indigo-500/50 transform hover:-translate-y-0.5 cursor-pointer"
            aria-label="Create a new account"
          >
            Get Started
          </Link>
        </div>
      </nav>

      {/* Main Content */}
      <main className="relative container mx-auto px-6 py-12 z-0">
        <div className="max-w-5xl mx-auto">
          {/* Hero Section */}
          <div className="text-center mb-16">
            <h1 className="text-4xl md:text-6xl font-bold mb-6 bg-gradient-to-r from-slate-100 via-indigo-100 to-purple-100 bg-clip-text text-transparent">
              About Eloomen
            </h1>
            <p className="text-lg md:text-xl text-slate-400 max-w-3xl mx-auto leading-relaxed">
              A secure, relationship-based digital vault platform designed to help you
              protect and share your most important information with the people who matter most.
            </p>
          </div>

          {/* Mission Section */}
          <section className="mb-16">
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 md:p-12 border border-slate-700/50 shadow-xl">
              <h2 className="text-2xl md:text-3xl font-bold mb-4 text-slate-100">
                Our Mission
              </h2>
              <p className="text-slate-300 leading-relaxed text-lg">
                Eloomen was built to solve a critical real-world problem: how to securely
                share sensitive information with specific people, either immediately or at
                a future time. Whether you&apos;re planning your digital estate, sharing important
                documents with family, or managing access to critical information, Eloomen
                gives you complete control over who sees what and when.
              </p>
            </div>
          </section>

          {/* Security Features */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold mb-8 text-slate-100 text-center">
              Security & Privacy
            </h2>
            <div className="grid md:grid-cols-2 gap-6">
              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center mb-4">
                  <Lock className="w-6 h-6 text-white" />
                </div>
                <h3 className="text-xl font-bold mb-3 text-slate-100">
                  End-to-End Encryption
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  All sensitive data, including passwords, crypto wallet keys, and personal
                  notes, are encrypted using industry-standard AES-256 encryption before
                  being stored. Your data is encrypted on your device before transmission,
                  ensuring only you and authorized recipients can access it.
                </p>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-600 rounded-xl flex items-center justify-center mb-4">
                  <Shield className="w-6 h-6 text-white" />
                </div>
                <h3 className="text-xl font-bold mb-3 text-slate-100">
                  Multi-Layer Security
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  We employ multiple layers of security including secure authentication,
                  device verification, role-based access control, and comprehensive audit
                  logging. Every action is tracked and logged for your security and peace
                  of mind.
                </p>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center mb-4">
                  <Eye className="w-6 h-6 text-white" />
                </div>
                <h3 className="text-xl font-bold mb-3 text-slate-100">
                  Granular Permissions
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  Control access at both the vault and item level. Set different permissions
                  for different people - some can view, others can edit. You have complete
                  control over who sees what information.
                </p>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-600 rounded-xl flex items-center justify-center mb-4">
                  <Key className="w-6 h-6 text-white" />
                </div>
                <h3 className="text-xl font-bold mb-3 text-slate-100">
                  Secure Authentication
                </h3>
                <p className="text-slate-400 leading-relaxed">
                  Advanced authentication system with device verification, secure token
                  management, and automatic session handling. New devices require verification
                  to ensure your account stays secure.
                </p>
              </div>
            </div>
          </section>

          {/* Key Features */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold mb-8 text-slate-100 text-center">
              Key Features
            </h2>
            <div className="space-y-6">
              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <Users className="w-6 h-6 text-white" />
                  </div>
                  <div>
                    <h3 className="text-xl font-bold mb-2 text-slate-100">
                      Relationship-Based Sharing
                    </h3>
                    <p className="text-slate-400 leading-relaxed">
                      Create custom groups and share vaults with specific people. Manage
                      relationships dynamically - add or remove members as needed, with
                      different privilege levels (Owner, Admin, Member) for fine-grained
                      control.
                    </p>
                  </div>
                </div>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <Clock className="w-6 h-6 text-white" />
                  </div>
                  <div>
                    <h3 className="text-xl font-bold mb-2 text-slate-100">
                      Time-Based Access Policies
                    </h3>
                    <p className="text-slate-400 leading-relaxed">
                      Set sophisticated access policies for your vaults. Choose immediate
                      access, schedule release for a future date, set expiration dates, or
                      require manual approval. Perfect for estate planning, conditional
                      sharing, and time-sensitive information.
                    </p>
                  </div>
                </div>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <FileText className="w-6 h-6 text-white" />
                  </div>
                  <div>
                    <h3 className="text-xl font-bold mb-2 text-slate-100">
                      Multiple Data Types
                    </h3>
                    <p className="text-slate-400 leading-relaxed">
                      Store and organize various types of sensitive information: documents,
                      passwords, cryptocurrency wallet information, personal notes, and links.
                      Everything is encrypted and organized in secure vaults.
                    </p>
                  </div>
                </div>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-6 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <div className="flex items-start gap-4">
                  <div className="w-12 h-12 bg-gradient-to-br from-purple-500 to-pink-600 rounded-xl flex items-center justify-center flex-shrink-0">
                    <Bell className="w-6 h-6 text-white" />
                  </div>
                  <div>
                    <h3 className="text-xl font-bold mb-2 text-slate-100">
                      Real-Time Notifications
                    </h3>
                    <p className="text-slate-400 leading-relaxed">
                      Stay informed with real-time notifications. Get notified when vaults
                      are released, items are edited, invites are sent or accepted, and
                      other important events. Both in-app and email notifications keep you
                      in the loop.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </section>

          {/* Use Cases */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold mb-8 text-slate-100 text-center">
              Perfect For
            </h2>
            <div className="grid md:grid-cols-3 gap-6">
              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <h3 className="text-xl font-bold mb-3 text-slate-100">Families</h3>
                <p className="text-slate-400 leading-relaxed">
                  Parents can securely share important documents, account information, and
                  estate details with children, with access policies that ensure information
                  is available when needed.
                </p>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <h3 className="text-xl font-bold mb-3 text-slate-100">Partners</h3>
                <p className="text-slate-400 leading-relaxed">
                  Spouses and partners can share subscriptions, financial information, and
                  personal data with conditional access, ensuring both parties have access
                  when necessary.
                </p>
              </div>

              <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-8 border border-slate-700/50 hover:border-indigo-600 transition-all shadow-lg">
                <h3 className="text-xl font-bold mb-3 text-slate-100">Professionals</h3>
                <p className="text-slate-400 leading-relaxed">
                  Patients, lawyers, caregivers, and other professionals can securely access
                  and share critical information with appropriate access controls and audit
                  trails.
                </p>
              </div>
            </div>
          </section>

          {/* CTA Section */}
          <section className="text-center">
            <div className="bg-gradient-to-r from-indigo-600/20 via-purple-600/20 to-pink-600/20 backdrop-blur-md rounded-2xl p-12 border border-indigo-500/30 shadow-xl">
              <h2 className="text-3xl font-bold mb-4 text-slate-100">
                Ready to Get Started?
              </h2>
              <p className="text-slate-300 mb-8 text-lg">
                Join Eloomen today and start securing your digital life.
              </p>
              <div className="flex flex-col sm:flex-row gap-4 justify-center">
                <Link
                  href="/signup"
                  className="px-8 py-4 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-xl font-semibold text-lg hover:shadow-2xl transition-all shadow-xl hover:shadow-indigo-500/50 transform hover:-translate-y-1 cursor-pointer"
                >
                  Create Account
                </Link>
                <Link
                  href="/login"
                  className="px-8 py-4 bg-slate-800/80 backdrop-blur-sm text-slate-100 rounded-xl font-semibold text-lg border-2 border-slate-700 hover:border-indigo-600 transition-all shadow-lg hover:shadow-xl cursor-pointer"
                >
                  Sign In
                </Link>
              </div>
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
    </div>
  );
}


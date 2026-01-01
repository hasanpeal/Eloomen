"use client";

import Link from "next/link";
import { ArrowLeft, Shield, Lock, Eye, Database, Key } from "lucide-react";

export default function PrivacyPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 relative overflow-hidden">
      {/* Animated background elements */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-80 h-80 bg-indigo-500/10 rounded-full blur-3xl animate-pulse" />
        <div className="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-500/10 rounded-full blur-3xl animate-pulse delay-1000" />
      </div>

      {/* Navigation */}
      <nav className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10 border-b border-slate-800/50">
        <Link href="/" className="group">
          <span className="text-xl md:text-3xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>
        <Link
          href="/signup"
          className="flex items-center gap-2 text-slate-300 hover:text-indigo-400 transition-colors cursor-pointer"
        >
          <ArrowLeft className="w-4 h-4" />
          <span className="hidden sm:inline">Back to Sign Up</span>
          <span className="sm:hidden">Back</span>
        </Link>
      </nav>

      {/* Content */}
      <main className="relative container mx-auto px-4 sm:px-6 py-8 sm:py-12 z-0">
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="text-center mb-12">
            <div className="inline-flex items-center justify-center w-16 h-16 bg-indigo-500/20 rounded-2xl mb-6">
              <Shield className="w-8 h-8 text-indigo-400" />
            </div>
            <h1 className="text-3xl md:text-5xl font-bold text-slate-100 mb-4">
              Privacy Policy
            </h1>
            <p className="text-slate-400 text-sm sm:text-base">
              Last updated: {new Date().toLocaleDateString("en-US", { year: "numeric", month: "long", day: "numeric" })}
            </p>
            <p className="mt-2 text-xs text-slate-500 italic">
              Eloomen is a side project committed to protecting your privacy
            </p>
          </div>

          {/* Privacy Content */}
          <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-6 sm:p-10 border border-slate-700/50 shadow-2xl space-y-8">
            {/* Introduction */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Eye className="w-6 h-6 text-indigo-400" />
                1. Introduction
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  At Eloomen, we take your privacy seriously. This Privacy Policy
                  explains how we collect, use, protect, and share your personal
                  information when you use our Service.
                </p>
                <p>
                  <strong className="text-slate-200">Side Project Notice:</strong>{" "}
                  Eloomen is a side project built for secure digital vault
                  management. We are committed to transparency about how we handle
                  your data.
                </p>
              </div>
            </section>

            {/* Information We Collect */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Database className="w-6 h-6 text-indigo-400" />
                2. Information We Collect
              </h2>
              <div className="text-slate-300 space-y-4 text-sm sm:text-base leading-relaxed">
                <div>
                  <p className="font-semibold text-slate-200 mb-2">
                    Account Information:
                  </p>
                  <ul className="list-disc list-inside space-y-1 ml-4">
                    <li>Username and email address</li>
                    <li>Hashed password (we never store plain text passwords)</li>
                    <li>Account creation and last login timestamps</li>
                  </ul>
                </div>

                <div>
                  <p className="font-semibold text-slate-200 mb-2">
                    Vault and Item Data:
                  </p>
                  <ul className="list-disc list-inside space-y-1 ml-4">
                    <li>
                      Vault names, descriptions, and policies (stored in plain
                      text for functionality)
                    </li>
                    <li>
                      <strong>Encrypted</strong> sensitive item data (passwords,
                      notes, crypto secrets, etc.)
                    </li>
                    <li>Item metadata (titles, types, creation dates)</li>
                    <li>Document files stored on Cloudflare R2</li>
                  </ul>
                </div>

                <div>
                  <p className="font-semibold text-slate-200 mb-2">
                    Usage and Activity Data:
                  </p>
                  <ul className="list-disc list-inside space-y-1 ml-4">
                    <li>Account activity logs (login, password changes, etc.)</li>
                    <li>Vault activity logs (item creation, member actions, etc.)</li>
                    <li>Device information for verification purposes</li>
                    <li>IP addresses and request metadata (for security)</li>
                  </ul>
                </div>
              </div>
            </section>

            {/* How We Use Your Information */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                3. How We Use Your Information
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>We use your information to:</p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>Provide, maintain, and improve the Service</li>
                  <li>Authenticate your identity and secure your account</li>
                  <li>Process your requests and transactions</li>
                  <li>Send verification emails and security notifications</li>
                  <li>Maintain activity logs for security and audit purposes</li>
                  <li>Enforce vault policies and access controls</li>
                  <li>Respond to your inquiries and support requests</li>
                </ul>
                <p>
                  <strong className="text-slate-200">We do NOT:</strong> Sell
                  your data, use it for advertising, or share it with third
                  parties except as described in this policy.
                </p>
              </div>
            </section>

            {/* Data Encryption and Security */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Key className="w-6 h-6 text-indigo-400" />
                4. Data Encryption and Security Measures
              </h2>
              <div className="bg-indigo-900/20 border border-indigo-800/50 rounded-xl p-6 mb-4">
                <h3 className="text-lg font-semibold text-indigo-300 mb-3">
                  Comprehensive Security Implementation
                </h3>
                <div className="text-slate-300 space-y-4 text-sm sm:text-base leading-relaxed">
                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      Encryption Standards:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        <strong>AES-256 Encryption:</strong> All sensitive vault
                        item data is encrypted using Advanced Encryption Standard
                        with 256-bit keys, which is the same standard used by
                        banks and government agencies
                      </li>
                      <li>
                        <strong>CBC Mode:</strong> Cipher Block Chaining ensures
                        that identical plaintext blocks produce different
                        ciphertext blocks
                      </li>
                      <li>
                        <strong>PKCS7 Padding:</strong> Standard padding scheme
                        for secure block encryption
                      </li>
                      <li>
                        <strong>SHA-256 Key Derivation:</strong> Encryption keys
                        are derived using Secure Hash Algorithm 256-bit, making
                        them computationally infeasible to reverse
                      </li>
                    </ul>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      Encryption Key Management:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        Each vault has a unique encryption key derived from: vault
                        ID + vault owner ID + secure server signing key
                      </li>
                      <li>
                        Keys are never stored in plain text or transmitted to
                        clients
                      </li>
                      <li>
                        Keys are derived on-demand during encryption/decryption
                        operations
                      </li>
                      <li>
                        All members of a vault use the same encryption key (derived
                        from vault owner) to ensure shared items can be decrypted
                        by authorized users
                      </li>
                    </ul>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      What Data is Encrypted:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        <strong>Password Items:</strong> Passwords, usernames,
                        website URLs, and password notes
                      </li>
                      <li>
                        <strong>Note Items:</strong> All note content and
                        formatting
                      </li>
                      <li>
                        <strong>Crypto Wallet Items:</strong> Seed phrases,
                        private keys, exchange credentials, and wallet notes
                      </li>
                      <li>
                        <strong>Link Items:</strong> Link notes and sensitive
                        metadata
                      </li>
                    </ul>
                    <p className="mt-2 text-slate-400 italic">
                      Note: Vault names, item titles, and metadata are stored in
                      plain text to enable search and organization functionality.
                    </p>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      Authentication Security:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        <strong>Password Hashing:</strong> User passwords are
                        hashed using ASP.NET Core Identity's PBKDF2 algorithm
                        with salt, making them impossible to recover even if our
                        database is compromised
                      </li>
                      <li>
                        <strong>JWT Tokens:</strong> Secure authentication using
                        JSON Web Tokens signed with HMAC-SHA512
                      </li>
                      <li>
                        <strong>Security Stamps:</strong> Token revocation system
                        that invalidates all sessions when password is changed
                      </li>
                      <li>
                        <strong>Device Verification:</strong> New devices require
                        email verification before access
                      </li>
                      <li>
                        <strong>Email Verification:</strong> Required for account
                        activation and security
                      </li>
                    </ul>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      Access Control:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        Vault-level policies control when data becomes accessible
                        (immediate, time-based, expiry-based, manual release)
                      </li>
                      <li>
                        Item-level permissions (View/Edit) for granular access
                        control
                      </li>
                      <li>
                        Role-based access control (Owner, Admin, Member) with
                        appropriate privileges
                      </li>
                      <li>
                        Owners always have Edit permission on all items in their
                        vaults
                      </li>
                    </ul>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      Infrastructure Security:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        Database connections use encrypted connections (SSL/TLS)
                      </li>
                      <li>
                        Documents stored on Cloudflare R2 with secure access
                        controls
                      </li>
                      <li>
                        API endpoints protected with authentication and
                        authorization
                      </li>
                      <li>
                        HTTPS encryption for all data in transit
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </section>

            {/* Data Sharing */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Lock className="w-6 h-6 text-indigo-400" />
                5. Data Sharing and Disclosure
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  <strong className="text-slate-200">We do not sell your data.</strong>{" "}
                  We may share your information only in the following
                  circumstances:
                </p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>
                    <strong>With Your Consent:</strong> When you explicitly
                    share vaults with other users through our invite system
                  </li>
                  <li>
                    <strong>Service Providers:</strong> With trusted third-party
                    services that help us operate (e.g., email delivery, cloud
                    storage) under strict confidentiality agreements
                  </li>
                  <li>
                    <strong>Legal Requirements:</strong> If required by law,
                    court order, or government regulation
                  </li>
                  <li>
                    <strong>Security:</strong> To protect our rights, prevent
                    fraud, or ensure user safety
                  </li>
                </ul>
              </div>
            </section>

            {/* Your Rights */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                6. Your Privacy Rights
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>You have the right to:</p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>
                    <strong>Access:</strong> View and download your data through
                    the Service
                  </li>
                  <li>
                    <strong>Modify:</strong> Update your account information and
                    vault contents
                  </li>
                  <li>
                    <strong>Delete:</strong> Delete items, vaults, or your entire
                    account at any time
                  </li>
                  <li>
                    <strong>Export:</strong> Access your data through the Service
                    interface
                  </li>
                  <li>
                    <strong>Revoke Access:</strong> Remove members from vaults or
                    revoke device access
                  </li>
                </ul>
                <p>
                  To exercise these rights, use the features available in your
                  account settings or contact us.
                </p>
              </div>
            </section>

            {/* Data Retention */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                7. Data Retention
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  We retain your data for as long as your account is active or as
                  needed to provide the Service. When you delete:
                </p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>
                    <strong>Items:</strong> Marked as deleted but may be retained
                    temporarily for recovery purposes
                  </li>
                  <li>
                    <strong>Vaults:</strong> Permanently deleted along with all
                    items and member relationships
                  </li>
                  <li>
                    <strong>Account:</strong> All your data is deleted, except
                    items in vaults you don't own (ownership transferred to vault
                    owner)
                  </li>
                  <li>
                    <strong>Activity Logs:</strong> Deleted when you delete your
                    account
                  </li>
                </ul>
              </div>
            </section>

            {/* Cookies and Tracking */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                8. Cookies and Tracking
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  We use minimal cookies and local storage for:
                </p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>
                    <strong>Authentication:</strong> Storing JWT tokens for session
                    management
                  </li>
                  <li>
                    <strong>Device Identification:</strong> Secure device IDs for
                    verification purposes
                  </li>
                  <li>
                    <strong>Preferences:</strong> User interface preferences
                  </li>
                </ul>
                <p>
                  We do not use tracking cookies, advertising cookies, or
                  third-party analytics that track your behavior across websites.
                </p>
              </div>
            </section>

            {/* Children's Privacy */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                9. Children's Privacy
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  Eloomen is not intended for users under 18 years of age. We do
                  not knowingly collect personal information from children. If you
                  believe we have collected information from a child, please
                  contact us immediately.
                </p>
              </div>
            </section>

            {/* Changes to Privacy Policy */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                10. Changes to This Privacy Policy
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  We may update this Privacy Policy from time to time. We will
                  notify you of material changes by updating the "Last updated"
                  date. Your continued use of the Service after changes
                  constitutes acceptance of the updated policy.
                </p>
              </div>
            </section>

            {/* Contact */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                11. Contact Us
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  If you have questions about this Privacy Policy or our data
                  practices, please contact us through the application or review
                  our{" "}
                  <Link
                    href="/terms"
                    className="text-indigo-400 hover:underline font-semibold"
                  >
                    Terms of Service
                  </Link>
                  .
                </p>
              </div>
            </section>

            {/* Security Best Practices */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                12. Security Best Practices for Users
              </h2>
              <div className="bg-slate-900/50 border border-slate-700/50 rounded-xl p-6">
                <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                  <p>
                    While we implement strong security measures, you play a
                    crucial role in protecting your data:
                  </p>
                  <ul className="list-disc list-inside space-y-2 ml-4">
                    <li>Use a strong, unique password for your account</li>
                    <li>Enable email verification and device verification</li>
                    <li>Regularly review your vault members and permissions</li>
                    <li>Be cautious when sharing vaults with others</li>
                    <li>Log out from shared or public devices</li>
                    <li>Keep your email account secure (used for verification)</li>
                    <li>Report suspicious activity immediately</li>
                  </ul>
                </div>
              </div>
            </section>
          </div>

          {/* Footer Actions */}
          <div className="mt-8 flex flex-col sm:flex-row gap-4 justify-center">
            <Link
              href="/signup"
              className="px-6 py-3 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-xl font-semibold hover:shadow-xl transition-all shadow-lg hover:shadow-indigo-500/50 text-center cursor-pointer"
            >
              Return to Sign Up
            </Link>
            <Link
              href="/terms"
              className="px-6 py-3 bg-slate-800/80 backdrop-blur-sm text-slate-100 rounded-xl font-semibold border-2 border-slate-700 hover:border-indigo-600 transition-all text-center cursor-pointer"
            >
              View Terms of Service
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}


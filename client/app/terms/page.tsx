"use client";

import Link from "next/link";
import { ArrowLeft, Shield, Lock, FileText, AlertTriangle } from "lucide-react";

export default function TermsPage() {
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
              <FileText className="w-8 h-8 text-indigo-400" />
            </div>
            <h1 className="text-3xl md:text-5xl font-bold text-slate-100 mb-4">
              Terms of Service
            </h1>
            <p className="text-slate-400 text-sm sm:text-base">
              Last updated: {new Date().toLocaleDateString("en-US", { year: "numeric", month: "long", day: "numeric" })}
            </p>
            <p className="mt-2 text-xs text-slate-500 italic">
              Eloomen is a side project built for secure digital vault management
            </p>
          </div>

          {/* Terms Content */}
          <div className="bg-slate-800/60 backdrop-blur-md rounded-3xl p-6 sm:p-10 border border-slate-700/50 shadow-2xl space-y-8">
            {/* Introduction */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Shield className="w-6 h-6 text-indigo-400" />
                1. Introduction
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  Welcome to Eloomen. These Terms of Service ("Terms") govern
                  your access to and use of Eloomen's digital vault platform
                  ("Service"). By creating an account or using our Service, you
                  agree to be bound by these Terms.
                </p>
                <p>
                  <strong className="text-slate-200">Important Note:</strong>{" "}
                  Eloomen is a side project developed for personal and
                  educational purposes. While we strive to provide a secure and
                  reliable service, please review these terms carefully.
                </p>
              </div>
            </section>

            {/* Acceptance of Terms */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Lock className="w-6 h-6 text-indigo-400" />
                2. Acceptance of Terms
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  By accessing or using Eloomen, you confirm that:
                </p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>You are at least 18 years old or have parental consent</li>
                  <li>You have the legal capacity to enter into these Terms</li>
                  <li>You will provide accurate and complete information</li>
                  <li>You are responsible for maintaining account security</li>
                </ul>
              </div>
            </section>

            {/* Service Description */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                3. Service Description
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  Eloomen provides a secure digital vault platform that allows
                  you to:
                </p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>Store sensitive information (passwords, documents, notes, etc.)</li>
                  <li>Organize data into customizable vaults</li>
                  <li>Share vaults with specific individuals or groups</li>
                  <li>Set time-based and conditional access policies</li>
                  <li>Manage item-level permissions within vaults</li>
                </ul>
              </div>
            </section>

            {/* Security and Encryption */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <Shield className="w-6 h-6 text-indigo-400" />
                4. Security and Data Encryption
              </h2>
              <div className="bg-indigo-900/20 border border-indigo-800/50 rounded-xl p-6 mb-4">
                <h3 className="text-lg font-semibold text-indigo-300 mb-3">
                  How We Protect Your Data
                </h3>
                <div className="text-slate-300 space-y-4 text-sm sm:text-base leading-relaxed">
                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      End-to-End Encryption:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        All sensitive vault item data is encrypted using{" "}
                        <strong>AES-256 encryption</strong> (Advanced Encryption
                        Standard with 256-bit keys)
                      </li>
                      <li>
                        Encryption uses <strong>CBC mode</strong> (Cipher Block
                        Chaining) with <strong>PKCS7 padding</strong>
                      </li>
                      <li>
                        Encryption keys are derived using{" "}
                        <strong>SHA-256 hashing</strong> from vault-specific
                        identifiers
                      </li>
                      <li>
                        Each vault has a unique encryption key based on the vault
                        owner, ensuring data isolation between vaults
                      </li>
                    </ul>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      What Gets Encrypted:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>Password fields (usernames, passwords, website URLs)</li>
                      <li>Note content and formatting</li>
                      <li>Crypto wallet secrets (seed phrases, private keys)</li>
                      <li>Link notes and sensitive metadata</li>
                    </ul>
                  </div>

                  <div>
                    <p className="font-semibold text-slate-200 mb-2">
                      Authentication Security:
                    </p>
                    <ul className="list-disc list-inside space-y-1 ml-4">
                      <li>
                        User passwords are hashed using ASP.NET Core Identity's
                        secure password hashing (PBKDF2)
                      </li>
                      <li>
                        JWT (JSON Web Tokens) with HMAC-SHA512 signatures for
                        secure authentication
                      </li>
                      <li>
                        Device verification system to prevent unauthorized access
                      </li>
                      <li>
                        Email verification required for account activation
                      </li>
                      <li>
                        Security stamps for token revocation on password changes
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
                        Role-based access (Owner, Admin, Member) with appropriate
                        privileges
                      </li>
                    </ul>
                  </div>

                  <div className="mt-4 p-4 bg-yellow-900/20 border border-yellow-800/50 rounded-lg">
                    <p className="text-yellow-300 text-sm">
                      <strong>Important:</strong> While we implement strong
                      encryption, you are responsible for keeping your account
                      credentials secure. We cannot decrypt your data if you lose
                      your account access.
                    </p>
                  </div>
                </div>
              </div>
            </section>

            {/* User Responsibilities */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                5. User Responsibilities
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>You agree to:</p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>
                    Maintain the confidentiality of your account credentials
                  </li>
                  <li>
                    Use the Service only for lawful purposes and in accordance
                    with these Terms
                  </li>
                  <li>
                    Not share your account with others or allow unauthorized
                    access
                  </li>
                  <li>
                    Notify us immediately of any unauthorized use of your account
                  </li>
                  <li>
                    Be responsible for all activities that occur under your
                    account
                  </li>
                  <li>
                    Ensure that any data you store complies with applicable laws
                    and regulations
                  </li>
                </ul>
              </div>
            </section>

            {/* Prohibited Uses */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4 flex items-center gap-2">
                <AlertTriangle className="w-6 h-6 text-red-400" />
                6. Prohibited Uses
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>You may not:</p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>Use the Service for any illegal or unauthorized purpose</li>
                  <li>
                    Attempt to gain unauthorized access to other users' vaults or
                    data
                  </li>
                  <li>
                    Interfere with or disrupt the Service or servers connected to
                    the Service
                  </li>
                  <li>
                    Transmit any viruses, malware, or malicious code
                  </li>
                  <li>
                    Reverse engineer, decompile, or attempt to extract the source
                    code
                  </li>
                  <li>
                    Use automated systems to access the Service without
                    permission
                  </li>
                </ul>
              </div>
            </section>

            {/* Data Storage and Retention */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                7. Data Storage and Retention
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  <strong className="text-slate-200">Storage:</strong> Your data
                  is stored on secure cloud infrastructure. Documents are stored
                  on Cloudflare R2, while encrypted metadata is stored in a
                  PostgreSQL database.
                </p>
                <p>
                  <strong className="text-slate-200">Retention:</strong> Your
                  data remains stored until you delete it or close your account.
                  When you delete an item or vault, it is marked as deleted but
                  may be retained for a period to allow recovery. Permanent
                  deletion occurs according to our data retention policies.
                </p>
                <p>
                  <strong className="text-slate-200">Account Deletion:</strong>{" "}
                  When you delete your account, all vaults you own will be
                  permanently deleted. Items you created in vaults owned by
                  others will have their ownership transferred to the vault owner.
                </p>
              </div>
            </section>

            {/* Disclaimer */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                8. Disclaimer of Warranties
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  <strong className="text-slate-200">Side Project Notice:</strong>{" "}
                  Eloomen is provided as a side project "as is" and "as
                  available" without warranties of any kind, either express or
                  implied. While we implement industry-standard security
                  measures, we cannot guarantee:
                </p>
                <ul className="list-disc list-inside space-y-2 ml-4">
                  <li>
                    Uninterrupted, secure, or error-free operation of the Service
                  </li>
                  <li>
                    That the Service will meet your specific requirements
                  </li>
                  <li>
                    That defects will be corrected or that the Service is free of
                    viruses or other harmful components
                  </li>
                </ul>
                <p>
                  You use the Service at your own risk and are responsible for
                  backing up important data.
                </p>
              </div>
            </section>

            {/* Limitation of Liability */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                9. Limitation of Liability
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  To the maximum extent permitted by law, Eloomen and its
                  developers shall not be liable for any indirect, incidental,
                  special, consequential, or punitive damages, including but not
                  limited to loss of profits, data, or other intangible losses,
                  resulting from your use of the Service.
                </p>
              </div>
            </section>

            {/* Changes to Terms */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                10. Changes to Terms
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  We reserve the right to modify these Terms at any time. We will
                  notify users of material changes by updating the "Last updated"
                  date at the top of this page. Your continued use of the Service
                  after changes constitutes acceptance of the new Terms.
                </p>
              </div>
            </section>

            {/* Contact */}
            <section>
              <h2 className="text-2xl font-bold text-slate-100 mb-4">
                11. Contact Information
              </h2>
              <div className="text-slate-300 space-y-3 text-sm sm:text-base leading-relaxed">
                <p>
                  If you have questions about these Terms, please contact us
                  through the application or review our{" "}
                  <Link
                    href="/privacy"
                    className="text-indigo-400 hover:underline font-semibold"
                  >
                    Privacy Policy
                  </Link>
                  .
                </p>
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
              href="/privacy"
              className="px-6 py-3 bg-slate-800/80 backdrop-blur-sm text-slate-100 rounded-xl font-semibold border-2 border-slate-700 hover:border-indigo-600 transition-all text-center cursor-pointer"
            >
              View Privacy Policy
            </Link>
          </div>
        </div>
      </main>
    </div>
  );
}


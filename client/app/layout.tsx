import type { Metadata } from "next";
import { Montserrat } from "next/font/google";
import "./globals.css";
import { AuthProvider } from "./contexts/AuthContext";
import { Toaster } from "react-hot-toast";

const montserrat = Montserrat({
  subsets: ["latin"],
  variable: "--font-montserrat",
  display: "swap",
});

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL || "https://eloomen.com";
const siteName = "Eloomen";
const siteDescription =
  "Secure your digital life. Share it on your terms. Eloomen is a secure, relationship-based digital vault platform for storing and sharing sensitive data with time-based and conditional access policies. Perfect for families, partners, and professionals.";

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default:
      "Eloomen - Secure Digital Vault Platform | Control Your Digital Life",
    template: "%s | Eloomen",
  },
  description: siteDescription,
  keywords: [
    "digital vault",
    "secure storage",
    "password manager",
    "document storage",
    "estate planning",
    "secure sharing",
    "encrypted vault",
    "time-based access",
    "conditional access",
    "family vault",
    "secure notes",
    "crypto wallet storage",
    "digital legacy",
    "secure data sharing",
    "relationship-based vault",
  ],
  authors: [{ name: "Eloomen Team" }],
  creator: "Eloomen",
  publisher: "Eloomen",
  formatDetection: {
    email: false,
    address: false,
    telephone: false,
  },
  openGraph: {
    type: "website",
    locale: "en_US",
    url: siteUrl,
    siteName: siteName,
    title: "Eloomen - Secure Digital Vault Platform",
    description: siteDescription,
    images: [
      {
        url: "/privacy.png",
        width: 512,
        height: 512,
        alt: "Eloomen Logo",
        type: "image/png",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "Eloomen - Secure Digital Vault Platform",
    description: siteDescription,
    images: ["/privacy.png"],
    creator: "@eloomen",
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      "max-video-preview": -1,
      "max-image-preview": "large",
      "max-snippet": -1,
    },
  },
  icons: {
    icon: [
      { url: "/icon.png", sizes: "any" },
      { url: "/privacy.png", sizes: "512x512", type: "image/png" },
      { url: "/icon-16x16.png", sizes: "16x16", type: "image/png" },
      { url: "/icon-32x32.png", sizes: "32x32", type: "image/png" },
      { url: "/icon-192x192.png", sizes: "192x192", type: "image/png" },
      { url: "/icon-512x512.png", sizes: "512x512", type: "image/png" },
    ],
    apple: [
      { url: "/apple-touch-icon.png", sizes: "180x180", type: "image/png" },
    ],
    shortcut: [{ url: "/privacy.png", type: "image/png" }],
  },
  manifest: "/site.webmanifest",
  alternates: {
    canonical: siteUrl,
  },
  category: "technology",
  classification: "Digital Vault Platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "SoftwareApplication",
    name: "Eloomen",
    applicationCategory: "SecurityApplication",
    operatingSystem: "Web",
    offers: {
      "@type": "Offer",
      price: "0",
      priceCurrency: "USD",
    },
    description: siteDescription,
    url: siteUrl,
    logo: `${siteUrl}/privacy.png`,
    image: `${siteUrl}/privacy.png`,
    author: {
      "@type": "Organization",
      name: "Eloomen",
    },
    featureList: [
      "Secure Digital Vault",
      "Time-Based Access Policies",
      "Encrypted Storage",
      "Document Management",
      "Password Management",
      "Crypto Wallet Storage",
      "Secure Notes",
      "Relationship-Based Sharing",
      "Audit Logging",
      "Device Verification",
    ],
    aggregateRating: {
      "@type": "AggregateRating",
      ratingValue: "5",
      ratingCount: "1",
    },
  };

  return (
    <html
      lang="en"
      className="dark bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50"
      suppressHydrationWarning
    >
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
      </head>
      <body
        className={`${montserrat.variable} font-sans antialiased bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50`}
        suppressHydrationWarning
      >
        <AuthProvider>{children}</AuthProvider>
        <Toaster position="top-center" toastOptions={{}} />
      </body>
    </html>
  );
}

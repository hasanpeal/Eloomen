import type { Metadata } from "next";

const siteUrl = "https://eloomen.com";

export const metadata: Metadata = {
  title: "Sign In",
  description:
    "Sign in to your Eloomen account to access your secure digital vault. Manage your encrypted documents, passwords, crypto wallets, and sensitive information with time-based access policies.",
  keywords: [
    "sign in",
    "login",
    "Eloomen login",
    "secure vault login",
    "digital vault access",
    "encrypted storage login",
  ],
  openGraph: {
    title: "Sign In | Eloomen",
    description:
      "Sign in to your Eloomen account to access your secure digital vault.",
    url: `${siteUrl}/login`,
    type: "website",
    images: [
      {
        url: "/privacy.png",
        width: 512,
        height: 512,
        alt: "Eloomen Logo",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "Sign In | Eloomen",
    description: "Sign in to your Eloomen account to access your secure digital vault.",
    images: ["/privacy.png"],
  },
  alternates: {
    canonical: `${siteUrl}/login`,
  },
  robots: {
    index: true,
    follow: true,
  },
};

export default function LoginLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}


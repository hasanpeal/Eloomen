import type { Metadata } from "next";

const siteUrl = "https://eloomen.com";

export const metadata: Metadata = {
  title: "Sign Up",
  description:
    "Create your free Eloomen account and start securing your digital life. Store and share sensitive data with time-based access policies. Perfect for families, partners, and professionals.",
  keywords: [
    "sign up",
    "create account",
    "Eloomen signup",
    "free digital vault",
    "secure storage signup",
    "encrypted vault registration",
    "digital estate planning",
  ],
  openGraph: {
    title: "Sign Up | Eloomen",
    description:
      "Create your free Eloomen account and start securing your digital life with encrypted storage and time-based access policies.",
    url: `${siteUrl}/signup`,
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
    title: "Sign Up | Eloomen",
    description:
      "Create your free Eloomen account and start securing your digital life.",
    images: ["/privacy.png"],
  },
  alternates: {
    canonical: `${siteUrl}/signup`,
  },
  robots: {
    index: true,
    follow: true,
  },
};

export default function SignupLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}


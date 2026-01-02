import type { Metadata } from "next";

const siteUrl = "https://eloomen.com";

export const metadata: Metadata = {
  title: "About Us",
  description:
    "Learn about Eloomen - a secure, relationship-based digital vault platform designed to help you protect and share your most important information with the people who matter most. Discover our mission, security features, and use cases.",
  keywords: [
    "about Eloomen",
    "digital vault platform",
    "secure data sharing",
    "encrypted storage",
    "estate planning",
    "family vault",
    "secure information sharing",
  ],
  openGraph: {
    title: "About Us | Eloomen",
    description:
      "Learn about Eloomen - a secure, relationship-based digital vault platform designed to help you protect and share your most important information.",
    url: `${siteUrl}/about`,
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
    title: "About Us | Eloomen",
    description:
      "Learn about Eloomen - a secure, relationship-based digital vault platform.",
    images: ["/privacy.png"],
  },
  alternates: {
    canonical: `${siteUrl}/about`,
  },
  robots: {
    index: true,
    follow: true,
  },
};

export default function AboutLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}


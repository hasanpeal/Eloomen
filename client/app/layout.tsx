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

export const metadata: Metadata = {
  title: "Eloomen - Control your digital life",
  description:
    "A secure, relationship-based digital vault platform for storing and sharing sensitive data with time-based and conditional access policies.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="dark bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50" suppressHydrationWarning>
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

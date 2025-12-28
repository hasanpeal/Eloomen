"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../contexts/AuthContext";
import { apiClient } from "../lib/api";
import toast from "react-hot-toast";

export default function AcceptInvitePage() {
  const { isLoading, isAuthenticated, user } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  const [processing, setProcessing] = useState(false);
  const [success, setSuccess] = useState(false);
  const [checkingInvite, setCheckingInvite] = useState(true);
  const [inviteInfo, setInviteInfo] = useState<{ email: string; userExists: boolean } | null>(null);

  useEffect(() => {
    if (!token) {
      setCheckingInvite(false);
      return;
    }

    // Get invite info to check email and validity
    const checkInvite = async () => {
      try {
        setCheckingInvite(true);
        const info = await apiClient.getInviteInfo(token);
        
        if (!info.isValid) {
          toast.error(info.errorMessage || "Invalid invite");
          setCheckingInvite(false);
          return;
        }

        setInviteInfo({ email: info.inviteeEmail, userExists: info.userExists });

        // If user is authenticated, check if email matches
        if (isAuthenticated && user) {
          if (user.email === info.inviteeEmail) {
            // Email matches, accept invite
            await handleAcceptInvite(token, info.inviteeEmail);
          } else {
            toast.error("This invite is for a different email address. Please log out and use the correct account.");
            setCheckingInvite(false);
          }
        } else {
          // User not authenticated, redirect based on whether user exists
          setCheckingInvite(false);
          if (info.userExists) {
            // User exists, redirect to login
            router.push(`/login?email=${encodeURIComponent(info.inviteeEmail)}&token=${encodeURIComponent(token)}`);
          } else {
            // User doesn't exist, redirect to signup
            router.push(`/signup?email=${encodeURIComponent(info.inviteeEmail)}&token=${encodeURIComponent(token)}`);
          }
        }
      } catch (error: any) {
        console.error("Error checking invite:", error);
        toast.error("Failed to validate invite");
        setCheckingInvite(false);
      }
    };

    if (!isLoading) {
      checkInvite();
    }
  }, [isLoading, isAuthenticated, token, user, router]);

  const handleAcceptInvite = async (inviteToken: string, inviteEmail: string) => {
    try {
      setProcessing(true);
      await apiClient.acceptInvite(inviteToken, inviteEmail);
      setSuccess(true);
      toast.success("Invite accepted successfully!");
      setTimeout(() => {
        router.push("/vaults");
      }, 2000);
    } catch (error: any) {
      toast.error(error.message || "Failed to accept invite");
      setProcessing(false);
    }
  };

  if (isLoading || checkingInvite || processing) {
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
          <p className="mt-4 text-slate-400">Processing invitation...</p>
        </div>
      </div>
    );
  }

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 flex items-center justify-center">
        <div className="text-center bg-slate-800/60 backdrop-blur-md rounded-3xl p-12 border border-slate-700/50 shadow-2xl max-w-md">
          <div className="mb-6">
            <svg
              className="h-16 w-16 text-green-400 mx-auto"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          </div>
          <h1 className="text-3xl font-bold text-slate-100 mb-4">
            Invite Accepted!
          </h1>
          <p className="text-slate-400 mb-6">
            You've successfully joined the vault. Redirecting to your vaults...
          </p>
          <Link
            href="/vaults"
            className="inline-block px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all duration-200"
          >
            Go to Vaults
          </Link>
        </div>
      </div>
    );
  }

  if (!token) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50 flex items-center justify-center">
        <div className="text-center bg-slate-800/60 backdrop-blur-md rounded-3xl p-12 border border-slate-700/50 shadow-2xl max-w-md">
          <h1 className="text-3xl font-bold text-slate-100 mb-4">
            Invalid Invite
          </h1>
          <p className="text-slate-400 mb-6">
            This invite link is invalid or missing a token.
          </p>
          <Link
            href="/vaults"
            className="inline-block px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold rounded-lg hover:from-indigo-600 hover:to-purple-600 transition-all duration-200"
          >
            Go to Vaults
          </Link>
        </div>
      </div>
    );
  }

  return null;
}

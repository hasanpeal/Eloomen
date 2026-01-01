"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../../contexts/AuthContext";
import {
  apiClient,
  Vault,
  VaultMember,
  VaultInvite,
  VaultItem,
  VaultLog,
  CreateInviteRequest,
  CreateVaultRequest,
  SessionExpiredError,
} from "../../lib/api";
import toast from "react-hot-toast";
import VaultItemList from "../../components/VaultItemList";
import CreateVaultItemModal from "../../components/CreateVaultItemModal";
import DeleteItemModal from "../../components/DeleteItemModal";
import ViewItemModal from "../../components/ViewItemModal";
import DeleteVaultModal from "../../components/DeleteVaultModal";
import {
  Plus,
  ChevronDown,
  ArrowLeft,
  Lock,
  Unlock,
  AlertTriangle,
  Crown,
  ShieldCheck,
  User,
  History,
  FileText,
  Key,
  StickyNote,
  Link as LinkIcon,
  Wallet,
  Mail,
  UserPlus,
  UserMinus,
  UserX,
  Trash2,
  RotateCcw,
  Send,
  X,
  Edit,
  Shield,
  ArrowRight,
} from "lucide-react";

type Tab = "items" | "members" | "invites" | "history" | "about";

export default function VaultDetailPage() {
  const { isLoading, isAuthenticated, user } = useAuth();
  const router = useRouter();
  const params = useParams();
  const vaultId = parseInt(params.id as string);

  const [vault, setVault] = useState<Vault | null>(null);
  const [members, setMembers] = useState<VaultMember[]>([]);
  const [invites, setInvites] = useState<VaultInvite[]>([]);
  const [items, setItems] = useState<VaultItem[]>([]);
  const [logs, setLogs] = useState<VaultLog[]>([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [logsLoaded, setLogsLoaded] = useState(false);
  const [loading, setLoading] = useState(true);
  const [accessDenied, setAccessDenied] = useState(false);
  const [activeTab, setActiveTab] = useState<Tab>("items");
  const [showItemModal, setShowItemModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showViewModal, setShowViewModal] = useState(false);
  const [editingItem, setEditingItem] = useState<VaultItem | undefined>();
  const [deletingItem, setDeletingItem] = useState<VaultItem | null>(null);
  const [viewingItem, setViewingItem] = useState<VaultItem | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteVaultModal, setShowDeleteVaultModal] = useState(false);
  const [deletingVault, setDeletingVault] = useState(false);
  const [showLeaveModal, setShowLeaveModal] = useState(false);
  const [showTransferModal, setShowTransferModal] = useState(false);
  const [transferMember, setTransferMember] = useState<VaultMember | null>(
    null
  );
  const [isTabDropdownOpen, setIsTabDropdownOpen] = useState(false);
  const [inviteForm, setInviteForm] = useState<CreateInviteRequest>({
    inviteeEmail: "",
    privilege: "Member",
    note: "",
  });
  const [editForm, setEditForm] = useState<{
    name: string;
    description: string;
    policyType: "Immediate" | "TimeBased" | "ExpiryBased" | "ManualRelease";
    releaseDate?: string;
    expiresAt?: string;
  }>({
    name: "",
    description: "",
    policyType: "Immediate",
  });

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isLoading, isAuthenticated, router]);

  const loadVaultLogs = useCallback(async () => {
    if (!vault || logsLoading) return;
    try {
      setLogsLoading(true);
      const logsData = await apiClient.getVaultLogs(vaultId);
      setLogs(logsData || []);
      setLogsLoaded(true);
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to load vault logs";
      toast.error(errorMessage);
      setLogs([]);
    } finally {
      setLogsLoading(false);
    }
  }, [vaultId, vault, logsLoading]);

  // Reset logs when vault changes
  useEffect(() => {
    setLogs([]);
    setLogsLoaded(false);
  }, [vaultId]);

  useEffect(() => {
    if (activeTab === "history" && vault && !logsLoaded && !logsLoading) {
      loadVaultLogs();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeTab, vault, logsLoaded, logsLoading]);

  const loadVaultData = useCallback(async () => {
    try {
      setLoading(true);
      setAccessDenied(false);

      // Try to load vault first - this will fail with 404 if access is denied
      let vaultData: Vault;
      try {
        vaultData = await apiClient.getVault(vaultId);
      } catch (vaultError) {
        // Check if this is a 404/access denied error
        const errorMessage =
          vaultError instanceof Error ? vaultError.message : String(vaultError);
        console.error("Error loading vault:", vaultError);

        // Check for any 404 or access denied indicators
        if (
          errorMessage.includes("Vault not found or access denied") ||
          errorMessage.includes("access denied") ||
          errorMessage.includes("404") ||
          errorMessage.includes("Not Found") ||
          (vaultError instanceof Error && vaultError.message.includes("404"))
        ) {
          setAccessDenied(true);
          setVault(null);
          toast.error(
            "Access denied. This vault is not accessible due to its release policy."
          );
          // Redirect to dashboard after a short delay
          setTimeout(() => {
            router.push("/dashboard");
          }, 2000);
          return;
        }
        throw vaultError; // Re-throw if it's a different error
      }

      // If we got here, vault loaded successfully
      // Now check if the user (non-owner) should have access based on policy
      const isOwner = vaultData.userPrivilege === "Owner";
      if (!isOwner && vaultData.policy) {
        // Check policy access using the same logic as backend
        const policy = vaultData.policy;
        const now = new Date();

        // Check if expired or revoked
        if (
          policy.releaseStatus === "Expired" ||
          policy.releaseStatus === "Revoked"
        ) {
          setAccessDenied(true);
          setVault(null);
          toast.error(
            "Access denied. This vault is not accessible due to its release policy."
          );
          setTimeout(() => {
            router.push("/dashboard");
          }, 2000);
          return;
        }

        // Check if pending (not released yet)
        if (policy.releaseStatus === "Pending") {
          // For TimeBased, check if release date has passed
          if (policy.policyType === "TimeBased" && policy.releaseDate) {
            const releaseDate = new Date(policy.releaseDate);
            if (now < releaseDate) {
              setAccessDenied(true);
              setVault(null);
              toast.error(
                "Access denied. This vault is not accessible due to its release policy."
              );
              setTimeout(() => {
                router.push("/dashboard");
              }, 2000);
              return;
            }
          } else if (policy.policyType === "ManualRelease") {
            // These require action to release
            setAccessDenied(true);
            setVault(null);
            toast.error(
              "Access denied. This vault is not accessible due to its release policy."
            );
            setTimeout(() => {
              router.push("/dashboard");
            }, 2000);
            return;
          }
        }

        // Check if released but expired (for ExpiryBased or InactivityBased)
        if (policy.releaseStatus === "Released") {
          if (policy.policyType === "ExpiryBased" && policy.expiresAt) {
            const expiresAt = new Date(policy.expiresAt);
            if (now > expiresAt) {
              setAccessDenied(true);
              setVault(null);
              toast.error("Access denied. This vault has expired.");
              setTimeout(() => {
                router.push("/dashboard");
              }, 2000);
              return;
            }
          }
        }
      }

      // Load other data in parallel (these might also fail if access is denied)
      const [membersData, invitesData, itemsData] = await Promise.all([
        apiClient.getVaultMembers(vaultId).catch(() => []), // Don't fail if members fail to load
        apiClient.getVaultInvites(vaultId).catch(() => []), // Don't fail if invites fail to load
        apiClient.getVaultItems(vaultId).catch(() => []), // Don't fail if items fail to load
      ]);

      setVault(vaultData);
      setMembers(membersData || []);
      setInvites(invitesData || []);
      setItems(itemsData || []);
      setEditForm({
        name: vaultData.name,
        description: vaultData.description || "",
        policyType: vaultData.policy?.policyType || "Immediate",
        releaseDate: vaultData.policy?.releaseDate,
        expiresAt: vaultData.policy?.expiresAt,
      });
      // Debug logging
      console.log("Vault members loaded:", membersData);
      console.log("Vault userPrivilege:", vaultData.userPrivilege);
      console.log(
        "Active members:",
        membersData?.filter((m) => m.status === "Active")
      );
    } catch (error) {
      // Don't show toast for session expiration - it's already handled in API client
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to load vault data";
      console.error("Error loading vault data:", error);

      // Final check for access denied
      if (
        errorMessage.includes("Vault not found or access denied") ||
        errorMessage.includes("access denied") ||
        errorMessage.includes("404") ||
        errorMessage.includes("Not Found")
      ) {
        setAccessDenied(true);
        setVault(null);
        toast.error(
          "Access denied. This vault is not accessible due to its release policy."
        );
        setTimeout(() => {
          router.push("/dashboard");
        }, 2000);
        return;
      }

      toast.error(errorMessage);
      // Don't redirect on other errors, just show empty state
      setMembers([]);
      setInvites([]);
      setVault(null);
    } finally {
      setLoading(false);
    }
  }, [vaultId, router]);

  useEffect(() => {
    if (isAuthenticated && vaultId) {
      loadVaultData();
    }
  }, [isAuthenticated, vaultId, loadVaultData]);

  const handleUpdateVault = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!vault) return;

    const canEdit =
      vault.userPrivilege === "Owner" || vault.userPrivilege === "Admin";
    if (!canEdit) {
      toast.error("You don't have permission to edit this vault");
      return;
    }

    try {
      const updated = await apiClient.updateVault(vaultId, editForm);
      setVault(updated);
      setShowEditModal(false);
      toast.success("Vault updated successfully");
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to update vault";
      toast.error(errorMessage);
    }
  };

  const handleDeleteVault = async () => {
    if (!vault) return;

    setDeletingVault(true);
    try {
      await apiClient.deleteVault(vaultId);
      toast.success("Vault deleted successfully");
      router.push("/dashboard");
    } catch (error: unknown) {
      // Don't show toast for session expiration - it's already handled in API client
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to delete vault";
      toast.error(errorMessage);
    } finally {
      setDeletingVault(false);
    }
  };

  const handleCreateInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await apiClient.createInvite(vaultId, inviteForm);
      toast.success("Invite sent successfully!");
      setShowInviteModal(false);
      setInviteForm({
        inviteeEmail: "",
        privilege: "Member",
        note: "",
      });
      loadVaultData();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to create invite";
      toast.error(errorMessage);
    }
  };

  const handleCancelInvite = async (inviteId: number) => {
    try {
      await apiClient.cancelInvite(vaultId, inviteId);
      toast.success("Invite cancelled");
      loadVaultData();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to cancel invite";
      toast.error(errorMessage);
    }
  };

  const handleResendInvite = async (inviteId: number) => {
    try {
      await apiClient.resendInvite(vaultId, inviteId);
      toast.success("Invite resent successfully");
      loadVaultData();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to resend invite";
      toast.error(errorMessage);
    }
  };

  const handleRemoveMember = async (memberId: number) => {
    if (!confirm("Are you sure you want to remove this member?")) return;
    try {
      await apiClient.removeMember(vaultId, memberId);
      toast.success("Member removed");
      loadVaultData();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to remove member";
      toast.error(errorMessage);
    }
  };

  const handleUpdatePrivilege = async (
    memberId: number,
    newPrivilege: "Owner" | "Admin" | "Member"
  ) => {
    if (newPrivilege === "Owner") {
      // Find the member to show in the modal
      const member = members.find((m) => m.id === memberId);
      if (member) {
        setTransferMember(member);
        setShowTransferModal(true);
      }
    } else {
      try {
        await apiClient.updateMemberPrivilege(vaultId, {
          memberId,
          privilege: newPrivilege,
        });
        toast.success("Privilege updated");
        loadVaultData();
      } catch (error) {
        if (error instanceof SessionExpiredError) {
          return;
        }
        const errorMessage =
          error instanceof Error ? error.message : "Failed to update privilege";
        toast.error(errorMessage);
      }
    }
  };

  const handleConfirmTransfer = async () => {
    if (!transferMember) return;

    try {
      await apiClient.transferOwnership(vaultId, {
        memberId: transferMember.id,
      });
      toast.success("Ownership transferred successfully");
      setShowTransferModal(false);
      setTransferMember(null);
      loadVaultData();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to transfer ownership";
      toast.error(errorMessage);
    }
  };

  const handleLeaveVault = async () => {
    try {
      await apiClient.leaveVault(vaultId);
      toast.success("Left vault successfully");
      router.push("/dashboard");
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to leave vault";
      toast.error(errorMessage);
    } finally {
      setShowLeaveModal(false);
    }
  };

  const handleReleaseVaultManually = async () => {
    try {
      await apiClient.releaseVaultManually(vaultId);
      toast.success("Vault released successfully");
      loadVaultData();
    } catch (error) {
      if (error instanceof SessionExpiredError) {
        return;
      }
      const errorMessage =
        error instanceof Error ? error.message : "Failed to release vault";
      toast.error(errorMessage);
    }
  };

  const getPrivilegeBadge = (privilege: string) => {
    switch (privilege) {
      case "Owner":
        return (
          <span className="px-3 py-1 bg-yellow-500/20 text-yellow-400 rounded-full text-xs font-semibold border border-yellow-500/30 flex items-center gap-1.5">
            <Crown className="w-3 h-3" />
            Owner
          </span>
        );
      case "Admin":
        return (
          <span className="px-3 py-1 bg-blue-500/20 text-blue-400 rounded-full text-xs font-semibold border border-blue-500/30 flex items-center gap-1.5">
            <ShieldCheck className="w-3 h-3" />
            Admin
          </span>
        );
      case "Member":
        return (
          <span className="px-3 py-1 bg-green-500/20 text-green-400 rounded-full text-xs font-semibold border border-green-500/30 flex items-center gap-1.5">
            <User className="w-3 h-3" />
            Member
          </span>
        );
      default:
        return null;
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Active":
        return (
          <span className="px-2 py-1 bg-green-500/20 text-green-400 rounded text-xs">
            Active
          </span>
        );
      case "Pending":
        return (
          <span className="px-2 py-1 bg-yellow-500/20 text-yellow-400 rounded text-xs">
            Pending
          </span>
        );
      case "Sent":
        return (
          <span className="px-2 py-1 bg-blue-500/20 text-blue-400 rounded text-xs">
            Sent
          </span>
        );
      case "Accepted":
        return (
          <span className="px-2 py-1 bg-green-500/20 text-green-400 rounded text-xs">
            Accepted
          </span>
        );
      case "Cancelled":
        return (
          <span className="px-2 py-1 bg-red-500/20 text-red-400 rounded text-xs">
            Cancelled
          </span>
        );
      case "Expired":
        return (
          <span className="px-2 py-1 bg-gray-500/20 text-gray-400 rounded text-xs">
            Expired
          </span>
        );
      default:
        return null;
    }
  };

  if (isLoading || loading) {
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
          <p className="mt-4 text-slate-400">Loading vault</p>
        </div>
      </div>
    );
  }

  // Show access denied page if backend blocked access
  if (accessDenied) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50">
        {/* Navigation */}
        <nav className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10 border-b border-slate-800/50">
          <Link href="/" className="group">
            <span className="text-xl md:text-2xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
              Eloomen
            </span>
          </Link>
          <div className="flex items-center space-x-4">
            <Link
              href="/dashboard"
              className="px-3 py-2 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm flex items-center gap-2"
            >
              <ArrowLeft className="w-5 h-5" />
              <span className="hidden sm:inline">Back</span>
            </Link>
          </div>
        </nav>

        {/* Main Content - Access Denied */}
        <main className="container mx-auto px-6 py-12">
          <div className="max-w-4xl mx-auto">
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-12 border border-slate-700/50 shadow-xl text-center">
              <div className="mb-6">
                <div className="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-yellow-500/20 mb-4">
                  <svg
                    className="h-10 w-10 text-yellow-400"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                  </svg>
                </div>
                <div className="flex items-center justify-center gap-3 mb-4">
                  <Lock className="w-8 h-8 text-yellow-400" />
                  <h1 className="text-2xl md:text-3xl font-bold text-slate-100">
                    Access Denied
                  </h1>
                </div>
                <p className="text-slate-400 text-lg mb-4">
                  This vault is not accessible due to its release policy.
                </p>
                <p className="text-slate-500 text-sm">
                  You will be redirected to the dashboard shortly...
                </p>
              </div>
              <Link
                href="/dashboard"
                className="inline-flex items-center gap-2 px-4 py-2.5 bg-indigo-500 text-white font-semibold rounded-lg hover:bg-indigo-600 transition-colors"
              >
                <ArrowLeft className="w-5 h-5" />
                Back
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  if (!vault || !isAuthenticated) {
    return null;
  }

  const canEdit =
    vault.userPrivilege === "Owner" || vault.userPrivilege === "Admin";
  const canManageMembers = canEdit;
  const isOwner = vault.userPrivilege === "Owner";
  const isAdmin = vault.userPrivilege === "Admin";

  // Helper function to check if vault is accessible and get access message
  const getVaultAccessInfo = () => {
    // Owner and Admin always have access
    if (isOwner || isAdmin) {
      return { hasAccess: true, message: null };
    }

    // If no policy, vault is accessible
    if (!vault.policy) {
      return { hasAccess: true, message: null };
    }

    const policy = vault.policy;
    const now = new Date();

    // Check if expired or revoked
    if (policy.releaseStatus === "Expired") {
      return {
        hasAccess: false,
        message: "This vault has expired and is no longer accessible.",
      };
    }

    if (policy.releaseStatus === "Revoked") {
      return {
        hasAccess: false,
        message: "This vault has been revoked and is no longer accessible.",
      };
    }

    // Check if released
    if (policy.releaseStatus === "Released") {
      // Check if expired (for ExpiryBased)
      if (policy.policyType === "ExpiryBased" && policy.expiresAt) {
        const expiresAt = new Date(policy.expiresAt);
        if (now > expiresAt) {
          return {
            hasAccess: false,
            message: `This vault expired on ${expiresAt.toLocaleString()}.`,
          };
        }
        return {
          hasAccess: true,
          message: `This vault will expire on ${expiresAt.toLocaleString()}.`,
        };
      }

      return { hasAccess: true, message: null };
    }

    // Check if pending
    if (policy.releaseStatus === "Pending") {
      if (policy.policyType === "TimeBased" && policy.releaseDate) {
        const releaseDate = new Date(policy.releaseDate);
        return {
          hasAccess: false,
          message: `This vault will be released on ${releaseDate.toLocaleString()}.`,
        };
      }

      if (policy.policyType === "ManualRelease") {
        return {
          hasAccess: false,
          message: "This vault requires manual release by the owner.",
        };
      }
    }

    return { hasAccess: false, message: "This vault is not yet accessible." };
  };

  const accessInfo = getVaultAccessInfo();

  // If vault is not accessible for non-owners/admins, show access denied message instead of vault content
  if (!accessInfo.hasAccess && !canEdit) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50">
        {/* Navigation */}
        <nav className="relative container mx-auto px-6 py-6 flex items-center justify-between z-10 border-b border-slate-800/50">
          <Link href="/" className="group">
            <span className="text-xl md:text-2xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
              Eloomen
            </span>
          </Link>
          <div className="flex items-center space-x-4">
            <Link
              href="/dashboard"
              className="px-3 py-2 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm flex items-center gap-2"
            >
              <ArrowLeft className="w-5 h-5" />
              <span className="hidden sm:inline">Back</span>
            </Link>
          </div>
        </nav>

        {/* Main Content - Access Denied */}
        <main className="container mx-auto px-6 py-12">
          <div className="max-w-4xl mx-auto">
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-12 border border-slate-700/50 shadow-xl text-center">
              <div className="mb-6">
                <div className="mx-auto flex items-center justify-center h-20 w-20 rounded-full bg-yellow-500/20 mb-4">
                  <svg
                    className="h-10 w-10 text-yellow-400"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                  </svg>
                </div>
                <div className="flex items-center justify-center gap-3 mb-4">
                  <Lock className="w-8 h-8 text-yellow-400" />
                  <h1 className="text-2xl md:text-3xl font-bold text-slate-100">
                    Vault Not Accessible
                  </h1>
                </div>
                <h2 className="text-xl font-semibold text-slate-300 mb-4">
                  {vault.name}
                </h2>
                <p className="text-slate-400 text-lg">{accessInfo.message}</p>
              </div>
              <Link
                href="/dashboard"
                className="inline-flex items-center gap-2 px-4 py-2.5 bg-indigo-500 text-white font-semibold rounded-lg hover:bg-indigo-600 transition-colors"
              >
                <ArrowLeft className="w-5 h-5" />
                Back
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950/50">
      {/* Navigation */}
      <nav className="relative container mx-auto px-4 sm:px-6 py-4 sm:py-6 flex items-center justify-between z-10 border-b border-slate-800/50">
        <Link href="/" className="group">
          <span className="text-xl md:text-2xl font-extrabold tracking-tight bg-gradient-to-r from-indigo-400 via-purple-400 to-pink-400 bg-clip-text text-transparent group-hover:from-indigo-300 group-hover:via-purple-300 group-hover:to-pink-300 transition-all duration-300">
            Eloomen
          </span>
        </Link>
        <div className="flex items-center space-x-2 sm:space-x-4">
          <Link
            href="/dashboard"
            className="px-2 sm:px-3 py-1.5 sm:py-2 text-slate-300 hover:text-indigo-400 font-medium transition-colors rounded-lg hover:bg-slate-800/50 backdrop-blur-sm flex items-center gap-1.5 sm:gap-2"
          >
            <ArrowLeft className="w-4 h-4 sm:w-5 sm:h-5" />
            <span className="hidden sm:inline">Back</span>
          </Link>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container mx-auto px-4 sm:px-6 py-8 sm:py-12">
        <div className="max-w-7xl mx-auto">
          {/* Header */}
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-6 md:mb-8">
            <div className="flex-1 min-w-0">
              <div className="flex flex-wrap items-center gap-2 sm:gap-3 mb-2">
                <h1 className="text-xl md:text-3xl lg:text-4xl font-bold text-slate-100 break-words">
                  {vault.name}
                </h1>
                {getPrivilegeBadge(vault.userPrivilege || "")}
                {/* Show manual release button if vault has ManualRelease policy and is pending */}
                {vault?.policy?.policyType === "ManualRelease" &&
                  vault?.policy?.releaseStatus === "Pending" &&
                  isOwner && (
                    <button
                      onClick={handleReleaseVaultManually}
                      className="px-3 sm:px-4 py-2 bg-green-500/20 text-green-400 rounded-lg hover:bg-green-500/30 transition-colors cursor-pointer border border-green-500/30 flex items-center gap-2 text-sm sm:text-base"
                      title="Manually release vault"
                    >
                      <Unlock className="w-4 h-4" />
                      <span className="hidden sm:inline">Release Vault</span>
                      <span className="sm:hidden">Release</span>
                    </button>
                  )}
              </div>
              {vault.description && (
                <p className="text-slate-400 mt-2 text-sm sm:text-base">
                  {vault.description}
                </p>
              )}
            </div>
            {/* Only owners can edit and delete vault */}
            {isOwner && (
              <div className="flex gap-2">
                <button
                  onClick={() => setShowEditModal(true)}
                  className="px-3 sm:px-4 py-2 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer text-sm sm:text-base whitespace-nowrap"
                >
                  Edit Vault
                </button>
                <button
                  onClick={() => setShowDeleteVaultModal(true)}
                  className="px-3 sm:px-4 py-2 bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 transition-colors cursor-pointer text-sm sm:text-base whitespace-nowrap"
                >
                  Delete Vault
                </button>
              </div>
            )}
          </div>

          {/* Tabs - Only show items tab for non-owners, all tabs for owners */}
          <div className="border-b border-slate-700/50 mb-6">
            {/* Desktop Tabs */}
            <div className="hidden md:flex space-x-6">
              {(canEdit
                ? [
                    "items",
                    "members",
                    ...(canManageMembers
                      ? ["invites", "history", "about"]
                      : []),
                  ]
                : ["items"]
              ).map((tab) => (
                <button
                  key={tab}
                  onClick={() => setActiveTab(tab as Tab)}
                  className={`px-4 py-3 font-semibold transition-colors border-b-2 cursor-pointer ${
                    activeTab === tab
                      ? "border-indigo-500 text-indigo-400"
                      : "border-transparent text-slate-400 hover:text-slate-300"
                  }`}
                >
                  {tab.charAt(0).toUpperCase() + tab.slice(1)}
                </button>
              ))}
            </div>

            {/* Mobile Tab Dropdown */}
            <div className="md:hidden relative">
              <button
                onClick={() => setIsTabDropdownOpen(!isTabDropdownOpen)}
                className="w-full px-4 py-3 font-semibold text-slate-300 hover:text-indigo-400 transition-colors rounded-lg hover:bg-slate-700/50 flex items-center justify-between cursor-pointer"
              >
                <span>
                  {activeTab.charAt(0).toUpperCase() + activeTab.slice(1)}
                </span>
                <ChevronDown
                  className={`w-5 h-5 transition-transform ${
                    isTabDropdownOpen ? "rotate-180" : ""
                  }`}
                />
              </button>

              {isTabDropdownOpen && (
                <div className="absolute top-full left-0 right-0 mt-2 bg-slate-800/95 backdrop-blur-md rounded-xl border border-slate-700/50 shadow-2xl z-50">
                  <div className="flex flex-col p-2">
                    {(canEdit
                      ? [
                          "items",
                          "members",
                          ...(canManageMembers
                            ? ["invites", "history", "about"]
                            : []),
                        ]
                      : ["items"]
                    ).map((tab) => (
                      <button
                        key={tab}
                        onClick={() => {
                          setActiveTab(tab as Tab);
                          setIsTabDropdownOpen(false);
                        }}
                        className={`px-4 py-3 font-semibold transition-colors rounded-lg cursor-pointer text-left ${
                          activeTab === tab
                            ? "bg-indigo-600/20 text-indigo-400"
                            : "text-slate-300 hover:bg-slate-700/50 hover:text-slate-100"
                        }`}
                      >
                        {tab.charAt(0).toUpperCase() + tab.slice(1)}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Tab Content */}
          {activeTab === "items" && (
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-4 md:p-8 border border-slate-700/50 shadow-xl">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
                <h2 className="text-xl md:text-2xl font-bold text-slate-100">
                  Items
                </h2>
                {/* Owners and Admins can add items - vault-level policy is superior */}
                {accessInfo.hasAccess && canEdit && (
                  <button
                    onClick={() => {
                      setEditingItem(undefined);
                      setShowItemModal(true);
                    }}
                    className="px-4 py-2 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center gap-2"
                  >
                    <Plus className="w-4 h-4" />
                    <span className="hidden sm:inline">Add Item</span>
                    <span className="sm:hidden">Add</span>
                  </button>
                )}
              </div>
              {!accessInfo.hasAccess ? (
                <div className="text-center py-12">
                  <div className="bg-yellow-500/10 border border-yellow-500/30 rounded-lg p-6 mb-4">
                    <div className="flex items-center gap-2 mb-2">
                      <Lock className="w-5 h-5 text-yellow-400" />
                      <p className="text-yellow-400 font-semibold text-base md:text-lg">
                        Vault Not Accessible
                      </p>
                    </div>
                    <p className="text-slate-300">{accessInfo.message}</p>
                  </div>
                </div>
              ) : (
                <>
                  {accessInfo.message && (
                    <div className="bg-blue-500/10 border border-blue-500/30 rounded-lg p-4 mb-6">
                      <p className="text-blue-400 text-sm">
                        {accessInfo.message}
                      </p>
                    </div>
                  )}
                  {items.length === 0 ? (
                    <div className="text-center py-12">
                      <p className="text-slate-400 text-lg mb-4">
                        No items in this vault yet
                      </p>
                    </div>
                  ) : (
                    <VaultItemList
                      items={items}
                      onEdit={(item) => {
                        setEditingItem(item);
                        setShowItemModal(true);
                      }}
                      onDelete={(item) => {
                        setDeletingItem(item);
                        setShowDeleteModal(true);
                      }}
                      onView={async (item) => {
                        try {
                          // Fetch full item with decrypted data
                          const fullItem = await apiClient.getVaultItem(
                            vaultId,
                            item.id
                          );
                          setViewingItem(fullItem);
                          setShowViewModal(true);
                        } catch (error: unknown) {
                          // Don't show toast for session expiration - it's already handled in API client
                          if (error instanceof SessionExpiredError) {
                            return;
                          }
                          const errorMessage =
                            error instanceof Error
                              ? error.message
                              : "Failed to load item";
                          toast.error(errorMessage);
                        }
                      }}
                      canView={true}
                    />
                  )}
                </>
              )}
            </div>
          )}

          {activeTab === "members" && (
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-4 md:p-8 border border-slate-700/50 shadow-xl">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
                <h2 className="text-xl md:text-2xl font-bold text-slate-100">
                  Members
                </h2>
                {canManageMembers && (
                  <button
                    onClick={() => {
                      setInviteForm({
                        inviteeEmail: "",
                        privilege: "Member",
                        note: "",
                      });
                      setShowInviteModal(true);
                    }}
                    className="px-4 py-2 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center gap-2"
                  >
                    <Plus className="w-4 h-4" />
                    <span className="hidden sm:inline">Invite Member</span>
                    <span className="sm:hidden">Invite</span>
                  </button>
                )}
              </div>
              <div className="space-y-3">
                {members.filter((m) => m.status === "Active").length === 0 ? (
                  <div className="text-center py-12">
                    <p className="text-slate-400 mb-4">
                      No active members found
                    </p>
                    {canManageMembers && (
                      <button
                        onClick={() => {
                          setInviteForm({
                            inviteeEmail: "",
                            privilege: "Member",
                            note: "",
                          });
                          setShowInviteModal(true);
                        }}
                        className="px-6 py-3 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center justify-center gap-2"
                      >
                        <Plus className="w-5 h-5" />
                        Invite Your First Member
                      </button>
                    )}
                  </div>
                ) : (
                  members
                    .filter((m) => m.status === "Active")
                    .map((member) => (
                      <div
                        key={member.id}
                        className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4"
                      >
                        <div className="flex items-center gap-3 sm:gap-4 flex-1 min-w-0">
                          <div className="flex-1 min-w-0">
                            <p className="text-slate-100 font-semibold text-sm sm:text-base truncate">
                              {member.userName || member.userEmail || "Unknown"}
                            </p>
                            <p className="text-slate-400 text-xs sm:text-sm truncate">
                              {member.userEmail}
                            </p>
                            {/* Show vault policy status for non-owners */}
                            {vault?.policy &&
                              vault.ownerId !== member.userId && (
                                <div className="mt-2 flex items-center gap-2">
                                  <span className="text-xs text-slate-500">
                                    Policy: {vault.policy.policyType}
                                  </span>
                                  {vault.policy.releaseStatus === "Pending" && (
                                    <span className="px-2 py-0.5 bg-yellow-500/20 text-yellow-400 rounded text-xs">
                                      Pending Release
                                    </span>
                                  )}
                                  {vault.policy.releaseStatus ===
                                    "Released" && (
                                    <span className="px-2 py-0.5 bg-green-500/20 text-green-400 rounded text-xs">
                                      Released
                                    </span>
                                  )}
                                </div>
                              )}
                          </div>
                          <div className="flex-shrink-0">
                            {getPrivilegeBadge(member.privilege)}
                          </div>
                        </div>
                        <div className="flex items-center gap-2 flex-wrap">
                          {canManageMembers && (
                            <>
                              {/* Owner can update any member except themselves */}
                              {isOwner && member.privilege !== "Owner" && (
                                <select
                                  value={member.privilege}
                                  onChange={(e) =>
                                    handleUpdatePrivilege(
                                      member.id,
                                      e.target.value as
                                        | "Owner"
                                        | "Admin"
                                        | "Member"
                                    )
                                  }
                                  className="px-2 sm:px-3 py-1.5 bg-slate-800 border border-slate-700 rounded text-slate-100 text-xs sm:text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer flex-1 sm:flex-none min-w-[100px]"
                                >
                                  <option value="Member">Member</option>
                                  <option value="Admin">Admin</option>
                                  <option value="Owner">
                                    Owner (Transfer)
                                  </option>
                                </select>
                              )}
                              {/* Admin can update Members (promote to Admin) and Admins (demote to Member), but not Owners */}
                              {isAdmin && member.privilege !== "Owner" && (
                                <select
                                  value={member.privilege}
                                  onChange={(e) =>
                                    handleUpdatePrivilege(
                                      member.id,
                                      e.target.value as "Admin" | "Member"
                                    )
                                  }
                                  className="px-2 sm:px-3 py-1.5 bg-slate-800 border border-slate-700 rounded text-slate-100 text-xs sm:text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 cursor-pointer flex-1 sm:flex-none min-w-[100px]"
                                >
                                  <option value="Member">Member</option>
                                  <option value="Admin">Admin</option>
                                </select>
                              )}
                              {canManageMembers &&
                                (member.privilege !== "Owner" ||
                                  (isOwner &&
                                    member.userId !== vault.ownerId)) && (
                                  <button
                                    onClick={() =>
                                      handleRemoveMember(member.id)
                                    }
                                    className="px-2 sm:px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-xs sm:text-sm hover:bg-red-500/30 transition-colors cursor-pointer whitespace-nowrap"
                                  >
                                    Remove
                                  </button>
                                )}
                            </>
                          )}
                        </div>
                      </div>
                    ))
                )}
              </div>
            </div>
          )}

          {activeTab === "invites" && (
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-4 md:p-8 border border-slate-700/50 shadow-xl">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
                <h2 className="text-xl md:text-2xl font-bold text-slate-100">
                  Invites
                </h2>
                {canManageMembers && (
                  <button
                    onClick={() => {
                      setInviteForm({
                        inviteeEmail: "",
                        privilege: "Member",
                        note: "",
                      });
                      setShowInviteModal(true);
                    }}
                    className="px-4 py-2 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer flex items-center gap-2"
                  >
                    <Plus className="w-4 h-4" />
                    <span className="hidden sm:inline">Create Invite</span>
                    <span className="sm:hidden">Create</span>
                  </button>
                )}
              </div>
              <div className="space-y-3">
                {invites.length === 0 ? (
                  <div className="text-center py-12">
                    <p className="text-slate-400 mb-4">No invites yet</p>
                  </div>
                ) : (
                  invites.map((invite) => (
                    <div
                      key={invite.id}
                      className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4"
                    >
                      <div className="flex items-center gap-3 sm:gap-4 flex-1 min-w-0">
                        <div className="flex-1 min-w-0">
                          <p className="text-slate-100 font-semibold text-sm sm:text-base truncate">
                            {invite.inviteeEmail}
                          </p>
                          <div className="flex items-center gap-2 mt-1 flex-wrap">
                            {getPrivilegeBadge(invite.privilege)}
                            {getStatusBadge(invite.status)}
                          </div>
                        </div>
                      </div>
                      {canManageMembers &&
                        (invite.status === "Pending" ||
                          invite.status === "Sent") && (
                          <div className="flex items-center gap-2 flex-wrap">
                            {invite.status === "Sent" && (
                              <button
                                onClick={() => handleResendInvite(invite.id)}
                                className="px-2 sm:px-3 py-1.5 bg-blue-500/20 text-blue-400 rounded text-xs sm:text-sm hover:bg-blue-500/30 transition-colors cursor-pointer whitespace-nowrap"
                              >
                                Resend
                              </button>
                            )}
                            <button
                              onClick={() => handleCancelInvite(invite.id)}
                              className="px-2 sm:px-3 py-1.5 bg-red-500/20 text-red-400 rounded text-xs sm:text-sm hover:bg-red-500/30 transition-colors cursor-pointer whitespace-nowrap"
                            >
                              Cancel
                            </button>
                          </div>
                        )}
                    </div>
                  ))
                )}
              </div>
            </div>
          )}

          {activeTab === "history" && (
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-4 sm:p-6 md:p-8 border border-slate-700/50 shadow-xl">
              <h2 className="text-xl md:text-2xl font-bold text-slate-100 mb-4 sm:mb-6">
                Vault History
              </h2>
              <div className="space-y-3 sm:space-y-4">
                {logsLoading ? (
                  <div className="text-center py-8 sm:py-12">
                    <svg
                      className="animate-spin h-6 w-6 sm:h-8 sm:w-8 text-indigo-400 mx-auto"
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
                    <p className="mt-4 text-sm sm:text-base text-slate-400">
                      Loading history
                    </p>
                  </div>
                ) : logs.length === 0 ? (
                  <div className="text-center py-8 sm:py-12">
                    <History className="w-10 h-10 sm:w-12 sm:h-12 text-slate-500 mx-auto mb-4" />
                    <p className="text-sm sm:text-base text-slate-400">
                      No history available
                    </p>
                  </div>
                ) : (
                  logs.map((log) => {
                    const formatLogMessage = (
                      log: VaultLog
                    ): {
                      message: string;
                      icon: React.ComponentType<{ className?: string }>;
                      color: string;
                    } => {
                      const userName =
                        log.userName || log.userEmail || "Unknown";
                      const targetUserName =
                        log.targetUserName || log.targetUserEmail || "Unknown";

                      switch (log.action) {
                        case "CreateVault":
                          return {
                            message: `${userName} created this vault`,
                            icon: Plus,
                            color: "text-green-400",
                          };
                        case "UpdateVault":
                          return {
                            message: `${userName} updated the vault`,
                            icon: Edit,
                            color: "text-blue-400",
                          };
                        case "DeleteVault":
                          return {
                            message: `${userName} deleted this vault`,
                            icon: Trash2,
                            color: "text-red-400",
                          };
                        case "RestoreVault":
                          return {
                            message: `${userName} restored this vault`,
                            icon: RotateCcw,
                            color: "text-green-400",
                          };
                        case "CreateInvite":
                          return {
                            message: `${userName} invited ${targetUserName}`,
                            icon: Mail,
                            color: "text-indigo-400",
                          };
                        case "CancelInvite":
                          return {
                            message: `${userName} cancelled invite for ${targetUserName}`,
                            icon: X,
                            color: "text-yellow-400",
                          };
                        case "ResendInvite":
                          return {
                            message: `${userName} resent invite to ${targetUserName}`,
                            icon: Send,
                            color: "text-blue-400",
                          };
                        case "AcceptInvite":
                          return {
                            message: `${userName} accepted the invite`,
                            icon: UserPlus,
                            color: "text-green-400",
                          };
                        case "RemoveMember":
                          return {
                            message: `${userName} removed ${targetUserName} from the vault`,
                            icon: UserMinus,
                            color: "text-red-400",
                          };
                        case "UpdateMemberPrivilege":
                          return {
                            message: `${userName} updated ${targetUserName}'s privilege`,
                            icon: Shield,
                            color: "text-blue-400",
                          };
                        case "TransferOwnership":
                          return {
                            message: `${userName} transferred ownership to ${targetUserName}`,
                            icon: Crown,
                            color: "text-purple-400",
                          };
                        case "TransferItemOwnership":
                          return {
                            message:
                              log.additionalContext ||
                              `${userName} transferred item ownership`,
                            icon: ArrowRight,
                            color: "text-blue-400",
                          };
                        case "LeaveVault":
                          return {
                            message: `${userName} left the vault`,
                            icon: UserX,
                            color: "text-yellow-400",
                          };
                        case "ReleaseVaultManually":
                          return {
                            message: `${userName} manually released the vault`,
                            icon: Unlock,
                            color: "text-green-400",
                          };
                        case "CreateItem":
                          const itemTitle =
                            log.additionalContext?.match(
                              /Title: ([^,]+)/
                            )?.[1] || "an item";
                          const itemType =
                            log.additionalContext?.match(
                              /ItemType: (\w+)/
                            )?.[1] || "";
                          const itemIcon =
                            itemType === "Password"
                              ? Key
                              : itemType === "Note"
                              ? StickyNote
                              : itemType === "Link"
                              ? LinkIcon
                              : itemType === "CryptoWallet"
                              ? Wallet
                              : FileText;
                          return {
                            message: `${userName} created ${itemTitle}`,
                            icon: itemIcon,
                            color: "text-green-400",
                          };
                        case "UpdateItem":
                          const updateTitle =
                            log.additionalContext?.match(
                              /Title: ([^,]+)/
                            )?.[1] || "an item";
                          return {
                            message: `${userName} updated ${updateTitle}`,
                            icon: Edit,
                            color: "text-blue-400",
                          };
                        case "DeleteItem":
                          const deleteTitle =
                            log.additionalContext?.match(
                              /Title: ([^,]+)/
                            )?.[1] || "an item";
                          return {
                            message: `${userName} deleted ${deleteTitle}`,
                            icon: Trash2,
                            color: "text-red-400",
                          };
                        case "RestoreItem":
                          const restoreTitle =
                            log.additionalContext?.match(
                              /Title: ([^,]+)/
                            )?.[1] || "an item";
                          return {
                            message: `${userName} restored ${restoreTitle}`,
                            icon: RotateCcw,
                            color: "text-green-400",
                          };
                        default:
                          return {
                            message: `${userName} performed ${log.action}`,
                            icon: History,
                            color: "text-slate-400",
                          };
                      }
                    };

                    const {
                      message,
                      icon: Icon,
                      color,
                    } = formatLogMessage(log);

                    return (
                      <div
                        key={log.id}
                        className="bg-slate-900/50 rounded-lg p-3 sm:p-4 border border-slate-700/50"
                      >
                        <div className="flex items-start gap-3 sm:gap-4">
                          <div className={`flex-shrink-0 ${color}`}>
                            <Icon className="w-4 h-4 sm:w-5 sm:h-5" />
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-xs sm:text-sm text-slate-100 font-medium break-words">
                              {message}
                            </p>
                            {log.additionalContext &&
                              log.action !== "CreateItem" &&
                              log.action !== "UpdateItem" &&
                              log.action !== "DeleteItem" &&
                              log.action !== "RestoreItem" && (
                                <p className="text-xs text-slate-400 mt-1 break-words">
                                  {log.additionalContext}
                                </p>
                              )}
                            <p className="text-xs text-slate-500 mt-1.5 sm:mt-2">
                              {new Date(log.timestamp).toLocaleString()}
                            </p>
                          </div>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>
          )}

          {activeTab === "about" && (
            <div className="bg-slate-800/60 backdrop-blur-md rounded-2xl p-4 md:p-8 border border-slate-700/50 shadow-xl">
              <h2 className="text-xl md:text-2xl font-bold text-slate-100 mb-6">
                Vault Information
              </h2>
              <div className="space-y-6">
                <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                  <p className="text-slate-400 text-sm mb-1">Vault Name</p>
                  <p className="text-slate-100 font-semibold text-lg">
                    {vault.name}
                  </p>
                </div>

                {vault.description && (
                  <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                    <p className="text-slate-400 text-sm mb-1">Description</p>
                    <p className="text-slate-100">{vault.description}</p>
                  </div>
                )}

                <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                  <p className="text-slate-400 text-sm mb-1">Current Owner</p>
                  <p className="text-slate-100 font-semibold">
                    {vault.ownerName || vault.ownerEmail || "Unknown"}
                  </p>
                  {vault.ownerEmail && vault.ownerName && (
                    <p className="text-slate-400 text-sm mt-1">
                      {vault.ownerEmail}
                    </p>
                  )}
                </div>

                <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                  <p className="text-slate-400 text-sm mb-1">Original Owner</p>
                  <p className="text-slate-100 font-semibold">
                    {vault.originalOwnerName ||
                      vault.originalOwnerEmail ||
                      "Unknown"}
                  </p>
                  {vault.originalOwnerEmail && vault.originalOwnerName && (
                    <p className="text-slate-400 text-sm mt-1">
                      {vault.originalOwnerEmail}
                    </p>
                  )}
                </div>

                <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                  <p className="text-slate-400 text-sm mb-1">Created</p>
                  <p className="text-slate-100 font-semibold">
                    {new Date(vault.createdAt).toLocaleDateString()}
                  </p>
                  <p className="text-slate-400 text-sm mt-1">
                    {new Date(vault.createdAt).toLocaleTimeString()}
                  </p>
                </div>

                <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                  <p className="text-slate-400 text-sm mb-1">Status</p>
                  <span
                    className={`px-3 py-1 rounded-full text-sm font-semibold ${
                      vault.status === "Active"
                        ? "bg-green-500/20 text-green-400 border border-green-500/30"
                        : "bg-red-500/20 text-red-400 border border-red-500/30"
                    }`}
                  >
                    {vault.status}
                  </span>
                </div>

                {vault.deletedAt && (
                  <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
                    <p className="text-slate-400 text-sm mb-1">Deleted</p>
                    <p className="text-slate-100 font-semibold">
                      {new Date(vault.deletedAt).toLocaleDateString()}
                    </p>
                    <p className="text-slate-400 text-sm mt-1">
                      {new Date(vault.deletedAt).toLocaleTimeString()}
                    </p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </main>

      {/* Edit Vault Modal */}
      {showEditModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
            <h2 className="text-2xl font-bold text-slate-100 mb-6">
              Edit Vault
            </h2>
            <form onSubmit={handleUpdateVault}>
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Vault Name *
                </label>
                <input
                  type="text"
                  required
                  value={editForm.name}
                  onChange={(e) =>
                    setEditForm({ ...editForm, name: e.target.value })
                  }
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Description
                </label>
                <textarea
                  value={editForm.description}
                  onChange={(e) =>
                    setEditForm({ ...editForm, description: e.target.value })
                  }
                  rows={3}
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
                />
              </div>

              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Release Policy *
                </label>
                <select
                  value={editForm.policyType || "Immediate"}
                  onChange={(e) =>
                    setEditForm({
                      ...editForm,
                      policyType: e.target
                        .value as CreateVaultRequest["policyType"],
                      releaseDate: undefined,
                      expiresAt: undefined,
                    })
                  }
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                >
                  <option value="Immediate">Immediate Release</option>
                  <option value="TimeBased">Time-Based Release</option>
                  <option value="ExpiryBased">Expiry-Based</option>
                  <option value="ManualRelease">Manual Release</option>
                </select>
              </div>

              {/* Time-Based Policy Configuration */}
              {editForm.policyType === "TimeBased" && (
                <div className="mb-4">
                  <label className="block text-sm font-semibold text-slate-300 mb-2">
                    Release Date *
                  </label>
                  <input
                    type="date"
                    required
                    value={
                      editForm.releaseDate
                        ? new Date(editForm.releaseDate)
                            .toISOString()
                            .slice(0, 10)
                        : ""
                    }
                    onChange={(e) =>
                      setEditForm({
                        ...editForm,
                        releaseDate: e.target.value
                          ? new Date(
                              e.target.value + "T00:00:00Z"
                            ).toISOString()
                          : undefined,
                      })
                    }
                    min={new Date(Date.now() + 24 * 60 * 60 * 1000)
                      .toISOString()
                      .slice(0, 10)}
                    className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  />
                </div>
              )}

              {/* Expiry-Based Policy Configuration */}
              {editForm.policyType === "ExpiryBased" && (
                <div className="mb-4">
                  <label className="block text-sm font-semibold text-slate-300 mb-2">
                    Expires At *
                  </label>
                  <input
                    type="date"
                    required
                    value={
                      editForm.expiresAt
                        ? new Date(editForm.expiresAt)
                            .toISOString()
                            .slice(0, 10)
                        : ""
                    }
                    onChange={(e) =>
                      setEditForm({
                        ...editForm,
                        expiresAt: e.target.value
                          ? new Date(
                              e.target.value + "T00:00:00Z"
                            ).toISOString()
                          : undefined,
                      })
                    }
                    min={new Date(Date.now() + 24 * 60 * 60 * 1000)
                      .toISOString()
                      .slice(0, 10)}
                    className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  />
                </div>
              )}

              <div className="mb-6"></div>
              <div className="flex space-x-3">
                <button
                  type="button"
                  onClick={() => setShowEditModal(false)}
                  className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 px-4 py-3 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
                >
                  Save Changes
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Invite Modal */}
      {showInviteModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl max-h-[90vh] overflow-y-auto">
            <h2 className="text-2xl font-bold text-slate-100 mb-6">
              Invite Member
            </h2>
            <form onSubmit={handleCreateInvite}>
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Email Address *
                </label>
                <input
                  type="email"
                  required
                  value={inviteForm.inviteeEmail}
                  onChange={(e) =>
                    setInviteForm({
                      ...inviteForm,
                      inviteeEmail: e.target.value,
                    })
                  }
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="user@example.com"
                />
              </div>
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Privilege *
                </label>
                <select
                  value={inviteForm.privilege}
                  onChange={(e) =>
                    setInviteForm({
                      ...inviteForm,
                      privilege: e.target.value as "Owner" | "Admin" | "Member",
                    })
                  }
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  disabled={!isOwner}
                >
                  {isOwner && <option value="Owner">Owner</option>}
                  <option value="Admin">Admin</option>
                  <option value="Member">Member</option>
                </select>
                {!isOwner && (
                  <p className="text-xs text-slate-400 mt-1">
                    Only owners can invite as Owner
                  </p>
                )}
              </div>
              {/* Invite Expiration */}
              <div className="mb-4">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Invite Expires At (Optional)
                </label>
                <input
                  type="date"
                  value={
                    inviteForm.inviteExpiresAt
                      ? new Date(inviteForm.inviteExpiresAt)
                          .toISOString()
                          .slice(0, 10)
                      : ""
                  }
                  onChange={(e) =>
                    setInviteForm({
                      ...inviteForm,
                      inviteExpiresAt: e.target.value
                        ? new Date(e.target.value + "T00:00:00Z").toISOString()
                        : undefined,
                    })
                  }
                  min={new Date(Date.now() + 24 * 60 * 60 * 1000)
                    .toISOString()
                    .slice(0, 10)}
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                />
                <p className="text-xs text-slate-400 mt-1">
                  When the invite itself expires (default: 7 days). Note: The
                  vault&apos;s release policy applies to all members.
                </p>
              </div>
              <div className="mb-6">
                <label className="block text-sm font-semibold text-slate-300 mb-2">
                  Note (Optional)
                </label>
                <textarea
                  value={inviteForm.note}
                  onChange={(e) =>
                    setInviteForm({ ...inviteForm, note: e.target.value })
                  }
                  rows={3}
                  className="w-full px-4 py-3 bg-slate-900/50 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
                  placeholder="Optional message for the invitee..."
                />
              </div>
              <div className="flex space-x-3">
                <button
                  type="button"
                  onClick={() => {
                    setShowInviteModal(false);
                    // Reset form to defaults when closing
                    setInviteForm({
                      inviteeEmail: "",
                      privilege: "Member",
                      note: "",
                    });
                  }}
                  className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="flex-1 px-4 py-3 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
                >
                  Send Invite
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Leave Vault Confirmation Modal */}
      {showLeaveModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
            <div className="mb-6">
              <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-500/20 mb-4">
                <svg
                  className="h-6 w-6 text-red-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
              </div>
              <h2 className="text-2xl font-bold text-slate-100 mb-2 text-center">
                Leave Vault?
              </h2>
              <p className="text-slate-400 text-center">
                Are you sure you want to leave{" "}
                <span className="font-semibold text-slate-200">
                  {vault?.name}
                </span>
                ? You will lose access to this vault and all its contents.
              </p>
            </div>
            <div className="flex space-x-3">
              <button
                type="button"
                onClick={() => setShowLeaveModal(false)}
                className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleLeaveVault}
                className="flex-1 px-4 py-3 bg-red-500 text-white font-semibold rounded-lg hover:bg-red-600 transition-colors cursor-pointer"
              >
                Leave Vault
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Transfer Ownership Confirmation Modal */}
      {showTransferModal && transferMember && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-slate-800 rounded-2xl p-8 max-w-md w-full border border-slate-700/50 shadow-2xl">
            <div className="mb-6">
              <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-yellow-500/20 mb-4">
                <svg
                  className="h-6 w-6 text-yellow-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
              </div>
              <h2 className="text-2xl font-bold text-slate-100 mb-2 text-center">
                Transfer Ownership?
              </h2>
              <p className="text-slate-400 text-center mb-4">
                You are about to transfer ownership of{" "}
                <span className="font-semibold text-slate-200">
                  {vault?.name}
                </span>{" "}
                to{" "}
                <span className="font-semibold text-slate-200">
                  {transferMember.userName ||
                    transferMember.userEmail ||
                    "this member"}
                </span>
                .
              </p>
              <div className="bg-yellow-500/10 border border-yellow-500/30 rounded-lg p-4 mt-4">
                <div className="flex items-start gap-2">
                  <AlertTriangle className="w-5 h-5 text-yellow-400 flex-shrink-0 mt-0.5" />
                  <p className="text-yellow-400 text-sm font-medium">
                    Important: After transferring ownership, you will become an
                    Admin and will no longer have full control over this vault.
                  </p>
                </div>
              </div>
            </div>
            <div className="flex space-x-3">
              <button
                type="button"
                onClick={() => {
                  setShowTransferModal(false);
                  setTransferMember(null);
                }}
                className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleConfirmTransfer}
                className="flex-1 px-4 py-3 bg-yellow-500 text-white font-semibold rounded-lg hover:bg-yellow-600 transition-colors cursor-pointer"
              >
                Transfer Ownership
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Create/Edit Item Modal */}
      <CreateVaultItemModal
        isOpen={showItemModal}
        onClose={() => {
          setShowItemModal(false);
          setEditingItem(undefined);
        }}
        vaultId={vaultId}
        members={members.filter((m) => m.status === "Active")}
        onSuccess={loadVaultData}
        editingItem={editingItem}
        currentUserEmail={user?.email}
      />

      {/* Delete Item Modal */}
      <DeleteItemModal
        isOpen={showDeleteModal}
        onClose={() => {
          setShowDeleteModal(false);
          setDeletingItem(null);
          setDeleting(false);
        }}
        onConfirm={async () => {
          if (!deletingItem) return;
          setDeleting(true);
          try {
            await apiClient.deleteVaultItem(vaultId, deletingItem.id);
            toast.success("Item deleted successfully");
            setShowDeleteModal(false);
            setDeletingItem(null);
            loadVaultData();
          } catch (error: unknown) {
            // Don't show toast for session expiration - it's already handled in API client
            if (error instanceof SessionExpiredError) {
              return;
            }
            const errorMessage =
              error instanceof Error ? error.message : "Failed to delete item";
            toast.error(errorMessage);
          } finally {
            setDeleting(false);
          }
        }}
        itemTitle={deletingItem?.title || ""}
        itemType={deletingItem?.itemType || ""}
        hasDocument={!!deletingItem?.document}
        loading={deleting}
      />

      {/* View Item Modal */}
      <ViewItemModal
        isOpen={showViewModal}
        onClose={() => {
          setShowViewModal(false);
          setViewingItem(null);
        }}
        item={viewingItem}
        vaultId={vaultId}
        onEdit={async () => {
          if (viewingItem) {
            try {
              // Fetch full item data with decrypted content
              const fullItem = await apiClient.getVaultItem(
                vaultId,
                viewingItem.id
              );
              setEditingItem(fullItem);
              setShowViewModal(false);
              setShowItemModal(true);
            } catch (error: unknown) {
              // Don't show toast for session expiration - it's already handled in API client
              if (error instanceof SessionExpiredError) {
                return;
              }
              const errorMessage =
                error instanceof Error
                  ? error.message
                  : "Failed to load item for editing";
              toast.error(errorMessage);
            }
          }
        }}
        onDelete={() => {
          if (viewingItem) {
            setDeletingItem(viewingItem);
            setShowViewModal(false);
            setShowDeleteModal(true);
          }
        }}
        canEdit={
          vault &&
          (vault.userPrivilege === "Owner" ||
            vault.userPrivilege === "Admin" ||
            vault.userPrivilege === "Member") &&
          viewingItem?.userPermission === "Edit"
        }
      />

      {/* Delete Vault Modal */}
      <DeleteVaultModal
        isOpen={showDeleteVaultModal}
        onClose={() => {
          setShowDeleteVaultModal(false);
          setDeletingVault(false);
        }}
        onConfirm={handleDeleteVault}
        vaultName={vault?.name || ""}
        itemCount={items.length}
        loading={deletingVault}
      />
    </div>
  );
}

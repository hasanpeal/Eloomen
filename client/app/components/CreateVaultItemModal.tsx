"use client";

import { useState, useEffect } from "react";
import {
  CreateVaultItemRequest,
  UpdateVaultItemRequest,
  ItemType,
  VaultMember,
  VaultItem,
  ItemPermission,
  ItemVisibilityRequest,
  SessionExpiredError,
  WalletType,
} from "../lib/api";
import toast from "react-hot-toast";

interface CreateVaultItemModalProps {
  isOpen: boolean;
  onClose: () => void;
  vaultId: number;
  members: VaultMember[];
  onSuccess: () => void;
  editingItem?: VaultItem;
  currentUserEmail?: string;
}

export default function CreateVaultItemModal({
  isOpen,
  onClose,
  vaultId,
  members,
  onSuccess,
  editingItem,
  currentUserEmail,
}: CreateVaultItemModalProps) {
  const [itemType, setItemType] = useState<ItemType>(
    editingItem?.itemType || "Password"
  );
  const [title, setTitle] = useState(editingItem?.title || "");
  const [description, setDescription] = useState(
    editingItem?.description || ""
  );
  const [loading, setLoading] = useState(false);

  // Document fields
  const [documentFile, setDocumentFile] = useState<File | null>(null);

  // Password fields
  const [username, setUsername] = useState(
    editingItem?.password?.username || ""
  );
  const [password, setPassword] = useState(
    editingItem?.password?.password || ""
  );
  const [websiteUrl, setWebsiteUrl] = useState(
    editingItem?.password?.websiteUrl || ""
  );
  const [passwordNotes, setPasswordNotes] = useState(
    editingItem?.password?.notes || ""
  );

  // Note fields
  const [noteContent, setNoteContent] = useState(
    editingItem?.note?.content || ""
  );

  // Link fields
  const [url, setUrl] = useState(editingItem?.link?.url || "");
  const [linkNotes, setLinkNotes] = useState(editingItem?.link?.notes || "");

  // CryptoWallet fields
  const [walletType, setWalletType] = useState(
    editingItem?.cryptoWallet?.walletType || "SeedPhrase"
  );
  const [platformName, setPlatformName] = useState(
    editingItem?.cryptoWallet?.platformName || ""
  );
  const [blockchain, setBlockchain] = useState(
    editingItem?.cryptoWallet?.blockchain || ""
  );
  const [publicAddress, setPublicAddress] = useState(
    editingItem?.cryptoWallet?.publicAddress || ""
  );
  const [secret, setSecret] = useState(
    editingItem?.cryptoWallet?.secret || ""
  );
  const [cryptoNotes, setCryptoNotes] = useState(
    editingItem?.cryptoWallet?.notes || ""
  );

  // Visibility settings - map of memberId to permission (only View or Edit)
  const [visibilities, setVisibilities] = useState<
    Map<number, ItemPermission>
  >(new Map());

  // Reset form when modal opens/closes or editingItem changes
  useEffect(() => {
    if (isOpen) {
      if (editingItem) {
        setItemType(editingItem.itemType);
        setTitle(editingItem.title);
        setDescription(editingItem.description || "");
        setDocumentFile(null);
        setUsername(editingItem.password?.username || "");
        setPassword(editingItem.password?.password || "");
        setWebsiteUrl(editingItem.password?.websiteUrl || "");
        setPasswordNotes(editingItem.password?.notes || "");
        setNoteContent(editingItem.note?.content || "");
        setUrl(editingItem.link?.url || "");
        setLinkNotes(editingItem.link?.notes || "");
        setWalletType(editingItem.cryptoWallet?.walletType || "SeedPhrase");
        setPlatformName(editingItem.cryptoWallet?.platformName || "");
        setBlockchain(editingItem.cryptoWallet?.blockchain || "");
        setPublicAddress(editingItem.cryptoWallet?.publicAddress || "");
        setSecret(editingItem.cryptoWallet?.secret || "");
        setCryptoNotes(editingItem.cryptoWallet?.notes || "");

        // Load existing visibilities
        const visibilityMap = new Map<number, ItemPermission>();
        if (editingItem.visibilities) {
          editingItem.visibilities.forEach((vis) => {
            visibilityMap.set(vis.vaultMemberId, vis.permission);
          });
        }
        // Set defaults for members without visibility - creator gets Edit, others get View
        members.forEach((member) => {
          if (member.status === "Active" && !visibilityMap.has(member.id)) {
            const isCreator = currentUserEmail && 
              (member.userEmail?.toLowerCase() === currentUserEmail.toLowerCase());
            visibilityMap.set(member.id, isCreator ? "Edit" : "View");
          }
        });
        setVisibilities(visibilityMap);
      } else {
        // Reset to defaults for new item
        setItemType("Password");
        setTitle("");
        setDescription("");
        setDocumentFile(null);
        setUsername("");
        setPassword("");
        setWebsiteUrl("");
        setPasswordNotes("");
        setNoteContent("");
        setUrl("");
        setLinkNotes("");
        setWalletType("SeedPhrase");
        setPlatformName("");
        setBlockchain("");
        setPublicAddress("");
        setSecret("");
        setCryptoNotes("");
        // Initialize visibilities - creator gets Edit, others get View
        const defaultVisibilities = new Map<number, ItemPermission>();
        members.forEach((member) => {
          if (member.status === "Active") {
            // If this member is the current user (creator), give them Edit permission
            const isCreator = currentUserEmail && 
              (member.userEmail?.toLowerCase() === currentUserEmail.toLowerCase());
            defaultVisibilities.set(member.id, isCreator ? "Edit" : "View");
          }
        });
        setVisibilities(defaultVisibilities);
      }
    }
  }, [isOpen, editingItem, members, currentUserEmail]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const { apiClient } = await import("../lib/api");
      if (editingItem) {
        // Build visibilities array for all members (exclude owners - they always get Edit)
        const visibilityArray: ItemVisibilityRequest[] = [];
        visibilities.forEach((permission, memberId) => {
          const member = members.find(m => m.id === memberId);
          // Don't send owner visibilities - backend will automatically set them to Edit
          if (member && member.privilege !== "Owner") {
            visibilityArray.push({
              vaultMemberId: memberId,
              permission: permission,
            });
          }
        });

        const data: UpdateVaultItemRequest = {
          title: title || undefined,
          description: description || undefined,
          documentFile: documentFile || undefined,
          username: username || undefined,
          password: password || undefined,
          websiteUrl: websiteUrl || undefined,
          passwordNotes: passwordNotes || undefined,
          noteContent: noteContent || undefined,
          url: url || undefined,
          linkNotes: linkNotes || undefined,
          walletType: walletType as WalletType,
          platformName: platformName || undefined,
          blockchain: blockchain || undefined,
          publicAddress: publicAddress || undefined,
          secret: secret || undefined,
          cryptoNotes: cryptoNotes || undefined,
          visibilities: visibilityArray.length > 0 ? visibilityArray : undefined,
        };
        await apiClient.updateVaultItem(vaultId, editingItem.id, data);
        toast.success("Item updated");
      } else {
        // Build visibilities array for all members (exclude owners - they always get Edit)
        const visibilityArray: ItemVisibilityRequest[] = [];
        visibilities.forEach((permission, memberId) => {
          const member = members.find(m => m.id === memberId);
          // Don't send owner visibilities - backend will automatically set them to Edit
          if (member && member.privilege !== "Owner") {
            visibilityArray.push({
              vaultMemberId: memberId,
              permission: permission,
            });
          }
        });

        console.log("Creating item with visibilities:", visibilityArray);
        console.log("Visibility map size:", visibilities.size);
        console.log("Members count:", members.filter(m => m.status === "Active").length);

        const data: CreateVaultItemRequest = {
        vaultId,
        itemType,
        title,
        description: description || undefined,
        documentFile: documentFile || undefined,
        username: username || undefined,
        password: password || undefined,
        websiteUrl: websiteUrl || undefined,
        passwordNotes: passwordNotes || undefined,
        noteContent: noteContent || undefined,
        url: url || undefined,
        linkNotes: linkNotes || undefined,
        walletType: walletType as WalletType,
        platformName: platformName || undefined,
        blockchain: blockchain || undefined,
        publicAddress: publicAddress || undefined,
        secret: secret || undefined,
          cryptoNotes: cryptoNotes || undefined,
          visibilities: visibilityArray.length > 0 ? visibilityArray : undefined,
        };
        await apiClient.createVaultItem(vaultId, data);
        toast.success("Item created");
      }

      onSuccess();
      onClose();
    } catch (error: unknown) {
      // Don't show toast for session expiration - it's already handled in API client
      if (error instanceof SessionExpiredError) {
        return;
      }
      toast.error(error instanceof Error ? error.message : "Failed to save item");
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setLoading(false);
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-slate-800 rounded-2xl p-8 w-full max-w-2xl max-h-[90vh] overflow-y-auto border border-slate-700/50 shadow-2xl">
        <h2 className="text-2xl font-bold text-slate-100 mb-6">
          {editingItem ? "Edit Item" : "Create New Item"}
        </h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              Item Type
            </label>
            <select
              value={itemType}
              onChange={(e) => setItemType(e.target.value as ItemType)}
              className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent cursor-pointer disabled:cursor-not-allowed"
              disabled={!!editingItem}
            >
              <option value="Password">Password</option>
              <option value="Note">Note</option>
              <option value="Link">Link</option>
              <option value="CryptoWallet">Crypto Wallet</option>
              <option value="Document">Document</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              Title <span className="text-red-400">*</span>
            </label>
            <input
              type="text"
              autoComplete="off"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              required
              placeholder="Enter item title"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-300 mb-2">
              Description
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              rows={2}
              placeholder="Optional description"
            />
          </div>

          {itemType === "Document" && (
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">
                File
              </label>
              <input
                type="file"
                onChange={(e) =>
                  setDocumentFile(e.target.files?.[0] || null)
                }
                className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-indigo-500 file:text-white hover:file:bg-indigo-600"
                required={!editingItem}
              />
            </div>
          )}

          {itemType === "Password" && (
            <>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Username
                </label>
                <input
                  type="text"
                  autoComplete="off"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="Enter username"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Password
                </label>
                <input
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="Enter password"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Website URL
                </label>
                <input
                  type="url"
                  autoComplete="off"
                  value={websiteUrl}
                  onChange={(e) => setWebsiteUrl(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="https://example.com"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Notes
                </label>
                <textarea
                  value={passwordNotes}
                  onChange={(e) => setPasswordNotes(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  rows={3}
                  placeholder="Optional notes"
                />
              </div>
            </>
          )}

          {itemType === "Note" && (
            <div>
              <label className="block text-sm font-medium text-slate-300 mb-2">
                Content <span className="text-red-400">*</span>
              </label>
              <textarea
                value={noteContent}
                onChange={(e) => setNoteContent(e.target.value)}
                className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                rows={6}
                required
                placeholder="Enter note content"
              />
            </div>
          )}

          {itemType === "Link" && (
            <>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  URL <span className="text-red-400">*</span>
                </label>
                <input
                  type="url"
                  autoComplete="off"
                  value={url}
                  onChange={(e) => setUrl(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  required
                  placeholder="https://example.com"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Notes
                </label>
                <textarea
                  value={linkNotes}
                  onChange={(e) => setLinkNotes(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  rows={3}
                  placeholder="Optional notes"
                />
              </div>
            </>
          )}

          {itemType === "CryptoWallet" && (
            <>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Wallet Type
                </label>
                <select
                  value={walletType}
                  onChange={(e) => setWalletType(e.target.value as WalletType)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent cursor-pointer"
                >
                  <option value="SeedPhrase">Seed Phrase</option>
                  <option value="PrivateKey">Private Key</option>
                  <option value="ExchangeLogin">Exchange Login</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Platform Name
                </label>
                <input
                  type="text"
                  autoComplete="off"
                  value={platformName}
                  onChange={(e) => setPlatformName(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="e.g., MetaMask, Coinbase"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Blockchain
                </label>
                <input
                  type="text"
                  autoComplete="off"
                  value={blockchain}
                  onChange={(e) => setBlockchain(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="e.g., Ethereum, Bitcoin"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Public Address
                </label>
                <input
                  type="text"
                  autoComplete="off"
                  value={publicAddress}
                  onChange={(e) => setPublicAddress(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  placeholder="0x..."
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Secret (Seed Phrase/Private Key/Credentials){" "}
                  <span className="text-red-400">*</span>
                </label>
                <textarea
                  value={secret}
                  onChange={(e) => setSecret(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  rows={4}
                  required
                  placeholder="Enter seed phrase, private key, or exchange credentials"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-300 mb-2">
                  Notes
                </label>
                <textarea
                  value={cryptoNotes}
                  onChange={(e) => setCryptoNotes(e.target.value)}
                  className="w-full bg-slate-900/50 border border-slate-700 rounded-lg px-4 py-2 text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                  rows={3}
                  placeholder="Optional notes"
                />
              </div>
            </>
          )}

          {/* Member Visibility Configuration - Only show for creator when editing */}
          {(!editingItem || (editingItem && currentUserEmail && 
            (() => {
              const currentMember = members.find(m => 
                m.userEmail?.toLowerCase() === currentUserEmail.toLowerCase()
              );
              return currentMember && currentMember.userId === editingItem.createdByUserId;
            })())) && (
          <div className="pt-6 border-t border-slate-700/50">
            <label className="block text-sm font-medium text-slate-300 mb-3">
              Member Permissions
            </label>
            <p className="text-xs text-slate-400 mb-4">
              {editingItem ? "Update which members can view or edit this item" : "Configure which members can view or edit this item"}
            </p>
            <div className="space-y-2 max-h-60 overflow-y-auto">
              {members
                .filter((m) => m.status === "Active")
                .map((member) => (
                  <div
                    key={member.id}
                    className="flex items-center justify-between bg-slate-900/50 rounded-lg p-3 border border-slate-700/50"
                  >
                    <div className="flex-1">
                      <p className="text-sm font-medium text-slate-100">
                        {member.userName || member.userEmail || "Unknown User"}{" "}
                        <span className="text-slate-400 font-normal">
                          ({member.privilege})
                        </span>
                      </p>
                    </div>
                    {member.privilege === "Owner" ? (
                      <span className="ml-4 text-sm text-slate-400 italic">
                        Always Edit
                      </span>
                    ) : (
                      <select
                        value={visibilities.get(member.id) || "View"}
                        onChange={(e) => {
                          const newVisibilities = new Map(visibilities);
                          const value = e.target.value as ItemPermission;
                          newVisibilities.set(member.id, value);
                          setVisibilities(newVisibilities);
                        }}
                        className="ml-4 bg-slate-800 border border-slate-600 rounded-lg px-3 py-1.5 text-sm text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent cursor-pointer"
                      >
                        <option value="View">View</option>
                        <option value="Edit">Edit</option>
                      </select>
                    )}
                  </div>
                ))}
              {members.filter((m) => m.status === "Active").length === 0 && (
                <p className="text-sm text-slate-400 text-center py-4">
                  No active members in this vault
                </p>
              )}
            </div>
          </div>
          )}

          <div className="flex gap-3 justify-end pt-6 border-t border-slate-700/50">
            <button
              type="button"
              onClick={handleClose}
              className="px-6 py-2 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-6 py-2 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
            >
              {loading ? "Saving" : editingItem ? "Update" : "Create"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}


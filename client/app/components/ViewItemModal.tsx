"use client";

import { useState, useEffect } from "react";
import { VaultItem, apiClient } from "../lib/api";
import { FileText, Key, StickyNote, Link as LinkIcon, Wallet, Package, X } from "lucide-react";

interface ViewItemModalProps {
  isOpen: boolean;
  onClose: () => void;
  item: VaultItem | null;
  vaultId: number;
  onEdit?: () => void;
  onDelete?: () => void;
  canEdit?: boolean;
}

export default function ViewItemModal({
  isOpen,
  onClose,
  item,
  vaultId,
  onEdit,
  onDelete,
  canEdit = false,
}: ViewItemModalProps) {
  const [fullItem, setFullItem] = useState<VaultItem | null>(item);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen && item) {
      // Fetch full item with decrypted data
      const fetchFullItem = async () => {
        setLoading(true);
        try {
          const fetched = await apiClient.getVaultItem(vaultId, item.id);
          setFullItem(fetched);
        } catch (error) {
          console.error("Failed to fetch full item:", error);
          setFullItem(item); // Fallback to item from list
        } finally {
          setLoading(false);
        }
      };
      fetchFullItem();
    }
  }, [isOpen, item, vaultId]);

  if (!isOpen || !item) return null;

  const displayItem = fullItem || item;

  const getItemIcon = (itemType: string) => {
    const iconClass = "w-8 h-8 text-slate-400";
    switch (itemType) {
      case "Document":
        return <FileText className={iconClass} />;
      case "Password":
        return <Key className={iconClass} />;
      case "Note":
        return <StickyNote className={iconClass} />;
      case "Link":
        return <LinkIcon className={iconClass} />;
      case "CryptoWallet":
        return <Wallet className={iconClass} />;
      default:
        return <Package className={iconClass} />;
    }
  };

  const handleViewDocument = () => {
    if (displayItem?.document?.downloadUrl) {
      window.open(displayItem.document.downloadUrl, "_blank");
    }
  };

  const handleDownload = () => {
    if (displayItem?.document?.downloadUrl) {
      // Create a temporary anchor element to trigger download
      const link = document.createElement("a");
      link.href = displayItem.document.downloadUrl;
      link.download = displayItem.document.originalFileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  };

  const handleOpenLink = () => {
    if (displayItem?.link?.url) {
      window.open(displayItem.link.url, "_blank");
    }
  };

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-slate-800 rounded-2xl p-4 md:p-8 max-w-2xl w-full max-h-[90vh] overflow-y-auto border border-slate-700/50 shadow-2xl">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-3 flex-1 min-w-0">
            <div className="flex-shrink-0">
              {getItemIcon(displayItem.itemType)}
            </div>
            <div className="min-w-0 flex-1">
              <h2 className="text-xl md:text-2xl font-bold text-slate-100 truncate">
                {displayItem.title}
              </h2>
              <p className="text-sm text-slate-400 capitalize">
                {displayItem.itemType}
              </p>
            </div>
          </div>
          {loading && (
            <div className="text-sm text-slate-400 mb-4">
              Loading item details...
            </div>
          )}
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-200 transition-colors flex-shrink-0 ml-2"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {displayItem.description && (
          <div className="mb-6">
            <h3 className="text-sm font-semibold text-slate-300 mb-2">
              Description
            </h3>
            <p className="text-slate-400">{displayItem.description}</p>
          </div>
        )}

        {/* Document View */}
        {displayItem.itemType === "Document" && displayItem.document && (
          <div className="mb-6">
            <h3 className="text-sm font-semibold text-slate-300 mb-3">
              Document
            </h3>
            <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
              <div className="flex items-center justify-between mb-2">
                <div>
                  <p className="text-slate-100 font-medium">
                    {displayItem.document.originalFileName}
                  </p>
                  <p className="text-xs text-slate-400">
                    {(displayItem.document.fileSize / 1024).toFixed(2)} KB •{" "}
                    {displayItem.document.contentType}
                  </p>
                </div>
                {displayItem.document.downloadUrl && (
                  <div className="flex gap-2">
                    <button
                      onClick={handleViewDocument}
                      className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
                    >
                      View
                    </button>
                    <button
                      onClick={handleDownload}
                      className="px-4 py-2 bg-slate-700 text-slate-200 rounded-lg hover:bg-slate-600 transition-colors"
                    >
                      Download
                    </button>
                  </div>
                )}
              </div>
              <p className="text-xs text-slate-500">
                Uploaded:{" "}
                {new Date(displayItem.document.uploadedAt).toLocaleString()}
              </p>
            </div>
          </div>
        )}

        {/* Password View */}
        {displayItem.itemType === "Password" && displayItem.password && (
          <div className="mb-6">
            <h3 className="text-sm font-semibold text-slate-300 mb-3">
              Password Details
            </h3>
            <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50 space-y-3">
              {displayItem.password.username && (
                <div>
                  <label className="text-xs text-slate-400">Username</label>
                  <p className="text-slate-100 font-mono">
                    {displayItem.password.username}
                  </p>
                </div>
              )}
              {displayItem.password.password && (
                <div>
                  <label className="text-xs text-slate-400">Password</label>
                  <p className="text-slate-100 font-mono">
                    {displayItem.password.password}
                  </p>
                </div>
              )}
              {displayItem.password.websiteUrl && (
                <div>
                  <label className="text-xs text-slate-400">Website</label>
                  <a
                    href={displayItem.password.websiteUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-indigo-400 hover:text-indigo-300"
                  >
                    <p>{displayItem.password.websiteUrl}</p>
                  </a>
                </div>
              )}
              {displayItem.password.notes && (
                <div>
                  <label className="text-xs text-slate-400">Notes</label>
                  <p className="text-slate-100 whitespace-pre-wrap">
                    {displayItem.password.notes}
                  </p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Note View */}
        {displayItem.itemType === "Note" && displayItem.note && (
          <div className="mb-6">
            <h3 className="text-sm font-semibold text-slate-300 mb-3">
              Note Content
            </h3>
            <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50">
              <p className="text-slate-100 whitespace-pre-wrap">
                {displayItem.note.content}
              </p>
            </div>
          </div>
        )}

        {/* Link View */}
        {displayItem.itemType === "Link" && displayItem.link && (
          <div className="mb-6">
            <h3 className="text-sm font-semibold text-slate-300 mb-3">Link</h3>
            <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50 space-y-3">
              <div>
                <label className="text-xs text-slate-400">URL</label>
                <a
                  href={displayItem.link.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  onClick={handleOpenLink}
                  className="block text-indigo-400 hover:text-indigo-300 break-all"
                >
                  {displayItem.link.url}
                </a>
              </div>
              {displayItem.link.notes && (
                <div>
                  <label className="text-xs text-slate-400">Notes</label>
                  <p className="text-slate-100 whitespace-pre-wrap">
                    {displayItem.link.notes}
                  </p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* CryptoWallet View */}
        {displayItem.itemType === "CryptoWallet" &&
          displayItem.cryptoWallet && (
            <div className="mb-6">
              <h3 className="text-sm font-semibold text-slate-300 mb-3">
                Crypto Wallet
              </h3>
              <div className="bg-slate-900/50 rounded-lg p-4 border border-slate-700/50 space-y-3">
                <div>
                  <label className="text-xs text-slate-400">Wallet Type</label>
                  <p className="text-slate-100 capitalize">
                    {displayItem.cryptoWallet.walletType}
                  </p>
                </div>
                {displayItem.cryptoWallet.platformName && (
                  <div>
                    <label className="text-xs text-slate-400">Platform</label>
                    <p className="text-slate-100">
                      {displayItem.cryptoWallet.platformName}
                    </p>
                  </div>
                )}
                {displayItem.cryptoWallet.blockchain && (
                  <div>
                    <label className="text-xs text-slate-400">Blockchain</label>
                    <p className="text-slate-100">
                      {displayItem.cryptoWallet.blockchain}
                    </p>
                  </div>
                )}
                {displayItem.cryptoWallet.publicAddress && (
                  <div>
                    <label className="text-xs text-slate-400">
                      Public Address
                    </label>
                    <p className="text-slate-100 font-mono text-sm break-all">
                      {displayItem.cryptoWallet.publicAddress}
                    </p>
                  </div>
                )}
                {displayItem.cryptoWallet.secret && (
                  <div>
                    <label className="text-xs text-slate-400">Secret</label>
                    <p className="text-slate-100 font-mono text-sm break-all bg-slate-800 p-2 rounded">
                      {displayItem.cryptoWallet.secret}
                    </p>
                  </div>
                )}
                {displayItem.cryptoWallet.notes && (
                  <div>
                    <label className="text-xs text-slate-400">Notes</label>
                    <p className="text-slate-100 whitespace-pre-wrap">
                      {displayItem.cryptoWallet.notes}
                    </p>
                  </div>
                )}
              </div>
            </div>
          )}

        {/* Metadata */}
        <div className="mb-6 pt-6 border-t border-slate-700/50">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <label className="text-slate-400">Created</label>
              <p className="text-slate-100">
                {new Date(displayItem.createdAt).toLocaleString()}
              </p>
            </div>
            <div>
              <label className="text-slate-400">Last Updated</label>
              <p className="text-slate-100">
                {new Date(displayItem.updatedAt).toLocaleString()}
              </p>
            </div>
            {displayItem.createdByUserName && (
              <div>
                <label className="text-slate-400">Created By</label>
                <p className="text-slate-100">
                  {displayItem.createdByUserName}
                </p>
              </div>
            )}
            {displayItem.userPermission && (
              <div>
                <label className="text-slate-400">Your Permission</label>
                <p className="text-slate-100 capitalize">
                  {displayItem.userPermission}
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex gap-3 pt-6 border-t border-slate-700/50">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors cursor-pointer"
          >
            Close
          </button>
          {displayItem.userPermission === "Edit" && (
            <>
              <button
                onClick={() => {
                  onClose();
                  onEdit?.();
                }}
                className="px-4 py-2 bg-indigo-600 text-white font-semibold rounded-lg hover:bg-indigo-700 transition-colors cursor-pointer"
              >
                Edit
              </button>
              <button
                onClick={() => {
                  onClose();
                  onDelete?.();
                }}
                className="px-4 py-2 bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700 transition-colors cursor-pointer"
              >
                Delete
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}


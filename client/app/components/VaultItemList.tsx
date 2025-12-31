"use client";

import { VaultItem } from "../lib/api";
import { FileText, Key, StickyNote, Link as LinkIcon, Wallet, Package } from "lucide-react";

interface VaultItemListProps {
  items: VaultItem[];
  onEdit?: (item: VaultItem) => void;
  onDelete?: (item: VaultItem) => void;
  onView?: (item: VaultItem) => void;
  canView?: boolean;
}

export default function VaultItemList({
  items,
  onEdit,
  onDelete,
  onView,
  canView = false,
}: VaultItemListProps) {
  const getItemIcon = (itemType: string) => {
    const iconClass = "w-5 h-5 text-slate-400";
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

  const canUserEdit = (item: VaultItem) => {
    // Only allow edit if userPermission is explicitly "Edit"
    return item.userPermission === "Edit";
  };

  const canUserView = (item: VaultItem) => {
    return (
      item.userPermission === "View" ||
      item.userPermission === "Edit" ||
      canView
    );
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      {items.map((item) => (
        <div
          key={item.id}
          className="bg-slate-900/50 rounded-lg p-4 hover:bg-slate-900/70 transition-all border border-slate-700/50 cursor-pointer"
          onClick={() => canUserView(item) && onView?.(item)}
        >
          <div className="flex items-start justify-between mb-2">
            <div
              className="flex items-center gap-2 flex-1 cursor-pointer min-w-0"
              onClick={() => canUserView(item) && onView?.(item)}
            >
              <div className="flex-shrink-0">{getItemIcon(item.itemType)}</div>
              <h3 className="font-semibold text-base md:text-lg text-slate-100 truncate">
                {item.title}
              </h3>
            </div>
            <div className="flex gap-2 ml-2">
              {canUserView(item) && (
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onView?.(item);
                  }}
                  className="px-2 sm:px-3 py-1 text-xs bg-slate-700 text-slate-200 rounded hover:bg-slate-600 transition-colors whitespace-nowrap"
                  title="View"
                >
                  View
                </button>
              )}
              {canUserEdit(item) && (
                <>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onEdit?.(item);
                    }}
                    className="px-2 sm:px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 transition-colors whitespace-nowrap"
                    title="Edit"
                  >
                    Edit
                  </button>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onDelete?.(item);
                    }}
                    className="px-2 sm:px-3 py-1 text-xs bg-red-600 text-white rounded hover:bg-red-700 transition-colors whitespace-nowrap"
                    title="Delete"
                  >
                    Delete
                  </button>
                </>
              )}
            </div>
          </div>
          {item.description && (
            <p className="text-slate-400 text-xs sm:text-sm mb-2 line-clamp-2">
              {item.description}
            </p>
          )}
        </div>
      ))}
      {items.length === 0 && (
        <div className="col-span-full text-center py-12 text-slate-400"></div>
      )}
    </div>
  );
}

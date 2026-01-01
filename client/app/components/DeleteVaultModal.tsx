"use client";

interface DeleteVaultModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  vaultName: string;
  itemCount: number;
  loading?: boolean;
}

export default function DeleteVaultModal({
  isOpen,
  onClose,
  onConfirm,
  vaultName,
  itemCount,
  loading = false,
}: DeleteVaultModalProps) {
  if (!isOpen) return null;

  return (
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
          <h3 className="text-xl font-bold text-slate-100 text-center mb-2">
            Delete Vault?
          </h3>
          <p className="text-slate-400 text-center mb-4">
            Are you sure you want to delete{" "}
            <span className="font-semibold text-slate-200">&quot;{vaultName}&quot;</span>?
          </p>
          <div className="bg-red-500/10 border border-red-500/30 rounded-lg p-4">
            <p className="text-red-400 text-sm text-center">
              <strong>Warning:</strong> This action cannot be undone. This will
              permanently delete:
            </p>
            <ul className="text-red-300 text-sm mt-2 space-y-1 list-disc list-inside">
              <li>The vault and all its data</li>
              <li>
                All {itemCount} item{itemCount !== 1 ? "s" : ""} in this vault
              </li>
              <li>All documents stored</li>
              <li>All member associations</li>
              <li>All invites and policies</li>
            </ul>
          </div>
        </div>

        <div className="flex gap-3">
          <button
            onClick={onClose}
            disabled={loading}
            className="flex-1 px-4 py-3 bg-slate-700 text-slate-200 font-semibold rounded-lg hover:bg-slate-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            disabled={loading}
            className="flex-1 px-4 py-3 bg-red-500 text-white font-semibold rounded-lg hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {loading ? "Deleting..." : "Delete Vault"}
          </button>
        </div>
      </div>
    </div>
  );
}

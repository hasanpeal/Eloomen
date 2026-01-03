"use client";

import { useState } from "react";
import { X } from "lucide-react";
import { apiClient } from "../lib/api";
import toast from "react-hot-toast";
import { useAuth } from "../contexts/AuthContext";

interface ContactModalProps {
  isOpen: boolean;
  onClose: () => void;
  isPublic?: boolean; // If true, use public contact endpoint (no auth required)
}

export default function ContactModal({ isOpen, onClose, isPublic = false }: ContactModalProps) {
  const { isAuthenticated } = useAuth();
  const [formData, setFormData] = useState({
    name: "",
    email: "",
    message: "",
  });
  const [isLoading, setIsLoading] = useState(false);
  
  // Determine if we should use public endpoint
  const usePublicEndpoint = isPublic || !isAuthenticated;

  if (!isOpen) return null;

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (usePublicEndpoint) {
      if (!formData.name.trim() || !formData.email.trim() || !formData.message.trim()) {
        toast.error("All fields required");
        return;
      }
      // Basic email validation
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(formData.email.trim())) {
        toast.error("Email required");
        return;
      }
    } else {
      if (!formData.name.trim() || !formData.message.trim()) {
        toast.error("All fields required");
        return;
      }
    }

    setIsLoading(true);
    try {
      if (usePublicEndpoint) {
        await apiClient.sendPublicContact(
          formData.name.trim(),
          formData.email.trim(),
          formData.message.trim()
        );
      } else {
        await apiClient.sendContact(formData.name.trim(), formData.message.trim());
      }
      toast.success("Message sent");
      setFormData({ name: "", email: "", message: "" });
      onClose();
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : "Failed to send message";
      toast.error(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
      <div className="bg-slate-800/95 backdrop-blur-md rounded-2xl border border-slate-700/50 shadow-2xl w-full max-w-md">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-700/50">
          <h2 className="text-xl font-bold text-slate-100">Contact Us</h2>
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-slate-200 transition-colors cursor-pointer rounded-lg hover:bg-slate-700/50"
            aria-label="Close"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          <div>
            <label
              htmlFor="name"
              className="block text-sm font-semibold text-slate-300 mb-2"
            >
              Name
            </label>
            <input
              type="text"
              id="name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              required
              maxLength={200}
              autoComplete="name"
              className="w-full px-4 py-3 bg-slate-900/80 backdrop-blur-sm border border-slate-600 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all text-slate-100 font-medium placeholder:text-slate-500"
              placeholder="Your name"
            />
          </div>

          {usePublicEndpoint && (
            <div>
              <label
                htmlFor="email"
                className="block text-sm font-semibold text-slate-300 mb-2"
              >
                Email
              </label>
              <input
                type="email"
                id="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                required
                maxLength={256}
                autoComplete="email"
                className="w-full px-4 py-3 bg-slate-900/80 backdrop-blur-sm border border-slate-600 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all text-slate-100 font-medium placeholder:text-slate-500"
                placeholder="your.email@example.com"
              />
            </div>
          )}

          <div>
            <label
              htmlFor="message"
              className="block text-sm font-semibold text-slate-300 mb-2"
            >
              Message
            </label>
            <textarea
              id="message"
              name="message"
              value={formData.message}
              onChange={handleChange}
              required
              maxLength={5000}
              rows={6}
              className="w-full px-4 py-3 bg-slate-900/80 backdrop-blur-sm border border-slate-600 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none transition-all text-slate-100 font-medium placeholder:text-slate-500 resize-none"
              placeholder="Your message..."
            />
            <p className="mt-1 text-xs text-slate-500">
              {formData.message.length}/5000 characters
            </p>
          </div>

          {/* Actions */}
          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={isLoading}
              className="flex-1 px-4 py-3 bg-slate-700/50 hover:bg-slate-700 text-slate-200 rounded-xl font-semibold transition-all disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className="flex-1 px-4 py-3 bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white rounded-xl font-semibold hover:shadow-xl transition-all shadow-lg hover:shadow-indigo-500/50 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
            >
              {isLoading ? (
                <span className="flex items-center justify-center">
                  <svg
                    className="animate-spin -ml-1 mr-2 h-4 w-4 text-white"
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
                  Sending
                </span>
              ) : (
                "Send Message"
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}


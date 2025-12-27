"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { apiClient } from "../lib/api";

interface User {
  username: string;
  email: string;
}

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (
    usernameOrEmail: string,
    password: string,
    rememberMe: boolean
  ) => Promise<{
    success: boolean;
    requiresVerification?: boolean;
    verificationType?: "Email" | "Device";
    message?: string;
  }>;
  logout: () => Promise<void>;
  refreshAuth: () => Promise<void>;
  setUser: (user: User) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    // Check if user is authenticated on mount
    const checkAuth = async () => {
      const token = apiClient.getAccessToken();
      if (token) {
        // Try to decode token to get user info (basic implementation)
        try {
          const payload = JSON.parse(atob(token.split(".")[1]));
          setUser({
            username: payload.unique_name || payload.sub || "",
            email: payload.email || "",
          });
        } catch (error) {
          console.error("Error decoding token:", error);
          await apiClient.logout();
        }
      }
      setIsLoading(false);
    };
    checkAuth();
  }, []);

  const login = async (
    usernameOrEmail: string,
    password: string,
    rememberMe: boolean
  ) => {
    try {
      const response = await apiClient.login(
        usernameOrEmail,
        password,
        rememberMe
      );

      if (response.requireVerification) {
        return {
          success: false,
          requiresVerification: true,
          verificationType: response.verificationType,
          message: response.message,
        };
      }

      if (response.token && response.userName && response.email) {
        setUser({
          username: response.userName,
          email: response.email,
        });
        return { success: true };
      }

      return {
        success: false,
        message: "Login failed. Please try again.",
      };
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : "Login failed. Please try again.";
      return {
        success: false,
        message: errorMessage,
      };
    }
  };

  const logout = async () => {
    await apiClient.logout();
    setUser(null);
    router.push("/login");
  };

  const refreshAuth = async () => {
    try {
      await apiClient.refreshToken();
      // Re-fetch user info if needed
      const token = apiClient.getAccessToken();
      if (token) {
        try {
          const payload = JSON.parse(atob(token.split(".")[1]));
          setUser({
            username: payload.unique_name || payload.sub || "",
            email: payload.email || "",
          });
        } catch (error) {
          console.error("Error decoding token:", error);
        }
      }
    } catch (error) {
      console.error("Token refresh failed:", error);
      await logout();
    }
  };

  const setUserFromResponse = (userData: User) => {
    setUser(userData);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        logout,
        refreshAuth,
        setUser: setUserFromResponse,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

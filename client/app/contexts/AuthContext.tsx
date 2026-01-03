"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { apiClient, SessionExpiredError } from "../lib/api";

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
    rememberMe: boolean,
    inviteToken?: string
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
        try {
          // Fetch user info from backend - this will automatically refresh token if needed
          const userInfo = await apiClient.getCurrentUser();
          setUser({
            username: userInfo.username,
            email: userInfo.email,
          });
        } catch (error) {
          // If error is SessionExpiredError, user is already null, just set loading to false
          if (error instanceof SessionExpiredError) {
            // Session expired - user will be redirected by API client
            setUser(null);
          } else if (error instanceof Error) {
            console.error("Error fetching user info:", error);
            setUser(null);
          } else {
            setUser(null);
          }
        }
      } else {
        setUser(null);
      }
      setIsLoading(false);
    };
    checkAuth();
  }, []);

  const login = async (
    usernameOrEmail: string,
    password: string,
    rememberMe: boolean,
    inviteToken?: string
  ) => {
    try {
      const response = await apiClient.login(
        usernameOrEmail,
        password,
        rememberMe,
        inviteToken
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
        message: "Login failed",
      };
    } catch (error) {
      const errorMessage =
        error instanceof Error
          ? error.message
          : "Login failed";
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
      // Re-fetch user info from backend
      const userInfo = await apiClient.getCurrentUser();
      setUser({
        username: userInfo.username,
        email: userInfo.email,
      });
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

import toast from "react-hot-toast";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL;

// Custom error class for session expiration
export class SessionExpiredError extends Error {
  constructor() {
    super("Session expired. Please log in again.");
    this.name = "SessionExpiredError";
  }
}

export interface LoginResponse {
  requireVerification: boolean;
  verificationType?: "Email" | "Device";
  message?: string;
  userName?: string;
  email?: string;
  token?: string;
}

export interface VerifyEmailRequest {
  Email: string;
  Code: string;
}

export interface VerifyDeviceRequest {
  Code: string;
}

export interface RegisterRequest {
  Username: string;
  Email: string;
  Password: string;
}

export interface RegisterResponse {
  requireVerification: boolean;
  verificationType?: "Email" | "Device";
  message: string;
}

class ApiClient {
  private isRefreshing = false;
  private refreshPromise: Promise<string | null> | null = null;
  private sessionExpiredToastShown = false;
  private useSessionStorage = false; // Track whether to use sessionStorage (false = localStorage)

  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    retry = true
  ): Promise<T> {
    const token = this.getToken();
    const headers: HeadersInit = {
      "Content-Type": "application/json",
      ...options.headers,
    };

    if (token) {
      (headers as Record<string, string>).Authorization = `Bearer ${token}`;
    }

    try {
      const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...options,
        headers,
        credentials: "include", // Important for cookies (refresh token)
      });

      if (!response.ok) {
        // Handle 401 Unauthorized - try to refresh token
        // Don't retry if this is the refresh endpoint itself or auth endpoints
        const isRefreshEndpoint = endpoint === "/account/refresh";
        const isAuthEndpoint =
          endpoint === "/account/login" ||
          endpoint === "/account/register" ||
          endpoint === "/account/verify-email" ||
          endpoint === "/account/verify-device" ||
          endpoint === "/account/logout";

        if (
          response.status === 401 &&
          retry &&
          token &&
          !isRefreshEndpoint &&
          !isAuthEndpoint
        ) {
          try {
            // Attempt to refresh the token
            const newToken = await this.refreshTokenIfNeeded();
            if (newToken) {
              // Retry the original request with the new token
              return this.request<T>(endpoint, options, false);
            }
            // No valid refresh token - clear token and show toast
            this.removeToken();
            // Only show toast once
            if (!this.sessionExpiredToastShown) {
              this.sessionExpiredToastShown = true;
              toast.error("Session expired. Please log in again.");
              // Reset flag after a delay to allow for future session expirations
              setTimeout(() => {
                this.sessionExpiredToastShown = false;
              }, 5000);
            }
            // Redirect to login page after showing toast
            setTimeout(() => {
              if (typeof window !== "undefined") {
                window.location.href = "/login";
              }
            }, 1500);
            // Throw special error so callers can handle it
            throw new SessionExpiredError();
          } catch {
            // Refresh failed - clear token and show toast
            this.removeToken();
            // Only show toast once
            if (!this.sessionExpiredToastShown) {
              this.sessionExpiredToastShown = true;
              toast.error("Session expired. Please log in again.");
              // Reset flag after a delay to allow for future session expirations
              setTimeout(() => {
                this.sessionExpiredToastShown = false;
              }, 5000);
            }
            // Redirect to login page after showing toast
            setTimeout(() => {
              if (typeof window !== "undefined") {
                window.location.href = "/login";
              }
            }, 1500);
            // Throw special error so callers can handle it
            throw new SessionExpiredError();
          }
        }

        // Try to parse as JSON first
        const contentType = response.headers.get("content-type");
        let errorMessage = "An error occurred. Please try again.";

        if (contentType && contentType.includes("application/json")) {
          try {
            const errorData = await response.json();
            // Handle different error response formats
            if (typeof errorData === "string") {
              errorMessage = errorData;
            } else if (errorData.message) {
              errorMessage = errorData.message;
            } else if (errorData.error) {
              errorMessage = errorData.error;
            } else if (Array.isArray(errorData)) {
              // Handle validation errors array
              errorMessage = errorData.join(", ");
            } else if (typeof errorData === "object") {
              // Handle ModelState errors (object with property names as keys)
              const keys = Object.keys(errorData);
              if (keys.length > 0) {
                const firstKey = keys[0];
                const firstError = errorData[firstKey];
                if (Array.isArray(firstError)) {
                  errorMessage = firstError[0];
                } else if (typeof firstError === "string") {
                  errorMessage = firstError;
                } else {
                  errorMessage = "Validation failed. Please check your input.";
                }
              } else {
                errorMessage = "An error occurred. Please try again.";
              }
            }
          } catch {
            // If JSON parsing fails, try text
            const text = await response.text();
            if (text && text.length < 200 && !text.includes("<!DOCTYPE")) {
              errorMessage = text;
            }
          }
        } else {
          // Not JSON, try to get text but avoid HTML
          const text = await response.text();
          if (text && text.length < 200 && !text.includes("<!DOCTYPE")) {
            errorMessage = text;
          }
        }

        // Fallback to user-friendly messages based on status code if no message was found
        if (errorMessage === "An error occurred. Please try again.") {
          if (response.status === 404) {
            errorMessage = "Resource not found.";
          } else if (response.status === 500) {
            errorMessage = "Server error. Please try again later.";
          } else if (response.status === 401) {
            errorMessage = "Unauthorized. Please check your credentials.";
          } else if (response.status === 403) {
            errorMessage = "You don't have permission to access this resource.";
          } else if (response.status === 400) {
            errorMessage = "Invalid request. Please check your input.";
          }
        }

        throw new Error(errorMessage);
      }

      return response.json();
    } catch (error) {
      // Re-throw SessionExpiredError as-is so callers can handle it
      if (error instanceof SessionExpiredError) {
        throw error;
      }
      // Handle network errors
      if (error instanceof TypeError && error.message.includes("fetch")) {
        throw new Error(
          "Unable to connect to the server. Please check if the backend is running."
        );
      }
      throw error;
    }
  }

  private async refreshTokenIfNeeded(): Promise<string | null> {
    // If already refreshing, wait for the existing refresh to complete
    if (this.isRefreshing && this.refreshPromise) {
      return this.refreshPromise;
    }

    // Start a new refresh
    this.isRefreshing = true;
    this.refreshPromise = (async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/account/refresh`, {
          method: "POST",
          credentials: "include", // Important for refresh token cookie
        });

        if (!response.ok) {
          // No valid refresh token - return null
          return null;
        }

        const data = await response.json();
        if (data.token) {
          // Maintain the same storage type (sessionStorage vs localStorage) when refreshing
          this.setToken(data.token, this.useSessionStorage);
          return data.token;
        }
        return null;
      } catch {
        // Network error or invalid response - no valid refresh token
        return null;
      } finally {
        this.isRefreshing = false;
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  private getToken(): string | null {
    if (typeof window === "undefined") return null;
    // Check both storages - if sessionStorage has a token, we're in a non-remembered session
    const sessionToken = sessionStorage.getItem("accessToken");
    if (sessionToken) {
      this.useSessionStorage = true;
      return sessionToken;
    }
    const localToken = localStorage.getItem("accessToken");
    if (localToken) {
      this.useSessionStorage = false;
      return localToken;
    }
    return null;
  }

  private setToken(token: string, useSessionStorage: boolean = false): void {
    if (typeof window === "undefined") return;
    this.useSessionStorage = useSessionStorage;

    // Clear token from both storages first
    localStorage.removeItem("accessToken");
    sessionStorage.removeItem("accessToken");

    // Store in the appropriate storage
    if (useSessionStorage) {
      sessionStorage.setItem("accessToken", token);
    } else {
      localStorage.setItem("accessToken", token);
    }
  }

  private removeToken(): void {
    if (typeof window === "undefined") return;
    // Remove from both storages to be safe
    localStorage.removeItem("accessToken");
    sessionStorage.removeItem("accessToken");
    this.useSessionStorage = false;
  }

  async login(
    usernameOrEmail: string,
    password: string,
    rememberMe: boolean = false,
    inviteToken?: string
  ): Promise<LoginResponse> {
    const response = await this.request<LoginResponse>("/account/login", {
      method: "POST",
      body: JSON.stringify({
        UsernameOrEmail: usernameOrEmail,
        Password: password,
        RememberMe: rememberMe,
        InviteToken: inviteToken,
      }),
    });

    if (response.token) {
      this.setToken(response.token, !rememberMe); // Use sessionStorage when RememberMe is false
    }

    return response;
  }

  async register(
    data: RegisterRequest & { inviteToken?: string }
  ): Promise<RegisterResponse> {
    return this.request<RegisterResponse>("/account/register", {
      method: "POST",
      body: JSON.stringify(data),
    });
  }

  async verifyEmail(data: VerifyEmailRequest): Promise<{ message: string }> {
    const response = await this.request<{ message: string }>(
      "/account/verify-email",
      {
        method: "POST",
        body: JSON.stringify(data),
      }
    );

    // After email verification, try to get token if available
    // The backend might return a token in the response
    return response;
  }

  async resendVerification(email: string): Promise<{ message: string }> {
    return this.request<{ message: string }>("/account/resend-verification", {
      method: "POST",
      body: JSON.stringify({ Email: email }),
    });
  }

  async verifyDevice(
    usernameOrEmail: string,
    code: string,
    inviteToken?: string
  ): Promise<{
    message: string;
    userName?: string;
    email?: string;
    token?: string;
    inviteAccepted?: boolean;
  }> {
    const response = await this.request<{
      message: string;
      userName?: string;
      email?: string;
      token?: string;
      inviteAccepted?: boolean;
    }>("/account/verify-device", {
      method: "POST",
      body: JSON.stringify({
        UsernameOrEmail: usernameOrEmail,
        Code: code,
        InviteToken: inviteToken || undefined,
      }),
    });

    if (response.token) {
      // Use sessionStorage for device verification (non-persistent session)
      this.setToken(response.token, true);
    }

    return response;
  }

  async refreshToken(): Promise<{ token: string }> {
    // Use the internal refresh method which handles the refresh token cookie
    const newToken = await this.refreshTokenIfNeeded();
    if (newToken) {
      return { token: newToken };
    }
    throw new Error(
      "Failed to refresh token. No valid refresh token available."
    );
  }

  async logout(): Promise<void> {
    try {
      await this.request("/account/logout", {
        method: "POST",
      });
    } catch {
      toast.error("Failed to logout. Please try again.");
    } finally {
      this.removeToken();
      // Reset session expired flag on logout
      this.sessionExpiredToastShown = false;
    }
  }

  getAccessToken(): string | null {
    return this.getToken();
  }

  async forgotPassword(email: string): Promise<{ message: string }> {
    return this.request<{ message: string }>("/account/forgot-password", {
      method: "POST",
      body: JSON.stringify({ Email: email }),
    });
  }

  async resetPassword(
    email: string,
    code: string,
    newPassword: string
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>("/account/reset-password", {
      method: "POST",
      body: JSON.stringify({
        Email: email,
        Code: code,
        NewPassword: newPassword,
      }),
    });
  }

  async getCurrentUser(): Promise<{ username: string; email: string }> {
    return this.request<{ username: string; email: string }>("/account/user", {
      method: "GET",
    });
  }

  async getUserDevices(): Promise<UserDevice[]> {
    return this.request<UserDevice[]>("/account/devices", {
      method: "GET",
    });
  }

  async revokeDevice(deviceId: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/account/device/${deviceId}`, {
      method: "DELETE",
    });
  }

  async getAccountLogs(): Promise<AccountLog[]> {
    return this.request<AccountLog[]>("/account/logs", {
      method: "GET",
    });
  }

  async updateProfile(data: {
    username?: string;
    email?: string;
  }): Promise<{ message: string }> {
    return this.request<{ message: string }>("/account/profile", {
      method: "PUT",
      body: JSON.stringify(data),
    });
  }

  async deleteAccount(): Promise<{ message: string }> {
    return this.request<{ message: string }>("/account/account", {
      method: "DELETE",
    });
  }

  async changePassword(
    currentPassword: string,
    newPassword: string
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>("/account/change-password", {
      method: "POST",
      body: JSON.stringify({
        CurrentPassword: currentPassword,
        NewPassword: newPassword,
      }),
    });
  }

  async sendContact(name: string, message: string): Promise<{ message: string }> {
    return this.request<{ message: string }>("/contact", {
      method: "POST",
      body: JSON.stringify({
        Name: name,
        Message: message,
      }),
    });
  }

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }

  // Vault API methods
  async getVaults(): Promise<Vault[]> {
    return this.request<Vault[]>("/vault", {
      method: "GET",
    });
  }

  async getVault(id: number): Promise<Vault> {
    return this.request<Vault>(`/vault/${id}`, {
      method: "GET",
    });
  }

  async createVault(data: CreateVaultRequest): Promise<Vault> {
    return this.request<Vault>("/vault", {
      method: "POST",
      body: JSON.stringify(data),
    });
  }

  async updateVault(id: number, data: UpdateVaultRequest): Promise<Vault> {
    return this.request<Vault>(`/vault/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    });
  }

  async deleteVault(id: number): Promise<void> {
    return this.request<void>(`/vault/${id}`, {
      method: "DELETE",
    });
  }

  async restoreVault(id: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/vault/${id}/restore`, {
      method: "POST",
    });
  }

  // Invite methods
  async createInvite(
    vaultId: number,
    data: CreateInviteRequest
  ): Promise<VaultInvite> {
    return this.request<VaultInvite>(`/vault/${vaultId}/invites`, {
      method: "POST",
      body: JSON.stringify(data),
    });
  }

  async getVaultInvites(vaultId: number): Promise<VaultInvite[]> {
    return this.request<VaultInvite[]>(`/vault/${vaultId}/invites`, {
      method: "GET",
    });
  }

  async cancelInvite(
    vaultId: number,
    inviteId: number
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>(
      `/vault/${vaultId}/invites/${inviteId}/cancel`,
      {
        method: "POST",
      }
    );
  }

  async resendInvite(
    vaultId: number,
    inviteId: number
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>(
      `/vault/${vaultId}/invites/${inviteId}/resend`,
      {
        method: "POST",
      }
    );
  }

  async getInviteInfo(token: string): Promise<InviteInfo> {
    return this.request<InviteInfo>(
      `/vault/invites/info?token=${encodeURIComponent(token)}`,
      {
        method: "GET",
      }
    );
  }

  async acceptInvite(
    token: string,
    email: string
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>("/vault/invites/accept", {
      method: "POST",
      body: JSON.stringify({ token, email }),
    });
  }

  // Member methods
  async getVaultMembers(vaultId: number): Promise<VaultMember[]> {
    return this.request<VaultMember[]>(`/vault/${vaultId}/members`, {
      method: "GET",
    });
  }

  async getVaultLogs(vaultId: number): Promise<VaultLog[]> {
    return this.request<VaultLog[]>(`/vault/${vaultId}/logs`, {
      method: "GET",
    });
  }

  async removeMember(vaultId: number, memberId: number): Promise<void> {
    return this.request<void>(`/vault/${vaultId}/members/${memberId}`, {
      method: "DELETE",
    });
  }

  async updateMemberPrivilege(
    vaultId: number,
    data: UpdateMemberPrivilegeRequest
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>(
      `/vault/${vaultId}/members/privilege`,
      {
        method: "PUT",
        body: JSON.stringify(data),
      }
    );
  }

  async transferOwnership(
    vaultId: number,
    data: TransferOwnershipRequest
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>(
      `/vault/${vaultId}/transfer-ownership`,
      {
        method: "POST",
        body: JSON.stringify(data),
      }
    );
  }

  async leaveVault(vaultId: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/vault/${vaultId}/leave`, {
      method: "POST",
    });
  }

  // Policy methods
  async releaseVaultManually(vaultId: number): Promise<{ message: string }> {
    return this.request<{ message: string }>(`/vault/${vaultId}/release`, {
      method: "POST",
    });
  }

  // Vault Item API methods
  async getVaultItems(vaultId: number): Promise<VaultItem[]> {
    return this.request<VaultItem[]>(`/vault/${vaultId}/items`, {
      method: "GET",
    });
  }

  async getVaultItem(vaultId: number, itemId: number): Promise<VaultItem> {
    return this.request<VaultItem>(`/vault/${vaultId}/items/${itemId}`, {
      method: "GET",
    });
  }

  async createVaultItem(
    vaultId: number,
    data: CreateVaultItemRequest
  ): Promise<VaultItem> {
    const formData = new FormData();
    formData.append("vaultId", vaultId.toString());
    formData.append("itemType", data.itemType);
    formData.append("title", data.title);
    if (data.description) formData.append("description", data.description);

    // Document
    if (data.documentFile) {
      formData.append("documentFile", data.documentFile);
    }

    // Password
    if (data.itemType === "Password") {
      if (data.username) formData.append("username", data.username);
      if (data.password) formData.append("password", data.password);
      if (data.websiteUrl) formData.append("websiteUrl", data.websiteUrl);
      if (data.passwordNotes)
        formData.append("passwordNotes", data.passwordNotes);
    }

    // Note
    if (data.itemType === "Note") {
      if (data.noteContent) formData.append("noteContent", data.noteContent);
      if (data.contentFormat)
        formData.append("contentFormat", data.contentFormat);
    }

    // Link
    if (data.itemType === "Link") {
      if (data.url) formData.append("url", data.url);
      if (data.linkNotes) formData.append("linkNotes", data.linkNotes);
    }

    // CryptoWallet
    if (data.itemType === "CryptoWallet") {
      if (data.walletType) formData.append("walletType", data.walletType);
      if (data.platformName) formData.append("platformName", data.platformName);
      if (data.blockchain) formData.append("blockchain", data.blockchain);
      if (data.publicAddress)
        formData.append("publicAddress", data.publicAddress);
      if (data.secret) formData.append("secret", data.secret);
      if (data.cryptoNotes) formData.append("cryptoNotes", data.cryptoNotes);
    }

    // Visibilities
    if (data.visibilities && data.visibilities.length > 0) {
      formData.append("visibilities", JSON.stringify(data.visibilities));
    }

    const token = this.getToken();
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE_URL}/vault/${vaultId}/items`, {
      method: "POST",
      headers,
      credentials: "include",
      body: formData,
    });

    if (!response.ok) {
      const errorMessage = await this.getErrorMessage(response);
      throw new Error(errorMessage);
    }

    return response.json();
  }

  async updateVaultItem(
    vaultId: number,
    itemId: number,
    data: UpdateVaultItemRequest
  ): Promise<VaultItem> {
    const formData = new FormData();
    if (data.title) formData.append("title", data.title);
    if (data.description !== undefined)
      formData.append("description", data.description || "");

    // Document
    if (data.documentFile) {
      formData.append("documentFile", data.documentFile);
    }
    if (data.deleteDocument !== undefined)
      formData.append("deleteDocument", data.deleteDocument.toString());

    // Password
    if (data.username !== undefined)
      formData.append("username", data.username || "");
    if (data.password !== undefined)
      formData.append("password", data.password || "");
    if (data.websiteUrl !== undefined)
      formData.append("websiteUrl", data.websiteUrl || "");
    if (data.passwordNotes !== undefined)
      formData.append("passwordNotes", data.passwordNotes || "");

    // Note
    if (data.noteContent !== undefined)
      formData.append("noteContent", data.noteContent || "");
    if (data.contentFormat)
      formData.append("contentFormat", data.contentFormat);

    // Link
    if (data.url !== undefined) formData.append("url", data.url || "");
    if (data.linkNotes !== undefined)
      formData.append("linkNotes", data.linkNotes || "");

    // CryptoWallet
    if (data.walletType) formData.append("walletType", data.walletType);
    if (data.platformName !== undefined)
      formData.append("platformName", data.platformName || "");
    if (data.blockchain !== undefined)
      formData.append("blockchain", data.blockchain || "");
    if (data.publicAddress !== undefined)
      formData.append("publicAddress", data.publicAddress || "");
    if (data.secret !== undefined) formData.append("secret", data.secret || "");
    if (data.cryptoNotes !== undefined)
      formData.append("cryptoNotes", data.cryptoNotes || "");

    // Visibilities
    if (data.visibilities && data.visibilities.length > 0) {
      formData.append("visibilities", JSON.stringify(data.visibilities));
    }

    const token = this.getToken();
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(
      `${API_BASE_URL}/vault/${vaultId}/items/${itemId}`,
      {
        method: "PUT",
        headers,
        credentials: "include",
        body: formData,
      }
    );

    if (!response.ok) {
      const errorMessage = await this.getErrorMessage(response);
      throw new Error(errorMessage);
    }

    return response.json();
  }

  async deleteVaultItem(vaultId: number, itemId: number): Promise<void> {
    const token = this.getToken();
    const headers: HeadersInit = {};
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(
      `${API_BASE_URL}/vault/${vaultId}/items/${itemId}`,
      {
        method: "DELETE",
        headers,
        credentials: "include",
      }
    );

    if (!response.ok) {
      const errorMessage = await this.getErrorMessage(response);
      throw new Error(errorMessage);
    }

    // Handle 204 No Content (empty response)
    if (response.status === 204) {
      return;
    }

    // Try to parse JSON if there's content
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      await response.json();
    }
  }

  async restoreVaultItem(
    vaultId: number,
    itemId: number
  ): Promise<{ message: string }> {
    return this.request<{ message: string }>(
      `/vault/${vaultId}/items/${itemId}/restore`,
      {
        method: "POST",
      }
    );
  }

  private async getErrorMessage(response: Response): Promise<string> {
    const contentType = response.headers.get("content-type");
    if (contentType && contentType.includes("application/json")) {
      try {
        const errorData = await response.json();
        if (typeof errorData === "string") return errorData;
        if (errorData.message) return errorData.message;
        if (errorData.error) return errorData.error;
        if (Array.isArray(errorData)) return errorData.join(", ");
        // Handle ModelState errors (object with property names as keys)
        if (typeof errorData === "object") {
          const keys = Object.keys(errorData);
          if (keys.length > 0) {
            const firstKey = keys[0];
            const firstError = errorData[firstKey];
            if (Array.isArray(firstError)) {
              return firstError[0];
            } else if (typeof firstError === "string") {
              return firstError;
            }
          }
        }
      } catch {
        // Fall through to default message
      }
    }

    // Fallback to user-friendly messages based on status code
    if (response.status === 404) {
      return "Resource not found.";
    } else if (response.status === 500) {
      return "Server error. Please try again later.";
    } else if (response.status === 401) {
      return "Unauthorized. Please check your credentials.";
    } else if (response.status === 403) {
      return "You don't have permission to access this resource.";
    } else if (response.status === 400) {
      return "Invalid request. Please check your input.";
    }

    return "An error occurred. Please try again.";
  }
}

// Vault Types
export interface Vault {
  id: number;
  ownerId: string;
  ownerEmail?: string;
  ownerName?: string;
  originalOwnerId: string;
  originalOwnerEmail?: string;
  originalOwnerName?: string;
  name: string;
  description?: string;
  status: "Active" | "Deleted";
  createdAt: string;
  deletedAt?: string;
  userPrivilege?: "Owner" | "Admin" | "Member";
  policy?: VaultPolicy;
}

export interface VaultInvite {
  id: number;
  vaultId: number;
  inviterId: string;
  inviterEmail?: string;
  inviteeEmail: string;
  inviteeId?: string;
  privilege: "Owner" | "Admin" | "Member";
  status: "Pending" | "Sent" | "Accepted" | "Cancelled" | "Expired";
  expiresAt?: string;
  createdAt: string;
  acceptedAt?: string;
  note?: string;
}

export interface VaultMember {
  id: number;
  vaultId: number;
  userId: string;
  userEmail?: string;
  userName?: string;
  privilege: "Owner" | "Admin" | "Member";
  status: "Active" | "Left" | "Removed";
  removedById?: string;
  removedByEmail?: string;
  removedByName?: string;
  addedById?: string;
  addedByEmail?: string;
  addedByName?: string;
  joinedAt: string;
  leftAt?: string;
  removedAt?: string;
}

export interface VaultLog {
  id: number;
  vaultId: number;
  userId: string;
  userEmail?: string;
  userName?: string;
  targetUserId?: string;
  targetUserEmail?: string;
  targetUserName?: string;
  itemId?: number;
  action: string;
  timestamp: string;
  additionalContext?: string;
}

export interface VaultPolicy {
  policyType: "Immediate" | "TimeBased" | "ExpiryBased" | "ManualRelease";
  releaseStatus: "Pending" | "Released" | "Expired" | "Revoked";
  releaseDate?: string;
  expiresAt?: string;
  releasedAt?: string;
}

export interface CreateVaultRequest {
  name: string;
  description?: string;
  policyType: "Immediate" | "TimeBased" | "ExpiryBased" | "ManualRelease";
  releaseDate?: string;
  expiresAt?: string;
}

export interface UpdateVaultRequest {
  name: string;
  description?: string;
  policyType: "Immediate" | "TimeBased" | "ExpiryBased" | "ManualRelease";
  releaseDate?: string;
  expiresAt?: string;
}

export interface CreateInviteRequest {
  inviteeEmail: string;
  privilege: "Owner" | "Admin" | "Member";
  inviteExpiresAt?: string; // When the invite itself expires - ISO date string
  note?: string;
}

export interface UpdateMemberPrivilegeRequest {
  memberId: number;
  privilege: "Owner" | "Admin" | "Member";
}

export interface TransferOwnershipRequest {
  memberId: number;
}

export interface InviteInfo {
  inviteeEmail: string;
  isValid: boolean;
  errorMessage?: string;
  userExists: boolean;
}

// Vault Item Types
export type ItemType =
  | "Document"
  | "Password"
  | "Note"
  | "Link"
  | "CryptoWallet";
export type ItemStatus = "Active" | "Deleted";
export type ItemPermission = "View" | "Edit";
export type WalletType = "SeedPhrase" | "PrivateKey" | "ExchangeLogin";
export type ContentFormat = "PlainText";

export interface VaultItem {
  id: number;
  vaultId: number;
  createdByUserId: string;
  createdByUserName?: string;
  itemType: ItemType;
  title: string;
  description?: string;
  status: ItemStatus;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  deletedBy?: string;
  document?: VaultDocument;
  password?: VaultPassword;
  note?: VaultNote;
  link?: VaultLink;
  cryptoWallet?: VaultCryptoWallet;
  visibilities: ItemVisibility[];
  userPermission?: ItemPermission;
}

export interface VaultDocument {
  objectKey: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  downloadUrl?: string;
}

export interface VaultPassword {
  username?: string;
  password?: string;
  websiteUrl?: string;
  notes?: string;
}

export interface VaultNote {
  content: string;
  contentFormat: ContentFormat;
}

export interface VaultLink {
  url: string;
  notes?: string;
}

export interface VaultCryptoWallet {
  walletType: WalletType;
  platformName?: string;
  blockchain?: string;
  publicAddress?: string;
  secret?: string;
  notes?: string;
}

export interface ItemVisibility {
  id: number;
  vaultItemId: number;
  vaultMemberId: number;
  memberEmail?: string;
  memberName?: string;
  permission: ItemPermission;
}

export interface CreateVaultItemRequest {
  vaultId: number;
  itemType: ItemType;
  title: string;
  description?: string;
  documentFile?: File;
  // Password fields
  username?: string;
  password?: string;
  websiteUrl?: string;
  passwordNotes?: string;
  // Note fields
  noteContent?: string;
  contentFormat?: ContentFormat;
  // Link fields
  url?: string;
  linkNotes?: string;
  // CryptoWallet fields
  walletType?: WalletType;
  platformName?: string;
  blockchain?: string;
  publicAddress?: string;
  secret?: string;
  cryptoNotes?: string;
  // Visibility
  visibilities?: ItemVisibilityRequest[];
}

export interface UpdateVaultItemRequest {
  title?: string;
  description?: string;
  documentFile?: File;
  deleteDocument?: boolean;
  // Password fields
  username?: string;
  password?: string;
  websiteUrl?: string;
  passwordNotes?: string;
  // Note fields
  noteContent?: string;
  contentFormat?: ContentFormat;
  // Link fields
  url?: string;
  linkNotes?: string;
  // CryptoWallet fields
  walletType?: WalletType;
  platformName?: string;
  blockchain?: string;
  publicAddress?: string;
  secret?: string;
  cryptoNotes?: string;
  // Visibility
  visibilities?: ItemVisibilityRequest[];
}

export interface ItemVisibilityRequest {
  vaultMemberId: number;
  permission: ItemPermission;
}

export interface UserDevice {
  id: number;
  deviceIdentifier: string;
  isVerified: boolean;
  verifiedAt?: string;
  createdAt: string;
  activeTokens: number;
}

export interface AccountLog {
  id: number;
  action: string;
  timestamp: string;
  additionalContext?: string;
}

export const apiClient = new ApiClient();

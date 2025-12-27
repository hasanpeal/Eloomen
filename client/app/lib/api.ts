const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL;

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
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
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
        // Try to parse as JSON first
        const contentType = response.headers.get("content-type");
        let errorMessage = `HTTP error! status: ${response.status}`;

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
              errorMessage = errorData.join(", ");
            } else if (typeof errorData === "object") {
              // Try to extract meaningful error message
              const errorText = JSON.stringify(errorData);
              if (errorText.length < 200) {
                errorMessage = errorText;
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
          } else if (response.status === 404) {
            errorMessage =
              "API endpoint not found. Please check if the backend server is running.";
          } else if (response.status === 500) {
            errorMessage = "Server error. Please try again later.";
          } else if (response.status === 401) {
            errorMessage = "Unauthorized. Please check your credentials.";
          } else if (response.status === 403) {
            errorMessage =
              "Forbidden. You don't have permission to access this resource.";
          }
        }

        throw new Error(errorMessage);
      }

      return response.json();
    } catch (error) {
      // Handle network errors
      if (error instanceof TypeError && error.message.includes("fetch")) {
        throw new Error(
          "Unable to connect to the server. Please check if the backend is running."
        );
      }
      throw error;
    }
  }

  private getToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem("accessToken");
  }

  private setToken(token: string): void {
    if (typeof window === "undefined") return;
    localStorage.setItem("accessToken", token);
  }

  private removeToken(): void {
    if (typeof window === "undefined") return;
    localStorage.removeItem("accessToken");
  }

  async login(
    usernameOrEmail: string,
    password: string,
    rememberMe: boolean = false
  ): Promise<LoginResponse> {
    const response = await this.request<LoginResponse>("/account/login", {
      method: "POST",
      body: JSON.stringify({
        UsernameOrEmail: usernameOrEmail,
        Password: password,
        RememberMe: rememberMe,
      }),
    });

    if (response.token) {
      this.setToken(response.token);
    }

    return response;
  }

  async register(data: RegisterRequest): Promise<RegisterResponse> {
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
    code: string
  ): Promise<{
    message: string;
    userName?: string;
    email?: string;
    token?: string;
  }> {
    const response = await this.request<{
      message: string;
      userName?: string;
      email?: string;
      token?: string;
    }>("/account/verify-device", {
      method: "POST",
      body: JSON.stringify({
        UsernameOrEmail: usernameOrEmail,
        Code: code,
      }),
    });

    if (response.token) {
      this.setToken(response.token);
    }

    return response;
  }

  async refreshToken(): Promise<{ token: string }> {
    const response = await this.request<{ token: string }>("/account/refresh", {
      method: "POST",
    });

    if (response.token) {
      this.setToken(response.token);
    }

    return response;
  }

  async logout(): Promise<void> {
    try {
      await this.request("/account/logout", {
        method: "POST",
      });
    } catch (error) {
      console.error("Logout error:", error);
    } finally {
      this.removeToken();
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

  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }
}

export const apiClient = new ApiClient();

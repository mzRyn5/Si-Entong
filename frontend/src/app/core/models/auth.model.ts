export type UserRole = 'sysadmin' | 'owner' | 'admin';

export interface AuthUser {
  id: string;
  name: string;
  username: string;
  role: UserRole;
  isActive?: boolean;
  storeId?: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: AuthUser;
}

export interface AuthSession extends LoginResponse {}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
}

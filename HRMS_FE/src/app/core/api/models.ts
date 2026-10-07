export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface TwoFactorChallenge {
  challengeToken: string;
  expiresAtUtc: string;
}

export interface LoginResult {
  tokens: AuthTokens | null;
  challenge: TwoFactorChallenge | null;
  requiresTwoFactor: boolean;
}

export interface CurrentUser {
  userId: string;
  email: string;
  fullName: string;
  tenantId: string;
  tenantSlug: string;
  emailConfirmed: boolean;
}

export interface Employee {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  hireDate: string;
  employmentType: string;
  userId: string | null;
  createdAtUtc: string;
}

export interface CreateEmployee {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  workEmail: string;
  hireDate: string;
  employmentType: number;
}

export interface Plan {
  id: string;
  code: string;
  name: string;
  description: string | null;
  monthlyPriceMinor: number;
  yearlyPriceMinor: number;
  currency: string;
  maxEmployees: number | null;
  features: string[];
}

export interface Entitlements {
  hasSubscription: boolean;
  isEntitled: boolean;
  planCode: string;
  status: number;
  currentPeriodEndUtc: string | null;
  maxEmployees: number | null;
  usedEmployees: number;
  features: string[];
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

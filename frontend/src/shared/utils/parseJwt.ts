interface JwtPayload {
  sub: string;
  name: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string;
  PatientId?: string;
}

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export function parseJwtClaims(token: string): {
  userId: string;
  role: 'Patient' | 'Provider' | 'Admin' | 'FrontDesk';
  fullName: string;
  patientId: string | null;
} | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const payload = JSON.parse(atob(parts[1]!)) as JwtPayload;
    const role = payload[ROLE_CLAIM];

    if (!payload.sub || !role || !payload.name) return null;
    if (role !== 'Patient' && role !== 'Provider' && role !== 'Admin' && role !== 'FrontDesk') return null;

    return {
      userId: payload.sub,
      role,
      fullName: payload.name,
      patientId: payload.PatientId ?? null,
    };
  } catch {
    return null;
  }
}

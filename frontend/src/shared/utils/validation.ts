const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PHONE_RE = /^\+?[\d\s().-]{7,20}$/;
const PASSWORD_RE = /^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=[\]{};':"\\|,.<>/?]).{8,}$/;

export function validateRequired(value: string, label: string): string | null {
  return value.trim() ? null : `${label} is required.`;
}

export function validateEmail(value: string): string | null {
  if (!value.trim()) return 'Email address is required.';
  return EMAIL_RE.test(value) ? null : 'Please enter a valid email address.';
}

export function validatePhone(value: string): string | null {
  if (!value.trim()) return 'Phone number is required.';
  return PHONE_RE.test(value) ? null : 'Please enter a valid phone number.';
}

export function validateDob(value: string): string | null {
  if (!value) return 'Date of birth is required.';
  const date = new Date(value);
  if (isNaN(date.getTime())) return 'Please enter a valid date.';
  if (date > new Date()) return 'Date of birth cannot be in the future.';
  return null;
}

export function validatePassword(value: string): string | null {
  if (!value) return 'Password is required.';
  return PASSWORD_RE.test(value)
    ? null
    : 'At least 8 characters with one uppercase, one number, and one special character.';
}

export function validateConfirmPassword(
  password: string,
  confirmPassword: string,
): string | null {
  if (!confirmPassword) return 'Please confirm your password.';
  return password === confirmPassword ? null : 'Passwords do not match.';
}

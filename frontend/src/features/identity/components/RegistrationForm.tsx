import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { cn } from '@/lib/utils';
import { Eye, EyeOff, Loader2 } from 'lucide-react';
import { type FormEvent, useCallback, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ErrorBanner } from '../../../shared/components/ErrorBanner';
import {
  validateConfirmPassword,
  validateDob,
  validateEmail,
  validatePassword,
  validatePhone,
  validateRequired,
} from '../../../shared/utils/validation';
import { registerPatient, type RegistrationRequest } from '../api/authApi';

interface FieldErrors {
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
  phone?: string | null;
  dob?: string | null;
  password?: string | null;
  confirmPassword?: string | null;
}

type FieldName = keyof FieldErrors;

export function RegistrationForm() {
  const navigate = useNavigate();
  const formRef = useRef<HTMLFormElement>(null);

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [dob, setDob] = useState('');
  const [gender, setGender] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [errors, setErrors] = useState<FieldErrors>({});
  const [touched, setTouched] = useState<Partial<Record<FieldName, boolean>>>(
    {},
  );
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const [lastSubmission, setLastSubmission] =
    useState<RegistrationRequest | null>(null);

  const validateField = useCallback(
    (name: FieldName): string | null => {
      switch (name) {
        case 'firstName':
          return validateRequired(firstName, 'First name');
        case 'lastName':
          return validateRequired(lastName, 'Last name');
        case 'email':
          return validateEmail(email);
        case 'phone':
          return validatePhone(phone);
        case 'dob':
          return validateDob(dob);
        case 'password':
          return validatePassword(password);
        case 'confirmPassword':
          return validateConfirmPassword(password, confirmPassword);
        default:
          return null;
      }
    },
    [firstName, lastName, email, phone, dob, password, confirmPassword],
  );

  const handleBlur = useCallback(
    (name: FieldName) => {
      setTouched((prev) => ({ ...prev, [name]: true }));
      setErrors((prev) => ({ ...prev, [name]: validateField(name) }));
    },
    [validateField],
  );

  const validateAll = useCallback((): boolean => {
    const fields: FieldName[] = [
      'firstName',
      'lastName',
      'email',
      'phone',
      'dob',
      'password',
      'confirmPassword',
    ];
    const newErrors: FieldErrors = {};
    const newTouched: Partial<Record<FieldName, boolean>> = {};
    let valid = true;

    for (const name of fields) {
      newTouched[name] = true;
      const err = validateField(name);
      newErrors[name] = err;
      if (err) valid = false;
    }

    setErrors(newErrors);
    setTouched(newTouched);

    if (!valid) {
      const firstErrorField = fields.find((f) => newErrors[f]);
      if (firstErrorField) {
        const el = formRef.current?.querySelector<HTMLElement>(
          `[name="${firstErrorField}"]`,
        );
        el?.focus();
      }
    }

    return valid;
  }, [validateField]);

  const doSubmit = useCallback(
    async (data: RegistrationRequest) => {
      setIsSubmitting(true);
      setApiError(null);

      const result = await registerPatient(data);
      setIsSubmitting(false);

      if (result.success) {
        void navigate('/login');
        return;
      }

      if (result.error.field) {
        setErrors((prev) => ({
          ...prev,
          [result.error.field as FieldName]: result.error.message,
        }));
        setTouched((prev) => ({
          ...prev,
          [result.error.field as FieldName]: true,
        }));
        const el = formRef.current?.querySelector<HTMLElement>(
          `[name="${result.error.field}"]`,
        );
        el?.focus();
      } else {
        setApiError(result.error.message);
      }
    },
    [navigate],
  );

  const handleSubmit = useCallback(
    (e: FormEvent) => {
      e.preventDefault();
      if (!validateAll()) return;

      const data: RegistrationRequest = {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        phone: phone.trim(),
        dateOfBirth: dob,
        gender,
        password,
      };

      setLastSubmission(data);
      void doSubmit(data);
    },
    [
      validateAll,
      firstName,
      lastName,
      email,
      phone,
      dob,
      gender,
      password,
      doSubmit,
    ],
  );

  const handleRetry = useCallback(() => {
    if (lastSubmission) {
      void doSubmit(lastSubmission);
    }
  }, [lastSubmission, doSubmit]);

  const hasError = (name: FieldName) => touched[name] && errors[name];
  const errorClass = (name: FieldName) =>
    cn(hasError(name) && 'border-destructive focus-visible:ring-destructive');

  return (
    <form
      ref={formRef}
      onSubmit={handleSubmit}
      noValidate
      aria-label="Patient registration form"
    >
      {apiError && (
        <ErrorBanner
          message={apiError}
          onRetry={handleRetry}
          onDismiss={() => {
            setApiError(null);
          }}
        />
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
        {/* First Name */}
        <div className="space-y-1">
          <Label htmlFor="firstName">
            First Name{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <Input
            type="text"
            id="firstName"
            name="firstName"
            placeholder="Enter first name"
            value={firstName}
            onChange={(e) => {
              setFirstName(e.target.value);
            }}
            onBlur={() => {
              handleBlur('firstName');
            }}
            required
            aria-describedby="firstName-error"
            aria-invalid={touched.firstName && !!errors.firstName}
            autoComplete="given-name"
            disabled={isSubmitting}
            className={errorClass('firstName')}
          />
          {hasError('firstName') && (
            <p
              className="text-xs text-destructive"
              id="firstName-error"
              role="alert"
            >
              {errors.firstName}
            </p>
          )}
        </div>

        {/* Last Name */}
        <div className="space-y-1">
          <Label htmlFor="lastName">
            Last Name{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <Input
            type="text"
            id="lastName"
            name="lastName"
            placeholder="Enter last name"
            value={lastName}
            onChange={(e) => {
              setLastName(e.target.value);
            }}
            onBlur={() => {
              handleBlur('lastName');
            }}
            required
            aria-describedby="lastName-error"
            aria-invalid={touched.lastName && !!errors.lastName}
            autoComplete="family-name"
            disabled={isSubmitting}
            className={errorClass('lastName')}
          />
          {hasError('lastName') && (
            <p
              className="text-xs text-destructive"
              id="lastName-error"
              role="alert"
            >
              {errors.lastName}
            </p>
          )}
        </div>

        {/* Email */}
        <div className="space-y-1 sm:col-span-2">
          <Label htmlFor="email">
            Email Address{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <Input
            type="email"
            id="email"
            name="email"
            placeholder="you@example.com"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value);
            }}
            onBlur={() => {
              handleBlur('email');
            }}
            required
            aria-describedby="email-error"
            aria-invalid={touched.email && !!errors.email}
            autoComplete="email"
            disabled={isSubmitting}
            className={errorClass('email')}
          />
          {hasError('email') && (
            <p
              className="text-xs text-destructive"
              id="email-error"
              role="alert"
            >
              {errors.email}
            </p>
          )}
        </div>

        {/* Phone */}
        <div className="space-y-1 sm:col-span-2">
          <Label htmlFor="phone">
            Phone Number{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <Input
            type="tel"
            id="phone"
            name="phone"
            placeholder="(555) 123-4567"
            value={phone}
            onChange={(e) => {
              setPhone(e.target.value);
            }}
            onBlur={() => {
              handleBlur('phone');
            }}
            required
            aria-describedby="phone-error"
            aria-invalid={touched.phone && !!errors.phone}
            autoComplete="tel"
            disabled={isSubmitting}
            className={errorClass('phone')}
          />
          {hasError('phone') && (
            <p
              className="text-xs text-destructive"
              id="phone-error"
              role="alert"
            >
              {errors.phone}
            </p>
          )}
        </div>

        {/* Date of Birth */}
        <div className="space-y-1">
          <Label htmlFor="dob">
            Date of Birth{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <Input
            type="date"
            id="dob"
            name="dob"
            value={dob}
            onChange={(e) => {
              setDob(e.target.value);
            }}
            onBlur={() => {
              handleBlur('dob');
            }}
            required
            aria-describedby="dob-error"
            aria-invalid={touched.dob && !!errors.dob}
            autoComplete="bday"
            disabled={isSubmitting}
            className={errorClass('dob')}
          />
          {hasError('dob') && (
            <p className="text-xs text-destructive" id="dob-error" role="alert">
              {errors.dob}
            </p>
          )}
        </div>

        {/* Gender */}
        <div className="space-y-1">
          <Label htmlFor="gender">Gender</Label>
          <select
            id="gender"
            name="gender"
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-xs transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            value={gender}
            onChange={(e) => {
              setGender(e.target.value);
            }}
            autoComplete="sex"
            disabled={isSubmitting}
          >
            <option value="" disabled>
              Select gender
            </option>
            <option value="male">Male</option>
            <option value="female">Female</option>
            <option value="non-binary">Non-binary</option>
            <option value="prefer-not">Prefer not to say</option>
          </select>
        </div>

        {/* Password */}
        <div className="space-y-1 sm:col-span-2">
          <Label htmlFor="password">
            Password{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <div className="relative">
            <Input
              type={showPassword ? 'text' : 'password'}
              id="password"
              name="password"
              placeholder="Min 8 characters"
              value={password}
              onChange={(e) => {
                setPassword(e.target.value);
              }}
              onBlur={() => {
                handleBlur('password');
              }}
              required
              aria-describedby="password-hint password-error"
              aria-invalid={touched.password && !!errors.password}
              autoComplete="new-password"
              disabled={isSubmitting}
              className={cn('pr-10', errorClass('password'))}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="absolute right-0 top-0 h-full px-3 hover:bg-transparent text-muted-foreground"
              onClick={() => {
                setShowPassword((v) => !v);
              }}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? (
                <EyeOff className="h-4 w-4" />
              ) : (
                <Eye className="h-4 w-4" />
              )}
            </Button>
          </div>
          <p className="text-xs text-muted-foreground" id="password-hint">
            At least 8 characters with one uppercase, one number, and one
            special character.
          </p>
          {hasError('password') && (
            <p
              className="text-xs text-destructive"
              id="password-error"
              role="alert"
            >
              {errors.password}
            </p>
          )}
        </div>

        {/* Confirm Password */}
        <div className="space-y-1 sm:col-span-2">
          <Label htmlFor="confirmPassword">
            Confirm Password{' '}
            <span className="text-destructive" aria-label="required">
              *
            </span>
          </Label>
          <div className="relative">
            <Input
              type={showConfirmPassword ? 'text' : 'password'}
              id="confirmPassword"
              name="confirmPassword"
              placeholder="Re-enter password"
              value={confirmPassword}
              onChange={(e) => {
                setConfirmPassword(e.target.value);
              }}
              onBlur={() => {
                handleBlur('confirmPassword');
              }}
              required
              aria-describedby="confirmPassword-error"
              aria-invalid={touched.confirmPassword && !!errors.confirmPassword}
              autoComplete="new-password"
              disabled={isSubmitting}
              className={cn('pr-10', errorClass('confirmPassword'))}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="absolute right-0 top-0 h-full px-3 hover:bg-transparent text-muted-foreground"
              onClick={() => {
                setShowConfirmPassword((v) => !v);
              }}
              aria-label={
                showConfirmPassword ? 'Hide password' : 'Show password'
              }
            >
              {showConfirmPassword ? (
                <EyeOff className="h-4 w-4" />
              ) : (
                <Eye className="h-4 w-4" />
              )}
            </Button>
          </div>
          {hasError('confirmPassword') && (
            <p
              className="text-xs text-destructive"
              id="confirmPassword-error"
              role="alert"
            >
              {errors.confirmPassword}
            </p>
          )}
        </div>
      </div>

      <Separator className="my-6" />

      <Button
        type="submit"
        size="lg"
        className="w-full"
        disabled={isSubmitting}
      >
        {isSubmitting ? (
          <>
            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            Creating Accountâ€¦
          </>
        ) : (
          'Create Account'
        )}
      </Button>

      <span className="sr-only" aria-live="polite" id="live-region" />
    </form>
  );
}

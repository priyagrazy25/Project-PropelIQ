import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { Eye, EyeOff, Loader2 } from 'lucide-react';
import { type FormEvent, useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch } from '../../../app/hooks';
import { setAccessToken } from '../../../shared/api/authInterceptor';
import { ErrorBanner } from '../../../shared/components/ErrorBanner';
import { parseJwtClaims } from '../../../shared/utils/parseJwt';
import { validateEmail } from '../../../shared/utils/validation';
import { loginUser } from '../api/loginApi';
import { loginSuccess } from '../identitySlice';

function getRoleRedirectPath(
  role: 'Patient' | 'Provider' | 'Admin' | 'FrontDesk',
): string {
  switch (role) {
    case 'Patient':
      return '/dashboard';
    case 'Provider':
    case 'FrontDesk':
      return '/queue';
    case 'Admin':
      return '/management/dashboard';
  }
}

export function LoginForm() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);

  const [emailError, setEmailError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [emailTouched, setEmailTouched] = useState(false);
  const [passwordTouched, setPasswordTouched] = useState(false);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [apiError, setApiError] = useState<string | null>(null);
  const [remainingAttempts, setRemainingAttempts] = useState<number | null>(
    null,
  );
  const [isLocked, setIsLocked] = useState(false);

  const validateEmailField = useCallback((): string | null => {
    return validateEmail(email);
  }, [email]);

  const validatePasswordField = useCallback((): string | null => {
    if (!password) return 'Password is required.';
    return null;
  }, [password]);

  const handleEmailBlur = useCallback(() => {
    setEmailTouched(true);
    setEmailError(validateEmailField());
  }, [validateEmailField]);

  const handlePasswordBlur = useCallback(() => {
    setPasswordTouched(true);
    setPasswordError(validatePasswordField());
  }, [validatePasswordField]);

  const handleSubmit = useCallback(
    async (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault();

      const emailErr = validateEmailField();
      const passwordErr = validatePasswordField();

      setEmailTouched(true);
      setPasswordTouched(true);
      setEmailError(emailErr);
      setPasswordError(passwordErr);

      if (emailErr ?? passwordErr) {
        return;
      }

      setIsSubmitting(true);
      setApiError(null);
      setRemainingAttempts(null);

      const result = await loginUser({
        email: email.trim().toLowerCase(),
        password,
      });

      setIsSubmitting(false);

      if (result.success) {
        setAccessToken(result.data.accessToken);
        // Parse JWT to get patientId claim for patient users
        const claims = parseJwtClaims(result.data.accessToken);
        dispatch(
          loginSuccess({
            userId: result.data.userId,
            role: result.data.role,
            fullName: result.data.fullName,
            patientId: claims?.patientId ?? null,
          }),
        );
        void navigate(getRoleRedirectPath(result.data.role), { replace: true });
        return;
      }

      if (result.error.status === 423) {
        setIsLocked(true);
        setApiError(result.error.message);
        return;
      }

      setApiError(result.error.message);
      if (result.error.remainingAttempts !== undefined) {
        setRemainingAttempts(result.error.remainingAttempts);
      }
    },
    [
      email,
      password,
      validateEmailField,
      validatePasswordField,
      dispatch,
      navigate,
    ],
  );

  return (
    <form
      className="flex flex-col"
      onSubmit={(e) => void handleSubmit(e)}
      noValidate
      aria-label="Sign in form"
    >
      {apiError && (
        <ErrorBanner
          message={
            <>
              {apiError}
              {remainingAttempts !== null && remainingAttempts > 0 && (
                <span className="font-semibold">
                  {' '}
                  {remainingAttempts} attempt
                  {remainingAttempts !== 1 ? 's' : ''} remaining.
                </span>
              )}
            </>
          }
          onDismiss={isLocked ? undefined : () => setApiError(null)}
        />
      )}

      <div className="space-y-1 mb-5">
        <Label htmlFor="login-email">
          Email Address{' '}
          <span className="text-destructive" aria-label="required">
            *
          </span>
        </Label>
        <Input
          type="email"
          id="login-email"
          placeholder="you@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          onBlur={handleEmailBlur}
          required
          autoComplete="email"
          aria-describedby="login-email-error"
          aria-invalid={emailTouched && emailError ? true : undefined}
          disabled={isLocked}
          className={cn(
            emailTouched &&
              emailError &&
              'border-destructive focus-visible:ring-destructive',
          )}
        />
        {emailTouched && emailError && (
          <p
            className="text-xs text-destructive"
            id="login-email-error"
            role="alert"
          >
            {emailError}
          </p>
        )}
      </div>

      <div className="space-y-1 mb-5">
        <Label htmlFor="login-password">
          Password{' '}
          <span className="text-destructive" aria-label="required">
            *
          </span>
        </Label>
        <div className="relative">
          <Input
            type={showPassword ? 'text' : 'password'}
            id="login-password"
            placeholder="Enter your password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            onBlur={handlePasswordBlur}
            required
            autoComplete="current-password"
            aria-describedby="login-password-error"
            aria-invalid={passwordTouched && passwordError ? true : undefined}
            disabled={isLocked}
            className={cn(
              'pr-10',
              passwordTouched &&
                passwordError &&
                'border-destructive focus-visible:ring-destructive',
            )}
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="absolute right-0 top-0 h-full px-3 hover:bg-transparent text-muted-foreground"
            onClick={() => setShowPassword((v) => !v)}
            aria-label={showPassword ? 'Hide password' : 'Show password'}
          >
            {showPassword ? (
              <EyeOff className="h-4 w-4" />
            ) : (
              <Eye className="h-4 w-4" />
            )}
          </Button>
        </div>
        {passwordTouched && passwordError && (
          <p
            className="text-xs text-destructive"
            id="login-password-error"
            role="alert"
          >
            {passwordError}
          </p>
        )}
      </div>

      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-2">
          <Checkbox
            id="remember-me"
            checked={rememberMe}
            onCheckedChange={(checked) => setRememberMe(checked === true)}
            disabled={isLocked}
          />
          <Label
            htmlFor="remember-me"
            className="text-sm font-normal text-muted-foreground cursor-pointer"
          >
            Remember me
          </Label>
        </div>
        <a
          href="/forgot-password"
          className="text-sm font-medium text-primary hover:underline"
        >
          Forgot password?
        </a>
      </div>

      <Button
        type="submit"
        size="lg"
        className="w-full"
        disabled={isSubmitting || isLocked}
      >
        {isSubmitting ? (
          <>
            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            Signing in…
          </>
        ) : (
          'Sign In'
        )}
      </Button>

      <span className="sr-only" aria-live="polite">
        {isSubmitting ? 'Signing in, please wait.' : ''}
      </span>
    </form>
  );
}

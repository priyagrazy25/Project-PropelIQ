import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
} from '@/components/ui/card';
import { Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { LoginForm } from '../components/LoginForm';

export function LoginPage() {
  return (
    <>
      <header
        className="fixed top-0 left-0 right-0 h-16 bg-card border-b border-border flex items-center justify-between px-8 shadow-[var(--shadow-1)] z-50"
        role="banner"
      >
        <Link
          to="/login"
          className="flex items-center gap-2 text-xl font-bold text-primary no-underline"
          aria-label="Unified Patient Access - Home"
        >
          <span className="w-8 h-8 bg-primary rounded-md flex items-center justify-center text-primary-foreground">
            <Plus className="h-5 w-5" />
          </span>
          Unified Patient Access
        </Link>
        <Button variant="ghost" asChild>
          <Link to="/register" className="text-sm font-medium text-primary">
            Create Account
          </Link>
        </Button>
      </header>

      <main
        className="mt-[calc(var(--header-height)+40px)] w-full max-w-[440px] px-4 mx-auto mb-10"
        role="main"
      >
        <Card className="shadow-[var(--shadow-3)]">
          <CardHeader className="px-8 pt-10 pb-0">
            <h1 className="text-[32px] font-bold leading-10 text-foreground mb-2">
              Sign In
            </h1>
            <p className="text-sm text-muted-foreground">
              Enter your credentials to access your account.
            </p>
          </CardHeader>

          <CardContent className="px-8 pt-8">
            <LoginForm />
          </CardContent>

          <CardFooter className="px-8 pb-10 justify-center">
            <p className="text-sm text-muted-foreground">
              Don&apos;t have an account?{' '}
              <Link
                to="/register"
                className="text-primary font-medium hover:underline"
              >
                Create account
              </Link>
            </p>
          </CardFooter>
        </Card>
      </main>
    </>
  );
}

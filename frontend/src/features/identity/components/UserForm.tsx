import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { Loader2 } from 'lucide-react';
import { useRef, useState } from 'react';
import type { UserListItem, UserRole } from '../api/adminApi';

interface UserFormProps {
  mode: 'create' | 'edit';
  user?: UserListItem | null;
  onSubmit: (data: UserFormData) => void;
  onCancel: () => void;
  loading: boolean;
}

export interface UserFormData {
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole | '';
  phone: string;
}

interface FormErrors {
  firstName?: string;
  lastName?: string;
  email?: string;
  role?: string;
}

function validate(data: UserFormData): FormErrors {
  const errors: FormErrors = {};
  if (!data.firstName.trim()) errors.firstName = 'First name is required.';
  if (!data.lastName.trim()) errors.lastName = 'Last name is required.';
  if (!data.email.trim()) {
    errors.email = 'Email is required.';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) {
    errors.email = 'Enter a valid email address.';
  }
  if (!data.role) errors.role = 'Role is required.';
  return errors;
}

export function UserForm({
  mode,
  user,
  onSubmit,
  onCancel,
  loading,
}: UserFormProps) {
  const initialForm = (): UserFormData => {
    if (mode === 'edit' && user) {
      const nameParts = user.fullName.split(' ');
      return {
        firstName: nameParts[0] ?? '',
        lastName: nameParts.slice(1).join(' '),
        email: user.email,
        role: user.role,
        phone: user.phone ?? '',
      };
    }
    return { firstName: '', lastName: '', email: '', role: '', phone: '' };
  };

  const [form, setForm] = useState<UserFormData>(initialForm);
  const [errors, setErrors] = useState<FormErrors>({});
  const firstInputRef = useRef<HTMLInputElement>(null);

  const handleChange = (field: keyof UserFormData, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    if (errors[field as keyof FormErrors]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const formErrors = validate(form);
    if (Object.keys(formErrors).length > 0) {
      setErrors(formErrors);
      return;
    }
    onSubmit(form);
  };

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onCancel();
      }}
    >
      <DialogContent
        className="sm:max-w-[500px]"
        onOpenAutoFocus={(e) => {
          e.preventDefault();
          firstInputRef.current?.focus();
        }}
      >
        <DialogHeader>
          <DialogTitle>
            {mode === 'create' ? 'Create User' : 'Edit User'}
          </DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 py-4">
            <div className="space-y-1">
              <Label htmlFor="admin-u-first">
                First Name <span className="text-destructive">*</span>
              </Label>
              <Input
                ref={firstInputRef}
                type="text"
                id="admin-u-first"
                value={form.firstName}
                onChange={(e) => {
                  handleChange('firstName', e.target.value);
                }}
                aria-invalid={errors.firstName ? 'true' : undefined}
                aria-describedby={
                  errors.firstName ? 'admin-u-first-err' : undefined
                }
                className={cn(errors.firstName && 'border-destructive')}
              />
              {errors.firstName && (
                <p
                  className="text-xs text-destructive"
                  id="admin-u-first-err"
                  role="alert"
                >
                  {errors.firstName}
                </p>
              )}
            </div>
            <div className="space-y-1">
              <Label htmlFor="admin-u-last">
                Last Name <span className="text-destructive">*</span>
              </Label>
              <Input
                type="text"
                id="admin-u-last"
                value={form.lastName}
                onChange={(e) => {
                  handleChange('lastName', e.target.value);
                }}
                aria-invalid={errors.lastName ? 'true' : undefined}
                aria-describedby={
                  errors.lastName ? 'admin-u-last-err' : undefined
                }
                className={cn(errors.lastName && 'border-destructive')}
              />
              {errors.lastName && (
                <p
                  className="text-xs text-destructive"
                  id="admin-u-last-err"
                  role="alert"
                >
                  {errors.lastName}
                </p>
              )}
            </div>
            <div className="space-y-1 sm:col-span-2">
              <Label htmlFor="admin-u-email">
                Email <span className="text-destructive">*</span>
              </Label>
              <Input
                type="email"
                id="admin-u-email"
                value={form.email}
                onChange={(e) => {
                  handleChange('email', e.target.value);
                }}
                aria-invalid={errors.email ? 'true' : undefined}
                aria-describedby={
                  errors.email ? 'admin-u-email-err' : undefined
                }
                className={cn(errors.email && 'border-destructive')}
              />
              {errors.email && (
                <p
                  className="text-xs text-destructive"
                  id="admin-u-email-err"
                  role="alert"
                >
                  {errors.email}
                </p>
              )}
            </div>
            <div className="space-y-1">
              <Label htmlFor="admin-u-role">
                Role <span className="text-destructive">*</span>
              </Label>
              <select
                id="admin-u-role"
                className={cn(
                  'flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring',
                  errors.role && 'border-destructive',
                )}
                value={form.role}
                onChange={(e) => {
                  handleChange('role', e.target.value);
                }}
                aria-invalid={errors.role ? 'true' : undefined}
                aria-describedby={errors.role ? 'admin-u-role-err' : undefined}
              >
                <option value="">Select role</option>
                <option value="Admin">Admin</option>
                <option value="Patient">Patient</option>
                <option value="FrontDesk">Staff</option>
              </select>
              {errors.role && (
                <p
                  className="text-xs text-destructive"
                  id="admin-u-role-err"
                  role="alert"
                >
                  {errors.role}
                </p>
              )}
            </div>
            <div className="space-y-1">
              <Label htmlFor="admin-u-phone">Phone</Label>
              <Input
                type="tel"
                id="admin-u-phone"
                value={form.phone}
                onChange={(e) => {
                  handleChange('phone', e.target.value);
                }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={onCancel}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Saving...
                </>
              ) : mode === 'create' ? (
                'Create User'
              ) : (
                'Save Changes'
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

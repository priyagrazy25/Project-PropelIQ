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
import { useState } from 'react';

export interface NewPatientData {
  firstName: string;
  lastName: string;
  contactNumber: string;
  dateOfBirth: string;
}

interface CreatePatientModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSubmit: (data: NewPatientData) => void;
  isSubmitting: boolean;
}

export function CreatePatientModal({
  open,
  onOpenChange,
  onSubmit,
  isSubmitting,
}: CreatePatientModalProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [contactNumber, setContactNumber] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!firstName.trim()) newErrors.firstName = 'First name is required';
    if (!lastName.trim()) newErrors.lastName = 'Last name is required';
    if (!contactNumber.trim()) newErrors.contactNumber = 'Phone is required';
    if (!dateOfBirth) newErrors.dateOfBirth = 'Date of birth is required';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    onSubmit({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      contactNumber: contactNumber.trim(),
      dateOfBirth,
    });
  };

  const handleOpenChange = (value: boolean) => {
    if (!value) {
      setFirstName('');
      setLastName('');
      setContactNumber('');
      setDateOfBirth('');
      setErrors({});
    }
    onOpenChange(value);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Create New Patient</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} noValidate>
          <div className="grid grid-cols-2 gap-5">
            <div className="flex flex-col gap-1">
              <Label htmlFor="np-first">
                First Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="np-first"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                aria-invalid={!!errors.firstName}
                aria-describedby={
                  errors.firstName ? 'np-first-error' : undefined
                }
              />
              {errors.firstName && (
                <span id="np-first-error" className="text-xs text-destructive">
                  {errors.firstName}
                </span>
              )}
            </div>
            <div className="flex flex-col gap-1">
              <Label htmlFor="np-last">
                Last Name <span className="text-destructive">*</span>
              </Label>
              <Input
                id="np-last"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                aria-invalid={!!errors.lastName}
                aria-describedby={errors.lastName ? 'np-last-error' : undefined}
              />
              {errors.lastName && (
                <span id="np-last-error" className="text-xs text-destructive">
                  {errors.lastName}
                </span>
              )}
            </div>
            <div className="flex flex-col gap-1">
              <Label htmlFor="np-phone">
                Phone <span className="text-destructive">*</span>
              </Label>
              <Input
                id="np-phone"
                type="tel"
                value={contactNumber}
                onChange={(e) => setContactNumber(e.target.value)}
                aria-invalid={!!errors.contactNumber}
                aria-describedby={
                  errors.contactNumber ? 'np-phone-error' : undefined
                }
              />
              {errors.contactNumber && (
                <span id="np-phone-error" className="text-xs text-destructive">
                  {errors.contactNumber}
                </span>
              )}
            </div>
            <div className="flex flex-col gap-1">
              <Label htmlFor="np-dob">
                Date of Birth <span className="text-destructive">*</span>
              </Label>
              <Input
                id="np-dob"
                type="date"
                value={dateOfBirth}
                onChange={(e) => setDateOfBirth(e.target.value)}
                aria-invalid={!!errors.dateOfBirth}
                aria-describedby={
                  errors.dateOfBirth ? 'np-dob-error' : undefined
                }
              />
              {errors.dateOfBirth && (
                <span id="np-dob-error" className="text-xs text-destructive">
                  {errors.dateOfBirth}
                </span>
              )}
            </div>
          </div>
          <DialogFooter className="mt-5">
            <Button
              type="button"
              variant="ghost"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Creating…' : 'Create Patient'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

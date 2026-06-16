import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import type { BookingResponse } from '../api/schedulingApi';

interface BookingConfirmationCardProps {
  booking: BookingResponse;
}

function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function getInitials(name: string): string {
  return name
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

export function BookingConfirmationCard({
  booking,
}: BookingConfirmationCardProps) {
  return (
    <Card>
      <CardContent className="p-6 space-y-5">
        <div className="flex items-center gap-4">
          <Avatar className="h-12 w-12">
            <AvatarFallback>{getInitials(booking.providerName)}</AvatarFallback>
          </Avatar>
          <div>
            <p className="font-semibold text-lg">{booking.providerName}</p>
            <p className="text-sm text-muted-foreground">
              {booking.specialty} · {booking.location}
            </p>
          </div>
        </div>

        <h3 className="text-base font-semibold">Appointment Details</h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-3 text-sm">
          <div>
            <span className="text-muted-foreground">Appointment ID</span>
            <p className="font-mono font-medium">{booking.appointmentId}</p>
          </div>
          <div>
            <span className="text-muted-foreground">Status</span>
            <p>
              <Badge className="bg-green-100 text-green-800 hover:bg-green-100">
                {booking.status}
              </Badge>
            </p>
          </div>
          <div>
            <span className="text-muted-foreground">Date</span>
            <p className="font-medium">{formatDate(booking.slotStartTime)}</p>
          </div>
          <div>
            <span className="text-muted-foreground">Time</span>
            <p className="font-medium">
              {formatTime(booking.slotStartTime)} –{' '}
              {formatTime(booking.slotEndTime)}
            </p>
          </div>
          <div>
            <span className="text-muted-foreground">Provider</span>
            <p className="font-medium">{booking.providerName}</p>
          </div>
          <div>
            <span className="text-muted-foreground">Location</span>
            <p className="font-medium">{booking.location}</p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import {
  Activity,
  CalendarDays,
  Clock,
  MapPin,
  Star,
  Stethoscope,
  UserCheck,
} from 'lucide-react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import type { ProviderResult, ProviderSlot } from '../api/schedulingApi';

// ── Specialty knowledge base ────────────────────────────────────────────────

interface SpecialtyInfo {
  description: string;
  expertiseAreas: string[];
  conditionsTreated: string[];
  appointmentTypes: string[];
  typicalDuration: string;
}

const SPECIALTY_INFO: Record<string, SpecialtyInfo> = {
  'Family Medicine': {
    description:
      'Family Medicine physicians provide comprehensive, continuous care for patients of all ages. They manage a wide range of acute and chronic conditions and serve as the first point of contact for most health concerns.',
    expertiseAreas: [
      'Preventive Care',
      'Chronic Disease Management',
      'Acute Illness',
      'Women\'s Health',
      'Geriatric Care',
      'Pediatric Care',
    ],
    conditionsTreated: [
      'Hypertension',
      'Diabetes',
      'Asthma',
      'Anxiety & Depression',
      'Infections',
      'Musculoskeletal Pain',
    ],
    appointmentTypes: [
      'Annual Wellness Exam',
      'Sick Visit',
      'Chronic Care Follow-up',
      'Preventive Screening',
    ],
    typicalDuration: '20 – 40 minutes',
  },
  'Internal Medicine': {
    description:
      'Internal Medicine specialists — also known as internists — focus on the prevention, diagnosis, and treatment of adult diseases. They are experts in managing complex, multi-system conditions.',
    expertiseAreas: [
      'Diagnostic Medicine',
      'Complex Chronic Conditions',
      'Hospital Medicine',
      'Cardiovascular Risk',
      'Endocrinology',
      'Pulmonology',
    ],
    conditionsTreated: [
      'Diabetes',
      'Heart Disease',
      'COPD',
      'Kidney Disease',
      'Autoimmune Disorders',
      'Hypertension',
    ],
    appointmentTypes: [
      'Comprehensive Evaluation',
      'Chronic Disease Management',
      'Pre-operative Clearance',
      'Second Opinion',
    ],
    typicalDuration: '30 – 60 minutes',
  },
  Cardiology: {
    description:
      'Cardiologists specialise in diagnosing and treating diseases of the heart and blood vessels. They provide both medical management and procedural interventions for a full spectrum of cardiovascular conditions.',
    expertiseAreas: [
      'Echocardiography',
      'Cardiac Catheterisation',
      'Arrhythmia Management',
      'Heart Failure',
      'Preventive Cardiology',
      'Vascular Medicine',
    ],
    conditionsTreated: [
      'Coronary Artery Disease',
      'Heart Failure',
      'Atrial Fibrillation',
      'Hypertension',
      'Valvular Heart Disease',
      'Peripheral Artery Disease',
    ],
    appointmentTypes: [
      'Cardiac Consultation',
      'ECG & Stress Test',
      'Echocardiogram',
      'Follow-up Visit',
    ],
    typicalDuration: '30 – 45 minutes',
  },
  Pediatrics: {
    description:
      'Pediatricians provide dedicated medical care for infants, children, and adolescents. They oversee physical, emotional, and developmental health from birth through young adulthood.',
    expertiseAreas: [
      'Well-Child Visits',
      'Immunisations',
      'Developmental Screening',
      'Adolescent Medicine',
      'Nutritional Guidance',
      'Behavioural Health',
    ],
    conditionsTreated: [
      'Respiratory Infections',
      'Ear Infections',
      'ADHD',
      'Allergies & Asthma',
      'Growth Disorders',
      'Eczema',
    ],
    appointmentTypes: [
      'Well-Child Check-up',
      'Sick Visit',
      'Developmental Assessment',
      'Vaccination',
    ],
    typicalDuration: '20 – 30 minutes',
  },
  Dermatology: {
    description:
      'Dermatologists diagnose and treat conditions affecting the skin, hair, and nails. They manage everything from common rashes to complex skin cancers and perform both medical and cosmetic procedures.',
    expertiseAreas: [
      'Medical Dermatology',
      'Skin Cancer Screening',
      'Cosmetic Procedures',
      'Paediatric Dermatology',
      'Patch Testing',
      'Dermatopathology',
    ],
    conditionsTreated: [
      'Acne',
      'Psoriasis',
      'Eczema',
      'Melanoma & Skin Cancer',
      'Rosacea',
      'Hair Loss',
    ],
    appointmentTypes: [
      'Skin Examination',
      'Mole Mapping',
      'Biopsy',
      'Acne / Rosacea Consult',
    ],
    typicalDuration: '15 – 30 minutes',
  },
};

const DEFAULT_SPECIALTY_INFO: SpecialtyInfo = {
  description:
    'This specialist provides expert medical care within their field. Please contact the clinic for more information about their services.',
  expertiseAreas: ['Clinical Consultation', 'Diagnostic Services', 'Treatment Planning'],
  conditionsTreated: ['Contact the clinic for a full list of conditions treated.'],
  appointmentTypes: ['Consultation', 'Follow-up Visit'],
  typicalDuration: '20 – 45 minutes',
};

// ── Helpers ──────────────────────────────────────────────────────────────────

interface ProfileLocationState {
  provider: ProviderResult;
}

function isProfileState(state: unknown): state is ProfileLocationState {
  if (typeof state !== 'object' || state === null) return false;
  const s = state as Record<string, unknown>;
  return typeof s.provider === 'object' && s.provider !== null;
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

function formatTime(isoString: string): string {
  return new Date(isoString).toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}

function formatDateLabel(isoString: string): string {
  return new Date(isoString).toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });
}

/** Group a flat slot list by calendar date (YYYY-MM-DD key). */
function groupSlotsByDate(
  slots: ProviderSlot[],
): { dateKey: string; dateLabel: string; slots: ProviderSlot[] }[] {
  const map = new Map<string, ProviderSlot[]>();
  for (const slot of slots) {
    const key = slot.startTime.slice(0, 10); // "YYYY-MM-DD"
    const existing = map.get(key);
    if (existing) {
      existing.push(slot);
    } else {
      map.set(key, [slot]);
    }
  }
  return Array.from(map.entries()).map(([dateKey, slotList]) => ({
    dateKey,
    dateLabel: formatDateLabel(slotList[0]!.startTime),
    slots: slotList,
  }));
}

// ── Component ────────────────────────────────────────────────────────────────

export function ProviderProfilePage() {
  const location = useLocation();
  const navigate = useNavigate();

  if (!isProfileState(location.state)) {
    return <Navigate to="/search" replace />;
  }

  const { provider } = location.state;
  const info = SPECIALTY_INFO[provider.specialty] ?? DEFAULT_SPECIALTY_INFO;
  const availableSlots = provider.availableSlots.filter((s) => s.isAvailable);
  const nextSlot = availableSlots[0] ?? null;
  const slotGroups = groupSlotsByDate(availableSlots);

  return (
    <main className="max-w-2xl mx-auto space-y-6" role="main">
      {/* Breadcrumb */}
      <nav
        className="flex items-center gap-1 text-sm text-muted-foreground"
        aria-label="Breadcrumb"
      >
        <button
          className="hover:text-foreground transition-colors"
          onClick={() => void navigate(-1)}
        >
          Find a Provider
        </button>
        <span aria-hidden="true">›</span>
        <span aria-current="page" className="text-foreground">
          Provider Profile
        </span>
      </nav>

      {/* ── Hero header ── */}
      <Card>
        <CardContent className="p-6">
          <div className="flex gap-5">
            <div
              className="h-20 w-20 shrink-0 rounded-full bg-blue-50 text-[#1E6F9F] flex items-center justify-center font-bold text-2xl"
              aria-hidden="true"
            >
              {getInitials(provider.fullName)}
            </div>
            <div className="min-w-0 flex-1 space-y-2">
              <div>
                <h1 className="text-2xl font-bold text-foreground">
                  {provider.fullName}
                </h1>
                <p className="text-sm font-medium text-[#1E6F9F] mt-0.5">
                  {provider.specialty}
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <div className="flex items-center gap-1 text-sm text-orange-500">
                  <Star className="h-4 w-4 fill-current" />
                  <span className="font-semibold">{provider.rating.toFixed(1)}</span>
                  <span className="text-muted-foreground text-xs">/ 5.0</span>
                </div>
                {provider.isAcceptingPatients ? (
                  <Badge className="bg-emerald-50 text-emerald-700 hover:bg-emerald-50 text-xs font-medium">
                    <UserCheck className="h-3 w-3 mr-1" />
                    Accepting New Patients
                  </Badge>
                ) : (
                  <Badge variant="secondary" className="text-xs font-medium">
                    Not Accepting New Patients
                  </Badge>
                )}
              </div>
              <div className="flex flex-wrap gap-4 text-sm text-muted-foreground pt-1">
                <span className="flex items-center gap-1.5">
                  <MapPin className="h-3.5 w-3.5 shrink-0" />
                  {provider.location}
                </span>
                <span className="flex items-center gap-1.5">
                  <Clock className="h-3.5 w-3.5 shrink-0" />
                  Typical visit: {info.typicalDuration}
                </span>
                {provider.nextAvailableDate && (
                  <span className="flex items-center gap-1.5">
                    <CalendarDays className="h-3.5 w-3.5 shrink-0" />
                    Next available: {new Date(provider.nextAvailableDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                  </span>
                )}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* ── About & Specialty ── */}
      <Card>
        <CardContent className="p-6 space-y-5">
          <div className="flex items-center gap-2">
            <Stethoscope className="h-4 w-4 text-[#1E6F9F]" />
            <h2 className="text-base font-semibold text-foreground">
              About {provider.specialty}
            </h2>
          </div>
          <p className="text-sm text-muted-foreground leading-relaxed">
            {info.description}
          </p>

          <Separator />

          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Areas of Expertise
            </p>
            <div className="flex flex-wrap gap-2">
              {info.expertiseAreas.map((area) => (
                <Badge
                  key={area}
                  variant="secondary"
                  className="text-xs font-medium"
                >
                  {area}
                </Badge>
              ))}
            </div>
          </div>

          <Separator />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Conditions Treated
              </p>
              <ul className="space-y-1">
                {info.conditionsTreated.map((c) => (
                  <li key={c} className="flex items-start gap-2 text-sm text-foreground">
                    <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-[#1E6F9F]" />
                    {c}
                  </li>
                ))}
              </ul>
            </div>
            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Appointment Types
              </p>
              <ul className="space-y-1">
                {info.appointmentTypes.map((t) => (
                  <li key={t} className="flex items-start gap-2 text-sm text-foreground">
                    <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-[#1E6F9F]" />
                    {t}
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* ── Available slots grouped by date ── */}
      <Card>
        <CardContent className="p-6 space-y-4">
          <div className="flex items-center gap-2">
            <Activity className="h-4 w-4 text-[#1E6F9F]" />
            <h2 className="text-base font-semibold text-foreground">
              Available Appointments
            </h2>
          </div>

          {slotGroups.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No upcoming slots available. Please check back later or join the
              waitlist.
            </p>
          ) : (
            <div className="space-y-4">
              {slotGroups.map(({ dateKey, dateLabel, slots: daySlots }) => (
                <div key={dateKey} className="space-y-2">
                  <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    {dateLabel}
                  </p>
                  <div className="flex flex-wrap gap-2">
                    {daySlots.map((slot) => (
                      <button
                        key={slot.id}
                        className="inline-flex items-center rounded border border-[#1E6F9F] bg-blue-50 px-3 py-1.5 text-[13px] font-medium text-[#1E6F9F] hover:bg-[#1E6F9F] hover:text-white transition-colors cursor-pointer"
                        aria-label={`Book ${formatTime(slot.startTime)} slot on ${dateLabel}`}
                        onClick={() =>
                          void navigate('/booking/confirm', {
                            state: { provider, slot },
                          })
                        }
                      >
                        {formatTime(slot.startTime)}
                      </button>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* ── Actions ── */}
      <div className="flex gap-3 pb-6">
        <Button
          className="bg-[#1E6F9F] hover:bg-[#175F87] text-white"
          disabled={!nextSlot}
          onClick={() => {
            if (nextSlot) {
              void navigate('/booking/confirm', {
                state: { provider, slot: nextSlot },
              });
            }
          }}
        >
          Book Earliest Slot
        </Button>
        <Button variant="outline" onClick={() => void navigate(-1)}>
          Back to Search
        </Button>
      </div>
    </main>
  );
}


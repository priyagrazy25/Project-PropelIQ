import { useAppSelector } from '@/app/hooks';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Activity,
  AlertTriangle,
  ChevronRight,
  FileText,
  History,
  Pill,
  Stethoscope,
  TestTube,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  fetchPatient360View,
  type ConflictSummary,
  type DataCategory,
  type ExtractedDataPoint,
  type Patient360ViewResponse,
} from '../api/patient360Api';
import { ConflictBanner, ConflictWarningBanner } from '../components/ConflictBanner';
import { ConfidenceScoreBadge } from '../components/ConfidenceScoreBadge';
import {
  ConflictCountBadge,
  ConflictInlineIndicator,
  VerifiedBadge,
} from '../components/ConflictInlineIndicator';

/** Tab definitions matching DR-006 categories. */
const TABS = [
  { id: 'vitals', label: 'Vitals', icon: Activity, category: 'vital' as DataCategory },
  { id: 'history', label: 'Medical History', icon: History, category: 'history' as DataCategory },
  { id: 'medications', label: 'Medications', icon: Pill, category: 'medication' as DataCategory },
  { id: 'allergies', label: 'Allergies', icon: AlertTriangle, category: 'allergy' as DataCategory },
  { id: 'labs', label: 'Lab Results', icon: TestTube, category: 'lab' as DataCategory },
  { id: 'diagnoses', label: 'Diagnoses', icon: Stethoscope, category: 'diagnosis' as DataCategory },
] as const;

/**
 * 360-Degree Patient View page (SCR-016).
 * Displays aggregated clinical data in tabbed sections with confidence scores.
 * Implements AC-5 (3s render) and AC-6 (6 tabbed sections per DR-006).
 * Includes conflict detection and highlighting per US_036.
 */
export function PatientView360Page() {
  const navigate = useNavigate();
  const { patientId: urlPatientId } = useParams<{ patientId?: string }>();
  // For patient-facing routes (/health-profile), use logged-in user's patientId from JWT claim
  const currentPatientId = useAppSelector((state) => state.identity.patientId);
  const patientId = urlPatientId ?? currentPatientId;
  
  const [data, setData] = useState<Patient360ViewResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<string>('vitals');

  useEffect(() => {
    // Redirect to staff queue only if no patientId available at all
    if (!patientId) {
      void navigate('/staff/queue', { replace: true });
      return;
    }

    const loadData = async () => {
      setIsLoading(true);
      setError(null);

      const result = await fetchPatient360View(patientId);
      if (result.success) {
        setData(result.data);
      } else {
        // Show actual error instead of falling back to demo data
        setError(result.error);
      }
      setIsLoading(false);
    };

    void loadData();
  }, [patientId, navigate]);

  // Filter data by category for each tab
  const getDataForCategory = (category: DataCategory): ExtractedDataPoint[] => {
    if (!data) return [];
    return data.extractedData.filter((d) => d.category === category);
  };

  // Get conflicts for a specific category (AC-4: inline conflicts)
  const getConflictsForCategory = (category: DataCategory): ConflictSummary[] => {
    if (!data) return [];
    return data.conflicts.filter((c) => c.category === category);
  };

  // Get highest severity for a category's conflicts
  const getHighestSeverity = (
    conflicts: ConflictSummary[]
  ): 'high' | 'medium' | 'low' | undefined => {
    if (conflicts.length === 0) return undefined;
    if (conflicts.some((c) => c.severity === 'high')) return 'high';
    if (conflicts.some((c) => c.severity === 'medium')) return 'medium';
    return 'low';
  };

  // Check if category data is verified (no conflicts and multiple sources)
  const isCategoryVerified = (category: DataCategory): boolean => {
    const categoryData = getDataForCategory(category);
    const categoryConflicts = getConflictsForCategory(category);
    // Verified if no conflicts and at least 2 data points from different sources
    if (categoryConflicts.length > 0) return false;
    const uniqueSources = new Set(categoryData.map((d) => d.sourceDocument));
    return categoryData.length >= 2 && uniqueSources.size >= 2;
  };

  if (isLoading) {
    return <PatientView360Skeleton />;
  }

  if (error && !data) {
    return (
      <div className="mx-auto max-w-4xl space-y-6 p-6">
        <Card className="border-red-200 bg-red-50">
          <CardContent className="flex items-center gap-3 p-4">
            <AlertTriangle className="size-4 text-red-600" />
            <span className="text-red-800">{error}</span>
          </CardContent>
        </Card>
        <Button onClick={() => void navigate('/staff/queue')}>Back to Queue</Button>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6 p-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-1 text-sm" aria-label="Breadcrumb">
        <Link to="/staff/queue" className="text-primary hover:underline">
          Queue
        </Link>
        <ChevronRight className="size-4 text-muted-foreground" aria-hidden="true" />
        <span className="text-muted-foreground" aria-current="page">
          Patient View
        </span>
      </nav>

      {/* Patient Banner */}
      <Card>
        <CardContent className="flex items-center gap-5 p-5">
          <div
            className="flex size-16 shrink-0 items-center justify-center rounded-full bg-blue-100 text-2xl font-bold text-primary"
            aria-hidden="true"
          >
            {getInitials(data.demographics.fullName)}
          </div>
          <div className="min-w-0 flex-1">
            <h1 className="text-2xl font-bold">{data.demographics.fullName}</h1>
            <p className="text-sm text-muted-foreground">
              DOB: {data.demographics.dateOfBirth} · {data.demographics.gender} · MRN: {data.demographics.mrn}
            </p>
            <div className="mt-2 flex flex-wrap gap-2">
              <Badge variant="secondary" className="bg-emerald-100 text-emerald-700">
                Intake Complete
              </Badge>
              <Badge variant="secondary" className="bg-blue-100 text-primary">
                <FileText className="mr-1 size-3" />
                {data.documentCount} Documents
              </Badge>
              {data.conflicts.length > 0 && (
                <Badge variant="secondary" className="bg-amber-100 text-amber-700">
                  <AlertTriangle className="mr-1 size-3" />
                  {data.conflicts.length} Conflict{data.conflicts.length > 1 ? 's' : ''}
                </Badge>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Critical Conflict Banner (AC-4: Critical conflicts → banner alert) */}
      <ConflictBanner conflicts={data.conflicts} />

      {/* Warning Conflict Banner (AC-4: Warning conflicts → inline) */}
      <ConflictWarningBanner conflicts={data.conflicts} />

      {/* Tabbed Data Sections */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList variant="line" className="mb-4 w-full justify-start border-b">
          {TABS.map((tab) => {
            const count = getDataForCategory(tab.category).length;
            const categoryConflicts = getConflictsForCategory(tab.category);
            const conflictSeverity = getHighestSeverity(categoryConflicts);
            const verified = isCategoryVerified(tab.category);
            return (
              <TabsTrigger key={tab.id} value={tab.id} className="gap-2">
                <tab.icon className="size-4" />
                {tab.label}
                {count > 0 && (
                  <span className="ml-1 rounded-full bg-muted px-1.5 py-0.5 text-xs">{count}</span>
                )}
                {/* Conflict count badge on tab header (AC-4) */}
                {categoryConflicts.length > 0 && (
                  <ConflictCountBadge count={categoryConflicts.length} severity={conflictSeverity} />
                )}
                {/* Verified badge when no conflicts */}
                {verified && count > 0 && <VerifiedBadge category={tab.category} />}
              </TabsTrigger>
            );
          })}
        </TabsList>

        {TABS.map((tab) => {
          const categoryConflicts = getConflictsForCategory(tab.category);
          return (
            <TabsContent key={tab.id} value={tab.id}>
              {/* Inline conflict indicators for this category */}
              {categoryConflicts.length > 0 && (
                <div className="mb-4 space-y-2">
                  {categoryConflicts.map((conflict) => (
                    <ConflictInlineIndicator key={conflict.conflictId} conflict={conflict} />
                  ))}
                </div>
              )}
              <DataCategoryTable
                data={getDataForCategory(tab.category)}
                conflicts={data.conflicts}
                emptyMessage={`No ${tab.label.toLowerCase()} data available.`}
              />
            </TabsContent>
          );
        })}
      </Tabs>
    </div>
  );
}

/** Data table for a single category. */
function DataCategoryTable({
  data,
  conflicts,
  emptyMessage,
}: {
  data: ExtractedDataPoint[];
  conflicts: ConflictSummary[];
  emptyMessage: string;
}) {
  // Helper to find conflict for a data point
  const getConflictForItem = (item: ExtractedDataPoint): ConflictSummary | undefined => {
    if (!item.conflictId) return undefined;
    return conflicts.find((c) => c.conflictId === item.conflictId);
  };

  if (data.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center justify-center py-12 text-center">
          <FileText className="mb-4 size-12 text-muted-foreground" />
          <p className="text-muted-foreground">{emptyMessage}</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Field</TableHead>
            <TableHead>Value</TableHead>
            <TableHead>Source</TableHead>
            <TableHead className="text-right">Confidence</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.map((item) => {
            const conflict = getConflictForItem(item);
            return (
              <TableRow
                key={item.id}
                className={conflict ? 'bg-amber-50/50' : undefined}
              >
                <TableCell className="font-medium">
                  <div className="flex items-center gap-2">
                    {item.field}
                    {conflict && (
                      <span
                        className="inline-flex items-center gap-1 rounded bg-amber-100 px-1.5 py-0.5 text-xs font-medium text-amber-700"
                        title={`Conflict: ${conflict.values.join(' vs ')}`}
                      >
                        <AlertTriangle className="size-3" />
                        Conflict
                      </span>
                    )}
                  </div>
                </TableCell>
                <TableCell>
                  {conflict ? (
                    <Link
                      to={`/clinical/conflicts/${conflict.conflictId}`}
                      className="text-amber-700 underline hover:text-amber-900"
                      title="Click to view source documents"
                    >
                      {item.value}
                    </Link>
                  ) : (
                    item.value
                  )}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {conflict ? (
                    <Link
                      to={`/clinical/conflicts/${conflict.conflictId}`}
                      className="text-amber-600 underline hover:text-amber-800"
                      title="View source document"
                    >
                      {item.sourceDocument}
                    </Link>
                  ) : (
                    item.sourceDocument
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <ConfidenceScoreBadge score={item.confidence} />
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </Card>
  );
}

/** Loading skeleton for the 360 view. */
function PatientView360Skeleton() {
  return (
    <div className="mx-auto max-w-5xl space-y-6 p-6">
      {/* Breadcrumb skeleton */}
      <Skeleton className="h-4 w-32" />

      {/* Patient banner skeleton */}
      <Card>
        <CardContent className="flex items-center gap-5 p-5">
          <Skeleton className="size-16 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-7 w-48" />
            <Skeleton className="h-4 w-64" />
            <div className="flex gap-2">
              <Skeleton className="h-5 w-24" />
              <Skeleton className="h-5 w-28" />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tabs skeleton */}
      <div className="flex gap-4 border-b pb-2">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-8 w-24" />
        ))}
      </div>

      {/* Table skeleton */}
      <Card>
        <CardContent className="space-y-3 p-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="flex items-center gap-4">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 flex-1" />
              <Skeleton className="h-4 w-20" />
              <Skeleton className="h-5 w-12" />
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

/** Extract initials from full name. */
function getInitials(name: string): string {
  return name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
}

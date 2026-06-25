import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { cn } from '@/lib/utils';
import {
  ChevronLeft,
  ChevronRight,
  Filter,
  Loader2,
  Search,
  Shield,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import type {
  AuditLogEntry,
  AuditLogFilter,
} from '../api/auditApi';
import {
  fetchAuditLogs,
} from '../api/auditApi';

const PAGE_SIZE = 50;

const ACTION_OPTIONS = [
  'Login',
  'Login Failed',
  'Walk-in Registered',
  'Code Verified',
  'Conflict Resolved',
  'User Deactivated',
  'Document Uploaded',
];
const STATUS_OPTIONS = ['Success', 'Resolved', 'Warning', 'Blocked'];

export function AuditLogViewerPage() {
  const [searchParams] = useSearchParams();

  // Loading states
  const [loading, setLoading] = useState(true);

  // Action audit logs state
  const [auditLogs, setAuditLogs] = useState<AuditLogEntry[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);

  // Filters
  const [filters, setFilters] = useState<AuditLogFilter>({});
  const [showFilters, setShowFilters] = useState(false);

  // Filter inputs
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [actorName, setActorName] = useState('');
  const [action, setAction] = useState('');
  const [status, setStatus] = useState('');
  const [appliedStatus, setAppliedStatus] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  const initialActionFromQuery = searchParams.get('action') ?? '';

  const showToast = useCallback(
    (type: 'success' | 'error', message: string) => {
      if (type === 'success') {
        toast.success(message);
      } else {
        toast.error(message);
      }
    },
    []
  );

  // Load action audit logs
  useEffect(() => {
    let ignore = false;
    setLoading(true);

    fetchAuditLogs({ ...filters, page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (ignore) return;
        if (result.success) {
          setAuditLogs(result.data.items);
          setTotalCount(result.data.totalCount);
        } else {
          showToast('error', result.error.message);
        }
        setLoading(false);
      })
      .catch(() => {
        if (!ignore) setLoading(false);
      });

    return () => {
      ignore = true;
    };
  }, [filters, page, showToast]);

  useEffect(() => {
    if (!initialActionFromQuery) {
      return;
    }

    setAction(initialActionFromQuery);
    setShowFilters(true);
    setFilters((prev) => ({
      ...prev,
      action: initialActionFromQuery,
    }));
    setPage(1);
  }, [initialActionFromQuery]);

  const handleApplyFilters = () => {
    setFilters({
      startDate: startDate || undefined,
      endDate: endDate || undefined,
      actorName: actorName || undefined,
      action: action || undefined,
    });
    setAppliedStatus(status || '');
    setPage(1);
  };

  const handleClearFilters = () => {
    setStartDate('');
    setEndDate('');
    setActorName('');
    setAction('');
    setStatus('');
    setAppliedStatus('');
    setFilters({});
    setPage(1);
  };

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString();
  };

  const getActionStatus = (actionText: string) => {
    const action = actionText.toLowerCase();

    if (action.includes('failed')) {
      return { label: 'Blocked', className: 'bg-red-100 text-red-700' };
    }

    if (action.includes('rejected') || action.includes('deactivated') || action.includes('flagged')) {
      return { label: 'Warning', className: 'bg-amber-100 text-amber-700' };
    }

    if (action.includes('resolved')) {
      return { label: 'Resolved', className: 'bg-blue-100 text-blue-700' };
    }

    return { label: 'Success', className: 'bg-green-100 text-green-700' };
  };

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
            <Shield className="h-6 w-6 text-primary" />
            Audit Log Viewer
          </h1>
        </div>
      </div>

      <div className="space-y-4">
          {/* Filters */}
          <div className="bg-card border rounded-lg p-4 space-y-4">
            <div className="flex items-center justify-between">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="gap-2"
              >
                <Filter className="h-4 w-4" />
                {showFilters ? 'Hide Filters' : 'Show Filters'}
              </Button>
              <div className="flex items-center gap-2">
                <Search className="h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search actor name..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-64"
                />
              </div>
            </div>

            {showFilters && (
              <div className="grid grid-cols-1 md:grid-cols-5 gap-4 pt-4 border-t">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Start Date</label>
                  <Input
                    type="date"
                    value={startDate}
                    onChange={(e) => setStartDate(e.target.value)}
                  />
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">End Date</label>
                  <Input
                    type="date"
                    value={endDate}
                    onChange={(e) => setEndDate(e.target.value)}
                  />
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Action</label>
                  <Select value={action} onValueChange={(v) => setAction(v ?? '')}>
                    <SelectTrigger>
                      <SelectValue placeholder="All actions" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All actions</SelectItem>
                      {ACTION_OPTIONS.map((a) => (
                        <SelectItem key={a} value={a}>{a}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Status</label>
                  <Select value={status} onValueChange={(v) => setStatus(v ?? '')}>
                    <SelectTrigger>
                      <SelectValue placeholder="All statuses" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All statuses</SelectItem>
                      {STATUS_OPTIONS.map((s) => (
                        <SelectItem key={s} value={s}>{s}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-end gap-2">
                  <Button onClick={handleApplyFilters} className="flex-1">
                    Apply
                  </Button>
                  <Button variant="outline" onClick={handleClearFilters}>
                    Clear
                  </Button>
                </div>
              </div>
            )}
          </div>

          {/* Action Logs Table */}
          <div className="bg-card border rounded-lg overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Timestamp</TableHead>
                  <TableHead>Actor</TableHead>
                  <TableHead>Action</TableHead>
                  <TableHead>Resource</TableHead>
                  <TableHead>Resource ID</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8">
                      <Loader2 className="h-6 w-6 animate-spin mx-auto text-muted-foreground" />
                    </TableCell>
                  </TableRow>
                ) : auditLogs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                      No audit logs found
                    </TableCell>
                  </TableRow>
                ) : (
                  auditLogs
                    .filter((log) =>
                      searchTerm
                        ? log.actorName.toLowerCase().includes(searchTerm.toLowerCase())
                        : true
                    )
                    .filter((log) => {
                      if (!appliedStatus) return true;
                      return getActionStatus(log.action).label === appliedStatus;
                    })
                    .map((log, index) => {
                      const status = getActionStatus(log.action);

                      return (
                        <TableRow key={`${log.timestamp}-${index}`}>
                        <TableCell className="font-mono text-sm">
                          {formatTimestamp(log.timestamp)}
                        </TableCell>
                        <TableCell>{log.actorName}</TableCell>
                        <TableCell>
                          <span
                            className={cn(
                              'px-2 py-1 rounded-full text-xs font-medium',
                              log.action === 'Create' && 'bg-green-100 text-green-700',
                              log.action === 'Update' && 'bg-blue-100 text-blue-700',
                              log.action === 'Delete' && 'bg-red-100 text-red-700',
                              log.action === 'View' && 'bg-gray-100 text-gray-700'
                            )}
                          >
                            {log.action}
                          </span>
                        </TableCell>
                        <TableCell>{log.resource}</TableCell>
                        <TableCell className="font-mono text-xs">{log.resourceId || '-'}</TableCell>
                        <TableCell>
                          <span className={cn('px-2 py-1 rounded-full text-xs font-medium', status.className)}>
                            {status.label}
                          </span>
                        </TableCell>
                        </TableRow>
                      );
                    })
                )}
              </TableBody>
            </Table>

            {/* Pagination */}
            <div className="flex items-center justify-between px-4 py-3 border-t">
              <div className="text-sm text-muted-foreground">
                Showing {Math.min((page - 1) * PAGE_SIZE + 1, totalCount)} -{' '}
                {Math.min(page * PAGE_SIZE, totalCount)} of {totalCount.toLocaleString()} records
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page === 1 || loading}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-sm text-muted-foreground">
                  Page {page} of {totalPages || 1}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages || loading}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
      </div>
    </div>
  );
}

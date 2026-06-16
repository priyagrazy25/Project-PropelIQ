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
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import {
  Bot,
  ChevronLeft,
  ChevronRight,
  Download,
  Filter,
  Loader2,
  Search,
  Shield,
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';
import type {
  AiAuditLogEntry,
  AuditLogEntry,
  AuditLogFilter,
  AuditStatsResponse,
} from '../api/auditApi';
import {
  exportAuditLogs,
  fetchAiAuditLogs,
  fetchAuditLogs,
  fetchAuditStats,
} from '../api/auditApi';

const PAGE_SIZE = 50;

const ACTION_OPTIONS = ['Create', 'Update', 'Delete', 'View', 'Login', 'Logout'];
const RESOURCE_OPTIONS = [
  'Patient',
  'User',
  'Appointment',
  'ClinicalDocument',
  'ExtractedData',
  'InsuranceVerification',
];

export function AuditLogViewerPage() {
  // Tab state
  const [activeTab, setActiveTab] = useState<'actions' | 'ai'>('actions');

  // Loading states
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);

  // Action audit logs state
  const [auditLogs, setAuditLogs] = useState<AuditLogEntry[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);

  // AI audit logs state
  const [aiAuditLogs, setAiAuditLogs] = useState<AiAuditLogEntry[]>([]);
  const [aiTotalCount, setAiTotalCount] = useState(0);
  const [aiPage, setAiPage] = useState(1);

  // Stats state
  const [stats, setStats] = useState<AuditStatsResponse | null>(null);

  // Filters
  const [filters, setFilters] = useState<AuditLogFilter>({});
  const [showFilters, setShowFilters] = useState(false);

  // Filter inputs
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [actorName, setActorName] = useState('');
  const [action, setAction] = useState('');
  const [resource, setResource] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

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
    if (activeTab !== 'actions') return;

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
  }, [activeTab, filters, page, showToast]);

  // Load AI audit logs
  useEffect(() => {
    if (activeTab !== 'ai') return;

    let ignore = false;
    setLoading(true);

    fetchAiAuditLogs({ page: aiPage, pageSize: PAGE_SIZE })
      .then((result) => {
        if (ignore) return;
        if (result.success) {
          setAiAuditLogs(result.data.items);
          setAiTotalCount(result.data.totalCount);
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
  }, [activeTab, aiPage, showToast]);

  // Load stats
  useEffect(() => {
    fetchAuditStats().then((result) => {
      if (result.success) {
        setStats(result.data);
      }
    });
  }, []);

  const handleApplyFilters = () => {
    setFilters({
      startDate: startDate || undefined,
      endDate: endDate || undefined,
      actorName: actorName || undefined,
      action: action || undefined,
      resource: resource || undefined,
    });
    setPage(1);
  };

  const handleClearFilters = () => {
    setStartDate('');
    setEndDate('');
    setActorName('');
    setAction('');
    setResource('');
    setFilters({});
    setPage(1);
  };

  const handleExport = async () => {
    setExporting(true);
    const result = await exportAuditLogs(filters);
    setExporting(false);

    if (result.success) {
      const url = URL.createObjectURL(result.data);
      const a = document.createElement('a');
      a.href = url;
      a.download = `audit-logs-${new Date().toISOString().split('T')[0]}.csv`;
      a.click();
      URL.revokeObjectURL(url);
      showToast('success', 'Audit logs exported successfully');
    } else {
      showToast('error', result.error.message);
    }
  };

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);
  const aiTotalPages = Math.ceil(aiTotalCount / PAGE_SIZE);

  const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString();
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
          <p className="text-muted-foreground mt-1">
            View and export immutable audit records for compliance (DR-011)
          </p>
        </div>
        <Button
          variant="outline"
          onClick={handleExport}
          disabled={exporting}
          className="gap-2"
        >
          {exporting ? (
            <Loader2 className="h-4 w-4 animate-spin" />
          ) : (
            <Download className="h-4 w-4" />
          )}
          Export CSV
        </Button>
      </div>

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Total Records</div>
            <div className="text-2xl font-bold text-foreground">
              {stats.totalRecords.toLocaleString()}
            </div>
          </div>
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Unique Actors</div>
            <div className="text-2xl font-bold text-foreground">
              {stats.uniqueActors.toLocaleString()}
            </div>
          </div>
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Top Action</div>
            <div className="text-2xl font-bold text-foreground">
              {Object.entries(stats.actionBreakdown).sort(([, a], [, b]) => b - a)[0]?.[0] || 'N/A'}
            </div>
          </div>
          <div className="bg-card border rounded-lg p-4">
            <div className="text-sm text-muted-foreground">Top Resource</div>
            <div className="text-2xl font-bold text-foreground">
              {Object.entries(stats.resourceBreakdown).sort(([, a], [, b]) => b - a)[0]?.[0] || 'N/A'}
            </div>
          </div>
        </div>
      )}

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as 'actions' | 'ai')}>
        <TabsList>
          <TabsTrigger value="actions" className="gap-2">
            <Shield className="h-4 w-4" />
            Action Logs
          </TabsTrigger>
          <TabsTrigger value="ai" className="gap-2">
            <Bot className="h-4 w-4" />
            AI Invocations
          </TabsTrigger>
        </TabsList>

        <TabsContent value="actions" className="space-y-4">
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
                  <label className="text-sm font-medium text-muted-foreground">Resource</label>
                  <Select value={resource} onValueChange={(v) => setResource(v ?? '')}>
                    <SelectTrigger>
                      <SelectValue placeholder="All resources" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">All resources</SelectItem>
                      {RESOURCE_OPTIONS.map((r) => (
                        <SelectItem key={r} value={r}>{r}</SelectItem>
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
                  <TableHead>IP Address</TableHead>
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
                    .map((log, index) => (
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
                        <TableCell className="font-mono text-xs">{log.ipAddress || '-'}</TableCell>
                      </TableRow>
                    ))
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
        </TabsContent>

        <TabsContent value="ai" className="space-y-4">
          {/* AI Audit Logs Table */}
          <div className="bg-card border rounded-lg overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Function</TableHead>
                  <TableHead>Model</TableHead>
                  <TableHead>Tokens (In/Out)</TableHead>
                  <TableHead>Confidence</TableHead>
                  <TableHead>Duration</TableHead>
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
                ) : aiAuditLogs.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                      No AI invocation logs found
                    </TableCell>
                  </TableRow>
                ) : (
                  aiAuditLogs.map((log, index) => (
                    <TableRow key={`${log.correlationId}-${index}`}>
                      <TableCell className="font-mono text-sm">{log.functionName}</TableCell>
                      <TableCell>{log.modelId}</TableCell>
                      <TableCell className="font-mono text-sm">
                        {log.promptTokens} / {log.completionTokens}
                      </TableCell>
                      <TableCell>
                        {log.confidenceScore !== null
                          ? `${(log.confidenceScore * 100).toFixed(1)}%`
                          : '-'}
                      </TableCell>
                      <TableCell className="font-mono text-sm">{log.durationMs}ms</TableCell>
                      <TableCell>
                        <span
                          className={cn(
                            'px-2 py-1 rounded-full text-xs font-medium',
                            log.success
                              ? 'bg-green-100 text-green-700'
                              : 'bg-red-100 text-red-700'
                          )}
                        >
                          {log.success ? 'Success' : 'Failed'}
                        </span>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>

            {/* AI Pagination */}
            <div className="flex items-center justify-between px-4 py-3 border-t">
              <div className="text-sm text-muted-foreground">
                Showing {Math.min((aiPage - 1) * PAGE_SIZE + 1, aiTotalCount)} -{' '}
                {Math.min(aiPage * PAGE_SIZE, aiTotalCount)} of {aiTotalCount.toLocaleString()} records
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setAiPage((p) => Math.max(1, p - 1))}
                  disabled={aiPage === 1 || loading}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-sm text-muted-foreground">
                  Page {aiPage} of {aiTotalPages || 1}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setAiPage((p) => Math.min(aiTotalPages, p + 1))}
                  disabled={aiPage >= aiTotalPages || loading}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}

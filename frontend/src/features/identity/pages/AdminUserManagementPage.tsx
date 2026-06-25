import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Plus, Search } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import { useAppSelector } from '../../../app/hooks';
import type { UserListItem, UserRole, UserStatus } from '../api/adminApi';
import {
  createUser,
  deactivateUser,
  fetchUsers,
  reactivateUser,
  updateUser,
} from '../api/adminApi';
import { DeactivateDialog } from '../components/DeactivateDialog';
import type { UserFormData } from '../components/UserForm';
import { UserForm } from '../components/UserForm';
import { UserTable } from '../components/UserTable';

const PAGE_SIZE = 10;

export function AdminUserManagementPage() {
  const currentUserId = useAppSelector((state) => state.identity.userId);

  // Data state
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);

  // Filters
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('');
  const [statusFilter, setStatusFilter] = useState<UserStatus | ''>('');

  // Modal state
  const [formMode, setFormMode] = useState<'create' | 'edit' | null>(null);
  const [editingUser, setEditingUser] = useState<UserListItem | null>(null);
  const [formLoading, setFormLoading] = useState(false);

  // Deactivate dialog state
  const [deactivatingUser, setDeactivatingUser] = useState<UserListItem | null>(
    null,
  );
  const [deactivateLoading, setDeactivateLoading] = useState(false);

  const searchDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const showToast = useCallback(
    (type: 'success' | 'error', message: string) => {
      if (type === 'success') {
        toast.success(message);
      } else {
        toast.error(message);
      }
    },
    [],
  );

  // Reload trigger: increment to force re-fetch
  const [reloadKey, setReloadKey] = useState(0);
  const triggerReload = useCallback(() => {
    setLoading(true);
    setReloadKey((k) => k + 1);
  }, []);

  useEffect(() => {
    let ignore = false;

    fetchUsers({
      page,
      pageSize: PAGE_SIZE,
      search: search || undefined,
      role: roleFilter || undefined,
      status: statusFilter || undefined,
    })
      .then((result) => {
        if (ignore) return;
        if (result.success) {
          setUsers(result.data.users);
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
  }, [page, search, roleFilter, statusFilter, reloadKey, showToast]);

  const handleSearchChange = (value: string) => {
    setLoading(true);
    setSearch(value);
    setPage(1);
    if (searchDebounceRef.current) {
      clearTimeout(searchDebounceRef.current);
    }
    searchDebounceRef.current = setTimeout(() => {
      // loadUsers triggered via useEffect dependency on search
    }, 300);
  };

  const handleRoleFilterChange = (value: string) => {
    setLoading(true);
    setRoleFilter(value as UserRole | '');
    setPage(1);
  };

  const handleStatusFilterChange = (value: string) => {
    setLoading(true);
    setStatusFilter(value as UserStatus | '');
    setPage(1);
  };

  // Create / Edit form handlers
  const handleOpenCreate = () => {
    setEditingUser(null);
    setFormMode('create');
  };

  const handleOpenEdit = (user: UserListItem) => {
    setEditingUser(user);
    setFormMode('edit');
  };

  const handleFormCancel = () => {
    setFormMode(null);
    setEditingUser(null);
  };

  const handleFormSubmit = async (data: UserFormData) => {
    if (!data.role) return;
    setFormLoading(true);

    if (formMode === 'create') {
      const result = await createUser({
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        role: data.role,
        phone: data.phone || undefined,
      });
      if (result.success) {
        showToast(
          'success',
          `User "${data.firstName} ${data.lastName}" created successfully.`,
        );
        setFormMode(null);
        triggerReload();
      } else {
        showToast('error', result.error.message);
      }
    } else if (formMode === 'edit' && editingUser) {
      const result = await updateUser(editingUser.id, {
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        role: data.role,
        phone: data.phone || undefined,
      });
      if (result.success) {
        showToast(
          'success',
          `User "${data.firstName} ${data.lastName}" updated successfully.`,
        );
        setFormMode(null);
        setEditingUser(null);
        triggerReload();
      } else {
        showToast('error', result.error.message);
      }
    }

    setFormLoading(false);
  };

  // Deactivate handlers
  const handleDeactivateClick = (user: UserListItem) => {
    setDeactivatingUser(user);
  };

  const handleDeactivateConfirm = async () => {
    if (!deactivatingUser) return;
    setDeactivateLoading(true);

    const result = await deactivateUser(deactivatingUser.id);
    if (result.success) {
      showToast(
        'success',
        `User "${deactivatingUser.fullName}" has been deactivated.`,
      );
      setDeactivatingUser(null);
      triggerReload();
    } else {
      showToast('error', result.error.message);
    }

    setDeactivateLoading(false);
  };

  const handleDeactivateCancel = () => {
    setDeactivatingUser(null);
  };

  // Reactivate handler
  const handleReactivate = async (userId: string) => {
    const result = await reactivateUser(userId);
    if (result.success) {
      showToast('success', 'User reactivated successfully.');
      triggerReload();
    } else {
      showToast('error', result.error.message);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-foreground">User Management</h1>
        <Button onClick={handleOpenCreate}>
          <Plus className="h-4 w-4 mr-2" />
          Create User
        </Button>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            type="text"
            placeholder="Search by name or email..."
            aria-label="Search users"
            value={search}
            onChange={(e) => {
              handleSearchChange(e.target.value);
            }}
            className="pl-9"
          />
        </div>
        <select
          className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          aria-label="Filter by role"
          value={roleFilter}
          onChange={(e) => {
            handleRoleFilterChange(e.target.value);
          }}
        >
          <option value="">All Roles</option>
          <option value="Admin">Admin</option>
          <option value="Patient">Patient</option>
          <option value="FrontDesk">Staff</option>
        </select>
        <select
          className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-xs transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          aria-label="Filter by status"
          value={statusFilter}
          onChange={(e) => {
            handleStatusFilterChange(e.target.value);
          }}
        >
          <option value="">All Statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
        </select>
      </div>

      <UserTable
        users={users}
        loading={loading}
        currentUserId={currentUserId}
        onEdit={handleOpenEdit}
        onDeactivate={handleDeactivateClick}
        onReactivate={(id) => {
          void handleReactivate(id);
        }}
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
      />

      {formMode !== null && (
        <UserForm
          mode={formMode}
          user={editingUser}
          onSubmit={(data) => {
            void handleFormSubmit(data);
          }}
          onCancel={handleFormCancel}
          loading={formLoading}
        />
      )}

      {deactivatingUser !== null && (
        <DeactivateDialog
          userName={deactivatingUser.fullName}
          onConfirm={() => {
            void handleDeactivateConfirm();
          }}
          onCancel={handleDeactivateCancel}
          loading={deactivateLoading}
        />
      )}
    </div>
  );
}

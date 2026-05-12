import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ChevronRight,
  CircleSlash2,
  Mail,
  MailCheck,
  Search,
  ShieldCheck,
  Sparkles,
  UserPlus,
  Users as UsersIcon,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  listRoles,
  registerUser,
  searchUsers,
  type RoleDto,
  type UserDto,
  type RegisterUserInput,
} from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DensityToggle,
  EmptyState,
  ErrorBand,
  Field,
  ListHero,
  Pagination,
  SortChips,
  Stat,
  StatStrip,
  usePersistedDensity,
  type Density,
  type SortDir,
  type SortOption,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";
import { describe, pad2 } from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DENSITY_KEY = "fsh.dashboard.identity.users.density";

type SortKey = "userName" | "email" | "lastName";

const SORT_OPTIONS: SortOption<SortKey>[] = [
  { key: "userName", label: "Username" },
  { key: "lastName", label: "Last name" },
  { key: "email", label: "Email" },
];

type StatusFilter = "all" | "active" | "inactive";
type EmailFilter = "all" | "confirmed" | "unconfirmed";

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function UsersPage() {
  const { user: authedUser } = useAuth();
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [emailFilter, setEmailFilter] = useState<EmailFilter>("all");
  const [roleFilter, setRoleFilter] = useState<string | null>(null);

  const [sortKey, setSortKey] = useState<SortKey>("userName");
  const [sortDir, setSortDir] = useState<SortDir>("asc");

  const [density, setDensity] = usePersistedDensity(DENSITY_KEY);
  const [registerOpen, setRegisterOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      sort: `${sortKey} ${sortDir}`,
      isActive: statusFilter === "all" ? null : statusFilter === "active",
      emailConfirmed: emailFilter === "all" ? null : emailFilter === "confirmed",
      roleId: roleFilter,
    }),
    [pageNumber, debouncedSearch, sortKey, sortDir, statusFilter, emailFilter, roleFilter],
  );

  const query = useQuery({
    queryKey: ["identity", "users", queryParams],
    queryFn: () => searchUsers(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const stats = useMemo(() => {
    if (!data) return null;
    const active = items.filter((u) => u.isActive).length;
    const confirmed = items.filter((u) => u.emailConfirmed).length;
    const pendingPct =
      items.length === 0
        ? 0
        : Math.round(((items.length - confirmed) / items.length) * 100);
    return {
      total: data.totalCount,
      active,
      pendingPct,
    };
  }, [data, items]);

  const onSort = useCallback(
    (key: SortKey) => {
      if (sortKey === key) {
        setSortDir((d) => (d === "asc" ? "desc" : "asc"));
      } else {
        setSortKey(key);
        setSortDir("asc");
      }
    },
    [sortKey],
  );

  const rolesQuery = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
    staleTime: 60_000,
  });

  const filtersActive =
    debouncedSearch.length > 0 ||
    statusFilter !== "all" ||
    emailFilter !== "all" ||
    roleFilter !== null;

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setEmailFilter("all");
    setRoleFilter(null);
  };

  return (
    <div className="space-y-7 pb-12">
      <ListHero
        eyebrow="Identity · Members"
        tenant={authedUser?.tenant ?? "—"}
        subEyebrow="people with access"
        title="Users"
        totalCount={data?.totalCount ?? null}
        subtitle="Every member with access to this tenant. Register newcomers, review their status, and tune the roles attached to each account."
        searchValue={search}
        onSearch={setSearch}
        searchPlaceholder="Find by name, username, or email…"
        isFetching={query.isFetching}
        onRefresh={() => void query.refetch()}
        ctaLabel="Register user"
        onCreate={() => setRegisterOpen(true)}
      />

      {stats && data && data.totalCount > 0 && (
        <StatStrip cols={3}>
          <Stat label="Total members" value={pad2(stats.total)} hint="across this tenant" />
          <Stat
            label="Active on this page"
            value={pad2(stats.active)}
            hint={stats.active === items.length ? "all enabled" : `${items.length - stats.active} inactive`}
            accent
          />
          <Stat
            label="Email pending"
            value={`${stats.pendingPct}%`}
            hint={stats.pendingPct === 0 ? "all confirmed" : "still awaiting confirmation"}
            tone={stats.pendingPct > 50 ? "warning" : "default"}
          />
        </StatStrip>
      )}

      <FilterBar
        statusFilter={statusFilter}
        emailFilter={emailFilter}
        roleFilter={roleFilter}
        roles={rolesQuery.data ?? []}
        onStatusChange={(v) => {
          setStatusFilter(v);
          setPageNumber(1);
        }}
        onEmailChange={(v) => {
          setEmailFilter(v);
          setPageNumber(1);
        }}
        onRoleChange={(v) => {
          setRoleFilter(v);
          setPageNumber(1);
        }}
        active={filtersActive}
        onClear={clearFilters}
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      <section className="fsh-enter fsh-enter-3 space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <SortChips
            options={SORT_OPTIONS}
            sortKey={sortKey}
            sortDir={sortDir}
            onSort={onSort}
            prefixLabel={debouncedSearch ? "results" : "directory"}
          />
          <DensityToggle density={density} onChange={setDensity} />
        </div>

        <div
          className={cn(
            "card-shell overflow-hidden rounded-2xl",
            "bg-[var(--color-surface-3)]",
          )}
        >
          {query.isLoading && items.length === 0 ? (
            <ul aria-busy>
              {Array.from({ length: 6 }).map((_, i) => (
                <SkeletonRow key={i} delayMs={i * 40} density={density} />
              ))}
            </ul>
          ) : items.length === 0 ? (
            <EmptyState
              eyebrow={filtersActive ? "No matches" : "Empty directory"}
              headline={
                filtersActive
                  ? "No users match those filters."
                  : "No one's been registered yet."
              }
              body={
                filtersActive
                  ? "Search runs across name, username, and email. Try a different term, or clear the filters."
                  : "Register the first member to seed this tenant. They'll receive a confirmation email if email confirmation is enabled."
              }
              icon={
                filtersActive ? (
                  <Search className="h-6 w-6 text-[var(--color-primary)]" />
                ) : (
                  <UsersIcon className="h-6 w-6 text-[var(--color-primary)]" />
                )
              }
              primaryAction={{
                label: filtersActive ? "Register a new member" : "Register the first member",
                onClick: () => setRegisterOpen(true),
                icon: <Sparkles className="h-3.5 w-3.5" />,
              }}
              secondaryAction={
                filtersActive
                  ? {
                      label: "Clear filters",
                      onClick: clearFilters,
                      icon: <X className="h-3.5 w-3.5" />,
                    }
                  : undefined
              }
            />
          ) : (
            <ul>
              {items.map((user, i) => (
                <UserRow
                  key={user.id ?? i}
                  user={user}
                  density={density}
                  delayMs={Math.min(i, 8) * 30}
                />
              ))}
            </ul>
          )}
        </div>
      </section>

      {data && data.totalCount > 0 && (
        <Pagination
          page={data.pageNumber}
          totalPages={Math.max(data.totalPages, 1)}
          totalCount={data.totalCount}
          shown={items.length}
          fetching={query.isFetching}
          onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
          onNext={() => setPageNumber((p) => p + 1)}
          hasPrev={data.hasPrevious}
          hasNext={data.hasNext}
        />
      )}

      <RegisterUserDialog open={registerOpen} onClose={() => setRegisterOpen(false)} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Filters
// ───────────────────────────────────────────────────────────────────────

function FilterBar({
  statusFilter,
  emailFilter,
  roleFilter,
  roles,
  onStatusChange,
  onEmailChange,
  onRoleChange,
  active,
  onClear,
}: {
  statusFilter: StatusFilter;
  emailFilter: EmailFilter;
  roleFilter: string | null;
  roles: RoleDto[];
  onStatusChange: (v: StatusFilter) => void;
  onEmailChange: (v: EmailFilter) => void;
  onRoleChange: (v: string | null) => void;
  active: boolean;
  onClear: () => void;
}) {
  return (
    <div className="fsh-enter fsh-enter-2 flex flex-wrap items-center gap-2">
      <span className="font-mono text-[10px] font-medium uppercase tracking-[0.16em] text-[var(--color-muted-foreground)]">
        filter
      </span>
      <span aria-hidden className="h-3 w-px bg-[var(--color-border-strong)]" />

      <FilterGroup
        label="Account"
        value={statusFilter}
        onChange={onStatusChange}
        options={[
          { value: "all", label: "Any" },
          { value: "active", label: "Active", icon: <ShieldCheck className="h-3 w-3" /> },
          { value: "inactive", label: "Inactive", icon: <CircleSlash2 className="h-3 w-3" /> },
        ]}
      />

      <FilterGroup
        label="Email"
        value={emailFilter}
        onChange={onEmailChange}
        options={[
          { value: "all", label: "Any" },
          { value: "confirmed", label: "Confirmed", icon: <MailCheck className="h-3 w-3" /> },
          { value: "unconfirmed", label: "Pending", icon: <Mail className="h-3 w-3" /> },
        ]}
      />

      {roles.length > 0 && (
        <div className="surface-edge inline-flex h-8 items-center gap-1 rounded-full bg-[var(--color-surface-3)] px-2">
          <span className="font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/70">
            Role
          </span>
          <select
            value={roleFilter ?? ""}
            onChange={(e) => onRoleChange(e.target.value || null)}
            className={cn(
              "h-7 cursor-pointer rounded-full bg-transparent px-2 text-[11px] font-medium outline-none",
              "text-[var(--color-foreground)]",
              "focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            )}
          >
            <option value="">Any</option>
            {roles.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
          </select>
        </div>
      )}

      {active && (
        <Button
          variant="ghost"
          size="sm"
          onClick={onClear}
          className="ml-1 h-7 gap-1 px-2 text-[11px] font-mono uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]"
        >
          <X className="h-3 w-3" /> Clear
        </Button>
      )}
    </div>
  );
}

function FilterGroup<T extends string>({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: T;
  onChange: (v: T) => void;
  options: Array<{ value: T; label: string; icon?: React.ReactNode }>;
}) {
  return (
    <div
      role="group"
      aria-label={label}
      className="surface-edge inline-flex h-8 items-center rounded-full bg-[var(--color-surface-3)] p-0.5"
    >
      <span className="px-2 font-mono text-[10px] uppercase tracking-[0.14em] text-[var(--color-muted-foreground)]/70">
        {label}
      </span>
      {options.map((opt) => {
        const active = value === opt.value;
        return (
          <button
            key={opt.value}
            type="button"
            onClick={() => onChange(opt.value)}
            aria-pressed={active}
            className={cn(
              "inline-flex h-7 items-center gap-1 rounded-full px-2.5 text-[11px] font-medium",
              "transition-colors duration-[var(--duration-fast)]",
              active
                ? "bg-[var(--color-surface-1)] text-[var(--color-foreground)] shadow-[var(--highlight-top),0_1px_2px_oklch(0.115_0.010_270/0.06)]"
                : "text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]",
            )}
          >
            {opt.icon}
            {opt.label}
          </button>
        );
      })}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Row
// ───────────────────────────────────────────────────────────────────────

function fullName(u: UserDto): string {
  const parts = [u.firstName, u.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(" ");
  return u.userName ?? u.email ?? "Unnamed user";
}

function UserRow({
  user,
  density,
  delayMs,
}: {
  user: UserDto;
  density: Density;
  delayMs: number;
}) {
  const padY = density === "compact" ? "py-3" : "py-4";
  const id = user.id;
  const display = fullName(user);

  return (
    <li
      className={cn(
        "fsh-enter group/row relative border-b border-[var(--color-border)] last:border-b-0",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "hover:bg-[var(--color-surface-4)]",
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <span
        aria-hidden
        className={cn(
          "absolute inset-y-2.5 left-0 w-[2px] rounded-r-full bg-[var(--color-primary)]",
          "opacity-0 transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "group-hover/row:opacity-100 group-focus-within/row:opacity-100",
        )}
      />

      <Link
        to={id ? `/identity/users/${id}` : "#"}
        aria-disabled={!id}
        className={cn(
          "flex items-center gap-4 px-5 sm:px-6",
          padY,
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2 focus-visible:ring-offset-[var(--color-background)]",
        )}
      >
        <Avatar
          name={display}
          src={user.imageUrl ?? undefined}
          size={density === "compact" ? "sm" : "md"}
        />

        <div className="flex min-w-0 flex-1 items-center gap-6">
          <div className="min-w-0 flex-[1.4]">
            <div className="flex items-baseline gap-2">
              <span className="text-display truncate text-[15px] font-semibold leading-tight tracking-[-0.01em] sm:text-[15.5px]">
                {display}
              </span>
              {user.userName && user.userName !== display && (
                <code
                  title={user.userName}
                  className="hidden truncate rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)] sm:inline-block"
                >
                  @{user.userName}
                </code>
              )}
            </div>
            <div
              className={cn(
                "mt-0.5 truncate text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]",
                !user.email && "italic opacity-60",
              )}
            >
              {user.email ?? "no email on file"}
            </div>
          </div>

          <div className="hidden min-w-[200px] items-center gap-1.5 sm:flex">
            {user.isActive ? (
              <Badge variant="success">
                <ShieldCheck className="h-3 w-3" /> Active
              </Badge>
            ) : (
              <Badge variant="outline">
                <CircleSlash2 className="h-3 w-3" /> Inactive
              </Badge>
            )}
            {user.emailConfirmed ? (
              <Badge variant="brand">
                <MailCheck className="h-3 w-3" /> Confirmed
              </Badge>
            ) : (
              <Badge variant="warning">
                <Mail className="h-3 w-3" /> Pending
              </Badge>
            )}
          </div>
        </div>

        <ChevronRight className="h-4 w-4 shrink-0 text-[var(--color-muted-foreground)] opacity-0 transition-opacity group-hover/row:opacity-100" />
      </Link>
    </li>
  );
}

function SkeletonRow({ delayMs, density }: { delayMs: number; density: Density }) {
  const padY = density === "compact" ? "py-3" : "py-4";
  return (
    <li
      className={cn(
        "fsh-enter flex items-center gap-4 border-b border-[var(--color-border)] px-5 last:border-b-0 sm:px-6",
        padY,
      )}
      style={{ animationDelay: `${delayMs}ms` }}
    >
      <Skeleton className={cn("rounded-full", density === "compact" ? "h-7 w-7" : "h-9 w-9")} />
      <div className="flex flex-1 items-center gap-6">
        <div className="min-w-0 flex-[1.4] space-y-2">
          <Skeleton className="h-4 w-44" />
          <Skeleton className="h-3 w-72" />
        </div>
        <div className="hidden min-w-[200px] gap-2 sm:flex">
          <Skeleton className="h-5 w-16" />
          <Skeleton className="h-5 w-20" />
        </div>
      </div>
      <Skeleton className="h-3 w-3" />
    </li>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Register dialog
// ───────────────────────────────────────────────────────────────────────

function RegisterUserDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");

  useEffect(() => {
    if (!open) {
      setFirstName("");
      setLastName("");
      setEmail("");
      setUserName("");
      setPassword("");
      setConfirmPassword("");
      setPhoneNumber("");
    }
  }, [open]);

  const passwordMismatch = confirmPassword.length > 0 && password !== confirmPassword;

  const mutation = useMutation({
    mutationFn: (input: RegisterUserInput) => registerUser(input),
    onSuccess: () => {
      toast.success("User registered", {
        description: "An email confirmation may be required before sign-in.",
      });
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      onClose();
    },
    onError: (err) => toast.error("Registration failed", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (passwordMismatch) return;
    mutation.mutate({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      email: email.trim(),
      userName: userName.trim(),
      password,
      confirmPassword,
      phoneNumber: phoneNumber.trim() || undefined,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <span className="font-mono text-[10.5px] font-medium uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              New entry · directory
            </span>
            <DialogTitle>Register a member</DialogTitle>
            <DialogDescription>
              Add a new user to this tenant. Username and email must be unique. Passwords need an
              uppercase letter, lowercase letter, and a digit.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="reg-first" label="First name" required>
                <Input
                  id="reg-first"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Ada"
                  autoFocus
                  required
                />
              </Field>
              <Field id="reg-last" label="Last name" required>
                <Input
                  id="reg-last"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Lovelace"
                  required
                />
              </Field>
            </div>

            <Field id="reg-username" label="Username" required hint="Used at sign-in. Lowercase, no spaces.">
              <Input
                id="reg-username"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                placeholder="ada.lovelace"
                autoComplete="off"
                required
              />
            </Field>

            <Field id="reg-email" label="Email" required>
              <Input
                id="reg-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="ada@example.com"
                required
              />
            </Field>

            <Field id="reg-phone" label="Phone" hint="Optional contact number.">
              <Input
                id="reg-phone"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                placeholder="+44 …"
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="reg-pwd" label="Password" required>
                <Input
                  id="reg-pwd"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                />
              </Field>
              <Field
                id="reg-pwd2"
                label="Confirm"
                required
                hint={passwordMismatch ? "Passwords don't match." : undefined}
              >
                <Input
                  id="reg-pwd2"
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  autoComplete="new-password"
                  required
                  aria-invalid={passwordMismatch || undefined}
                />
              </Field>
            </div>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || passwordMismatch}
              className="brand-glow gradient-sheen gap-1.5"
            >
              <UserPlus className="h-4 w-4" />
              {mutation.isPending ? "Registering…" : "Register user"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}


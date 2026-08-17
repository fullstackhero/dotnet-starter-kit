import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ChevronRight,
  Plus,
  UserPlus,
  Users,
} from "lucide-react";
import { toast } from "sonner";
import {
  listRoles,
  registerUser,
  searchUsers,
  type UserDto,
  type RegisterUserInput,
} from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

type StatusFilter = "all" | "active" | "inactive";
type EmailFilter = "all" | "confirmed" | "unconfirmed";

// Desktop grid template, shared by header + rows + skeleton.
const DESKTOP_COLS =
  "grid-cols-[1fr_140px_24px] lg:grid-cols-[1.6fr_140px_180px_24px]";

function fullName(u: UserDto, t: TFunction): string {
  const parts = [u.firstName, u.lastName].filter(Boolean);
  if (parts.length > 0) return parts.join(" ");
  return u.userName ?? u.email ?? t("unnamedUser");
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function UsersPage() {
  const { t } = useTranslation("identity");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [emailFilter, setEmailFilter] = useState<EmailFilter>("all");
  const [roleFilter, setRoleFilter] = useState<string | null>(null);
  const [registerOpen, setRegisterOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => {
    setPageNumber(1);
  }, [statusFilter, emailFilter, roleFilter]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      sort: "userName asc",
      isActive: statusFilter === "all" ? null : statusFilter === "active",
      emailConfirmed:
        emailFilter === "all" ? null : emailFilter === "confirmed",
      roleId: roleFilter,
    }),
    [pageNumber, debouncedSearch, statusFilter, emailFilter, roleFilter],
  );

  const query = useQuery({
    queryKey: ["identity", "users", queryParams],
    queryFn: () => searchUsers(queryParams),
    placeholderData: keepPreviousData,
  });

  const rolesQuery = useQuery({
    queryKey: ["identity", "roles"],
    queryFn: listRoles,
    staleTime: 60_000,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const filtersApplied =
    statusFilter !== "all" || emailFilter !== "all" || roleFilter !== null;
  const searchActive = debouncedSearch.length > 0 || filtersApplied;

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setEmailFilter("all");
    setRoleFilter(null);
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Users}
        title={t("usersList.title")}
        total={data?.totalCount ?? null}
        unit="user"
        description={t("usersList.description")}
      >
        <Button
          onClick={() => setRegisterOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          {t("usersList.registerUser")}
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder={t("usersList.searchPlaceholder")}
      />

      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill
          label={t("usersList.filterAccountStatus")}
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: t("usersList.filterAll") },
            { value: "active", label: t("usersList.filterActive") },
            { value: "inactive", label: t("usersList.filterInactive") },
          ]}
        />
        <EntityFilterPill
          label={t("usersList.filterEmailStatus")}
          value={emailFilter}
          onChange={setEmailFilter}
          options={[
            { value: "all", label: t("usersList.filterAnyEmail") },
            { value: "confirmed", label: t("usersList.filterConfirmed") },
            { value: "unconfirmed", label: t("usersList.filterPending") },
          ]}
        />
        <Combobox
          label={t("usersList.filterRole")}
          value={roleFilter}
          onChange={setRoleFilter}
          options={(rolesQuery.data ?? []).map((r) => ({
            value: r.id,
            label: r.name,
          }))}
          variant="filter"
          searchable
          clearable
        />
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Users}
          title={searchActive ? t("usersList.emptyFoundTitle") : t("usersList.emptyTitle")}
          body={
            searchActive
              ? debouncedSearch
                ? t("usersList.emptySearchBodyTerm", { term: debouncedSearch })
                : t("usersList.emptySearchBodyNoTerm")
              : t("usersList.emptyBody")
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={clearFilters}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                {t("usersList.clearFilters")}
              </Button>
            ) : (
              <Button
                onClick={() => setRegisterOpen(true)}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                {t("usersList.registerUser")}
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {t("usersList.found", { count: data?.totalCount ?? 0 })}
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((user) => (
              <UserMobileCard key={user.id ?? user.userName} user={user} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>{t("usersList.colName")}</span>
              <span>{t("usersList.colUsername")}</span>
              <span className="hidden lg:block">{t("usersList.colStatus")}</span>
              <span />
            </EntityListHeader>

            {items.map((user, i) => (
              <UserDesktopRow
                key={user.id ?? i}
                user={user}
                isLast={i === items.length - 1}
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? 1}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <RegisterUserDialog
        open={registerOpen}
        onClose={() => setRegisterOpen(false)}
      />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Rows
// ───────────────────────────────────────────────────────────────────────

function UserMobileCard({ user }: { user: UserDto }) {
  const { t } = useTranslation("identity");
  const display = fullName(user, t);
  const href = user.id ? `/identity/users/${user.id}` : "#";
  return (
    <EntityMobileCard
      href={href}
      aria-label={t("usersList.openAria", { name: display })}
      dim={!user.isActive}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={display} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {display}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              {user.email ?? t("usersList.noEmail")}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-1.5">
        <EntityStatusBadge tone={user.isActive ? "success" : "default"}>
          {user.isActive ? t("badge.active") : t("badge.inactive")}
        </EntityStatusBadge>
        <EntityStatusBadge tone={user.emailConfirmed ? "info" : "warning"}>
          {user.emailConfirmed ? t("badge.emailConfirmed") : t("badge.emailPending")}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function UserDesktopRow({ user, isLast }: { user: UserDto; isLast: boolean }) {
  const { t } = useTranslation("identity");
  const display = fullName(user, t);
  const href = user.id ? `/identity/users/${user.id}` : "#";
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} dim={!user.isActive}>
      {/* Name + email */}
      <Link
        to={href}
        aria-disabled={!user.id}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={display} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {display}
          </span>
          <span
            className={cn(
              "block truncate text-[12px] text-[var(--color-muted-foreground)]",
              !user.email && "italic opacity-60",
            )}
          >
            {user.email ?? t("usersList.noEmailFile")}
          </span>
        </div>
      </Link>

      {/* Username */}
      <code
        title={user.userName ?? undefined}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {user.userName ? `@${user.userName}` : "—"}
      </code>

      {/* Status (lg+) */}
      <div className="hidden items-center gap-1.5 lg:flex">
        <EntityStatusBadge tone={user.isActive ? "success" : "default"}>
          {user.isActive ? t("badge.active") : t("badge.inactive")}
        </EntityStatusBadge>
        <EntityStatusBadge tone={user.emailConfirmed ? "info" : "warning"}>
          {user.emailConfirmed ? t("usersList.badgeConfirmed") : t("usersList.badgePending")}
        </EntityStatusBadge>
      </div>

      <div className="flex items-center justify-end">
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Register dialog — hooks/mutations preserved verbatim
// ───────────────────────────────────────────────────────────────────────

function RegisterUserDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const { t } = useTranslation("identity");
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

  const passwordMismatch =
    confirmPassword.length > 0 && password !== confirmPassword;

  const mutation = useMutation({
    mutationFn: (input: RegisterUserInput) => registerUser(input),
    onSuccess: () => {
      toast.success(t("usersList.toastRegistered"), {
        description: t("usersList.toastRegisteredDesc"),
      });
      void queryClient.invalidateQueries({ queryKey: ["identity", "users"] });
      onClose();
    },
    onError: (err) =>
      toast.error(t("usersList.toastRegisterFailed"), { description: describe(err) }),
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
            <DialogTitle>{t("usersList.registerTitle")}</DialogTitle>
            <DialogDescription>
              {t("usersList.registerDescription")}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="reg-first" label={t("field.firstName")} required>
                <Input
                  id="reg-first"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder={t("usersList.firstPlaceholder")}
                  autoFocus
                  required
                />
              </Field>
              <Field id="reg-last" label={t("field.lastName")} required>
                <Input
                  id="reg-last"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder={t("usersList.lastPlaceholder")}
                  required
                />
              </Field>
            </div>

            <Field
              id="reg-username"
              label={t("field.username")}
              required
              hint={t("usersList.usernameHint")}
            >
              <Input
                id="reg-username"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                placeholder={t("usersList.usernamePlaceholder")}
                autoComplete="off"
                required
              />
            </Field>

            <Field id="reg-email" label={t("field.email")} required>
              <Input
                id="reg-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t("usersList.emailPlaceholder")}
                required
              />
            </Field>

            <Field id="reg-phone" label={t("field.phone")} hint={t("usersList.phoneHint")}>
              <Input
                id="reg-phone"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                placeholder={t("usersList.phonePlaceholder")}
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="reg-pwd" label={t("field.password")} required>
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
                label={t("field.confirm")}
                required
                hint={passwordMismatch ? t("usersList.confirmMismatch") : undefined}
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
              <Button
                type="button"
                variant="outline"
                disabled={mutation.isPending}
              >
                {t("cancel")}
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || passwordMismatch}
              className="gap-1.5"
            >
              <UserPlus className="h-4 w-4" />
              {mutation.isPending ? t("usersList.registering") : t("usersList.registerSubmit")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}


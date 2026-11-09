import { Injectable, computed, signal } from '@angular/core';

const TOKEN_KEY = 'bud-tack.token';
const ROLE_KEY = 'bud-tack.role';

/** The roles of identity.yaml (ADR-007). */
export type Role = 'USER' | 'ADMIN';

/**
 * Holds the bearer token from `POST /sessions`, and the role it came with, in
 * memory and in `sessionStorage` (`12-ux-ui/screens/sign-in.md`). Sign-in
 * (HU-01) sets them; until then they stay empty and no request carries a token.
 * The role only decides what the portal offers: the API checks it on every call.
 */
@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  private readonly current = signal<string | null>(read(TOKEN_KEY));
  private readonly currentRole = signal<Role | null>(read(ROLE_KEY) as Role | null);

  readonly token = this.current.asReadonly();
  readonly role = this.currentRole.asReadonly();
  /** Who signed in, from the token's `sub`; only decides what the portal offers. */
  readonly userId = computed(() => subjectOf(this.current()));

  set(token: string, role: Role | null = null): void {
    this.current.set(token);
    this.currentRole.set(role);
    write(TOKEN_KEY, token);
    write(ROLE_KEY, role);
  }

  clear(): void {
    this.current.set(null);
    this.currentRole.set(null);
    write(TOKEN_KEY, null);
    write(ROLE_KEY, null);
  }
}

function subjectOf(token: string | null): string | null {
  try {
    const payload = token?.split('.')[1]?.replace(/-/g, '+').replace(/_/g, '/');
    return payload ? ((JSON.parse(atob(payload)) as { sub?: string }).sub ?? null) : null;
  } catch {
    return null;
  }
}

function read(key: string): string | null {
  try {
    return sessionStorage.getItem(key);
  } catch {
    return null;
  }
}

function write(key: string, value: string | null): void {
  try {
    if (value === null) {
      sessionStorage.removeItem(key);
    } else {
      sessionStorage.setItem(key, value);
    }
  } catch {
    // Storage blocked (private mode, policy): the in-memory values still work for this tab.
  }
}

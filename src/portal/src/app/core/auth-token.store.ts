import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'bud-tack.token';

/**
 * Holds the bearer token from `POST /sessions` in memory and in
 * `sessionStorage` (`12-ux-ui/screens/sign-in.md`). Sign-in (HU-01) sets it;
 * until then it stays empty and no request carries a token.
 */
@Injectable({ providedIn: 'root' })
export class AuthTokenStore {
  private readonly current = signal<string | null>(read());

  readonly token = this.current.asReadonly();

  set(token: string): void {
    this.current.set(token);
    write(token);
  }

  clear(): void {
    this.current.set(null);
    write(null);
  }
}

function read(): string | null {
  try {
    return sessionStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function write(token: string | null): void {
  try {
    if (token === null) {
      sessionStorage.removeItem(STORAGE_KEY);
    } else {
      sessionStorage.setItem(STORAGE_KEY, token);
    }
  } catch {
    // Storage blocked (private mode, policy): the in-memory token still works for this tab.
  }
}

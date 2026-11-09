import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { AuthTokenStore, Role } from './auth-token.store';

/** The schemas of bud-tack-docs 07-api/contracts/openapi/identity.yaml. */
export interface Credentials {
  readonly email: string;
  readonly password: string;
}

export interface User {
  readonly id: string;
  readonly name: string;
  readonly email: string;
  readonly role: Role;
  readonly status: 'PENDING' | 'ACTIVE' | 'INACTIVE';
}

export interface Session {
  readonly token: string;
  readonly tokenType: 'Bearer';
  readonly expiresIn: number;
  readonly role: Role;
}

/**
 * Sign-in and sign-out (HU-01). Sign-out is client-side only: the
 * token is discarded, there is no `DELETE /sessions` in V1.
 */
@Injectable({ providedIn: 'root' })
export class IdentityService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(AuthTokenStore);

  readonly signedIn = computed(() => this.tokens.token() !== null);
  readonly isAdmin = computed(() => this.tokens.role() === 'ADMIN');

  signIn(credentials: Credentials): Observable<Session> {
    return this.http
      .post<Session>('/api/v1/sessions', credentials)
      .pipe(tap((session) => this.tokens.set(session.token, session.role)));
  }

  signOut(): void {
    this.tokens.clear();
  }
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Role } from './auth-token.store';
import { Page } from './categories.service';
import { User } from './identity.service';

/** The users page size (07-api/rest-conventions.md, Pagination). */
export const USERS_PAGE_SIZE = 20;

export interface NewUser {
  readonly name: string;
  readonly email: string;
  readonly role: Role;
}

/** A single-use link's token, shown once (ADR-010). */
export interface AccountLink {
  readonly token: string;
  readonly expiresAt: string;
}

export interface CreatedUser {
  readonly user: User;
  readonly activation: AccountLink;
}

/** HU-09 and HU-10: the accounts and their single-use links. */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  list(page: number): Observable<Page<User>> {
    const params = new HttpParams().set('page', page).set('limit', USERS_PAGE_SIZE);
    return this.http.get<Page<User>>('/api/v1/users', { params });
  }

  create(user: NewUser): Observable<CreatedUser> {
    return this.http.post<CreatedUser>('/api/v1/users', user);
  }

  createActivationLink(userId: string): Observable<AccountLink> {
    return this.http.post<AccountLink>(`/api/v1/users/${userId}/activation-links`, null);
  }

  createPasswordReset(userId: string): Observable<AccountLink> {
    return this.http.post<AccountLink>(`/api/v1/users/${userId}/password-resets`, null);
  }

  /** Sets the password with a link's token. */
  confirmLink(token: string, password: string): Observable<void> {
    return this.http.post<void>('/api/v1/account-links/confirm', { token, password });
  }
}

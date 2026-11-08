import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Page } from './categories.service';
import { User } from './identity.service';

/** The users page size (07-api/rest-conventions.md, Pagination). */
export const USERS_PAGE_SIZE = 20;

/** HU-09: the accounts, for an administrator. */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  list(page: number): Observable<Page<User>> {
    const params = new HttpParams().set('page', page).set('limit', USERS_PAGE_SIZE);
    return this.http.get<Page<User>>('/api/v1/users', { params });
  }
}

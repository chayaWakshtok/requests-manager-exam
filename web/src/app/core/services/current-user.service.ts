import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'requests-manager.user';

/**
 * The exam has no login; the acting user name is sent in X-User-Name and stored in the audit (ChangedBy).
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  readonly name = signal(this.load());

  setName(value: string): void {
    const name = value.trim() || 'anonymous';
    this.name.set(name);
    try {
      localStorage.setItem(STORAGE_KEY, name);
    } catch {
      /* storage unavailable - keep the in-memory value */
    }
  }

  private load(): string {
    try {
      return localStorage.getItem(STORAGE_KEY) ?? 'user1';
    } catch {
      return 'user1';
    }
  }
}

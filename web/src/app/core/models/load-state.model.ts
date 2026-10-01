/** UI state of an async load: keeps the previous data while a new request is in flight. */
export interface LoadState<T> {
  loading: boolean;
  data: T | null;
  error: string | null;
}

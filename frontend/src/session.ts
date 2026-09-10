// The app's login gate (App.tsx checking /api/account/me) only runs at page load, so an
// auth cookie that expires while the tab stays open surfaces on authenticated API calls
// as a 401 — not as a backend outage. Pages branch on 401 and show this instead of the
// generic "Is the backend running?" message.
export const SESSION_EXPIRED = 'Your session has expired. Please reload the page and sign in again.';

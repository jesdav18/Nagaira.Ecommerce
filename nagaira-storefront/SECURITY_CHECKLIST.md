# Security Checklist

Legend:
- [OK] done/verified
- [ ] pending

## Backend/API
- [OK] HTTPS + HSTS enforced (curl -I shows strict-transport-security).
- [OK] Rate limiting on auth/login (429 after rapid attempts).
- [OK] Account lockout on failed logins (DB fields + logic).
- [OK] Refresh tokens + rotation (login/refresh/logout implemented).
- [OK] CORS restricted to allowed origins.
- [OK] Security headers enabled (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy).
- [ ] Verify JWT access token TTL and refresh TTL in config.
- [ ] Confirm refresh token rotation works in production (refresh -> new token, old invalid).
- [ ] Review input validation on sensitive endpoints (auth/admin/product).
- [ ] Ensure error responses do not leak stack traces or secrets.
- [ ] Verify logs capture auth failures and anomalies (rate limit, lockouts).

## Frontend
- [OK] CSP errors resolved (no inline handlers, fonts allowed).
- [OK] Tokens not stored in localStorage (memory + refresh flow).
- [OK] Production build without sourcemaps.
- [ ] Verify no secrets in frontend config (environment.ts).
- [ ] Audit external assets referenced by CSP (only needed domains).
- [ ] Confirm logout clears session and refresh cookie.

## Infra/VPS (Ubuntu)
- [ ] SSH hardening (keys only, disable root login).
- [ ] Firewall rules (UFW: allow 22/80/443 only).
- [ ] Fail2ban for SSH and API endpoints.
- [ ] Automatic security updates enabled.
- [ ] Backups configured (DB + uploads).
- [ ] Basic monitoring/alerting (disk, CPU, memory, logs).


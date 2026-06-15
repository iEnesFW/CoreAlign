# Subdomain Deployment Runbook

Operational guide for routing the tenant admin app, customer portal, and dealer (B2B) portal to dedicated subdomains per tenant.

## 1. DNS records

For each tenant, publish three records pointing at the reverse-proxy IP (or load balancer hostname):

```
app.{tenant}.example.com          A   <proxy-ip>     ; tenant admin SPA
customers.{tenant}.example.com    A   <proxy-ip>     ; customer portal SPA
b2b.{tenant}.example.com          A   <proxy-ip>     ; dealer (B2B) portal SPA
```

A wildcard CNAME also works and avoids per-tenant DNS work:

```
*.{tenant}.example.com  CNAME  edge.example.com.
```

Replace `{tenant}` with the tenant's subdomain slug and `example.com` with your production apex.

## 2. Reverse proxy / nginx

### 2.1 Dev: forward to Vite dev servers

The three SPAs run on different ports during development.

| App             | Port | Source                 |
| --------------- | ---- | ---------------------- |
| Tenant admin    | 5173 | `/` (root)             |
| Customer portal | 5274 | `apps/customer-portal` |
| Dealer (B2B)    | 5275 | `apps/b2b`             |

Sample nginx dev snippet:

```nginx
server {
  listen 80;
  server_name app.acme.localhost;
  location / { proxy_pass http://127.0.0.1:5173; proxy_set_header Host $host; }
  location /api/ { proxy_pass http://127.0.0.1:5000; proxy_set_header Host $host; }
}
server {
  listen 80;
  server_name customers.acme.localhost;
  location / { proxy_pass http://127.0.0.1:5274; proxy_set_header Host $host; }
  location /api/ { proxy_pass http://127.0.0.1:5000; proxy_set_header Host $host; }
}
server {
  listen 80;
  server_name b2b.acme.localhost;
  location / { proxy_pass http://127.0.0.1:5275; proxy_set_header Host $host; }
  location /api/ { proxy_pass http://127.0.0.1:5000; proxy_set_header Host $host; }
}
```

Add `*.acme.localhost 127.0.0.1` to your `hosts` file (or use dnsmasq).

### 2.2 Prod: serve built `dist/` from a single ASP.NET backend on :5000

Production builds each SPA to its own `dist/` (`npm --workspace apps/customer-portal run build`, etc.). nginx fronts the API and serves static assets per subdomain:

```nginx
server {
  listen 443 ssl http2;
  server_name app.acme.example.com;
  ssl_certificate     /etc/letsencrypt/live/acme.example.com/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/acme.example.com/privkey.pem;

  root /var/www/corealign/admin;
  index index.html;

  location /api/ {
    proxy_pass http://127.0.0.1:5000;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
  }
  location / { try_files $uri /index.html; }
}

server {
  listen 443 ssl http2;
  server_name customers.acme.example.com;
  ssl_certificate     /etc/letsencrypt/live/acme.example.com/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/acme.example.com/privkey.pem;
  root /var/www/corealign/customer-portal;
  index index.html;
  location /api/ { proxy_pass http://127.0.0.1:5000; proxy_set_header Host $host; proxy_set_header X-Forwarded-Proto $scheme; }
  location / { try_files $uri /index.html; }
}

server {
  listen 443 ssl http2;
  server_name b2b.acme.example.com;
  ssl_certificate     /etc/letsencrypt/live/acme.example.com/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/acme.example.com/privkey.pem;
  root /var/www/corealign/b2b;
  index index.html;
  location /api/ { proxy_pass http://127.0.0.1:5000; proxy_set_header Host $host; proxy_set_header X-Forwarded-Proto $scheme; }
  location / { try_files $uri /index.html; }
}
```

Deploy `apps/*/dist/` to the corresponding `/var/www/corealign/*` directory. Tenant resolution happens server-side from the `Host` header (or `X-Tenant` header set by the proxy if you prefer header-based routing).

## 3. ASP.NET CORS

Add the three subdomain origins for every tenant to the API CORS allow-list. For wildcards, configure `SetIsOriginAllowed` rather than `AllowAnyOrigin` so credentials still work:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(origin =>
        {
            var host = new Uri(origin).Host;
            return host.EndsWith(".example.com", StringComparison.OrdinalIgnoreCase)
                && (host.StartsWith("app.") || host.StartsWith("customers.") || host.StartsWith("b2b."));
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
```

For a hard-coded list (single tenant `acme`):

```
https://app.acme.example.com
https://customers.acme.example.com
https://b2b.acme.example.com
```

## 4. Cookie domain considerations

Today the SPAs send the JWT in the `Authorization: Bearer …` header, so there is **no cookie domain coupling** — each subdomain stores its own token in `localStorage`. No additional config required.

If you migrate to cookie auth later:

- Set `Cookie.Domain = ".{tenant}.example.com"` so the cookie is shared between `app.`, `customers.`, and `b2b.` (subdomain SSO).
- Set `SameSite=Lax` (or `None` + `Secure` for true cross-site).
- Be aware that sharing one cookie across subdomains means a customer-portal XSS could exfiltrate a tenant-admin session — keep the personas isolated by writing **three distinct cookies** with different paths/names rather than one shared cookie unless SSO is an explicit requirement.

## 5. TLS / Let's Encrypt

A wildcard certificate covers every persona for a tenant with a single cert. Use DNS-01 (HTTP-01 cannot issue wildcards):

```bash
certbot certonly \
  --dns-cloudflare \
  --dns-cloudflare-credentials /etc/letsencrypt/cloudflare.ini \
  -d "*.acme.example.com" -d "acme.example.com" \
  --agree-tos -m ops@example.com --non-interactive
```

Cron the renewal:

```
0 3 * * * /usr/bin/certbot renew --quiet --deploy-hook "systemctl reload nginx"
```

## 6. Tenant onboarding checklist

1. Create the tenant record in `Tenants` and grab the assigned slug.
2. Seed the initial admin user (assign role `TenantAdmin`) and send the invite email.
3. Publish DNS:
   - `app.{slug}.example.com`
   - `customers.{slug}.example.com`
   - `b2b.{slug}.example.com`
4. Confirm wildcard TLS already covers `{slug}.example.com` (renew if needed).
5. Reload nginx if a non-wildcard server block was added.
6. Verify health endpoint per subdomain:
   - `curl -sf https://app.{slug}.example.com/api/v1/health`
   - `curl -sf https://customers.{slug}.example.com/api/v1/health`
   - `curl -sf https://b2b.{slug}.example.com/api/v1/health`
7. Smoke-test login on each portal with the seeded admin / a test customer / a test dealer user.

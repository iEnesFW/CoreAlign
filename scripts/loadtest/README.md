# CoreAlign — k6 Load Tests

This directory contains six [k6](https://k6.io) scripts that exercise the
critical hot paths of the CoreAlign API. They are designed to run against a
locally seeded environment (`seed-demo.sql` + `seed-transactions.sql`) or any
staging deployment that has at least one demo tenant, customer and product.

## Prerequisites

1. Install k6 — https://grafana.com/docs/k6/latest/set-up/install-k6/.
2. Start the API locally with the demo seed loaded:
   ```sh
   dotnet run --project server/src/CoreAlign.API
   ```
3. (Optional) export environment variables that override the script defaults:
   | Variable | Default | Purpose |
   | ------------- | -------------------------------------- | ------------------------------------ |
   | `BASE_URL` | `http://localhost:5099` | API root URL |
   | `USERNAME` | `demo@corealign.local` | Login email |
   | `PASSWORD` | `Demo1234!` | Login password |

## Common stage / threshold profile

All scripts share the same VU profile (see `common.js`):

```
ramp-up   0  -> 50 VUs over 30s
hold      50 VUs for 5m
ramp-down 50 -> 0  VUs over 30s
```

| Profile | p95 latency | Failure rate |
| ------- | ----------- | ------------ |
| Read    | < 300 ms    | < 1%         |
| Write   | < 800 ms    | < 1%         |
| Report  | < 1500 ms   | < 1%         |

A failure is recorded whenever any check fails, captured by the `app_errors`
custom rate metric in addition to k6's built-in `http_req_failed`.

## Scenarios

| Script               | Hot path                                            | Profile |
| -------------------- | --------------------------------------------------- | ------- |
| `login.js`           | `POST /api/v1/auth/login`                           | Write   |
| `dashboard.js`       | `/api/v1/dashboard/stats` + report widgets          | Read    |
| `product-list.js`    | Paged product search                                | Read    |
| `order-create.js`    | `POST /api/v1/orders` with a single line            | Write   |
| `invoice-list.js`    | Paged invoice search                                | Read    |
| `report-download.js` | `GET /api/v1/reports/inventory-stock-on-hand` (PDF) | Report  |

## Running a single scenario

```sh
k6 run scripts/loadtest/login.js
k6 run -e BASE_URL=https://staging.corealign.test scripts/loadtest/product-list.js
```

## Running the full suite

```sh
for s in login dashboard product-list order-create invoice-list report-download; do
  echo "==> $s"
  k6 run "scripts/loadtest/$s.js"
done
```

## CI integration notes

- The scripts assume the seed has at least one customer and one product. The
  `order-create.js` and report scripts perform a discovery call during `setup`
  and abort with a clear error if no seed data is reachable.
- Each script returns a non-zero exit code when any threshold is breached, so
  they can be chained in CI without additional plumbing.
- For nightly runs against staging, prefer running each script in isolation
  (`k6 run --out json=results-<script>.json`) so the per-endpoint p95 metric is
  attributed cleanly.

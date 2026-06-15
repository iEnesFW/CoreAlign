import http from 'k6/http';
import { check } from 'k6';
import { Trend, Rate } from 'k6/metrics';

export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5099';
export const USERNAME = __ENV.USERNAME || 'demo@corealign.local';
export const PASSWORD = __ENV.PASSWORD || 'Demo1234!';
export const TENANT_SLUG = __ENV.TENANT_SLUG || 'demo';

export const readTrend = new Trend('http_req_read', true);
export const writeTrend = new Trend('http_req_write', true);
export const errorRate = new Rate('app_errors');

export function readThresholds() {
  return {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<300'],
    app_errors: ['rate<0.01'],
  };
}

export function writeThresholds() {
  return {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<800'],
    app_errors: ['rate<0.01'],
  };
}

export function standardStages() {
  return [
    { duration: '30s', target: 50 },
    { duration: '5m', target: 50 },
    { duration: '30s', target: 0 },
  ];
}

export function login() {
  const body = JSON.stringify({
    email: USERNAME,
    password: PASSWORD,
  });
  const params = { headers: { 'Content-Type': 'application/json' } };
  const res = http.post(`${BASE_URL}/api/v1/auth/login`, body, params);
  const ok = check(res, {
    'login 200': (r) => r.status === 200,
    'login has token': (r) =>
      r.json('data.accessToken') !== undefined && r.json('data.accessToken') !== '',
  });
  if (!ok) {
    errorRate.add(1);
    return null;
  }
  return res.json('data.accessToken');
}

export function authHeaders(token) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };
}

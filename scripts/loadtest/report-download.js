import http from 'k6/http';
import { check, sleep } from 'k6';
import {
  BASE_URL,
  errorRate,
  login,
  authHeaders,
  writeThresholds,
  standardStages,
} from './common.js';

export const options = {
  stages: standardStages(),
  thresholds: {
    ...writeThresholds(),
    http_req_duration: ['p(95)<1500'],
  },
};

export function setup() {
  const token = login();
  if (!token) {
    throw new Error('Setup login failed — cannot run report-download scenario');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const reportPath = `/api/v1/reports/inventory-stock-on-hand?format=pdf`;
  const res = http.get(`${BASE_URL}${reportPath}`, headers);
  const ok = check(res, {
    'status 200': (r) => r.status === 200,
    'pdf content type': (r) => (r.headers['Content-Type'] || '').toLowerCase().includes('pdf'),
  });
  if (!ok) errorRate.add(1);
  sleep(2);
}

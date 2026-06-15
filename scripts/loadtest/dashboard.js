import http from 'k6/http';
import { check, sleep } from 'k6';
import {
  BASE_URL,
  errorRate,
  login,
  authHeaders,
  readThresholds,
  standardStages,
} from './common.js';

export const options = {
  stages: standardStages(),
  thresholds: readThresholds(),
};

export function setup() {
  const token = login();
  if (!token) {
    throw new Error('Setup login failed — cannot run dashboard scenario');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const endpoints = [
    `${BASE_URL}/api/v1/dashboard/stats`,
    `${BASE_URL}/api/v1/reports/top-customers?limit=10`,
    `${BASE_URL}/api/v1/reports/top-products?limit=10`,
  ];
  for (const url of endpoints) {
    const res = http.get(url, headers);
    const ok = check(res, {
      'status 2xx': (r) => r.status >= 200 && r.status < 300,
    });
    if (!ok) errorRate.add(1);
  }
  sleep(1);
}

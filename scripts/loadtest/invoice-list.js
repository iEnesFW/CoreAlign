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
    throw new Error('Setup login failed — cannot run invoice-list scenario');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const page = Math.floor(Math.random() * 3) + 1;
  const res = http.get(`${BASE_URL}/api/v1/invoices?page=${page}&pageSize=25`, headers);
  const ok = check(res, {
    'status 200': (r) => r.status === 200,
    'paged response': (r) => typeof r.json('data.total') === 'number',
  });
  if (!ok) errorRate.add(1);
  sleep(0.5);
}

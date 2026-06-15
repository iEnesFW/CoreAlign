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
    throw new Error('Setup login failed — cannot run product-list scenario');
  }
  return { token };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const page = Math.floor(Math.random() * 5) + 1;
  const res = http.get(`${BASE_URL}/api/v1/products?page=${page}&pageSize=25`, headers);
  const ok = check(res, {
    'status 200': (r) => r.status === 200,
    'has items': (r) => Array.isArray(r.json('data.items')),
  });
  if (!ok) errorRate.add(1);
  sleep(0.5);
}

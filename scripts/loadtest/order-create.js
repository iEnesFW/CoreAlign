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
  thresholds: writeThresholds(),
};

export function setup() {
  const token = login();
  if (!token) {
    throw new Error('Setup login failed — cannot run order-create scenario');
  }
  const customers = http.get(`${BASE_URL}/api/v1/customers?page=1&pageSize=10`, authHeaders(token));
  const products = http.get(`${BASE_URL}/api/v1/products?page=1&pageSize=10`, authHeaders(token));
  const customerId = customers.json('data.items.0.id');
  const productId = products.json('data.items.0.id');
  if (!customerId || !productId) {
    throw new Error('Setup failed: no seed customer or product available');
  }
  return { token, customerId, productId };
}

export default function (data) {
  const headers = authHeaders(data.token);
  const orderNumber = `LOAD-${Date.now()}-${Math.floor(Math.random() * 1e6)}`;
  const body = JSON.stringify({
    orderNumber,
    customerId: data.customerId,
    orderDate: new Date().toISOString(),
    currency: 'TRY',
    notes: 'k6 load test',
    lines: [{ productId: data.productId, quantity: 1, unitPrice: 100 }],
  });
  const res = http.post(`${BASE_URL}/api/v1/orders`, body, headers);
  const ok = check(res, {
    'status 200/201': (r) => r.status === 200 || r.status === 201,
    'order id returned': (r) => r.json('data.id') !== undefined,
  });
  if (!ok) errorRate.add(1);
  sleep(1);
}

import http from 'k6/http';
import { check, sleep } from 'k6';
import {
  BASE_URL,
  USERNAME,
  PASSWORD,
  errorRate,
  writeThresholds,
  standardStages,
} from './common.js';

export const options = {
  stages: standardStages(),
  thresholds: writeThresholds(),
};

export default function () {
  const body = JSON.stringify({
    email: USERNAME,
    password: PASSWORD,
  });
  const res = http.post(`${BASE_URL}/api/v1/auth/login`, body, {
    headers: { 'Content-Type': 'application/json' },
  });
  const ok = check(res, {
    'status 200': (r) => r.status === 200,
    'token returned': (r) =>
      r.json('data.accessToken') !== undefined && r.json('data.accessToken') !== '',
  });
  if (!ok) {
    errorRate.add(1);
  }
  sleep(1);
}

import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

export const errorRate = new Rate('errors');

export default function () {
    const url = 'https://localhost:7173/todo/1';
    check(http.get(url), {
        'status is 200': (r) => r.status === 200,
    }) || errorRate.add(1);
}

//k6 run --vus 2 --duration 60s --rps 2 ./loadtest.js
import http from 'k6/http';
import { sleep } from 'k6';

export let options = {
    vus: 10,
    duration: '30s'
};


export default function () {
    const payload = JSON.stringify({ model: "llama3.1", prompt: "Décrit Microsoft en une phrase" });
    const params = { headers: { 'Content-Type': 'application/json' } };
    http.post('http://host.docker.internal:11434/api/generate', payload, params);
    sleep(1);
}

// docker run --rm -i grafana/k6 run - < test.js

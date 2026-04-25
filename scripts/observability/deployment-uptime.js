import http from 'k6/http';
import { sleep } from 'k6';

const targetUrl = __ENV.TARGET_URL;
const deploymentId = __ENV.DEPLOYMENT_ID || 'unknown-deployment';
const environment = __ENV.ENVIRONMENT || 'unknown';
const service = __ENV.SERVICE_NAME || 'resumeapp-api';
const workflow = __ENV.WORKFLOW_NAME || 'unknown-workflow';
const runId = __ENV.RUN_ID || 'unknown-run';
const probeIntervalMs = Number.parseInt(__ENV.PROBE_INTERVAL_MS || '100', 10);

if (!targetUrl) {
  throw new Error('TARGET_URL is required.');
}

export const options = {
  vus: 1,
  duration: __ENV.MAX_DURATION || '10m',
  noConnectionReuse: false,
  noVUConnectionReuse: false,
};

function emitProbeEvent(response, errorMessage = null) {
  const durationMs = response?.timings?.duration ?? 0;
  const statusCode = response?.status ?? 0;
  const success = statusCode === 200;

  const event = {
    event_type: 'probe',
    timestamp: new Date().toISOString(),
    deployment_id: deploymentId,
    environment,
    service,
    workflow,
    run_id: runId,
    target_url: targetUrl,
    status_code: statusCode,
    success,
    success_value: success ? 1 : 0,
    duration_ms: Number(durationMs.toFixed(3)),
  };

  if (errorMessage) {
    event.error = errorMessage;
  }

  console.log(JSON.stringify(event));
}

export default function () {
  try {
    const response = http.get(targetUrl, {
      redirects: 10,
      timeout: '5s',
      tags: {
        source: 'deployment-k6',
        service,
        environment,
      },
    });

    emitProbeEvent(response, response.error || null);
  } catch (error) {
    emitProbeEvent(null, String(error));
  }

  sleep(probeIntervalMs / 1000);
}

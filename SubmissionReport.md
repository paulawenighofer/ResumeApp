# ResumeApp Submission Report

This report explains how `ResumeApp` meets the rubric requirements for telemetry, operations, and PR environments based on the implementation in this repository.

## Telemetry

`ResumeApp` uses an OpenTelemetry-compatible monitoring setup. In the backend, OpenTelemetry is configured in [`API/Program.cs`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/API/Program.cs) to collect logs, metrics, and traces. These are sent through an OpenTelemetry Collector and then forwarded to Prometheus for metrics, Loki for logs, and Grafana for dashboards. The related infrastructure is defined in the Kubernetes manifests and configuration files such as [`kubernetes/16-otel-collector-configmap.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/kubernetes/16-otel-collector-configmap.yml), [`configs/otel-config.yaml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/configs/otel-config.yaml), and [`configs/grafana-dashboard.json`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/configs/grafana-dashboard.json).

The dashboard shows application health through `/health/live`, `/health/startup`, and `/health/ready`, which are also used by Kubernetes liveness, startup, and readiness probes. This helps track whether the app is healthy and whether a deployment causes temporary unavailability. The deployment also uses two replicas and rolling updates, so health and downtime during deployment can be monitored.

For performance and usage, the app exposes both generic and custom metrics in [`API/Services/ApiMetrics.cs`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/API/Services/ApiMetrics.cs). Generic metrics include total HTTP requests and active users. Custom app metrics include sign-ups, profile CRUD activity, resume draft generation, PDF generation, uploads, social logins, and email sends. Active user tracking is implemented in [`API/Services/UserActivityTracker.cs`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/API/Services/UserActivityTracker.cs). Route-level request tracking is also recorded in [`API/Middleware/RequestLoggingMiddleware.cs`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/API/Middleware/RequestLoggingMiddleware.cs), which supports identifying popular endpoints/pages.

Error tracking and near real-time logging are provided through structured application logging and Loki/Grafana log panels. The dashboard contains separate panels for info, warning, and error logs, and it refreshes frequently, giving developers near real-time visibility into failures and user activity.

## Operations

The project uses GitHub Actions for automation. The main workflows are:

- [`ci-cd.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/ci-cd.yml)
- [`production-pipeline.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/production-pipeline.yml)
- [`db-backup.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/db-backup.yml)
- [`db-restore.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/db-restore.yml)
- [`pr-environment.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/pr-environment.yml)

The CI pipeline performs automated building, linting, testing, and deployment. Linting is enforced with `dotnet format --verify-no-changes`, which ensures consistent code style. Automated testing is included with `dotnet test`, and the repository contains both unit and integration tests in [`Test/`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/Test) as well as MAUI tests in [`Maui Tests/`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/Maui%20Tests).

Automated deployment is implemented for both staging and production. Pushes to `main` deploy to Kubernetes, while pushes to `production` deploy to Azure Container Apps. In Kubernetes, zero-downtime deployment is achieved using a rolling update strategy with `maxUnavailable: 0`, `maxSurge: 1`, two API replicas, and health probes defined in [`kubernetes/4-api-deployment.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/kubernetes/4-api-deployment.yml). This means users should not notice application updates during normal deployments.

The application uses a reverse proxy through NGINX ingress in Kubernetes, defined in [`kubernetes/6-api-ingress.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/kubernetes/6-api-ingress.yml), and also includes an `nginx.conf` for local/container routing. SSL and DNS are configured using DuckDNS hostnames and Let’s Encrypt certificates, with certificate setup defined in [`kubernetes/cert-manager-clusterissuer.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/kubernetes/cert-manager-clusterissuer.yml).

Alerts are sent to developers when builds or deployments fail using `ntfy.sh` notifications inside the workflows. The project also includes automated PostgreSQL backups through [`db-backup.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/db-backup.yml), which runs on a schedule, creates a `pg_dump`, verifies the backup, and rotates old files. Automated restore is implemented in [`db-restore.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/db-restore.yml), which restores the latest backup, verifies success, and safely scales the app down and back up during the process.

Feature flags are implemented using `Microsoft.FeatureManagement`. The current example is `EmailOtpDelivery`, configured in [`API/Program.cs`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/API/Program.cs). When enabled, the app sends OTP emails through SMTP; when disabled, it falls back to a logging-based email service. This allows controlled rollout of features without changing application code.

One rubric item is only partially satisfied: the repository currently does not show an explicit project-wide `TreatWarningsAsErrors` setting or `dotnet build -warnaserror` step, so warnings-as-errors is not as strongly enforced as the other CI requirements.

## PR Environments

PR environments are automated in [`pr-environment.yml`](/c:/Users/paula/Desktop/USA/App/finalProject/ResumeApp/.github/workflows/pr-environment.yml). When a pull request is opened, synchronized, or reopened, the workflow runs linting and tests, so the PR receives status checks automatically through GitHub Actions.

The workflow then creates a dedicated Kubernetes namespace for the pull request using the pattern `resumeapp-pr-<PR_NUMBER>`, builds and pushes a PR-specific Docker image, and deploys the PR version of the app using the manifests in `kubernetes/pr-template/`. Each PR also gets a unique DuckDNS subdomain such as `pr-<PR_NUMBER>.resume-app-pb.duckdns.org`, along with separate Grafana and Prometheus URLs.

After deployment, the workflow posts or updates a comment on the pull request containing the environment URL, observability links, namespace, and commit SHA. When the PR is closed, the cleanup job deletes the Kubernetes namespace, waits for termination, removes the PR Docker image tag, and comments that the environment has been torn down. This ensures PR resources are created automatically and cleaned up automatically.

## Submission Evidence

For submission, the best supporting screenshots are:

- Grafana dashboard overview
- Grafana log panels
- OpenTelemetry configuration in `Program.cs`
- custom metrics in `ApiMetrics.cs`
- Kubernetes deployment and ingress files
- GitHub Actions workflow files
- backup and restore workflows
- PR comment showing deployed environment URL
- running application/site screenshots

## Summary

Overall, `ResumeApp` meets the rubric well with a real OpenTelemetry observability stack, automated CI/CD, Kubernetes zero-downtime deployment, reverse proxy and TLS setup, automated backups and restore, feature flags, and fully automated PR environments. The strongest parts are the deployment pipeline, observability infrastructure, and PR environment automation. The main remaining gap is explicit warnings-as-errors enforcement in the build configuration.

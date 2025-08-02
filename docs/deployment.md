# Deployment

This repository provides Helm charts for deploying the Sigma services to a Kubernetes cluster. Charts are located under `deploy/helm/` for the following components:

- `.NET` service (`deploy/helm/dotnet`)
- Python backend (`deploy/helm/backend`)
- Next.js frontend (`deploy/helm/frontend`)

## Prerequisites

- Docker images for each service are available in a registry and referenced in the chart values.
- A Kubernetes cluster and `helm` installed locally.

## Configuration

Each chart exposes configuration in its `values.yaml` file.

### Secrets

API keys and other sensitive values are defined in the `secrets` section. Update these before deploying. For example, the backend expects keys for providers such as OTX, Shodan, Censys, VirusTotal, and OpenAI.

### Storage

Both the .NET service and backend use SQLite databases and require persistent storage. The `storage` section configures a PersistentVolumeClaim. Adjust the size or disable storage as needed. The frontend chart includes a storage section for completeness but is disabled by default.

## Deploying

Install a chart by specifying a release name and the chart directory. For example, to deploy the backend:

```bash
helm install sigma-backend deploy/helm/backend \
  --set image.repository=myrepo/backend \
  --set secrets.OPENAI_API_KEY=your-openai-key
```

Repeat for the `.NET` and frontend charts, overriding images and secrets as appropriate. Use `helm upgrade` to apply changes and `helm uninstall` to remove a release.

## Accessing the Services

Each chart exposes a Kubernetes `Service` on the port defined in `values.yaml`. Configure an ingress or port-forward to interact with the applications.

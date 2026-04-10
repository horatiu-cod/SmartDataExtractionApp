# SmartDataExtractionApp

Project: SmartDataExtraction

CI badges

- Unit & default CI workflow: ![CI](https://github.com/horatiu-cod/SmartDataExtractionApp/actions/workflows/ci.yml/badge.svg)
- Manual integration workflow: ![Integration](https://github.com/horatiu-cod/SmartDataExtractionApp/actions/workflows/integration-dispatch.yml/badge.svg)

See `docs/INTEGRATION_TESTS.md` for details on running integration tests in CI and locally.

## Configure CI Variables and Secrets

1. Actions Variables (toggle integration tests)

   - Go to your repository on GitHub.
   - Navigate to `Settings` -> `Actions` -> `Variables`.
   - Add a variable named `RUN_INTEGRATION` with value `true` to enable integration tests in the CI workflow.

   Screenshot example:

   ![Actions Variables placeholder](docs/images/actions-variables.png)

2. Actions Secrets (external licenses)

   - Go to `Settings` -> `Secrets and variables` -> `Actions` -> `Secrets`.
   - Add a new secret named `SYNCFUSION_LICENSE` and paste your Syncfusion license key as the value.

   Screenshot example:

   ![Actions Secrets placeholder](docs/images/actions-secrets.png)

Notes

- The CI workflow will fail early if `RUN_INTEGRATION` is enabled but the `SYNCFUSION_LICENSE` secret is not set. Integration tests receive the license via the environment variable `SYNCFUSION_LICENSE`.
- For ad-hoc runs, use the `Integration tests (manual)` workflow in the Actions UI (Run workflow) and optionally provide the test filter and timeout.
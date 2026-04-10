# Integration tests in CI

This project separates unit tests and integration tests. Integration tests are marked with the xUnit trait `Category=IntegrationTest`.

CI behavior

- The main CI workflow (`.github/workflows/ci.yml`) always runs unit tests (tests not marked as `IntegrationTest`).
- Integration tests run only when the repository-level variable `RUN_INTEGRATION` is set to the string `true`.

How to enable integration tests for CI runs

1. Go to your repository on GitHub.
2. Navigate to `Settings` -> `Actions` -> `Variables` (not Secrets).
3. Add a new variable named `RUN_INTEGRATION` with value `true`.

4. Add the required external license as a secret:

   - Go to `Settings` -> `Secrets and variables` -> `Actions` -> `Secrets`.
   - Add a new secret named `SYNCFUSION_LICENSE` and paste your Syncfusion license key as the value.

   The CI workflow will check for the presence of this secret and fail early if it is missing when integration tests are enabled.

Notes

- Using repository-level Actions variables (not secrets) makes it easy to toggle integration tests without exposing secrets.
- If you prefer to run integration tests on demand, use the dedicated manual workflow `Integration tests (manual)` in `.github/workflows/integration-dispatch.yml` (Run workflow button in the Actions UI).

Running integration tests locally

You can run only integration tests locally with:

```
dotnet test --filter "Category=IntegrationTest"
```

Or run only unit tests (exclude integration):

```
dotnet test --filter "Category!=IntegrationTest"
```

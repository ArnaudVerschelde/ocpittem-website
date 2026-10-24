# .NET 8 → .NET 10 upgrade: production runbook (func-ocpittem-2026)

Status: **prepared, not executed.** Every step that touches Azure, Stripe, GitHub or `main` needs explicit
human approval. Nothing in this runbook has been run against production.

Target: Azure Functions v4, .NET 10 isolated, Linux Flex Consumption, West Europe.
Why now: Functions support for .NET 8 ends on **10 November 2026**.

## 1. What changed in the code

| Area | Before | After |
|---|---|---|
| Target framework (app + tests) | net8.0 | net10.0 |
| Microsoft.Azure.Functions.Worker | 1.23.0 | 2.52.0 (net10 needs ≥ 2.50.0) |
| Microsoft.Azure.Functions.Worker.Sdk | 1.18.1 | 2.0.7 |
| Worker.Extensions.Http.AspNetCore | 1.3.2 | 2.1.0 |
| Worker.Extensions.Http | 3.2.0 | 3.3.0 (required by Http.AspNetCore 2.1.0, NU1605) |
| Worker.Extensions.Timer | 4.3.1 | unchanged |
| `infrastructure/main.bicep` runtime | `dotnet-isolated` 8.0 | `dotnet-isolated` 10.0 |
| `deploy-backend.yml` | auto-deploy on push to `main`, .NET 8, no tests | **manual only**, .NET 10 SDK, build + test gate before publish |

`Program.cs`, all Functions, routes, DTOs and business logic are unchanged.
SDK choice: `Microsoft.Azure.Functions.Worker.Sdk` 2.0.7 was kept instead of `Azure.Functions.Sdk` 1.x
(Microsoft's newer recommendation) to avoid also changing the project SDK, and because
`Azure.Functions.Sdk` does not run its extension restore on solution-level restores (AZFW0108).
Moving to it is a possible follow-up.

## 2. Local verification results (2026-10-24)

- `dotnet restore` / `build -c Release` / `publish -c Release`: succeeded, 0 errors.
- `dotnet test`: **320 passed, 0 failed** (317 existing + 3 new). Re-run after the dependency fix below.
- `functions.metadata` lists **20 functions** (18 HTTP + 2 timer), the same as the `[Function]` attributes.
- Local host (`func host start`, .NET 10, sanitized settings, timers disabled, no Azurite):
  all 18 HTTP functions registered, `GET /api/health` → 200 `{"status":"ok"}`,
  `GET /api/cookie-sale/config` → 200 camelCase JSON.
- Dependency vulnerability: `NU1903` (CVE-2024-43483 / CVE-2024-43484, denial of service) for `System.IO.Packaging 6.0.0`,
  pulled in transitively by ClosedXML 0.102.3 (directly and via DocumentFormat.OpenXml 2.16.0). **Fixed** by pinning
  `System.IO.Packaging` 6.0.1 (first patched version, same major, patch release) in `OCPittem.Functions.csproj`.
  ClosedXML stays at 0.102.3 on purpose: 0.104+ moves to OpenXml 3.x, a larger change to the Excel reports.
  `dotnet list package --vulnerable --include-transitive` now reports no vulnerable packages; the warning is not suppressed.
- Remaining build warnings: only environmental `MSB3231` (cannot delete old output folders, access denied; local OneDrive/file locks).

**Not verified locally:** anything needing storage (no Azurite installed), real Stripe checkout/webhook,
real Mailjet, timer execution, Flex Consumption behaviour with the 10.0 runtime. These need step 3.

## 3. Pre-deployment validation in a non-production app (required before GO)

Create a separate Flex Consumption app (e.g. `func-ocpittem-test`) with:
- its **own storage account** (never the production one) and its own Key Vault or plain test values;
- Stripe **test-mode** keys, price IDs, and a test-mode webhook endpoint pointing at the test app;
- `Email__Enabled=false`, empty `App__ReportRecipients` and `App__CookieReportRecipients`;
- `AzureWebJobs.CookieSaleDailyReport.Disabled=true` and `AzureWebJobs.DailyReport.Disabled=true`.

Then run the **same workflow** against it (`app_name=func-ocpittem-test`, its resource group, `runtime_version=10.0`; the service principal needs access to that app),
which rehearses the exact cut-over steps. Then run: health → cookie-sale/config → create-checkout → pay with a Stripe test card →
webhook (check signature + status Paid + idempotent on resend) → order-status.
Known community reports of `dotnet exited with code 150` on Flex with .NET 10 (azure-functions-dotnet-worker#3351):
this test deployment is where such a problem would show up, so it is a hard gate.

## 4. Platform facts that shape the plan

- Flex Consumption has **no deployment slots**. There is no staging swap.
- Each deployment overwrites the package; the platform keeps **no revision history**.
- A runtime-stack change and a code deploy are two separate site updates; code and runtime must match,
  so there is a mismatch window. **Do not assume zero downtime.**
- `siteUpdateStrategy: RollingUpdate` is documented as GA (needs Azure CLI ≥ 2.87.0, API 2023-12-01+).
  The docs say changing the strategy is a no-op in GA regions, but I could not confirm that West Europe is
  one of them. It lets in-flight executions finish, but it does not remove the runtime/code mismatch window.
  Treat it as optional; if used, set it ahead of time and check `az functionapp show` for the stored value.
- Stripe retries failed live-mode webhook deliveries with backoff for up to ~3 days, and the app's
  `WebhookEvents` table makes processing idempotent, so webhooks that fail during the window are recoverable.

## 5. Backup checklist (do all of this before the change; store outside the repo)

1. Record the production Git SHA currently deployed (last successful run of "Deploy Backend" in GitHub Actions;
   at the time of writing `main` = `a7ec102`). Tag it: `git tag pre-net10-a7ec102 a7ec102` (push only with approval).
2. Download the current deployment package from the `func-deployments` blob container of `stocpittem2026`
   (`az storage blob list` / `download`). It is overwritten by the next deploy.
3. Runtime config: `az functionapp runtime config show -n func-ocpittem-2026 -g rg-ocpittem`
   and `az functionapp show -n func-ocpittem-2026 -g rg-ocpittem` (note `functionAppConfig.runtime`,
   `scaleAndConcurrency`, `siteUpdateStrategy`).
4. App settings: `az functionapp config appsettings list ...` → save to a protected file.
   **Contains the plaintext storage connection string (`AzureWebJobsStorage`); treat as a secret and delete afterwards.**
5. Storage data (read-only export): tables `CookieOrders`, `WebhookEvents`, `Orders`, `Tickets`, `SponsorRequests`
   (e.g. Storage Explorer export or `az storage entity query`). Note the latest `CookieOrders` row count and the number
   of `Pending` orders for comparison afterwards.
6. Stripe Dashboard: note the webhook endpoint ID, its signing-secret setup and the current delivery history (read only).
7. **Azure CLI requirement.** The local machine has Azure CLI 2.42.0, which does not know `az functionapp runtime config`
   (and `update-strategy` needs ≥ 2.87.0). Upgrade it (`winget upgrade Microsoft.AzureCLI`, then `az version`) before the
   backup step and before any manual rollback command. The GitHub-hosted runner already ships a recent CLI; the workflow
   prints its version in the "Show tool versions" step.
   Also confirm the service principal in the `AZURE_CREDENTIALS` secret may update site config
   (e.g. Contributor on the Function App), because the workflow now calls `az functionapp runtime config set`.

## 6. Deployment sequence

Auto-deploy is off: merging to `main` **does not** deploy (the merged workflow has only `workflow_dispatch`).

1. Non-production validation (section 3) passed; PR reviewed; backup checklist complete. Pick a low-traffic (evening) window.
2. Merge the PR into `main` (requires your approval). Confirm in the Actions tab that **no** run started.
3. GitHub → Actions → Deploy Backend → **Run workflow**, ref `main`, with:
   `app_name=func-ocpittem-2026`, `resource_group=rg-ocpittem`, **`runtime_version=10.0`**.
4. The workflow does the coordination itself, in this order:
   1. restore, build and **all tests**: if anything fails here the run stops and **nothing in Azure has changed**;
   2. Azure login, then prints the current runtime config (record it);
   3. `az functionapp runtime config set --runtime-version 10.0`, then waits 30 s (Microsoft's guidance);
   4. `func azure functionapp publish` (the .NET 10 package);
   5. health check against `/api/health` (retries up to ~2 minutes) and prints the final runtime config.
   Expected impact: planned downtime from step 3 until the publish completes, roughly 1–3 minutes of failed requests.
   Do not run any Bicep/app-settings change at the same time: each is a separate site update.
5. If the run fails after step 3 (runtime already 10.0, code possibly not), go straight to section 8; do not re-run blindly.
6. Run the post-deployment checks (section 7). Do not run `az deployment group create` for `main.bicep` as part of this
   rollout; it replaces the entire app-settings collection. If you want the Bicep and Azure to agree,
   run `az deployment group what-if` later as a separate, reviewed step.

### In-flight Stripe sessions and webhooks
- Checkout sessions created before the deploy stay valid (session ID and metadata are unchanged; webhook handling is the same code).
- A customer who pays during the window gets a redirect to the success page; `order-status` may briefly fail and the page should retry.
  The order stays `Pending` until the webhook is processed.
- After the deploy, open Stripe Dashboard → Developers → Webhooks → the endpoint → failed deliveries, and **resend** any that failed.
  Reprocessing is idempotent via `WebhookEvents`.
- Afterwards, compare `Pending` orders older than ~30 minutes with Stripe payments to catch any that missed a webhook.

## 7. Post-deployment checks

| Check | How | Expected |
|---|---|---|
| Runtime | `az functionapp runtime config show ...` | `dotnet-isolated` / `10.0` |
| Health | `GET https://<app>/api/health` | 200 `{"status":"ok"}` |
| Function list | Portal → Functions, or `az functionapp function list` | 20 functions; the disabled ones stay disabled (see `AzureWebJobs.*.Disabled` settings) |
| Cookie config | `GET /api/cookie-sale/config` | 200, `enabled: true`, classes and products as before |
| Checkout | Create a checkout, only with your approval, or watch the first real order | Pending row written, Stripe URL returned |
| Webhook | Stripe Dashboard → endpoint deliveries | 2xx; order becomes `Paid` and gets a confirmation number |
| Order status | `GET /api/cookie-sale/order-status?session_id=...` | `{"paymentStatus":"Paid","confirmationNumber":...}` |
| Confirmation email | Check the first real order's mailbox / Mailjet activity | Email delivered once |
| Daily Excel report | Next 06:00 UTC `CookieSaleDailyReport`, or the manual endpoint (function key, with approval) | Report email with paid-only data and the expected sheets |
| App Insights | Failures, Exceptions, Live Metrics; filter on the deploy time | No new exception types; no spike in 5xx |

## 8. Rollback (restore BOTH the .NET 8 code and the runtime)

Trigger: health fails after ~5 minutes, functions are missing, webhooks keep failing, or orders cannot be finalized.

1. Switch the runtime back (needs Azure CLI ≥ 2.87, see 5.7):
   `az functionapp runtime config set -n func-ocpittem-2026 -g rg-ocpittem --runtime-version 8.0`
   Code and runtime must match, so do step 2 immediately: the app is down until the .NET 8 package is published.
2. Redeploy the previous code:
   - **A (CI):** GitHub → Actions → Deploy Backend → Run workflow, choosing the tag `pre-net10-a7ec102` as the ref
     (that version's workflow builds with the .NET 8 SDK, publishes to `func-ocpittem-2026`, and has `workflow_dispatch`).
     It rebuilds from source (about 3–5 min), so the outage lasts that long; to shorten it, start this run first and
     run step 1 when it reaches the publish step; or
   - **B (artifact):** deploy the package saved in 5.2 to the app. Rehearse this once on the test app
     before relying on it; the exact zip-deploy command for Flex is not verified here.
3. Re-run the checks in section 7 (runtime shows 8.0, health, config).
4. Resend failed Stripe webhook deliveries; reconcile `Pending` orders against Stripe.
5. The `main` branch still holds the .NET 10 code: do not trigger further deploys from it until fixed
   (`git revert` the merge or fix forward).

Rehearse the rollback (set 8.0 + redeploy the tag) once on the non-production app before the real deployment.

Rollback caveat: .NET 8 on Functions is only supported until 10 Nov 2026. Storage data is not changed by this
migration, so no data rollback is expected; the backups in section 5 are for safety only.

## 9. GO / NO-GO

- Code readiness: **GO** (build, 320 tests, publish and local host all pass).
- Production deployment: **NO-GO until** (a) section 3 non-production validation passes on a real Flex app,
  (b) the backup checklist is complete, (c) you approve the window and each production step.

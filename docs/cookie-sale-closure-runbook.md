# Cookie sale — closure and reactivation runbook

Closing the cookie sale has **two independent layers**. Both are needed:

| Layer | What it does | How it is switched | Deploy needed? |
|-------|--------------|--------------------|----------------|
| Frontend (display) | Hides the homepage promo and order buttons, shows a thank-you message on `/koekjesverkoop` | `frontend/src/config/cookieSale.ts` | Yes, the Static Web App (automatic on merge to `main`) |
| Backend (enforcement) | Stops `POST /api/cookie-sale/create-checkout` from creating orders and Stripe Checkout sessions, including direct API calls | App setting `AzureWebJobs.CreateCookieSaleCheckout.Disabled` | No, it is an app setting on the Function App |

The frontend layer alone is **not** a security control: the API can be called directly. The backend switch is what
actually stops new orders.

Everything else keeps running and must **not** be disabled:

- `StripeWebhook` confirms payments for checkouts that were started before the deadline.
- `GetCookieSaleOrderStatus` powers the payment success page.
- `GetCookieSaleConfig` is harmless; it can stay enabled.
- `CookieSaleDailyReport` and `CookieSaleDailyReportManual` produce the reports.
- `Health` and all other functions stay as they are.

---

## A. Frontend: close the display

In `frontend/src/config/cookieSale.ts`:

```ts
export const COOKIE_SALE: CookieSaleSettings = {
  enabled: true,
  closesAt: 'YYYY-MM-DDTHH:mm', // confirmed deadline, local Brussels time
};
```

- `closesAt` is local time in Europe/Brussels. Daylight saving time is handled, and the result is the same for visitors
  in any time zone. Use `'YYYY-MM-DDTHH:mm'`; an invalid value closes the sale (fail-closed) and logs an error in the
  browser console.
- With a `closesAt` set, the change can be merged **before** the deadline. Pages switch at the deadline
  automatically, including pages that are already open.
- To close immediately instead, set `enabled: false`.
- The `/koekjesverkoop` route, the order form code and the payment return pages stay in place.

Deploy: open a PR to `main`, review it, then merge. The Static Web Apps workflow deploys automatically.

> ⚠️ PR preview environments use the **production API**. Do not place a test order on a preview URL.

Local check (no payments): `cd frontend && npm run dev`, temporarily set `closesAt` to a past time (do not commit
that), and check `/`, `/activiteiten`, `/koekjesverkoop` and `/koekjesverkoop/betaling/cancel`.

---

## B. Backend: disable `CreateCookieSaleCheckout` (no code deploy)

Function App `func-ocpittem-2026`, resource group `rg-ocpittem` (Flex Consumption, Linux). This uses the same
mechanism `main.bicep` already uses to keep the Bal Parental sales functions disabled.

### B1. Before the deadline

1. **Save a copy of the current app settings** (Portal → Function App → Settings → Environment variables →
   Advanced edit → copy, or `az functionapp config appsettings list -n func-ocpittem-2026 -g rg-ocpittem`). The copy
   contains secrets, such as the storage connection string, so store it securely and **never in the repository**.
2. Confirm that the setting `AzureWebJobs.CreateCookieSaleCheckout.Disabled` exists with value `false`.
3. Confirm that the disable mechanism works on this app: the Bal Parental function is disabled the same way, so
   `POST /api/tickets/create-checkout` with body `{}` must return **404**. A 400 means the setting is not honoured;
   stop and do not rely on this runbook.
4. Get the API host from the browser developer tools (Network tab) on `https://ocpittem.be/koekjesverkoop`.
5. Run the safe baseline probe. It creates no order and never reaches Stripe, because validation fails before
   anything is saved:

   ```bash
   curl -i -X POST "https://<api-host>/api/cookie-sale/create-checkout" \
     -H "Content-Type: application/json" -d "{}"
   # expected now: HTTP 400 {"error":"Vul een geldige naam in."}
   ```

### B2. At the deadline

**Azure Portal:** Function App `func-ocpittem-2026` → Settings → Environment variables → App settings → edit
`AzureWebJobs.CreateCookieSaleCheckout.Disabled` → set the value to `true` → Apply → Confirm.
(Equivalent: Functions → `CreateCookieSaleCheckout` → Disable.)

**Or Azure CLI / Cloud Shell.** This updates only this one key and leaves the other settings unchanged:

```bash
az functionapp config appsettings set \
  --name func-ocpittem-2026 --resource-group rg-ocpittem \
  --settings "AzureWebJobs.CreateCookieSaleCheckout.Disabled=true"
```

The app restarts. Stripe automatically retries webhook deliveries that fail during the restart, so no payment
confirmation is lost.

**Do not:**

- run `az deployment group create` with `infrastructure/main.bicep`. It replaces the entire app-settings collection,
  and the parameter file contains placeholder or empty values (for example the report recipients). It would also
  reset this switch to `false`; see section D;
- disable `StripeWebhook`, `GetCookieSaleOrderStatus` or the report functions;
- change the Stripe webhook endpoint or the Stripe products/prices;
- run the "Deploy Backend" workflow as part of this closure.

### B3. Verify (within a few minutes)

| Check | Expected |
|-------|----------|
| Probe from B1.5 (`{}` to `/api/cookie-sale/create-checkout`) | **404** (was 400) |
| `GET /api/health` | 200 |
| `GET /api/cookie-sale/order-status?session_id=x` | 404 with JSON `{"error":"Bestelling nog niet gevonden."}` (function alive) |
| Portal → Functions | `CreateCookieSaleCheckout` disabled, `StripeWebhook` enabled |
| Stripe Dashboard → Webhooks | Recent deliveries succeed |

### B4. Rollback

Set `AzureWebJobs.CreateCookieSaleCheckout.Disabled` back to `false`. The app restarts and ordering works again.
No data is changed by closing or reopening.

---

## C. Stripe: checkouts that are still open

`CreateCookieSaleCheckoutSessionAsync` does not set an expiry, so Stripe's default applies: a Checkout Session
stays payable for **24 hours** after it was created. Customers who started checkout before the deadline can still
pay afterwards. The webhook marks those orders `Paid` and they appear in the reports. Sessions that expire are
marked `Cancelled` by the webhook.

If late payments are not wanted, expire the remaining open sessions by hand in the Stripe Dashboard after the
deadline. Do not delete orders from the `CookieOrders` table.

---

## D. Infrastructure follow-up (required before any future Bicep deployment)

`infrastructure/main.bicep` hardcodes the switch as enabled:

```bicep
{ name: 'AzureWebJobs.CreateCookieSaleCheckout.Disabled', value: 'false' }
```

A future `az deployment group create` would therefore **silently reopen ordering**. Before any infrastructure
deployment:

1. Replace the hardcoded value with a parameter, similar to the Bal Parental flags, for example
   `param disableCookieSaleCheckout bool = true` and
   `{ name: 'AzureWebJobs.CreateCookieSaleCheckout.Disabled', value: string(disableCookieSaleCheckout) }`.
2. Add `"disableCookieSaleCheckout": { "value": true }` to `infrastructure/main.parameters.json`.
3. Reconcile **all** other parameters with the values currently in production (compare with the backup from B1.1).
   The file is not a complete mirror of production.

Recommended later, with the next planned backend deployment: add a server-side closing time in
`CookieSale2026Catalog` and check it in `CookieSaleOrderFunction` (in both `GetConfig` and `CreateCheckout`),
so the API also returns a friendly "afgelopen" message instead of 404.

---

## E. Reactivating for the next cookie sale

1. Backend: update the catalog (year, classes, prices, Stripe price IDs, event date) and deploy as usual.
2. Azure: set `AzureWebJobs.CreateCookieSaleCheckout.Disabled=false` (and `disableCookieSaleCheckout=false` in the
   Bicep parameters).
3. Frontend: in `frontend/src/config/cookieSale.ts` set `enabled: true` and `closesAt` to the new deadline (or `null`).
   Update the year, date and texts on `HomePage.tsx`, `ActiviteitenPage.tsx` (also `COOKIE_SALE_EVENT_ID`) and
   `CookieSalePage.tsx`.
4. Close again with this runbook.

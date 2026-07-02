# OC Pittem Website — [ocpittem.be](https://ocpittem.be)

Moderne website voor het **Oudercomité met Pit** (Pittem) met online ticketverkoop, sponsorpakketten, QR-ticketvalidatie en contactformulier.

De applicatie bestaat uit een **React single-page app** (frontend), een **Azure Functions API** (.NET 8 isolated) en een volledig via **Bicep** beschreven **Azure-infrastructuur**. Betalingen verlopen via **Stripe**, e-mails via **Mailjet** en tickets/orders worden opgeslagen in **Azure Table Storage**.

---

## 📐 Architectuuroverzicht

```
                            ┌──────────────────────────────┐
        Bezoeker  ─────────▶│  Azure Static Web App (Free)  │
       (browser)            │  React SPA  (Vite + Tailwind) │
                            └───────────────┬───────────────┘
                                            │  fetch /api/*
                                            ▼
                            ┌──────────────────────────────┐
                            │  Azure Functions (.NET 8)     │
                            │  Flex Consumption, HTTP + Timer│
                            └───┬─────────┬─────────┬───────┘
                                │         │         │
                 ┌──────────────┘         │         └───────────────┐
                 ▼                        ▼                         ▼
        ┌────────────────┐      ┌──────────────────┐      ┌──────────────────┐
        │ Azure Table &  │      │  Stripe          │      │  Mailjet / SMTP  │
        │ Blob Storage   │      │  (checkout +     │      │  (transactionele │
        │ (orders,       │      │   webhooks)      │      │   e-mails)       │
        │  tickets,      │      └──────────────────┘      └──────────────────┘
        │  sponsors,     │
        │  logo's)       │      ┌──────────────────┐      ┌──────────────────┐
        └────────────────┘      │  Key Vault       │      │ Application       │
                                │  (secrets, RBAC) │      │ Insights (logs)  │
                                └──────────────────┘      └──────────────────┘
```

**Belangrijkste flows**

- **Ticketverkoop** — de SPA roept `POST /api/tickets/create-checkout` aan → de Function maakt een Stripe Checkout Session → na betaling stuurt Stripe een webhook (`StripeWebhookFunction`) → de Function slaat de order/tickets op in Table Storage, genereert een PDF-ticket met QR-code (QuestPDF + QRCoder) en verstuurt dit via Mailjet.
- **Ticketvalidatie** — de `/scan`-pagina scant een QR-code (html5-qrcode) en valideert deze via `GET /api/tickets/validate?code=...`.
- **Sponsors** — sponsoraanvragen, betaling, logo-upload en fiscale attesten worden afgehandeld door de sponsor-functions en opgeslagen in Blob/Table Storage.
- **Contact** — `POST /api/contact` verstuurt een e-mail naar het oudercomité.
- **Dagrapport** — een timer-triggered Function (`DailyReportFunction`) bouwt dagelijkse verkoopstatistieken op.

---

## 🗂️ Projectstructuur

```
├── frontend/                       # React SPA (Vite + Tailwind CSS + TypeScript)
│   └── src/
│       ├── components/             # Herbruikbare UI (Navbar, Footer, Hero, Layout, carrousels …)
│       ├── pages/                  # Route-pagina's (Home, Activiteiten, BalParental, Contact, Scan …)
│       ├── services/api.ts         # Typed API-client richting de Functions backend
│       ├── config/                 # Feature-config (o.a. balParental active-flag)
│       └── App.tsx                 # react-router-dom routing
│
├── backend/
│   ├── OCPittem.Functions/         # Azure Functions app (.NET 8 isolated)
│   │   ├── Functions/              # HTTP- & Timer-triggers (endpoints)
│   │   ├── Services/               # Stripe, Mailjet/SMTP, Storage, PDF, QR, sponsors …
│   │   ├── Models/                 # DTO's & Table Storage entities
│   │   ├── Validators/             # Input-validatie
│   │   ├── ServiceOptions.cs       # Strongly-typed options (Stripe, Mailjet, Storage …)
│   │   └── Program.cs              # DI-registratie & host-configuratie
│   └── OCPittem.Functions.Tests/   # Unit tests
│
├── infrastructure/
│   ├── main.bicep                  # Volledige Azure-infra als code
│   └── main.parameters.json        # Deploy-parameters
│
├── .github/workflows/              # CI/CD (backend deploy + Static Web App deploy)
└── OCPittem.sln
```

---

## 🧩 Technologiestack

| Laag | Technologie |
|------|-------------|
| **Frontend** | React 18, TypeScript, Vite 5, Tailwind CSS 3, react-router-dom 6, html5-qrcode |
| **Backend** | .NET 8, Azure Functions v4 (isolated worker), HTTP- & Timer-triggers |
| **Betalingen** | Stripe.net (Checkout Sessions + webhooks) |
| **E-mail** | Mailjet.Api (met SMTP-fallback) |
| **Documenten** | QuestPDF (PDF-tickets), QRCoder (QR-codes), ClosedXML (Excel-rapporten) |
| **Opslag** | Azure Table Storage (orders, tickets, sponsors, webhook-events) + Blob Storage (logo's, attesten, deploy-package) |
| **Secrets** | Azure Key Vault (RBAC, via managed identity) |
| **Hosting** | Azure Static Web App (frontend) + Azure Functions Flex Consumption (backend) |
| **Observability** | Application Insights |
| **Infra as Code** | Azure Bicep |
| **CI/CD** | GitHub Actions |

---

## 🔌 API-endpoints (belangrijkste)

| Methode & pad | Function | Beschrijving |
|---------------|----------|--------------|
| `POST /api/tickets/create-checkout` | `TicketOrderFunction` | Start Stripe Checkout voor tickets |
| *(Stripe webhook)* | `StripeWebhookFunction` | Verwerkt Stripe-events, genereert tickets & mailt |
| `GET  /api/tickets/validate` | `TicketValidateFunction` | Valideert een ticket-QR aan de ingang |
| `POST /api/contact` | `ContactFunction` | Verstuurt contactbericht via e-mail |
| `POST /api/sponsors/...` | `SponsorRequestFunction` e.a. | Sponsoraanvraag, betaling, logo-upload, attest |
| `GET  /api/gallery/...` | `GalleryFunction` | Levert sfeerbeelden-galerij |
| `GET  /api/health` | `HealthFunction` | Health check |
| *(timer)* | `DailyReportFunction` | Dagelijks verkooprapport |

> De `Admin*`-functions bieden beheeracties (manueel order/sponsor aanmaken, als betaald markeren, e-mail opnieuw versturen).

---

## 💻 Lokaal ontwikkelen

### Frontend

```bash
cd frontend
npm install
npm run dev
# → http://localhost:5173
```

### Backend

Vereisten:
- .NET 8 SDK
- Azure Functions Core Tools v4
- Azurite (lokale storage-emulator)

```bash
cd backend/OCPittem.Functions
func start
# → http://localhost:7071
```

### Environment variables

- **Frontend**: kopieer `frontend/.env.example` naar `frontend/.env`
  - `VITE_API_BASE_URL` — basis-URL van de API (standaard `/api`)
- **Backend**: pas `backend/OCPittem.Functions/local.settings.json` aan met jouw Stripe- en Mailjet-keys. Configuratie is gegroepeerd per sectie (`Stripe`, `Mailjet`, `Email`, `Smtp`, `App`, `Storage`, `SponsorAttestation`) en wordt in `Program.cs` gebonden aan strongly-typed options.

---

## 🚀 Deployment

### Infrastructuur (eenmalig / bij wijzigingen)

```bash
az deployment group create \
  -g rg-ocpittem \
  -f infrastructure/main.bicep \
  -p infrastructure/main.parameters.json
```

Dit provisioned in **West Europe**: Storage Account (+ tables & blob-container), Application Insights, Key Vault (RBAC), een Flex Consumption Function App met system-assigned identity, en een Static Web App. De Function App krijgt automatisch de rol *Key Vault Secrets User* zodat secrets via Key Vault-references worden ingelezen. Vul na de deployment de secrets in Key Vault in (`stripe-secret-key`, `stripe-webhook-secret`, `mailjet-api-key`, `mailjet-api-secret`).

### Continue deployment (GitHub Actions)

- **Backend** — `.github/workflows/deploy-backend.yml`: bij een push naar `main` met wijzigingen in `backend/**` wordt de Function App gepubliceerd naar Flex Consumption.
- **Frontend** — de Azure Static Web Apps-workflow bouwt en deployt de React-app automatisch.

---

## 🔐 Secrets & security

- Alle gevoelige waarden (Stripe- en Mailjet-keys) staan in **Azure Key Vault** en worden via **managed identity + RBAC** ingelezen — nooit in broncode of parameterbestanden.
- De Function App draait met `httpsOnly` en CORS beperkt tot `ocpittem.be`.
- Storage staat op `TLS1_2` minimum en zonder publieke blob-toegang.

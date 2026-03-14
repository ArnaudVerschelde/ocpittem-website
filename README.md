# OC Pittem Website — ocpittem.be

Moderne website voor **Oudercomité met PIT!** (Pittem) met online ticketverkoop, sponsorpakketten en contactformulier.

## Projectstructuur

```
├── frontend/          # React SPA (Vite + Tailwind CSS)
├── backend/           # Azure Functions (.NET 8 Isolated)
│   └── OCPittem.Functions/
├── infrastructure/    # Azure Bicep deployment template
└── README.md
```

## Lokaal ontwikkelen

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
- Azurite (lokale storage emulator)

```bash
cd backend/OCPittem.Functions
func start
# → http://localhost:7071
```

### Environment variables

- Frontend: kopieer `frontend/.env.example` naar `frontend/.env`
- Backend: pas `backend/OCPittem.Functions/local.settings.json` aan met jouw Stripe/SendGrid keys

## Deployment

Zie de setup-instructies in de projectdocumentatie voor Azure resources, Stripe en SendGrid configuratie.

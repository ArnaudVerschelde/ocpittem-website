/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Backend API base URL — bv. https://func-ocpittem-2026.azurewebsites.net/api */
  readonly VITE_API_BASE_URL: string;
  /** Stripe publishable key (pk_live_... of pk_test_...) — momenteel niet actief gebruikt in frontend */
  readonly VITE_STRIPE_PUBLISHABLE_KEY: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}

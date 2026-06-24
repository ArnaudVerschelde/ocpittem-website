// ---------------------------------------------------------------------------
// Bal Parental feature-flag
// ---------------------------------------------------------------------------
//
// Zet deze waarde op `true` om Bal Parental terug publiek te maken:
//   - promo-banner op de homepage
//   - de eigen pagina /bal-parental met ticket- en sponsorverkoop
//   - de "Tickets & info" knop op de activiteitenpagina
//
// Zolang de waarde `false` is, is de editie afgelopen:
//   - de promo en de verkooppagina zijn niet meer zichtbaar
//   - op de activiteitenpagina blijft Bal Parental staan met een
//     "afgelopen"-melding (zie ActiviteitenPage).
//
// Vergeet bij een nieuwe editie ook de datums/deadlines in
// BalParentalPage.tsx na te kijken.
export const BAL_PARENTAL_ACTIVE = false;

// ---------------------------------------------------------------------------
// Sfeerbeelden-knop op de activiteitenpagina
// ---------------------------------------------------------------------------
//
// Zet deze waarde op `true` om de "Bekijk sfeerbeelden" knop bij Bal Parental
// op de activiteitenpagina te tonen.
//
// De pagina /sfeerbeelden-bal-parental-2026 blijft sowieso bereikbaar via de
// URL; deze vlag bepaalt enkel of er een knop verschijnt om er naartoe te
// navigeren. Zet op `true` zodra de sfeerbeelden klaar zijn om getoond te worden.
export const SFEERBEELDEN_BUTTON_ACTIVE = false;

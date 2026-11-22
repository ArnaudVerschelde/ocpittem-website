// ---------------------------------------------------------------------------
// Koekjesverkoop: centrale open/gesloten-configuratie
// ---------------------------------------------------------------------------
//
// Deze instellingen bepalen of de koekjesverkoop open is op de website:
//   - promo-banner op de homepage
//   - "Bestel je koekjes" knop op de activiteitenpagina
//   - het bestelformulier op /koekjesverkoop
//   - de "Opnieuw proberen" knop na een geannuleerde betaling
//
// Zolang de verkoop gesloten is, blijft /koekjesverkoop bereikbaar maar toont
// de pagina een bedankingsboodschap in plaats van het bestelformulier.
// Betaalbevestigingen, bestaande bestellingen en rapporten blijven werken.
//
// Gesloten = `enabled` is `false` OF het sluitingsmoment `closesAt` is voorbij.
//
// `closesAt` is lokale tijd in Brussel (Europe/Brussels), formaat
// 'YYYY-MM-DDTHH:mm', bv. '2026-11-22T23:59'. Zomer- en wintertijd worden
// automatisch verrekend, ongeacht de tijdzone van de bezoeker.
// `null` = geen automatisch sluitingsmoment (enkel `enabled` telt).
// Een ongeldige waarde sluit de verkoop (fail-closed) en logt een fout.
//
// BELANGRIJK: dit is enkel de weergave. Het afsluiten van nieuwe bestellingen
// in de API gebeurt apart in Azure (AzureWebJobs.CreateCookieSaleCheckout.Disabled),
// zie docs/cookie-sale-closure-runbook.md.
//
// Volgende koekjesverkoop heropenen:
//   1. zet `enabled` op `true` en `closesAt` op de nieuwe deadline (of `null`);
//   2. werk de backend-catalogus (klassen, prijzen, datum) bij;
//   3. heractiveer CreateCookieSaleCheckout in Azure (zie runbook).
// ---------------------------------------------------------------------------

export interface CookieSaleSettings {
  /** Hoofdschakelaar. `false` = verkoop gesloten, ongeacht `closesAt`. */
  enabled: boolean;
  /** Sluitingsmoment in Brusselse lokale tijd ('YYYY-MM-DDTHH:mm') of `null`. */
  closesAt: string | null;
}

// Koekjesverkoop 2026: bevestigde deadline zondag 22 november 2026 om 20:00 (Brussel, CET = UTC+1).
export const COOKIE_SALE: CookieSaleSettings = {
  enabled: true,
  closesAt: '2026-11-22T20:00',
};

export const COOKIE_SALE_TIME_ZONE = 'Europe/Brussels';

export const COOKIE_SALE_CLOSED_MESSAGE =
  'De koekjesverkoop is afgelopen. Bedankt voor jullie bestellingen!';

const LOCAL_DATE_TIME_PATTERN = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/;

interface WallClock {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
}

const formatters = new Map<string, Intl.DateTimeFormat>();

function wallClockAt(utcMs: number, timeZone: string): WallClock & { second: number } {
  let formatter = formatters.get(timeZone);
  if (!formatter) {
    formatter = new Intl.DateTimeFormat('en-GB', {
      timeZone,
      hourCycle: 'h23',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
    formatters.set(timeZone, formatter);
  }

  const parts: Record<string, number> = {};
  for (const part of formatter.formatToParts(new Date(utcMs))) {
    if (part.type !== 'literal') parts[part.type] = Number(part.value);
  }

  return {
    year: parts.year,
    month: parts.month,
    day: parts.day,
    hour: parts.hour,
    minute: parts.minute,
    second: parts.second,
  };
}

/**
 * Zet een lokale tijd ('YYYY-MM-DDTHH:mm') in de gegeven tijdzone om naar een
 * absoluut moment. Geeft `null` bij een ongeldige of niet-bestaande tijd
 * (bv. tijdens de zomertijdsprong).
 */
export function parseZonedDateTime(value: string, timeZone = COOKIE_SALE_TIME_ZONE): Date | null {
  const match = LOCAL_DATE_TIME_PATTERN.exec(value.trim());
  if (!match) return null;

  const [year, month, day, hour, minute] = match.slice(1).map(Number);
  const naiveUtcMs = Date.UTC(year, month - 1, day, hour, minute);

  // Verschuif met de tijdzone-offset; twee iteraties vangen DST-overgangen op.
  let utcMs = naiveUtcMs;
  for (let iteration = 0; iteration < 2; iteration++) {
    const local = wallClockAt(utcMs, timeZone);
    const localAsUtcMs = Date.UTC(local.year, local.month - 1, local.day, local.hour, local.minute, local.second);
    utcMs = naiveUtcMs - (localAsUtcMs - utcMs);
  }

  const check = wallClockAt(utcMs, timeZone);
  const roundTrips =
    check.year === year &&
    check.month === month &&
    check.day === day &&
    check.hour === hour &&
    check.minute === minute;

  return roundTrips ? new Date(utcMs) : null;
}

let reportedInvalidClosesAt: string | null = null;

/**
 * Het sluitingsmoment als absoluut tijdstip.
 * `undefined` = geen sluitingsmoment ingesteld, `null` = ongeldige waarde.
 */
export function getCookieSaleClosingTime(settings: CookieSaleSettings = COOKIE_SALE): Date | null | undefined {
  if (settings.closesAt === null) return undefined;

  const closingTime = parseZonedDateTime(settings.closesAt);
  if (!closingTime && reportedInvalidClosesAt !== settings.closesAt) {
    reportedInvalidClosesAt = settings.closesAt;
    console.error(
      `Ongeldige COOKIE_SALE.closesAt '${settings.closesAt}'; verwacht 'YYYY-MM-DDTHH:mm' (Europe/Brussels). De koekjesverkoop wordt als gesloten beschouwd.`,
    );
  }
  return closingTime;
}

export function isCookieSaleOpen(
  now: Date = new Date(),
  settings: CookieSaleSettings = COOKIE_SALE,
): boolean {
  if (!settings.enabled) return false;

  const closingTime = getCookieSaleClosingTime(settings);
  if (closingTime === undefined) return true;
  if (closingTime === null) return false;

  return now.getTime() < closingTime.getTime();
}

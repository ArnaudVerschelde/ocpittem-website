import { useEffect, useState } from 'react';
import { getCookieSaleClosingTime, isCookieSaleOpen } from '../config/cookieSale';

// setTimeout overflows (fires immediately) beyond ~24.8 days.
const MAX_TIMEOUT_MS = 2_147_483_647;

/**
 * Of de koekjesverkoop open is. Schakelt een open pagina automatisch om
 * naar "gesloten" zodra het sluitingsmoment bereikt wordt.
 */
export function useCookieSaleOpen(): boolean {
  const [open, setOpen] = useState(() => isCookieSaleOpen());

  useEffect(() => {
    if (!open) return;

    const closingTime = getCookieSaleClosingTime();
    if (!closingTime) return;

    const delay = closingTime.getTime() - Date.now();
    if (delay <= 0) {
      setOpen(false);
      return;
    }
    if (delay > MAX_TIMEOUT_MS) return;

    const timer = window.setTimeout(() => setOpen(false), delay);
    return () => window.clearTimeout(timer);
  }, [open]);

  return open;
}

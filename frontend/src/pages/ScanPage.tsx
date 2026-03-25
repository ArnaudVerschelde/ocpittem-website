import { useEffect, useRef, useState } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { api, ValidateTicketResponse } from '../services/api';

const SCAN_PIN = import.meta.env.VITE_SCAN_PIN as string | undefined;
const READER_ID = 'qr-reader';
const RESULT_DISPLAY_MS = 3000;

type ScanState = 'pin' | 'scanning' | 'loading' | 'result';

function isPinRequired() {
  return !!SCAN_PIN && sessionStorage.getItem('scan-unlocked') !== '1';
}

export default function ScanPage() {
  const [state, setState] = useState<ScanState>(isPinRequired() ? 'pin' : 'scanning');
  const [pin, setPin] = useState('');
  const [pinError, setPinError] = useState(false);
  const [result, setResult] = useState<ValidateTicketResponse | null>(null);
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const processingRef = useRef(false);

  useEffect(() => {
    if (state !== 'scanning') return;

    processingRef.current = false;
    const readerEl = document.getElementById(READER_ID);
    if (readerEl) readerEl.innerHTML = '';
    const scanner = new Html5Qrcode(READER_ID);
    scannerRef.current = scanner;

    scanner
      .start(
        { facingMode: 'environment' },
        { fps: 10, qrbox: { width: 260, height: 260 } },
        async (decodedText) => {
          if (processingRef.current) return;
          processingRef.current = true;

          setState('loading');
          await scanner.stop().catch(() => {});
          scannerRef.current = null;
          const readerEl = document.getElementById(READER_ID);
          if (readerEl) readerEl.innerHTML = '';

          try {
            const res = await api.validateTicket(decodedText);
            setResult(res);
          } catch {
            setResult({ valid: false, error: 'Verbindingsfout. Probeer opnieuw.' });
          }

          setState('result');
          setTimeout(() => setState('scanning'), RESULT_DISPLAY_MS);
        },
        () => {},
      )
      .catch(console.error);

    return () => {
      scanner.stop().catch(() => {});
      scannerRef.current = null;
    };
  }, [state]);

  function handlePinSubmit() {
    if (pin === SCAN_PIN) {
      sessionStorage.setItem('scan-unlocked', '1');
      setState('scanning');
    } else {
      setPinError(true);
      setPin('');
    }
  }

  if (state === 'pin') {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center bg-gray-900 p-8">
        <div className="w-full max-w-xs rounded-2xl bg-white p-8 shadow-xl">
          <h1 className="mb-1 text-center text-2xl">🎟️</h1>
          <h2 className="mb-6 text-center text-xl font-bold text-gray-800">Ticket scanner</h2>
          <label className="mb-1 block text-sm font-medium text-gray-600">Pincode</label>
          <input
            type="password"
            inputMode="numeric"
            value={pin}
            onChange={(e) => { setPin(e.target.value); setPinError(false); }}
            onKeyDown={(e) => e.key === 'Enter' && handlePinSubmit()}
            className="mb-2 w-full rounded-lg border border-gray-300 px-4 py-3 text-center text-2xl tracking-widest focus:border-[#13A2A3] focus:outline-none"
            autoFocus
          />
          {pinError && (
            <p className="mb-3 text-center text-sm text-red-500">Foute pincode, probeer opnieuw.</p>
          )}
          <button
            onClick={handlePinSubmit}
            className="w-full rounded-lg bg-[#13A2A3] py-3 font-semibold text-white hover:bg-[#0e8a8b] active:bg-[#0c7a7b]"
          >
            Ontgrendelen
          </button>
        </div>
      </div>
    );
  }

  if (state === 'result' && result) {
    return (
      <div
        className={`flex min-h-screen flex-col items-center justify-center p-8 ${
          result.valid ? 'bg-green-500' : 'bg-red-500'
        }`}
      >
        <div className="mb-4 text-8xl">{result.valid ? '✅' : '❌'}</div>
        {result.valid ? (
          <>
            <p className="text-3xl font-bold text-white">Geldig ticket</p>
            <p className="mt-3 text-xl text-white/90">
              {result.ticketType === 'Toegang' ? '🎉 Toegang' : '🍽️ Eten & Party'}
            </p>
          </>
        ) : (
          <>
            <p className="text-3xl font-bold text-white">Ongeldig</p>
            <p className="mt-3 text-center text-lg text-white/90">{result.error}</p>
          </>
        )}
        <p className="mt-10 text-sm text-white/50">Volgende scan over {RESULT_DISPLAY_MS / 1000}s…</p>
      </div>
    );
  }

  return (
    <div className="relative flex min-h-screen flex-col bg-gray-900">
      <div className="shrink-0 px-4 py-4 text-center">
        <h1 className="text-base font-bold text-white">🎟️ Ticket scanner — Bal Parental</h1>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center px-4">
        <div
          id={READER_ID}
          className="w-full max-w-sm overflow-hidden rounded-2xl border-4 border-[#13A2A3] shadow-lg"
        />
        <p className="mt-4 text-sm text-gray-400">
          {state === 'scanning' && 'Richt de camera op een QR code'}
          {state === 'loading' && '⏳ Valideren...'}
        </p>
      </div>
    </div>
  );
}

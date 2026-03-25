import { useEffect, useRef, useState } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { api, ValidateTicketResponse } from '../services/api';

const SCAN_PIN = import.meta.env.VITE_SCAN_PIN as string | undefined;
const READER_ID = 'qr-reader';
const RESULT_DISPLAY_MS = 3000;
const VALIDATE_TIMEOUT_MS = 8000;

type ScanState = 'pin' | 'scanning' | 'loading' | 'result';

function isPinRequired() {
    return !!SCAN_PIN && sessionStorage.getItem('scan-unlocked') !== '1';
}

function withTimeout<T>(promise: Promise<T>, ms: number): Promise<T> {
    return new Promise<T>((resolve, reject) => {
        const timeoutId = window.setTimeout(() => {
            reject(new Error('Timeout: validatie duurde te lang.'));
        }, ms);

        promise
            .then((value) => {
                window.clearTimeout(timeoutId);
                resolve(value);
            })
            .catch((error) => {
                window.clearTimeout(timeoutId);
                reject(error);
            });
    });
}

export default function ScanPage() {
    const [state, setState] = useState<ScanState>(isPinRequired() ? 'pin' : 'scanning');
    const [pin, setPin] = useState('');
    const [pinError, setPinError] = useState(false);
    const [result, setResult] = useState<ValidateTicketResponse | null>(null);

    const scannerRef = useRef<Html5Qrcode | null>(null);
    const processingRef = useRef(false);
    const restartTimerRef = useRef<number | null>(null);

    async function stopScanner(instance?: Html5Qrcode | null) {
        const scanner = instance ?? scannerRef.current;
        if (!scanner) return;

        if (scannerRef.current === scanner) {
            scannerRef.current = null;
        }

        try {
            await scanner.stop();
        } catch (err) {
            console.warn('scanner.stop failed or scanner already stopped', err);
        }

        try {
            scanner.clear();
        } catch (err) {
            console.warn('scanner.clear failed', err);
        }
    }

    useEffect(() => {
        return () => {
            if (restartTimerRef.current) {
                window.clearTimeout(restartTimerRef.current);
            }
            void stopScanner();
        };
    }, []);

    useEffect(() => {
        if (state !== 'scanning') return;

        let cancelled = false;
        processingRef.current = false;
        setResult(null);

        const startScanner = async () => {
            if (scannerRef.current) return;

            console.log('Starting scanner');

            const scanner = new Html5Qrcode(READER_ID);
            scannerRef.current = scanner;

            try {
                await scanner.start(
                    { facingMode: 'environment' },
                    {
                        fps: 10,
                        qrbox: { width: 260, height: 260 },
                        aspectRatio: 1,
                    },
                    async (decodedText) => {
                        if (processingRef.current) return;
                        processingRef.current = true;

                        console.log('QR scanned:', decodedText);

                        await stopScanner(scanner);
                        if (cancelled) return;

                        setState('loading');

                        try {
                            console.log('Starting ticket validation');

                            const res = await withTimeout(
                                api.validateTicket(decodedText),
                                VALIDATE_TIMEOUT_MS,
                            );

                            console.log('Validation response:', res);

                            if (!cancelled) {
                                setResult(res);
                            }
                        } catch (err) {
                            console.error('Validation failed:', err);

                            if (!cancelled) {
                                const message =
                                    err instanceof Error
                                        ? err.message
                                        : 'Verbindingsfout. Probeer opnieuw.';

                                setResult({
                                    valid: false,
                                    error: message,
                                });
                            }
                        }

                        if (cancelled) return;

                        setState('result');

                        if (restartTimerRef.current) {
                            window.clearTimeout(restartTimerRef.current);
                        }

                        restartTimerRef.current = window.setTimeout(() => {
                            setState('scanning');
                        }, RESULT_DISPLAY_MS);
                    },
                    (errorMessage) => {
                        // Niet elke scan failure is echt een fout, dus enkel loggen indien nuttig
                        // console.debug('Scan attempt:', errorMessage);
                    },
                );
            } catch (err) {
                console.error('Scanner start failed:', err);
                scannerRef.current = null;

                if (!cancelled) {
                    setResult({
                        valid: false,
                        error: 'Camera kon niet gestart worden. Controleer cameratoegang en probeer opnieuw.',
                    });
                    setState('result');
                }
            }
        };

        void startScanner();

        return () => {
            cancelled = true;
            void stopScanner();
        };
    }, [state]);

    function handlePinSubmit() {
        if (pin === SCAN_PIN) {
            sessionStorage.setItem('scan-unlocked', '1');
            setPinError(false);
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
                        onChange={(e) => {
                            setPin(e.target.value);
                            setPinError(false);
                        }}
                        onKeyDown={(e) => e.key === 'Enter' && handlePinSubmit()}
                        className="mb-2 w-full rounded-lg border border-gray-300 px-4 py-3 text-center text-2xl tracking-widest focus:border-[#13A2A3] focus:outline-none"
                        autoFocus
                    />

                    {pinError && (
                        <p className="mb-3 text-center text-sm text-red-500">
                            Foute pincode, probeer opnieuw.
                        </p>
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

    if (state === 'loading') {
        return (
            <div className="flex min-h-screen flex-col items-center justify-center bg-gray-900 p-8">
                <div className="text-5xl">⏳</div>
                <p className="mt-4 text-lg text-white">Ticket valideren...</p>
                <p className="mt-2 text-sm text-gray-400">Even wachten</p>
            </div>
        );
    }

    if (state === 'result' && result) {
        return (
            <div
                className={`flex min-h-screen flex-col items-center justify-center p-8 ${result.valid ? 'bg-green-500' : 'bg-red-500'
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
                        <p className="mt-3 text-center text-lg text-white/90">
                            {result.error ?? 'Onbekende fout'}
                        </p>
                    </>
                )}

                <p className="mt-10 text-sm text-white/70">
                    Volgende scan over {RESULT_DISPLAY_MS / 1000}s…
                </p>
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
                    className="min-h-[320px] w-full max-w-sm overflow-hidden rounded-2xl border-4 border-[#13A2A3] shadow-lg"
                />
                <p className="mt-4 text-sm text-gray-400">Richt de camera op een QR code</p>
            </div>
        </div>
    );
}
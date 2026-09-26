import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import Hero from '../components/Hero';
import Section from '../components/Section';
import { api, CookieSaleConfig } from '../services/api';

interface QuantitySelectorProps {
  label: string;
  description: string;
  value: number;
  maximum: number;
  disabled: boolean;
  onChange: (value: number) => void;
}

function QuantitySelector({
  label,
  description,
  value,
  maximum,
  disabled,
  onChange,
}: QuantitySelectorProps) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-xl border border-gray-200 p-4">
      <div>
        <p className="font-semibold text-gray-900">{label}</p>
        <p className="mt-1 text-sm text-gray-500">{description}</p>
      </div>
      <div className="flex items-center gap-2">
        <button
          type="button"
          aria-label={`Verminder ${label}`}
          disabled={disabled || value <= 0}
          onClick={() => onChange(Math.max(0, value - 1))}
          className="flex h-9 w-9 items-center justify-center rounded-full border border-gray-300 text-lg font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-30"
        >
          −
        </button>
        <input
          type="number"
          min={0}
          max={maximum}
          value={value}
          disabled={disabled}
          aria-label={`Aantal ${label}`}
          onChange={(event) => {
            const nextValue = Number.parseInt(event.target.value || '0', 10);
            onChange(Math.min(maximum, Math.max(0, Number.isNaN(nextValue) ? 0 : nextValue)));
          }}
          className="h-10 w-16 rounded-lg border border-gray-300 text-center font-semibold text-gray-900"
        />
        <button
          type="button"
          aria-label={`Verhoog ${label}`}
          disabled={disabled || value >= maximum}
          onClick={() => onChange(Math.min(maximum, value + 1))}
          className="flex h-9 w-9 items-center justify-center rounded-full border border-gray-300 text-lg font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-30"
        >
          +
        </button>
      </div>
    </div>
  );
}

export default function CookieSalePage() {
  const [config, setConfig] = useState<CookieSaleConfig | null>(null);
  const [configError, setConfigError] = useState('');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [className, setClassName] = useState('');
  const [coteDorQuantity, setCoteDorQuantity] = useState(0);
  const [lotusQuantity, setLotusQuantity] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');

  useEffect(() => {
    let active = true;

    api.getCookieSaleConfig()
      .then((response) => {
        if (active) setConfig(response);
      })
      .catch(() => {
        if (active) setConfigError('De bestelgegevens konden niet geladen worden. Probeer het later opnieuw.');
      });

    return () => {
      active = false;
    };
  }, []);

  const coteDorPrice = config?.products.find((product) => product.id === 'coteDor')?.unitPriceCents ?? 1100;
  const lotusPrice = config?.products.find((product) => product.id === 'lotus')?.unitPriceCents ?? 900;
  const totalPackages = coteDorQuantity + lotusQuantity;
  const totalAmountCents = useMemo(
    () => coteDorQuantity * coteDorPrice + lotusQuantity * lotusPrice,
    [coteDorPrice, coteDorQuantity, lotusPrice, lotusQuantity],
  );
  const maximum = config?.maximumTotalPackages ?? 50;
  const orderingAvailable = config?.enabled === true;

  const updateCoteDorQuantity = (nextValue: number) => {
    setCoteDorQuantity(Math.min(nextValue, maximum - lotusQuantity));
  };

  const updateLotusQuantity = (nextValue: number) => {
    setLotusQuantity(Math.min(nextValue, maximum - coteDorQuantity));
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setSubmitError('');

    if (!orderingAvailable) {
      setSubmitError('Bestellen is nog niet beschikbaar omdat de klassenlijst nog niet is ingesteld.');
      return;
    }

    if (!name.trim() || !email.trim() || !className) {
      setSubmitError('Vul je naam, e-mailadres en klas in.');
      return;
    }

    if (totalPackages < 1) {
      setSubmitError('Kies minstens één pakket.');
      return;
    }

    if (totalPackages > maximum) {
      setSubmitError(`Je kan maximaal ${maximum} pakketten bestellen.`);
      return;
    }

    setSubmitting(true);
    try {
      const response = await api.createCookieSaleCheckout({
        name,
        email,
        className,
        coteDorQuantity,
        lotusQuantity,
      });
      window.location.assign(response.checkoutUrl);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Er ging iets mis. Probeer het later opnieuw.');
    } finally {
      setSubmitting(false);
    }
  };

  const formatPrice = (amountCents: number) =>
    new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(amountCents / 100);

  return (
    <>
      <Hero
        title="Koekjesverkoop"
        subtitle="Bestel je favoriete pakketten en steun de activiteiten van Oudercomité met Pit."
        backgroundClass="bg-gradient-to-br from-amber-500 via-orange-500 to-amber-700"
      />

      <Section>
        <div className="grid gap-10 lg:grid-cols-2">
          <div>
            <span className="inline-flex rounded-full bg-amber-100 px-3 py-1 text-sm font-semibold text-amber-800">
              10 december 2026
            </span>
            <h2 className="mt-5 section-title">Samen smullen, samen steunen</h2>
            <p className="section-subtitle">
              Met je bestelling steun je rechtstreeks de activiteiten die OC Pittem organiseert
              voor de kinderen van basisschool PIT!.
            </p>

            <div className="mt-8 space-y-4">
              <div className="card">
                <h3 className="text-xl font-bold text-gray-900">Côte d'Or pakket</h3>
                <p className="mt-2 text-2xl font-extrabold text-primary-600">{formatPrice(coteDorPrice)}</p>
              </div>
              <div className="card">
                <h3 className="text-xl font-bold text-gray-900">Lotus pakket</h3>
                <p className="mt-2 text-2xl font-extrabold text-primary-600">{formatPrice(lotusPrice)}</p>
              </div>
            </div>
          </div>

          <div className="rounded-2xl bg-white p-6 shadow-xl ring-1 ring-gray-100 sm:p-8">
            <h2 className="text-2xl font-bold text-gray-900">Bestelling</h2>
            <p className="mt-2 text-sm text-gray-500">
              Vul je gegevens in en betaal veilig via Stripe.
            </p>

            {!config && !configError && (
              <p className="mt-6 rounded-lg bg-gray-50 p-4 text-sm text-gray-600">
                Bestelgegevens laden…
              </p>
            )}

            {configError && (
              <p className="mt-6 rounded-lg bg-red-50 p-4 text-sm text-red-700">{configError}</p>
            )}

            {config && !orderingAvailable && (
              <div className="mt-6 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                Online bestellen is nog niet beschikbaar. De definitieve klassenlijst moet nog
                worden ingesteld.
              </div>
            )}

            <form onSubmit={handleSubmit} className="mt-6 space-y-5">
              <div>
                <label htmlFor="cookie-name" className="block text-sm font-medium text-gray-700">Naam</label>
                <input
                  id="cookie-name"
                  type="text"
                  required
                  disabled={!orderingAvailable}
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200 disabled:bg-gray-100"
                />
              </div>

              <div>
                <label htmlFor="cookie-email" className="block text-sm font-medium text-gray-700">E-mailadres</label>
                <input
                  id="cookie-email"
                  type="email"
                  required
                  disabled={!orderingAvailable}
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200 disabled:bg-gray-100"
                />
              </div>

              <div>
                <label htmlFor="cookie-class" className="block text-sm font-medium text-gray-700">Klas</label>
                <select
                  id="cookie-class"
                  required
                  disabled={!orderingAvailable}
                  value={className}
                  onChange={(event) => setClassName(event.target.value)}
                  className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200 disabled:bg-gray-100"
                >
                  <option value="">Kies een klas</option>
                  {config?.classes.map((classOption) => (
                    <option key={classOption} value={classOption}>{classOption}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-3">
                <QuantitySelector
                  label="Côte d'Or pakket"
                  description={`${formatPrice(coteDorPrice)} per pakket`}
                  value={coteDorQuantity}
                  maximum={maximum}
                  disabled={!orderingAvailable}
                  onChange={updateCoteDorQuantity}
                />
                <QuantitySelector
                  label="Lotus pakket"
                  description={`${formatPrice(lotusPrice)} per pakket`}
                  value={lotusQuantity}
                  maximum={maximum}
                  disabled={!orderingAvailable}
                  onChange={updateLotusQuantity}
                />
              </div>

              <div className="rounded-lg bg-primary-50 px-4 py-3">
                <div className="flex items-center justify-between font-semibold text-primary-900">
                  <span>Totaal ({totalPackages} pakketten)</span>
                  <span>{formatPrice(totalAmountCents)}</span>
                </div>
                <p className="mt-1 text-xs text-primary-700">
                  De definitieve prijs wordt altijd door de server berekend.
                </p>
              </div>

              <p className="text-sm text-gray-600">
                Door te bestellen bevestig je dat je kennis hebt genomen van onze{' '}
                <Link to="/privacy" className="font-medium text-primary-600 underline hover:text-primary-700">
                  privacyverklaring
                </Link>.
              </p>

              {submitError && (
                <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{submitError}</div>
              )}

              <button
                type="submit"
                disabled={!orderingAvailable || submitting || totalPackages < 1}
                className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-50"
              >
                {submitting ? 'Betaling voorbereiden…' : 'Bestellen en betalen'}
              </button>
            </form>
          </div>
        </div>
      </Section>
    </>
  );
}

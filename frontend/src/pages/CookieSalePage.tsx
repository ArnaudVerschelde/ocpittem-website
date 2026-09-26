import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import Section from '../components/Section';
import { api, CookieSaleConfig } from '../services/api';
import coteDorPackage from '../assets/cookies/cote-dor-package.webp';
import lotusPackage from '../assets/cookies/lotus-package.webp';

interface ProductCardProps {
  imageSrc: string;
  imageAlt: string;
  name: string;
  description: string;
  price: string;
}

function ProductCard({
  imageSrc,
  imageAlt,
  name,
  description,
  price,
}: ProductCardProps) {
  return (
    <article className="group overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-lg shadow-gray-900/5 ring-1 ring-gray-100 transition duration-200 hover:-translate-y-0.5 hover:shadow-xl hover:shadow-gray-900/10">
      <div className="flex h-52 items-center justify-center bg-gradient-to-br from-amber-50 via-orange-50/60 to-yellow-50 p-5">
        <img
          src={imageSrc}
          alt={imageAlt}
          className="h-full w-full object-contain drop-shadow-[0_12px_16px_rgba(120,53,15,0.12)] transition duration-300 group-hover:scale-[1.02]"
        />
      </div>
      <div className="border-t border-gray-100 p-5">
        <h3 className="text-lg font-bold text-gray-900">{name}</h3>
        <p className="mt-1 min-h-10 text-sm leading-relaxed text-gray-500">{description}</p>
        <p className="mt-4 text-2xl font-extrabold text-primary-600">{price}</p>
      </div>
    </article>
  );
}

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
    <div className="flex items-center justify-between gap-4 rounded-xl border border-gray-200 bg-gray-50/50 p-4">
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
      <section className="relative isolate overflow-hidden bg-gradient-to-br from-amber-500 via-orange-600 to-orange-800">
        <div
          aria-hidden="true"
          className="absolute inset-0 bg-[radial-gradient(circle_at_15%_20%,rgba(255,255,255,0.16),transparent_22%),radial-gradient(circle_at_82%_70%,rgba(120,53,15,0.24),transparent_32%)]"
        />

        <div aria-hidden="true" className="absolute -left-20 -top-24 h-64 w-64 rounded-full border-[22px] border-amber-200/10 bg-amber-100/10" />
        <div aria-hidden="true" className="absolute right-[8%] top-10 h-36 w-36 rounded-full border border-orange-100/20 bg-amber-100/10 shadow-[inset_0_0_0_12px_rgba(255,255,255,0.035)]">
          <span className="absolute left-8 top-9 h-3 w-3 rounded-full bg-orange-950/15" />
          <span className="absolute right-9 top-7 h-2.5 w-2.5 rounded-full bg-orange-950/15" />
          <span className="absolute bottom-8 left-12 h-2 w-2 rounded-full bg-orange-950/15" />
          <span className="absolute bottom-12 right-7 h-3.5 w-3.5 rounded-full bg-orange-950/15" />
        </div>
        <div aria-hidden="true" className="absolute bottom-10 right-[30%] hidden sm:block">
          <span className="absolute h-2.5 w-2.5 rounded-full bg-amber-100/25" />
          <span className="absolute left-8 top-5 h-1.5 w-1.5 rounded-full bg-amber-100/30" />
          <span className="absolute -left-5 top-10 h-2 w-2 rounded-full bg-orange-950/15" />
        </div>
        <div aria-hidden="true" className="absolute -bottom-24 -right-20 h-72 w-72 rounded-full bg-orange-950/10" />

        <div className="relative mx-auto max-w-7xl px-4 py-20 sm:px-6 sm:py-28 lg:px-8">
          <div className="max-w-2xl">
            <div className="mb-6 h-1 w-16 rounded-full bg-primary-300" />
            <h1 className="font-display text-4xl font-extrabold tracking-tight text-white drop-shadow-sm sm:text-5xl lg:text-6xl">
              Koekjesverkoop
            </h1>
            <p className="mt-6 max-w-2xl text-lg leading-relaxed text-orange-50">
              Bestel je favoriete pakketten en steun de activiteiten van Oudercomité met Pit.
            </p>
          </div>
        </div>
      </section>

      <Section className="bg-gradient-to-b from-amber-50/60 via-white to-white">
        <div className="grid gap-10 lg:grid-cols-2">
          <div>
            <span className="inline-flex rounded-full border border-amber-200 bg-amber-100 px-3 py-1 text-sm font-semibold text-amber-800">
              10 december 2026
            </span>
            <h2 className="mt-5 section-title">Samen smullen, samen steunen</h2>
            <p className="section-subtitle">
              Met je bestelling steun je rechtstreeks de activiteiten die OC Pittem organiseert
              voor de kinderen van basisschool PIT!.
            </p>

            <div className="mt-8 grid gap-5 sm:grid-cols-2">
              <ProductCard
                imageSrc={coteDorPackage}
                imageAlt="Côte d'Or pakket met een assortiment chocoladeproducten"
                name="Côte d'Or pakket"
                description="Een assortiment Côte d'Or chocolade."
                price={formatPrice(coteDorPrice)}
              />
              <ProductCard
                imageSrc={lotusPackage}
                imageAlt="Lotus pakket met een assortiment koekjes en gebak"
                name="Lotus pakket"
                description="Een assortiment bekende Lotus-koekjes."
                price={formatPrice(lotusPrice)}
              />
            </div>
          </div>

          <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-xl shadow-gray-900/5 ring-1 ring-gray-100 sm:p-8">
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

              <div className="rounded-lg border border-primary-100 bg-primary-50 px-4 py-3">
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

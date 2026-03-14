import { useState, FormEvent, useEffect } from 'react';
import Hero from '../components/Hero';
import Section from '../components/Section';

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

const sponsorPackages = [
  {
    id: 'brons',
    label: 'Brons',
    emoji: '🥉',
    price: 150,
    color: {
      border: 'border-amber-400',
      bg: 'bg-amber-50',
      badge: 'bg-amber-100 text-amber-800',
      button: 'bg-amber-500 hover:bg-amber-600 text-white',
      ring: 'ring-amber-300',
    },
    tickets: 2,
    perks: [
      '2 tickets inbegrepen',
      'Naamsvermelding op de affiche',
      'Vermelding op onze sociale media',
    ],
  },
  {
    id: 'zilver',
    label: 'Zilver',
    emoji: '🥈',
    price: 250,
    color: {
      border: 'border-gray-400',
      bg: 'bg-gray-50',
      badge: 'bg-gray-200 text-gray-700',
      button: 'bg-gray-600 hover:bg-gray-700 text-white',
      ring: 'ring-gray-300',
    },
    tickets: 4,
    perks: [
      '4 tickets inbegrepen',
      'Logo op de affiche',
      'Logo op de banner ter plaatse',
      'Vermelding op onze sociale media',
      'Vermelding op de website',
    ],
  },
  {
    id: 'goud',
    label: 'Goud',
    emoji: '🥇',
    price: 400,
    popular: true,
    color: {
      border: 'border-yellow-400',
      bg: 'bg-yellow-50',
      badge: 'bg-yellow-100 text-yellow-800',
      button: 'bg-yellow-500 hover:bg-yellow-600 text-white',
      ring: 'ring-yellow-300',
    },
    tickets: 6,
    perks: [
      '6 tickets inbegrepen',
      'Groot logo op de affiche',
      'Groot logo op de banner ter plaatse',
      'Vermelding als hoofdsponsor',
      'Vermelding op onze sociale media',
      'Prominente vermelding op de website',
      'Persoonlijke bedanking op het evenement',
    ],
  },
];

// ---------------------------------------------------------------------------
// Terms Modal
// ---------------------------------------------------------------------------

function TermsModal({ onClose }: { onClose: () => void }) {
  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', handler);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', handler);
      document.body.style.overflow = '';
    };
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="relative max-h-[80vh] w-full max-w-lg overflow-y-auto rounded-2xl bg-white shadow-2xl">
        {/* Header */}
        <div className="sticky top-0 flex items-center justify-between border-b border-gray-100 bg-white px-6 py-4">
          <h2 className="text-lg font-bold text-gray-900">Algemene voorwaarden</h2>
          <button
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-full text-gray-400 hover:bg-gray-100 hover:text-gray-600"
            aria-label="Sluiten"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="space-y-5 px-6 py-5 text-sm leading-relaxed text-gray-600">
          <p className="rounded-lg bg-yellow-50 px-4 py-3 text-xs text-yellow-700">
            ⚠️ Dit zijn tijdelijke voorwaarden ter illustratie. De definitieve algemene voorwaarden worden later gepubliceerd.
          </p>

          <div>
            <h3 className="font-semibold text-gray-900">1. Ticketaankoop</h3>
            <p className="mt-1">
              Tickets voor het Bal Parental zijn persoonlijk en niet overdraagbaar. Na aankoop
              ontvangt u een bevestigingsmail met uw ticket(s) als PDF-bijlage. Elk ticket bevat
              een unieke QR-code die bij de ingang wordt gescand.
            </p>
          </div>

          <div>
            <h3 className="font-semibold text-gray-900">2. Betaling</h3>
            <p className="mt-1">
              Betaling verloopt via Stripe, een beveiligde betaalprovider. Oudercomité met PIT!
              bewaart geen betalingsgegevens op eigen servers. Alle transacties zijn beveiligd
              via SSL/TLS-encryptie.
            </p>
          </div>

          <div>
            <h3 className="font-semibold text-gray-900">3. Annulering & terugbetaling</h3>
            <p className="mt-1">
              Tickets kunnen niet worden geannuleerd of terugbetaald, tenzij het evenement
              geannuleerd of uitgesteld wordt door de organisatie. In dat geval worden kopers
              via e-mail op de hoogte gesteld en worden tickets terugbetaald.
            </p>
          </div>

          <div>
            <h3 className="font-semibold text-gray-900">4. Toegang</h3>
            <p className="mt-1">
              Toegang tot het evenement is enkel mogelijk met een geldig, gescand ticket.
              De organisatie behoudt zich het recht voor personen de toegang te weigeren bij
              ongepast gedrag of bij een ongeldig ticket.
            </p>
          </div>

          <div>
            <h3 className="font-semibold text-gray-900">5. Privacybeleid</h3>
            <p className="mt-1">
              Persoonlijke gegevens (naam, e-mailadres) worden uitsluitend gebruikt voor de
              verwerking van uw bestelling en worden niet gedeeld met derden. Gegevens worden
              bewaard conform de AVG/GDPR-regelgeving.
            </p>
          </div>

          <div>
            <h3 className="font-semibold text-gray-900">6. Sponsorpakketten</h3>
            <p className="mt-1">
              Sponsoraanvragen worden door het oudercomité behandeld en bevestigd via e-mail.
              De inbegrepen tickets worden verstuurd na bevestiging van de betaling. Het
              oudercomité behoudt zich het recht voor het ontwerp van promotiemateriaal te bepalen.
            </p>
          </div>

          <div>
            <h3 className="font-semibold text-gray-900">7. Aansprakelijkheid</h3>
            <p className="mt-1">
              Oudercomité met PIT! is niet aansprakelijk voor verlies, diefstal of schade aan
              persoonlijke bezittingen tijdens het evenement. Deelname is op eigen risico.
            </p>
          </div>
        </div>

        <div className="border-t border-gray-100 px-6 py-4">
          <button onClick={onClose} className="btn-primary w-full">
            Sluiten
          </button>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Spinner helper
// ---------------------------------------------------------------------------

function Spinner() {
  return (
    <span className="flex items-center gap-2">
      <svg className="h-5 w-5 animate-spin" viewBox="0 0 24 24" fill="none">
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
      Bezig...
    </span>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

interface TicketForm {
  name: string;
  email: string;
  quantity: number;
  acceptTerms: boolean;
}

interface SponsorForm {
  companyName: string;
  contactName: string;
  email: string;
  phone: string;
  package: string;
  message: string;
  acceptTerms: boolean;
}

export default function BalParentalPage() {
  const [activeTab, setActiveTab] = useState<'tickets' | 'sponsor'>('tickets');
  const [showTerms, setShowTerms] = useState(false);

  // Ticket form
  const [ticketForm, setTicketForm] = useState<TicketForm>({ name: '', email: '', quantity: 1, acceptTerms: false });
  const [ticketLoading, setTicketLoading] = useState(false);
  const [ticketError, setTicketError] = useState('');

  // Sponsor form
  const [sponsorForm, setSponsorForm] = useState<SponsorForm>({ companyName: '', contactName: '', email: '', phone: '', package: 'zilver', message: '', acceptTerms: false });
  const [sponsorLoading, setSponsorLoading] = useState(false);
  const [sponsorError, setSponsorError] = useState('');
  const [sponsorSuccess, setSponsorSuccess] = useState(false);

  const handleTicketSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setTicketError('');
    if (!ticketForm.acceptTerms) { setTicketError('Je moet de algemene voorwaarden accepteren.'); return; }
    setTicketLoading(true);
    try {
      const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await fetch(`${apiBase}/tickets/create-checkout`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: ticketForm.name, email: ticketForm.email, quantity: ticketForm.quantity }),
      });
      if (!res.ok) throw new Error();
      const data = await res.json();
      if (data.checkoutUrl) window.location.href = data.checkoutUrl;
    } catch {
      setTicketError('Er ging iets mis. Probeer het later opnieuw.');
    } finally {
      setTicketLoading(false);
    }
  };

  const handleSponsorSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSponsorError('');
    setSponsorSuccess(false);
    if (!sponsorForm.acceptTerms) { setSponsorError('Je moet de algemene voorwaarden accepteren.'); return; }
    setSponsorLoading(true);
    try {
      const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await fetch(`${apiBase}/sponsors/request`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          companyName: sponsorForm.companyName,
          contactName: sponsorForm.contactName,
          email: sponsorForm.email,
          phone: sponsorForm.phone,
          package: sponsorForm.package,
          message: sponsorForm.message,
        }),
      });
      if (!res.ok) throw new Error();
      setSponsorSuccess(true);
      setSponsorForm({ companyName: '', contactName: '', email: '', phone: '', package: 'zilver', message: '', acceptTerms: false });
    } catch {
      setSponsorError('Er ging iets mis. Probeer het later opnieuw.');
    } finally {
      setSponsorLoading(false);
    }
  };

  const inputClass = 'mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200';

  return (
    <>
      {showTerms && <TermsModal onClose={() => setShowTerms(false)} />}

      <Hero
        title="Bal Parental 2026"
        subtitle="Het jaarlijks feest van het Oudercomité met PIT! Een onvergetelijke avond vol muziek, gezelligheid en plezier."
        backgroundClass="bg-gradient-to-br from-primary-500 via-primary-600 to-secondary-700"
      />

      {/* Praktische info */}
      <Section>
        <div className="grid gap-12 lg:grid-cols-2">
          <div>
            <h2 className="section-title">Praktische info</h2>
            <dl className="mt-8 space-y-6">
              {[
                { label: '📅 Datum', value: 'zaterdag 20 juni 2026' },
                { label: '📍 Locatie', value: 'Pittem — locatie wordt later bekendgemaakt' },
                { label: '🎵 Muziek', value: 'DJ — wordt later bekendgemaakt' },
                { label: '💰 Prijs', value: '€25 per persoon (voorverkoop)' },
                { label: '🍹 Inclusief', value: 'Toegang + eerste drankje' },
              ].map((item) => (
                <div key={item.label} className="flex gap-4">
                  <dt className="w-28 flex-shrink-0 text-sm font-semibold text-gray-900">{item.label}</dt>
                  <dd className="text-gray-600">{item.value}</dd>
                </div>
              ))}
            </dl>
            <div className="mt-10 rounded-xl bg-primary-50 p-6">
              <h3 className="font-semibold text-primary-800">💡 Goed om te weten</h3>
              <ul className="mt-3 space-y-2 text-sm text-primary-700">
                <li>• Tickets worden per e-mail verstuurd als PDF</li>
                <li>• Elk ticket bevat een unieke QR-code</li>
                <li>• Betaling via beveiligde Stripe checkout</li>
                <li>• Geen kaartgegevens worden op onze servers bewaard</li>
              </ul>
            </div>
          </div>

          {/* Tab formulieren */}
          <div>
            {/* Tabs */}
            <div className="flex rounded-xl bg-gray-100 p-1">
              <button
                type="button"
                onClick={() => setActiveTab('tickets')}
                className={`flex-1 rounded-lg py-2.5 text-sm font-semibold transition-all ${activeTab === 'tickets' ? 'bg-white shadow text-gray-900' : 'text-gray-500 hover:text-gray-700'}`}
              >
                🎟️ Tickets bestellen
              </button>
              <button
                type="button"
                onClick={() => setActiveTab('sponsor')}
                className={`flex-1 rounded-lg py-2.5 text-sm font-semibold transition-all ${activeTab === 'sponsor' ? 'bg-white shadow text-gray-900' : 'text-gray-500 hover:text-gray-700'}`}
              >
                🤝 Sponsor worden
              </button>
            </div>

            {/* Ticket formulier */}
            {activeTab === 'tickets' && (
              <div className="mt-4 rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100">
                <h2 className="text-2xl font-bold text-gray-900">Tickets bestellen</h2>
                <p className="mt-2 text-sm text-gray-500">
                  Vul onderstaand formulier in. Je wordt doorgestuurd naar een beveiligde betaalpagina.
                </p>
                <form onSubmit={handleTicketSubmit} className="mt-6 space-y-5">
                  <div>
                    <label htmlFor="t-name" className="block text-sm font-medium text-gray-700">Naam</label>
                    <input id="t-name" type="text" required value={ticketForm.name}
                      onChange={(e) => setTicketForm({ ...ticketForm, name: e.target.value })}
                      className={inputClass} placeholder="Jouw volledige naam" />
                  </div>
                  <div>
                    <label htmlFor="t-email" className="block text-sm font-medium text-gray-700">E-mailadres</label>
                    <input id="t-email" type="email" required value={ticketForm.email}
                      onChange={(e) => setTicketForm({ ...ticketForm, email: e.target.value })}
                      className={inputClass} placeholder="jouw@email.be" />
                  </div>
                  <div>
                    <label htmlFor="t-qty" className="block text-sm font-medium text-gray-700">Aantal tickets</label>
                    <select id="t-qty" value={ticketForm.quantity}
                      onChange={(e) => setTicketForm({ ...ticketForm, quantity: Number(e.target.value) })}
                      className={inputClass}>
                      {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10].map((n) => (
                        <option key={n} value={n}>{n} {n === 1 ? 'ticket' : 'tickets'} — €{n * 25}</option>
                      ))}
                    </select>
                  </div>
                  <div className="flex items-start gap-3">
                    <input id="t-terms" type="checkbox" checked={ticketForm.acceptTerms}
                      onChange={(e) => setTicketForm({ ...ticketForm, acceptTerms: e.target.checked })}
                      className="mt-0.5 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500" />
                    <label htmlFor="t-terms" className="text-sm text-gray-600">
                      Ik ga akkoord met de{' '}
                      <button type="button" onClick={() => setShowTerms(true)}
                        className="font-medium text-primary-600 underline hover:text-primary-700">
                        algemene voorwaarden
                      </button>
                      {' '}en het privacybeleid.
                    </label>
                  </div>
                  {ticketError && <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{ticketError}</div>}
                  <button type="submit" disabled={ticketLoading}
                    className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-50">
                    {ticketLoading ? <Spinner /> : `Bestel ${ticketForm.quantity} ${ticketForm.quantity === 1 ? 'ticket' : 'tickets'} — €${ticketForm.quantity * 25}`}
                  </button>
                </form>
              </div>
            )}

            {/* Sponsor formulier */}
            {activeTab === 'sponsor' && (
              <div className="mt-4 rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100">
                <h2 className="text-2xl font-bold text-gray-900">Sponsoraanvraag</h2>
                <p className="mt-2 text-sm text-gray-500">
                  Kies een pakket hieronder en vul je gegevens in. We nemen zo snel mogelijk contact met je op.
                </p>
                {sponsorSuccess ? (
                  <div className="mt-6 rounded-xl bg-green-50 p-6 text-center">
                    <p className="text-3xl">🎉</p>
                    <p className="mt-3 font-semibold text-green-800">Aanvraag ontvangen!</p>
                    <p className="mt-1 text-sm text-green-700">
                      Bedankt voor je interesse. We nemen zo snel mogelijk contact met je op.
                    </p>
                  </div>
                ) : (
                  <form onSubmit={handleSponsorSubmit} className="mt-6 space-y-5">
                    <div>
                      <label className="block text-sm font-medium text-gray-700">Sponsorpakket</label>
                      <div className="mt-2 grid grid-cols-3 gap-2">
                        {sponsorPackages.map((pkg) => (
                          <button key={pkg.id} type="button"
                            onClick={() => setSponsorForm({ ...sponsorForm, package: pkg.id })}
                            className={`rounded-lg border-2 p-3 text-center text-sm font-semibold transition-all ${sponsorForm.package === pkg.id ? `${pkg.color.border} ${pkg.color.bg} ring-2 ${pkg.color.ring}` : 'border-gray-200 hover:border-gray-300'}`}>
                            <span className="block text-xl">{pkg.emoji}</span>
                            <span className="block mt-1">{pkg.label}</span>
                            <span className="block text-xs text-gray-500">€{pkg.price}</span>
                          </button>
                        ))}
                      </div>
                    </div>
                    <div className="grid gap-4 sm:grid-cols-2">
                      <div>
                        <label htmlFor="s-company" className="block text-sm font-medium text-gray-700">Bedrijfsnaam</label>
                        <input id="s-company" type="text" required value={sponsorForm.companyName}
                          onChange={(e) => setSponsorForm({ ...sponsorForm, companyName: e.target.value })}
                          className={inputClass} placeholder="Jouw bedrijfsnaam" />
                      </div>
                      <div>
                        <label htmlFor="s-contact" className="block text-sm font-medium text-gray-700">Contactpersoon</label>
                        <input id="s-contact" type="text" required value={sponsorForm.contactName}
                          onChange={(e) => setSponsorForm({ ...sponsorForm, contactName: e.target.value })}
                          className={inputClass} placeholder="Voor- en achternaam" />
                      </div>
                    </div>
                    <div className="grid gap-4 sm:grid-cols-2">
                      <div>
                        <label htmlFor="s-email" className="block text-sm font-medium text-gray-700">E-mailadres</label>
                        <input id="s-email" type="email" required value={sponsorForm.email}
                          onChange={(e) => setSponsorForm({ ...sponsorForm, email: e.target.value })}
                          className={inputClass} placeholder="info@bedrijf.be" />
                      </div>
                      <div>
                        <label htmlFor="s-phone" className="block text-sm font-medium text-gray-700">Telefoonnummer <span className="text-gray-400">(optioneel)</span></label>
                        <input id="s-phone" type="tel" value={sponsorForm.phone}
                          onChange={(e) => setSponsorForm({ ...sponsorForm, phone: e.target.value })}
                          className={inputClass} placeholder="+32 4xx xx xx xx" />
                      </div>
                    </div>
                    <div>
                      <label htmlFor="s-msg" className="block text-sm font-medium text-gray-700">Bericht <span className="text-gray-400">(optioneel)</span></label>
                      <textarea id="s-msg" rows={3} value={sponsorForm.message}
                        onChange={(e) => setSponsorForm({ ...sponsorForm, message: e.target.value })}
                        className={inputClass} placeholder="Eventuele opmerkingen of vragen..." />
                    </div>
                    <div className="flex items-start gap-3">
                      <input id="s-terms" type="checkbox" checked={sponsorForm.acceptTerms}
                        onChange={(e) => setSponsorForm({ ...sponsorForm, acceptTerms: e.target.checked })}
                        className="mt-0.5 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500" />
                      <label htmlFor="s-terms" className="text-sm text-gray-600">
                        Ik ga akkoord met de{' '}
                        <button type="button" onClick={() => setShowTerms(true)}
                          className="font-medium text-primary-600 underline hover:text-primary-700">
                          algemene voorwaarden
                        </button>.
                      </label>
                    </div>
                    {sponsorError && <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{sponsorError}</div>}
                    <button type="submit" disabled={sponsorLoading}
                      className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-50">
                      {sponsorLoading ? <Spinner /> : 'Aanvraag versturen'}
                    </button>
                  </form>
                )}
              </div>
            )}
          </div>
        </div>
      </Section>

      {/* Sponsorpakketten overzicht */}
      <div className="bg-gray-50">
        <Section>
          <div className="text-center">
            <h2 className="section-title">Onze sponsorpakketten</h2>
            <p className="section-subtitle mx-auto max-w-2xl">
              Steun het Bal Parental en maak reclame voor jouw zaak! Kies het pakket dat bij je past.
            </p>
          </div>

          <div className="mt-12 grid gap-8 sm:grid-cols-3">
            {sponsorPackages.map((pkg) => (
              <div
                key={pkg.id}
                className={`relative flex flex-col rounded-2xl border-2 bg-white p-6 shadow-lg ${pkg.color.border}`}
              >
                {pkg.popular && (
                  <div className="absolute -top-3.5 left-1/2 -translate-x-1/2">
                    <span className="rounded-full bg-yellow-400 px-4 py-1 text-xs font-bold text-yellow-900 shadow">
                      ⭐ Meest gekozen
                    </span>
                  </div>
                )}

                <div className="text-center">
                  <span className="text-5xl">{pkg.emoji}</span>
                  <h3 className="mt-3 font-display text-2xl font-bold text-gray-900">
                    Pakket {pkg.label}
                  </h3>
                  <div className="mt-2">
                    <span className="text-3xl font-extrabold text-gray-900">€{pkg.price}</span>
                  </div>
                  <span className={`mt-2 inline-block rounded-full px-3 py-1 text-xs font-semibold ${pkg.color.badge}`}>
                    {pkg.tickets} tickets inbegrepen
                  </span>
                </div>

                <ul className="mt-6 flex-1 space-y-3">
                  {pkg.perks.map((perk) => (
                    <li key={perk} className="flex items-start gap-2 text-sm text-gray-600">
                      <svg className="mt-0.5 h-4 w-4 flex-shrink-0 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                      </svg>
                      {perk}
                    </li>
                  ))}
                </ul>

                <button
                  type="button"
                  onClick={() => { setActiveTab('sponsor'); setSponsorForm((f) => ({ ...f, package: pkg.id })); window.scrollTo({ top: 0, behavior: 'smooth' }); }}
                  className={`mt-8 w-full rounded-lg py-3 text-sm font-semibold transition-all ${pkg.color.button}`}
                >
                  Kies {pkg.label}
                </button>
              </div>
            ))}
          </div>

          <p className="mt-8 text-center text-sm text-gray-400">
            Heb je een vraag of wil je een pakket op maat?{' '}
            <a href="mailto:oudercomitepittem@gmail.com" className="text-primary-600 underline hover:text-primary-700">
              Contacteer ons
            </a>.
          </p>
        </Section>
      </div>
    </>
  );
}

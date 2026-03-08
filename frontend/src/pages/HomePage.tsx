import { Link } from 'react-router-dom';
import Section from '../components/Section';

export default function HomePage() {
  return (
    <>
      {/* Hero */}
      <div className="relative overflow-hidden bg-gradient-to-br from-primary-400 via-primary-500 to-primary-700">
        <div className="absolute -left-20 -top-20 h-72 w-72 rounded-full bg-white/10" />
        <div className="absolute -bottom-16 -right-16 h-56 w-56 rounded-full bg-white/10" />
        <div className="absolute left-1/2 top-1/3 h-40 w-40 -translate-x-1/2 rounded-full bg-white/5" />

        <div className="relative mx-auto max-w-7xl px-4 py-24 sm:px-6 sm:py-32 lg:px-8">
          <div className="max-w-3xl">
            <p className="text-sm font-semibold uppercase tracking-widest text-primary-100">
              Basisschool PIT! Pittem
            </p>
            <h1 className="mt-3 font-display text-5xl font-extrabold tracking-tight text-white sm:text-6xl lg:text-7xl">
              Oudercomité
              <br />
              <span className="text-primary-100">met Pit!</span>
            </h1>
            <p className="mt-6 max-w-xl text-lg leading-relaxed text-white/90">
              Samen maken we er iets moois van voor onze kinderen. Ontdek wie we zijn,
              wat we doen en hoe jij kan meedoen!
            </p>
            <div className="mt-10 flex flex-wrap gap-4">
              <Link to="/activiteiten" className="btn-primary">
                Onze activiteiten
              </Link>
              <Link to="/contact" className="btn-secondary !border-white !text-white hover:!bg-white/10">
                Neem contact op
              </Link>
            </div>
          </div>
        </div>
      </div>

      {/* Kort overzicht */}
      <Section>
        <div className="text-center">
          <h2 className="section-title">Wat maakt ons bijzonder?</h2>
          <p className="section-subtitle mx-auto max-w-2xl">
            Het oudercomité van de gemeentelijke basisschool van Pittem zet zich in
            voor leuke activiteiten en een hechte schoolgemeenschap.
          </p>
        </div>

        <div className="mt-14 grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
          {/* Card 1 */}
          <div className="card text-center">
            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-amber-100 text-amber-600">
              <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.59 14.37a6 6 0 01-5.84 7.38v-4.8m5.84-2.58a14.98 14.98 0 006.16-12.12A14.98 14.98 0 009.63 8.37m5.96 6a14.926 14.926 0 01-5.84 2.58m0 0a14.926 14.926 0 01-5.96-6 14.98 14.98 0 006.16-12.12" />
              </svg>
            </div>
            <h3 className="mt-5 text-lg font-semibold text-gray-900">Activiteiten</h3>
            <p className="mt-2 text-sm leading-relaxed text-gray-500">
              Van het Bal Parental tot de koekjesverkoop — we organiseren doorheen het jaar diverse leuke activiteiten.
            </p>
          </div>

          {/* Card 2 */}
          <div className="card text-center">
            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-accent-100 text-accent-600">
              <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.479m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
              </svg>
            </div>
            <h3 className="mt-5 text-lg font-semibold text-gray-900">Gemeenschap</h3>
            <p className="mt-2 text-sm leading-relaxed text-gray-500">
              We brengen ouders, leerkrachten en kinderen samen. Iedereen is welkom om mee te helpen!
            </p>
          </div>

          {/* Card 3 */}
          <div className="card text-center">
            <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-secondary-100 text-secondary-600">
              <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 8.25c0-2.485-2.099-4.5-4.688-4.5-1.935 0-3.597 1.126-4.312 2.733-.715-1.607-2.377-2.733-4.313-2.733C5.1 3.75 3 5.765 3 8.25c0 7.22 9 12 9 12s9-4.78 9-12z" />
              </svg>
            </div>
            <h3 className="mt-5 text-lg font-semibold text-gray-900">Voor de kinderen</h3>
            <p className="mt-2 text-sm leading-relaxed text-gray-500">
              Alles wat we doen, doen we voor de kinderen van onze school. De opbrengsten gaan integraal naar leuke projecten.
            </p>
          </div>
        </div>
      </Section>

      {/* CTA */}
      <div className="bg-gray-50">
        <Section>
          <div className="flex flex-col items-center gap-6 text-center lg:flex-row lg:text-left">
            <div className="flex-1">
              <h2 className="section-title">Wil je op de hoogte blijven?</h2>
              <p className="section-subtitle">
                Volg ons op Facebook of neem contact met ons op voor meer informatie over onze activiteiten.
              </p>
            </div>
            <div className="flex flex-shrink-0 gap-4">
              <a
                href="https://www.facebook.com/oudercomitemetpit"
                target="_blank"
                rel="noopener noreferrer"
                className="btn-primary"
              >
                Volg ons op Facebook
              </a>
              <Link to="/contact" className="btn-secondary">
                Contact
              </Link>
            </div>
          </div>
        </Section>
      </div>
    </>
  );
}

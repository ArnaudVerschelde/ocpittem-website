import { Link } from 'react-router-dom';
import Hero from '../components/Hero';
import Section from '../components/Section';

const events = [
  {
    title: 'Bal Parental 2026',
    date: 'Datum wordt later bekendgemaakt',
    description:
      'Ons jaarlijks ouderbal! Een fantastische avond met muziek, drank en gezelligheid. Tickets zijn binnenkort online beschikbaar.',
    cta: { label: 'Tickets & info', to: '/bal-parental' },
    color: 'primary',
  },
  {
    title: 'Apero op de speelplaats',
    date: 'Zomer 2026',
    description:
        'Een gezellige dag waarbij ouders, kinderen en leerkrachten samenkomen op de speelplaats voor een hapje en een drankje. Ontspannen en gezellig!',
    cta: null,
    color: 'accent',
  },
  {
    title: 'Koekjesverkoop 2026',
    date: 'Najaar 2026',
    description:
        'Onze jaarlijkse koekjesverkoop. Bestel heerlijke koekjes en steun daarmee de school!',
    cta: null,
    color: 'accent',
  },
];

function getColorClasses(color: string) {
    if (color === 'accent')
        return {
            bg: 'bg-amber-50',
            border: 'border-amber-200',
            badge: 'bg-amber-100 text-amber-700',
        };

    return {
        bg: 'bg-primary-50',
        border: 'border-primary-200',
        badge: 'bg-primary-100 text-primary-700',
    };
}

export default function ActiviteitenPage() {
  return (
    <>
      <Hero
        title="Activiteiten"
        subtitle="Ontdek onze geplande activiteiten en evenementen. Er is altijd iets leuks op komst!"
        backgroundClass="bg-gradient-to-br from-accent-500 via-accent-600 to-accent-800"
      />

      <Section>
        <div className="space-y-8">
          {events.map((event) => {
            const colors = getColorClasses(event.color);
            return (
              <div
                key={event.title}
                className={`rounded-2xl border ${colors.border} ${colors.bg} p-6 sm:p-8`}
              >
                <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <span className={`inline-block rounded-full px-3 py-1 text-xs font-semibold ${colors.badge}`}>
                      {event.date}
                    </span>
                    <h3 className="mt-3 text-xl font-bold text-gray-900 sm:text-2xl">
                      {event.title}
                    </h3>
                    <p className="mt-2 max-w-2xl text-gray-600 leading-relaxed">{event.description}</p>
                  </div>
                  {event.cta && (
                    <Link
                      to={event.cta.to}
                      className="btn-primary mt-2 flex-shrink-0 sm:mt-0"
                    >
                      {event.cta.label}
                    </Link>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </Section>

      {/* Sponsoring CTA */}
      <div className="bg-gray-50">
        <Section>
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="section-title">Sponsor worden?</h2>
            <p className="section-subtitle">
              Wil je als bedrijf of zelfstandige onze activiteiten sponsoren? Neem contact met ons
              op en we bekijken samen de mogelijkheden.
            </p>
            <div className="mt-8">
              <Link to="/contact" className="btn-primary">
                Contacteer ons
              </Link>
            </div>
          </div>
        </Section>
      </div>
    </>
  );
}

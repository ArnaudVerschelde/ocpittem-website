import Hero from '../components/Hero';
import Section from '../components/Section';

const activities = [
  {
    icon: '🍪',
    title: 'Koekjesverkoop',
    description:
      'Elk jaar organiseren we een grote koekjesverkoop. De opbrengst gaat integraal naar leuke projecten voor de kinderen van de school.',
  },
  {
    icon: '🎉',
    title: 'Bal Parental',
    description:
      'Ons jaarlijks feest voor ouders en sympathisanten. Een avond vol muziek, gezelligheid en plezier!',
  },
  {
    icon: '🥂',
    title: 'Apero op de speelplaats',
    description:
      'Een gezellige dag waarbij ouders, kinderen en leerkrachten samenkomen op de speelplaats voor een hapje en een drankje. Ontspannen en gezellig!',
  },
  {
    icon: '📚',
    title: 'Ondersteuning schoolprojecten',
    description:
      'De opbrengsten van onze activiteiten worden geïnvesteerd in materiaal, uitstappen en leuke extra\'s voor alle kinderen.',
  },
  {
    icon: '🤝',
    title: 'Verbinding ouders & school',
    description:
      'We vormen de brug tussen ouders en de school. Zo zorgen we samen voor een warme, betrokken schoolgemeenschap.',
  },
  {
    icon: '🎪',
    title: 'Extra activiteiten',
    description:
      'Doorheen het jaar verrassen we met extra leuke initiatieven zoals een foodtruck, sportieve activiteiten en nog veel meer verrassingen.',
  },
];

export default function WatDoenWePage() {
  return (
    <>
      <Hero
        title="Wat doen we?"
        subtitle="Het oudercomité organiseert doorheen het jaar diverse activiteiten om de schoolgemeenschap te versterken en extra middelen in te zamelen voor onze kinderen."
      />

      <Section>
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
          {activities.map((activity) => (
            <div key={activity.title} className="card">
              <span className="text-4xl">{activity.icon}</span>
              <h3 className="mt-4 text-lg font-semibold text-gray-900">{activity.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-gray-500">{activity.description}</p>
            </div>
          ))}
        </div>
      </Section>

      {/* Extra info */}
      <div className="bg-primary-50">
        <Section>
          <div className="mx-auto max-w-3xl text-center">
            <h2 className="section-title">Waar gaat het geld naartoe?</h2>
            <p className="section-subtitle">
              Alle opbrengsten van onze activiteiten gaan rechtstreeks naar projecten voor de kinderen
              van de basisschool PIT! van Pittem. Denk aan nieuw speelplaatsmateriaal,
              leuke schooluitstappen, boeken voor de bib, en nog veel meer.
            </p>
          </div>
        </Section>
      </div>
    </>
  );
}

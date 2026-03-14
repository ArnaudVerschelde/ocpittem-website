import { Link } from 'react-router-dom';
import Hero from '../components/Hero';
import Section from '../components/Section';

type PersonBase = {
    name: string;
    initials?: string;
};

type BoardMember = PersonBase & {
    role: string;
    color: string; // tailwind classes voor de avatar (bg/text)
};

type Member = PersonBase & {
    description: string;
};

const board: BoardMember[] = [
    { name: 'Jolien De Neve', role: 'Voorzitter', initials: 'JDN', color: 'bg-primary-100 text-primary-700' },
    { name: 'Matthias Lefebvre', role: 'Ondervoorzitter', initials: 'ML', color: 'bg-primary-100 text-primary-700' },
    { name: 'Steven Truyaert', role: 'Secretaris', initials: 'ST', color: 'bg-primary-100 text-primary-700' },
    { name: 'Arnaud Verschelde', role: 'Penningmeester', initials: 'AV', color: 'bg-primary-100 text-primary-700' },
    { name: 'Nynke Colpaert', role: 'Bestuurslid', initials: 'NC', color: 'bg-primary-100 text-primary-700' },
    { name: 'Petra Vansteenhuyse', role: 'Bestuurslid', initials: 'PV', color: 'bg-primary-100 text-primary-700' },
];

const members: Member[] = [
    { name: 'Jolien De Neve', initials: 'JDN', description: 'mama van Nya en Zyas' },
    { name: 'Matthias Lefebvre', initials: 'ML', description: 'papa van Noémie en Alizée' },
    { name: 'Nynke Colpaert', initials: 'NC', description: 'mama van Anna, Helena en Marie' },
    { name: 'Petra Vansteenhuyse', initials: 'PV', description: 'mama van Marthe' },
    { name: 'Steven Truyaert', initials: 'ST', description: 'papa van Jasna en Otis' },
    { name: 'Annelies Rotsaert', initials: 'AR', description: 'mama van Noémie en Alizée' },
    { name: 'Arnaud Verschelde', initials: 'AV', description: 'papa van Henri en Alixe' },
    { name: 'Charlot Bossuyt', initials: 'CB', description: 'mama van Nya en Zyas' },
    { name: 'Cindy De Clercq', initials: 'CDC', description: 'mama van Maure' },
    { name: 'Delphine Lafaut', initials: 'DL', description: 'mama van Odiel' },
    { name: 'Dempsey Van Renterghem', initials: 'DVR', description: 'papa van Vince, Fons en Dré' },
    { name: 'Dominique Vermeersch', initials: 'DV', description: 'papa van Maure' },
    { name: 'Eline Desmet', initials: 'ED', description: 'mama van Louise en Victor' },
    { name: 'Elise De Jaeger', initials: 'EDJ', description: 'mama van Wies en Iza' },
    { name: 'Ellen Dewitte', initials: 'ED', description: 'mama van Warre en Jerome' },
    { name: 'Febe De Meyer', initials: 'FDM', description: 'mama van Léon' },
    { name: 'Jeroen Lafaut', initials: 'JL', description: 'papa van Léonie en Lucas' },
    { name: 'Jolien Wyballie', initials: 'JW', description: 'mama van Jasna en Otis' },
    { name: 'Kristof Van Dorpe', initials: 'KVD', description: 'papa van Odette en Maurice' },
    { name: 'Leen Pauwelyn', initials: 'LP', description: 'mama van Anna en Arthur' },
    { name: 'Marijn Tuytens', initials: 'MT', description: 'papa van Renée' },
    { name: 'Marisja Lapeire', initials: 'ML', description: 'mama van Amber' },
    { name: 'Melissa Naert', initials: 'MN', description: 'mama van Odette en Maurice' },
    { name: 'Nathalie Delaere', initials: 'ND', description: 'mama van Emile en Emma' },
    { name: 'Nele Desseyn', initials: 'ND', description: 'mama van Lisa en Mats' },
    { name: 'Nele Spriet', initials: 'NS', description: 'mama van Bas en Wies' },
    { name: 'Nid Pb', initials: 'NP', description: 'mama van Jeff' },
    { name: 'Pieter Van Coillie', initials: 'PVC', description: 'papa van Warre en Mila' },
    { name: 'Ruben Tavernier', initials: 'RT', description: 'papa van Arno, Nand en Gust' },
    { name: 'Sharon Verhenne', initials: 'SV', description: 'mama van Axel en Roxy' },
    { name: 'Steve Deloof', initials: 'SD', description: 'papa van Juliette' },
    { name: 'Stien Rothier', initials: 'SR', description: 'mama van Olivia' },
    { name: 'Tatjana Muylle', initials: 'TM', description: 'mama van Fayth en Luan' },
    { name: 'Thomas De Cuypere', initials: 'TDC', description: 'papa van Matisse' },
];

// Slimme fallback: pakt tot 3 initialen en negeert tussenwoorden zoals "de", "van", ...
function getInitialsFromName(name: string) {
    const parts = name.trim().split(/\s+/);
    const stop = new Set(['de', 'den', 'der', 'van', 'von', 'te', 'ten', 'ter', 'da', 'di', 'la', 'le']);
    const filtered = parts.filter((p) => !stop.has(p.toLowerCase()));
    return filtered
        .slice(0, 3)
        .map((p) => p[0])
        .join('')
        .toUpperCase();
}

function resolveInitials(person: PersonBase) {
    const initials = (person.initials ?? '').trim();
    if (initials) return initials.toUpperCase();
    return getInitialsFromName(person.name);
}

function AvatarInitials({
    initials,
    size = 40,
    className = '',
    variant = 'small',
}: {
    initials: string;
    size?: number;
    className?: string;
    variant?: 'small' | 'large';
}) {
    const fontClass =
        variant === 'large'
            ? (initials.length >= 3 ? 'text-base' : 'text-lg')
            : (initials.length >= 3 ? 'text-[10px]' : 'text-xs');

    return (
        <div
            className={`flex items-center justify-center rounded-full font-bold ${fontClass} ${className}`}
            style={{ width: size, height: size }}
            aria-label={`Initialen ${initials}`}
        >
            {initials}
        </div>
    );
}

export default function WieZijnWePage() {
    return (
        <>
            <Hero
                title="Wie zijn we?"
                subtitle="Een enthousiaste groep ouders die zich vrijwillig inzet voor de kinderen van de basisschool PIT! van Pittem."
                backgroundClass="bg-gradient-to-br from-secondary-500 via-secondary-600 to-secondary-800"
            />

            {/* Ons verhaal */}
            <Section>
                <div className="mx-auto max-w-3xl">
                    <h2 className="section-title">Ons verhaal</h2>
                    <div className="mt-6 space-y-4 text-gray-600 leading-relaxed">
                        <p>
                            Het <strong>Oudercomité met PIT!</strong> is het oudercomité van de basisschool PIT! van Pittem.
                            We zijn een groep enthousiaste ouders die zich vrijwillig
                            inzetten om het schoolleven van onze kinderen nog leuker te maken.
                        </p>
                        <p>
                            We organiseren doorheen het jaar verschillende activiteiten: van de jaarlijkse
                            koekjesverkoop tot het Bal Parental. De opbrengsten gaan integraal naar projecten
                            voor de kinderen: nieuw speelplaatsmateriaal, schooluitstappen, boeken en nog
                            veel meer.
                        </p>
                        <p>
                            Iedereen is welkom om mee te helpen — groot of klein, af en toe of regelmatig.
                            Samen maken we er iets moois van!
                        </p>
                    </div>
                </div>
            </Section>

            {/* Bestuur */}
            <div className="bg-gray-50">
                <Section>
                    <div className="text-center">
                        <h2 className="section-title">Het bestuur</h2>
                        <p className="section-subtitle mx-auto max-w-xl">
                            Ons bestuur zorgt voor de dagelijkse werking van het oudercomité.
                        </p>
                    </div>

                    <div className="mt-12 flex flex-wrap justify-center gap-6">
                        {board.map((member) => {
                            const initials = resolveInitials(member);
                            return (
                                <div key={member.name} className="card flex w-full flex-col items-center gap-3 text-center sm:w-60">
                                    <AvatarInitials
                                        initials={initials}
                                        size={64}
                                        variant="large"
                                        className={member.color}
                                    />
                                    <div>
                                        <p className="font-semibold text-gray-900">{member.name}</p>
                                        <p className="mt-0.5 text-sm text-gray-500">{member.role}</p>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </Section>
            </div>

            {/* Alle leden */}
            <Section>
                <div className="text-center">
                    <h2 className="section-title">Het Oudercomité</h2>
                    <p className="section-subtitle mx-auto max-w-xl">
                        Alle enthousiaste ouders die onze school een warm hart toedragen.
                    </p>
                </div>

                <div className="mt-12 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                    {members.map((member) => {
                        const initials = resolveInitials(member);
                        return (
                            <div
                                key={member.name}
                                className="flex items-center gap-3 rounded-xl border border-gray-100 bg-white px-4 py-3 shadow-sm"
                            >
                                <AvatarInitials
                                    initials={initials}
                                    size={36}
                                    className="bg-primary-50 text-primary-600"
                                />
                                <div className="min-w-0">
                                    <p className="truncate text-sm font-semibold text-gray-900">{member.name}</p>
                                    <p className="truncate text-xs text-gray-400">{member.description}</p>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </Section>

            {/* Vertegenwoordigers school */}
            <div className="bg-secondary-50">
                <Section>
                    <div className="mx-auto max-w-2xl">
                        <h2 className="section-title">Vertegenwoordigers school</h2>
                        <div className="mt-8 grid gap-4 sm:grid-cols-2">
                            <div className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-secondary-100">
                                <p className="text-xs font-semibold uppercase tracking-wider text-secondary-500">Directie</p>
                                <p className="mt-2 font-semibold text-gray-900">Hanne Devisch</p>
                            </div>
                            <div className="rounded-xl bg-white p-5 shadow-sm ring-1 ring-secondary-100">
                                <p className="text-xs font-semibold uppercase tracking-wider text-secondary-500">Leerkrachten</p>
                                <p className="mt-2 font-semibold text-gray-900">Ine Vandeputte</p>
                                <p className="font-semibold text-gray-900">Nele De Brabandere</p>
                            </div>
                        </div>
                    </div>
                </Section>
            </div>

            {/* Helpende Handen */}
            <Section>
                <div className="mx-auto max-w-3xl">
                    <div className="rounded-2xl bg-primary-50 p-8 sm:p-10">
                        <div className="flex items-start gap-4">
                            <span className="text-4xl">🤝</span>
                            <div>
                                <h2 className="text-2xl font-bold text-gray-900">De Helpende Handen</h2>
                                <p className="mt-4 leading-relaxed text-gray-600">
                                    Helpende Handen zijn mensen die wij ongelooflijk dankbaar zijn en regelmatig
                                    broodnodig hebben. Deze mensen zijn ouders van kinderen die niet meer op onze
                                    school zitten, of ouders die wel graag actief meehelpen aan de activiteiten maar
                                    liever niet deelnemen aan de vergaderingen van het oudercomité.
                                </p>
                                <p className="mt-3 leading-relaxed text-gray-600">
                                    Wij zijn altijd op zoek naar bijkomende Helpende Handen. Mogen we op u rekenen?
                                    Laat ons dan iets weten door contact op te nemen.
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
            </Section>

            {/* Interesse? */}
            <div className="bg-gray-50">
                <Section>
                    <div className="mx-auto max-w-xl text-center">
                        <span className="text-4xl">✉️</span>
                        <h2 className="section-title mt-4">Interesse?</h2>
                        <p className="section-subtitle">
                            Wil je ook deel uitmaken van het oudercomité of de Helpende Handen? Stuur ons een
                            mailtje of spreek één van onze leden aan — we heten je graag welkom!
                        </p>
                        <div className="mt-8 flex flex-col items-center gap-4 sm:flex-row sm:justify-center">
                            <a href="mailto:oudercomitepittem@gmail.com" className="btn-primary">
                                oudercomitepittem@gmail.com
                            </a>
                            <Link to="/contact" className="btn-secondary">
                                Contactformulier
                            </Link>
                        </div>
                    </div>
                </Section>
            </div>
        </>
    );
}
import { useEffect, useState, FormEvent, useCallback } from 'react';
import { Link } from 'react-router-dom';
import Section from '../components/Section';
import balPoster from '../assets/BalParental2026Poster.jpeg';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const contactEmail = 'balparental@ocpittem.be';

// Configuratie: deadlines (pas deze datums aan indien nodig)
const SPONSOR_DEADLINE    = new Date('2026-06-14T23:59:59');
const ETEN_PARTY_DEADLINE = new Date('2026-06-14T23:59:59');
const TICKET_DEADLINE     = new Date('2026-07-19T23:59:59');

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

const sponsorPackages = [
    {
        id: 'brons',
        label: 'Brons',
        emoji: '🥉',
        price: 100,
        color: {
            border: 'border-amber-400',
            bg: 'bg-amber-50',
            badge: 'bg-amber-100 text-amber-800',
            button: 'bg-amber-500 hover:bg-amber-600 text-white',
            ring: 'ring-amber-300',
        },
        tickets: 0,
        features: [
            { text: 'Logo op de schermen', included: true },
            { text: 'Vermelding op sociale media', included: true },
            { text: 'Logo gelinkt aan ons evenement', included: false },
            { text: 'Projectie op de avond', included: false },
            { text: 'Verlengde projectie (+2 sec)', included: false },
            { text: 'Tickets & menu inbegrepen', included: false },
            { text: 'Drankkaarten inbegrepen', included: false },
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
        tickets: 2,
        features: [
            { text: 'Logo op de schermen', included: true },
            { text: 'Vermelding op sociale media', included: true },
            { text: 'Logo gelinkt aan ons evenement', included: true },
            { text: 'Projectie op de avond', included: true },
            { text: 'Verlengde projectie (+2 sec)', included: false },
            { text: '2 Eten & Party tickets', included: true },
            { text: '€40 drankkaarten', included: true },
        ],
    },
    {
        id: 'goud',
        label: 'Goud',
        emoji: '🥇',
        price: 500,
        popular: true,
        color: {
            border: 'border-yellow-400',
            bg: 'bg-yellow-50',
            badge: 'bg-yellow-100 text-yellow-800',
            button: 'bg-yellow-500 hover:bg-yellow-600 text-white',
            ring: 'ring-yellow-300',
        },
        tickets: 4,
        features: [
            { text: 'Logo op de schermen', included: true },
            { text: 'Vermelding op sociale media', included: true },
            { text: 'Logo gelinkt aan ons evenement', included: true },
            { text: 'Projectie op de avond', included: true },
            { text: 'Verlengde projectie (+2 sec)', included: true },
            { text: '4 Eten & Party tickets', included: true },
            { text: '€80 drankkaarten', included: true },
        ],
    },
];

// ---------------------------------------------------------------------------
// Terms Modal
// ---------------------------------------------------------------------------


// ---------------------------------------------------------------------------
// Countdown helper
// ---------------------------------------------------------------------------

interface TimeLeft { days: number; hours: number; minutes: number; seconds: number; expired: boolean }

function useCountdown(deadline: Date): TimeLeft {
    const calc = useCallback(() => {
        const diff = deadline.getTime() - Date.now();
        if (diff <= 0) return { days: 0, hours: 0, minutes: 0, seconds: 0, expired: true };
        return {
            days:    Math.floor(diff / 86400000),
            hours:   Math.floor((diff % 86400000) / 3600000),
            minutes: Math.floor((diff % 3600000)  / 60000),
            seconds: Math.floor((diff % 60000)    / 1000),
            expired: false,
        };
    }, [deadline]);

    const [timeLeft, setTimeLeft] = useState<TimeLeft>(calc);

    useEffect(() => {
        const id = setInterval(() => setTimeLeft(calc()), 1000);
        return () => clearInterval(id);
    }, [calc]);

    return timeLeft;
}

function CountdownBanner({ deadline, label }: { deadline: Date; label: string }) {
    const t = useCountdown(deadline);

    if (t.expired) {
        return (
            <div className="mb-4 rounded-lg bg-red-50 px-4 py-3 text-center text-sm font-semibold text-red-700">
                De verkoop van {label} is gesloten.
            </div>
        );
    }

    const units = [
        { value: t.days,    label: 'dagen' },
        { value: t.hours,   label: 'uur' },
        { value: t.minutes, label: 'min' },
        { value: t.seconds, label: 'sec' },
    ];

    return (
        <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
            <p className="mb-2 text-center text-xs font-semibold text-amber-800">
                {`⏳ Nog beschikbaar tot ${deadline.toLocaleDateString('nl-BE', { day: 'numeric', month: 'long', year: 'numeric' })}`}
            </p>
            <div className="flex justify-center gap-3">
                {units.map((u) => (
                    <div key={u.label} className="flex flex-col items-center">
                        <span className="text-xl font-extrabold tabular-nums text-amber-900">
                            {String(u.value).padStart(2, '0')}
                        </span>
                        <span className="text-xs text-amber-600">{u.label}</span>
                    </div>
                ))}
            </div>
        </div>
    );
}

function TermsModal({ onClose }: { onClose: () => void }) {
    useEffect(() => {
        const handler = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onClose();
        };

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
            onClick={(e) => {
                if (e.target === e.currentTarget) onClose();
            }}
        >
            <div className="relative max-h-[85vh] w-full max-w-2xl overflow-y-auto rounded-2xl bg-white shadow-2xl">
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

                <div className="space-y-5 px-6 py-5 text-sm leading-relaxed text-gray-600">
                    <p className="rounded-lg bg-blue-50 px-4 py-3 text-xs text-blue-700">
                        Door een bestelling te plaatsen, verklaart de koper kennis te hebben genomen van deze
                        algemene voorwaarden en ermee akkoord te gaan.
                    </p>

                    <div>
                        <h3 className="font-semibold text-gray-900">1. Organisatie en toepassingsgebied</h3>
                        <p className="mt-1">
                            Deze algemene voorwaarden zijn van toepassing op de online aankoop van tickets en
                            sponsorpakketten voor het evenement <strong>Bal Parental</strong>, georganiseerd door
                            <strong> Oudercomité met Pit</strong>. Ze gelden voor alle bestellingen die via de
                            website of een gekoppelde online betaalpagina worden geplaatst en betaald.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">2. Bestelling en totstandkoming van de overeenkomst</h3>
                        <p className="mt-1">
                            Een bestelling is definitief zodra de online betaling succesvol is voltooid en de koper
                            een bevestigingsmail heeft ontvangen. De koper is verantwoordelijk voor het correct
                            invullen van de gevraagde gegevens, waaronder naam en e-mailadres.
                        </p>
                        <p className="mt-2">
                            Indien één persoon meerdere tickets bestelt, staat die persoon ervoor in dat de
                            medebezoekers op de hoogte worden gebracht van deze algemene voorwaarden en van de
                            praktische info die met de bestelling verband houdt.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">3. Tickets</h3>
                        <p className="mt-1">
                            Na aankoop ontvangt de koper een bevestigingsmail met de bestelde ticket(s), in
                            elektronische vorm of als PDF-bijlage. Elk ticket bevat een unieke QR-code en is slechts
                            één keer geldig. Een ticket dat reeds werd gescand of ongeldig blijkt, geeft geen recht
                            op toegang.
                        </p>
                        <p className="mt-2">
                            De organisatie kan verschillende ticketformules aanbieden, waaronder onder meer een
                            toegangsticket, een Eten &amp; Party-ticket en optionele drankkaarten. De kenmerken en
                            inhoud van elk product worden vermeld op de website en/of in het bestelproces.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">4. Sponsorpakketten</h3>
                        <p className="mt-1">
                            Sponsorpakketten worden online aangeboden volgens de beschrijving op de website of in
                            het bestelproces. Een bestelling van een sponsorpakket is pas definitief na succesvolle
                            online betaling en bevestiging per e-mail door de organisatie.
                        </p>
                        <p className="mt-2">
                            Indien voor het sponsorpakket logo’s, namen, teksten of ander materiaal nodig zijn, moet
                            de sponsor die tijdig en in een bruikbaar formaat aanleveren. Laattijdige of onvolledige
                            aanlevering kan ertoe leiden dat bepaalde sponsorvermeldingen niet of slechts beperkt
                            kunnen worden uitgevoerd, zonder dat dit automatisch recht geeft op terugbetaling.
                        </p>
                        <p className="mt-2">
                            De organisatie behoudt zich het recht voor een sponsorverzoek of sponsorinhoud te weigeren
                            indien die niet verenigbaar is met het karakter, de waarden of de wettelijke verplichtingen
                            van het evenement. Indien een sponsorpakket na betaling door de organisatie wordt geweigerd,
                            wordt het betaalde bedrag teruggestort.
                        </p>
                        <p className="mt-2">
                            De sponsor verklaart dat hij beschikt over de nodige rechten op alle aangeleverde logo’s,
                            afbeeldingen, teksten en andere materialen, en vrijwaart de organisatie voor aanspraken
                            van derden.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">5. Prijzen en betaling</h3>
                        <p className="mt-1">
                            Alle prijzen worden weergegeven in euro. De prijs die op het moment van bestelling op de
                            website of betaalpagina wordt vermeld, is de prijs die van toepassing is op de bestelling.
                        </p>
                        <p className="mt-2">
                            Betaling van tickets en sponsorpakketten verloopt online via Stripe of een andere door de
                            organisatie gekozen beveiligde betaalprovider. Oudercomité met Pit bewaart geen volledige
                            betaalkaartgegevens op eigen servers.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">6. Annulering, terugbetaling en herroepingsrecht</h3>
                        <p className="mt-1">
                            Voor tickets voor het Bal Parental geldt geen herroepingsrecht van 14 dagen, aangezien
                            het gaat om een vrijetijdsactiviteit op een specifieke datum.
                        </p>
                        <p className="mt-2">
                            Gekochte tickets worden in principe niet terugbetaald of omgeruild, behalve wanneer het
                            evenement door de organisatie volledig wordt geannuleerd. In dat geval worden kopers via
                            e-mail geïnformeerd over de verdere regeling.
                        </p>
                        <p className="mt-2">
                            Indien het evenement wordt verplaatst of inhoudelijk wezenlijk wijzigt, zal de organisatie
                            de kopers en, in voorkomend geval, sponsors zo goed mogelijk informeren over de gevolgen
                            voor hun bestelling.
                        </p>
                        <p className="mt-2">
                            Voor sponsorpakketten kan annulering enkel in onderling overleg met de organisatie.
                            Reeds uitgevoerde of geproduceerde sponsorvermeldingen, reservaties of andere gemaakte
                            kosten kunnen daarbij in rekening worden gebracht.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">7. Toegang en verloop van het evenement</h3>
                        <p className="mt-1">
                            Toegang tot het evenement is enkel mogelijk met een geldig ticket. De organisatie behoudt
                            zich het recht voor personen de toegang te weigeren of te verwijderen in geval van
                            fraude, misbruik van tickets, storend gedrag, niet-naleving van veiligheidsinstructies of
                            andere omstandigheden die een veilig en ordelijk verloop van het evenement in het gedrang
                            brengen.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">8. Foto’s en video-opnames</h3>
                        <p className="mt-1">
                            Tijdens het evenement kunnen sfeerbeelden, foto’s en video-opnames worden gemaakt voor
                            verslaggeving en promotie van Bal Parental en Oudercomité met Pit, onder meer via de
                            website, sociale media en ander communicatie- of promotiemateriaal.
                        </p>
                        <p className="mt-2">
                            Door deel te nemen aan het evenement neemt de bezoeker kennis van het feit dat dergelijke
                            opnames kunnen plaatsvinden. Voor gerichte, individueel herkenbare portret- of close-up-
                            beelden wordt, waar dat vereist is, afzonderlijk toestemming gevraagd.
                        </p>
                        <p className="mt-2">
                            Bezoekers die niet herkenbaar in beeld wensen te komen, kunnen dit vooraf of ter plaatse
                            melden aan de organisatie. De organisatie zal hiermee in de mate van het redelijke
                            rekening houden.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">9. Privacy en verwerking van persoonsgegevens</h3>
                        <p className="mt-1">
                            Oudercomité met Pit verwerkt persoonsgegevens zoals naam, e-mailadres en bestelgegevens
                            voor het verwerken van bestellingen, het verzenden van tickets of bevestigingen,
                            klantencommunicatie, opvolging van sponsorpakketten en het naleven van wettelijke
                            verplichtingen. Deze verwerking gebeurt op basis van de uitvoering van de overeenkomst
                            en, waar van toepassing, om te voldoen aan wettelijke verplichtingen.
                        </p>
                        <p className="mt-2">
                            Voor de uitvoering van de bestelling kunnen gegevens worden gedeeld met verwerkers of
                            dienstverleners die daarbij noodzakelijk betrokken zijn, zoals de betaalprovider en de
                            e-mail- of ticketverzenddienst. Persoonsgegevens worden niet langer bewaard dan nodig is
                            voor deze doeleinden of zolang een wettelijke bewaartermijn dit vereist.
                        </p>
                        <p className="mt-2">
                            Betrokkenen hebben, binnen de grenzen van de toepasselijke regelgeving, recht op inzage,
                            verbetering, beperking of verwijdering van hun persoonsgegevens. Verzoeken of vragen
                            hierover kunnen worden gericht aan <strong>oudercomitepittem@ocpittem.be</strong>.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">10. Aansprakelijkheid</h3>
                        <p className="mt-1">
                            De organisatie is niet aansprakelijk voor verlies, diefstal of schade aan persoonlijke
                            bezittingen tijdens het evenement, behalve in geval van bedrog, opzettelijke fout of
                            wanneer aansprakelijkheid wettelijk niet kan worden uitgesloten.
                        </p>
                        <p className="mt-2">
                            De organisatie is evenmin aansprakelijk voor schade die voortvloeit uit foutief door de
                            koper ingegeven gegevens, overmacht of technische storingen buiten haar redelijke
                            controle.
                        </p>
                    </div>

                    <div>
                        <h3 className="font-semibold text-gray-900">11. Contact</h3>
                        <p className="mt-1">
                            Voor vragen over tickets, sponsorpakketten, persoonsgegevens of deze voorwaarden kan
                            contact worden opgenomen via <strong>{contactEmail}</strong>.
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
        <span className="flex items-center justify-center gap-2">
            <svg className="h-5 w-5 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
            Bezig...
        </span>
    );
}

// ---------------------------------------------------------------------------
// Stepper helper
// ---------------------------------------------------------------------------

function Stepper({
    value,
    onChange,
    min = 0,
    max = 20,
    disabled = false,
}: {
    value: number;
    onChange: (n: number) => void;
    min?: number;
    max?: number;
    disabled?: boolean;
}) {
    return (
        <div className="flex items-center gap-2">
            <button
                type="button"
                onClick={() => onChange(Math.max(min, value - 1))}
                disabled={disabled || value <= min}
                className="flex h-8 w-8 items-center justify-center rounded-full border border-gray-300 text-gray-700 hover:bg-gray-50 disabled:opacity-30"
            >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M5 12h14" />
                </svg>
            </button>

            <span className="w-6 text-center text-sm font-semibold tabular-nums">{value}</span>

            <button
                type="button"
                onClick={() => onChange(Math.min(max, value + 1))}
                disabled={disabled || value >= max}
                className="flex h-8 w-8 items-center justify-center rounded-full border border-gray-300 text-gray-700 hover:bg-gray-50 disabled:opacity-30"
            >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 5v14M5 12h14" />
                </svg>
            </button>
        </div>
    );
}

// ---------------------------------------------------------------------------
// Belgian enterprise number validation
// ---------------------------------------------------------------------------

function validateEnterpriseNumber(value: string): boolean {
    const digits = value.replace(/[^0-9]/g, '');

    if (digits.length !== 10) return false;
    if (digits[0] !== '0' && digits[0] !== '1') return false;

    const prefix = parseInt(digits.substring(0, 8), 10);
    const check = parseInt(digits.substring(8), 10);
    const expected = prefix % 97 === 0 ? 97 : 97 - (prefix % 97);

    return check === expected;
}

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface TicketForm {
    name: string;
    email: string;
    toegangsticketCount: number;
    etenPartyCount: number;
    vegetarischCount: number;
    drankkaart10Count: number;
    drankkaart20Count: number;
    acceptTerms: boolean;
}

interface SponsorForm {
    companyName: string;
    contactName: string;
    enterpriseNumber: string;
    street: string;
    houseNumber: string;
    postalCode: string;
    city: string;
    email: string;
    phone: string;
    package: string;
    extraEtenPartyCount: number;
    extraVegetarischCount: number;
    extraDrankkaart20Count: number;
    includedVegetarischCount: number;
    sponsorAttends: boolean;
    sponsorAttendeesCount: number;
    acceptTerms: boolean;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function getInitialSponsorForm(): SponsorForm {
    return {
        companyName: '',
        contactName: '',
        enterpriseNumber: '',
        street: '',
        houseNumber: '',
        postalCode: '',
        city: '',
        email: '',
        phone: '',
        package: 'zilver',
        extraEtenPartyCount: 0,
        extraVegetarischCount: 0,
        extraDrankkaart20Count: 0,
        includedVegetarischCount: 0,
        sponsorAttends: false,
        sponsorAttendeesCount: 0,
        acceptTerms: false,
    };
}

function getResetSponsorSelection(form: SponsorForm, packageId: string): SponsorForm {
    return {
        ...form,
        package: packageId,
        includedVegetarischCount: 0,
        extraEtenPartyCount: 0,
        extraVegetarischCount: 0,
        extraDrankkaart20Count: 0,
        sponsorAttends: false,
        sponsorAttendeesCount: 0,
    };
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function BalParentalPage() {
    const [activeTab, setActiveTab] = useState<'tickets' | 'sponsor'>('sponsor');
    const [showTerms, setShowTerms] = useState(false);

    const [ticketForm, setTicketForm] = useState<TicketForm>({
        name: '',
        email: '',
        toegangsticketCount: 0,
        etenPartyCount: 0,
        vegetarischCount: 0,
        drankkaart10Count: 0,
        drankkaart20Count: 0,
        acceptTerms: false,
    });
    const [ticketLoading, setTicketLoading] = useState(false);
    const [ticketError, setTicketError] = useState('');

    const [sponsorForm, setSponsorForm] = useState<SponsorForm>(getInitialSponsorForm());
    const [sponsorLoading, setSponsorLoading] = useState(false);
    const [sponsorError, setSponsorError] = useState('');
    const [enterpriseNumberError, setEnterpriseNumberError] = useState('');
    const [logoFile, setLogoFile] = useState<File | null>(null);
    const [logoError, setLogoError] = useState('');

    const etenPartyExpired = useCountdown(ETEN_PARTY_DEADLINE).expired;
    const ticketExpired    = useCountdown(TICKET_DEADLINE).expired;
    const sponsorExpired   = useCountdown(SPONSOR_DEADLINE).expired;

    const inputClass =
        'mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200';

    const selectedSponsorPackage = sponsorPackages.find((pkg) => pkg.id === sponsorForm.package);
    const selectedSponsorPackagePrice = selectedSponsorPackage?.price ?? 0;
    const sponsorTotal =
        selectedSponsorPackagePrice +
        sponsorForm.extraEtenPartyCount * 50 +
        sponsorForm.extraDrankkaart20Count * 20;

    const handleTicketSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setTicketError('');

        if (!ticketForm.acceptTerms) {
            setTicketError('Je moet de algemene voorwaarden accepteren.');
            return;
        }

        const totalTickets = ticketForm.toegangsticketCount + ticketForm.etenPartyCount;
        if (totalTickets < 1) {
            setTicketError('Kies minstens 1 ticket.');
            return;
        }

        setTicketLoading(true);

        try {
            const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';

            const res = await fetch(`${apiBase}/tickets/create-checkout`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    name: ticketForm.name,
                    email: ticketForm.email,
                    toegangsticketCount: ticketForm.toegangsticketCount,
                    etenPartyCount: ticketForm.etenPartyCount,
                    vegetarischCount: ticketForm.vegetarischCount,
                    drankkaart10Count: ticketForm.drankkaart10Count,
                    drankkaart20Count: ticketForm.drankkaart20Count,
                }),
            });

            if (!res.ok) throw new Error();

            const data = await res.json();
            const checkoutUrl = data.checkoutUrl || data.url;

            if (checkoutUrl) {
                window.location.href = checkoutUrl;
            } else {
                throw new Error('No checkout url in response');
            }
        } catch {
            setTicketError('Er ging iets mis. Probeer het later opnieuw.');
        } finally {
            setTicketLoading(false);
        }
    };

    const handleSponsorSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setSponsorError('');

        if (!sponsorForm.acceptTerms) {
            setSponsorError('Je moet de algemene voorwaarden accepteren.');
            return;
        }

        if (!validateEnterpriseNumber(sponsorForm.enterpriseNumber)) {
            setSponsorError('Ongeldig Belgisch ondernemingsnummer. Controleer het nummer en probeer opnieuw.');
            return;
        }

        if (!/^\d{4}$/.test(sponsorForm.postalCode.trim())) {
            setSponsorError('Ongeldige postcode. Voer een Belgische postcode van 4 cijfers in.');
            return;
        }

        setSponsorLoading(true);

        try {
            const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';

            let logoUrl = '';

            if (logoFile) {
                const formData = new FormData();
                formData.append('logo', logoFile);

                const uploadRes = await fetch(`${apiBase}/sponsors/upload-logo`, {
                    method: 'POST',
                    body: formData,
                });

                if (!uploadRes.ok) {
                    const uploadData = await uploadRes.json().catch(() => ({}));
                    setSponsorError(uploadData.error || 'Logo upload mislukt. Probeer het later opnieuw.');
                    return;
                }

                const uploadData = await uploadRes.json();
                logoUrl = uploadData.logoUrl || '';
            }

            const res = await fetch(`${apiBase}/sponsors/checkout`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    companyName: sponsorForm.companyName,
                    contactName: sponsorForm.contactName,
                    email: sponsorForm.email,
                    phone: sponsorForm.phone,
                    package: sponsorForm.package,
                    enterpriseNumber: sponsorForm.enterpriseNumber,
                    street: sponsorForm.street,
                    houseNumber: sponsorForm.houseNumber,
                    postalCode: sponsorForm.postalCode,
                    city: sponsorForm.city,
                    extraEtenPartyCount: sponsorForm.extraEtenPartyCount,
                    extraVegetarischCount: sponsorForm.extraVegetarischCount,
                    extraDrankkaart20Count: sponsorForm.extraDrankkaart20Count,
                    includedVegetarischCount: sponsorForm.includedVegetarischCount,
                    sponsorAttends: sponsorForm.sponsorAttends,
                    sponsorAttendeesCount: sponsorForm.sponsorAttends ? sponsorForm.sponsorAttendeesCount : 0,
                    logoUrl,
                }),
            });

            if (!res.ok) throw new Error();

            const data = await res.json();
            const checkoutUrl = data.checkoutUrl || data.url;

            if (checkoutUrl) {
                window.location.href = checkoutUrl;
            } else {
                throw new Error('No checkout url in response');
            }
        } catch {
            setSponsorError('Er ging iets mis. Probeer het later opnieuw.');
        } finally {
            setSponsorLoading(false);
        }
    };

    return (
        <>
            {showTerms && <TermsModal onClose={() => setShowTerms(false)} />}

            {/* Hero / Header */}
            <section className="relative overflow-hidden bg-black">
                <div className="absolute inset-0">
                    <img
                        src={balPoster}
                        alt="Bal Parental poster"
                        className="h-full w-full object-cover object-center opacity-25"
                    />
                    <div className="absolute inset-0 bg-gradient-to-br from-black via-[#120018]/90 to-[#3b0764]/80" />
                    <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(236,72,153,0.25),transparent_30%),radial-gradient(circle_at_top_right,rgba(139,92,246,0.30),transparent_35%),radial-gradient(circle_at_bottom,rgba(168,85,247,0.18),transparent_40%)]" />
                </div>

                <div className="relative mx-auto max-w-7xl px-6 py-20 sm:px-8 lg:grid lg:grid-cols-2 lg:gap-12 lg:px-12 lg:py-24">
                    <div className="max-w-3xl">
                        <span className="inline-flex rounded-full border border-fuchsia-400/40 bg-white/10 px-4 py-1 text-sm font-semibold tracking-wide text-fuchsia-200 backdrop-blur">
                            ✨ Het feest van Oudercomité met Pit
                        </span>

                        <h1 className="mt-6 text-5xl font-extrabold uppercase tracking-tight text-white sm:text-6xl lg:text-7xl">
                            <span className="drop-shadow-[0_0_18px_rgba(255,255,255,0.22)]">Bal</span>{' '}
                            <span className="text-fuchsia-300 drop-shadow-[0_0_22px_rgba(217,70,239,0.45)]">
                                Parental
                            </span>
                        </h1>

                        <p className="mt-5 max-w-2xl text-lg leading-8 text-purple-100/90">
                            Een onvergetelijke avond vol muziek, gezelligheid en sfeer in{' '}
                            <span className="font-semibold text-white">De Magneet Pittem</span>.
                        </p>

                        <div className="mt-8 flex flex-wrap gap-3">
                            <span className="rounded-full border border-white/10 bg-white/10 px-4 py-2 text-sm font-medium text-white backdrop-blur">
                                📅 Zaterdag 20 juni 2026
                            </span>
                            <span className="rounded-full border border-white/10 bg-white/10 px-4 py-2 text-sm font-medium text-white backdrop-blur">
                                📍 De Magneet – Egemstraat 49 – Pittem
                            </span>
                            <span className="rounded-full border border-white/10 bg-white/10 px-4 py-2 text-sm font-medium text-white backdrop-blur">
                                🎵 DJ Feliz &amp; DJ Dennis Cartier
                            </span>
                        </div>

                        <div className="mt-8 flex flex-col gap-3 sm:flex-row">
                            <a
                                href="#bestellen"
                                className="inline-flex items-center justify-center rounded-full bg-gradient-to-r from-pink-500 to-violet-500 px-6 py-3 text-sm font-bold text-white shadow-lg shadow-fuchsia-900/30 transition hover:scale-[1.02]"
                            >
                                Tickets & sponsorpakketten
                            </a>

                            <a
                                href={`mailto:${contactEmail}`}
                                className="inline-flex items-center justify-center rounded-full border border-fuchsia-300/40 bg-white/10 px-6 py-3 text-sm font-semibold text-fuchsia-100 backdrop-blur transition hover:bg-white/15"
                            >
                                ✉️ {contactEmail}
                            </a>
                        </div>

                        <p className="mt-5 text-sm text-purple-200/80">
                            Vragen over tickets of sponsoring? Contacteer ons via{' '}
                            <a
                                href={`mailto:${contactEmail}`}
                                className="font-semibold text-fuchsia-200 underline decoration-fuchsia-400/60 underline-offset-4 hover:text-white"
                            >
                                {contactEmail}
                            </a>
                            .
                        </p>
                    </div>

                    <div className="mt-12 hidden lg:flex lg:items-center lg:justify-end">
                        <div className="overflow-hidden rounded-[2rem] border border-white/10 bg-white/5 shadow-2xl backdrop-blur">
                            <img
                                src={balPoster}
                                alt="Bal Parental 2026 poster"
                                className="w-[340px] object-cover"
                            />
                        </div>
                    </div>
                </div>
            </section>

            {/* Praktische info + bestelgedeelte */}
            <Section>
                <div className="grid gap-12 lg:grid-cols-2">
                    <div>
                        <h2 className="section-title">Praktische info</h2>

                        <dl className="mt-8 space-y-6">
                            {[
                                { label: '📅 Datum', value: 'zaterdag 20 juni 2026' },
                                { label: '📍 Locatie', value: 'De Magneet - Egemstraat 49 - Pittem' },
                                { label: '🎵 Muziek', value: 'DJ Feliz - DJ Dennis Cartier' },
                                { label: '✉️ Contact', value: contactEmail },
                            ].map((item) => (
                                <div key={item.label} className="flex gap-4">
                                    <dt className="w-28 flex-shrink-0 text-sm font-semibold text-gray-900">{item.label}</dt>
                                    <dd className="text-gray-600">
                                        {item.value === contactEmail ? (
                                            <a
                                                href={`mailto:${contactEmail}`}
                                                className="text-primary-600 underline hover:text-primary-700"
                                            >
                                                {contactEmail}
                                            </a>
                                        ) : (
                                            item.value
                                        )}
                                    </dd>
                                </div>
                            ))}
                        </dl>

                        <div className="mt-8">
                            <h3 className="mb-3 text-sm font-semibold text-gray-900">🎫 Kies je ticket</h3>

                            <div className="space-y-3">
                                <div className="rounded-lg border border-gray-200 bg-white p-4">
                                    <p className="font-semibold text-gray-900">🎉 Toegangsticket — €8 p.p.</p>
                                    <ul className="mt-2 space-y-1 text-sm text-gray-500">
                                        <li>• Vanaf 22u30</li>
                                        <li>• Inclusief toegang tot het feest</li>
                                        <li>• Geen diner inbegrepen</li>
                                        <li>• Optioneel: drankkaarten van €10 of €20</li>
                                    </ul>
                                </div>

                                <div className="rounded-lg border border-gray-200 bg-white p-4">
                                    <p className="font-semibold text-gray-900">🍽️ Eten &amp; Party — €50 p.p.</p>
                                    <ul className="mt-2 space-y-1 text-sm text-gray-500">
                                        <li>• Vanaf 19u30</li>
                                        <li>• Inclusief diner + feest</li>
                                        <li>• Vegetarische optie mogelijk</li>
                                        <li>• Optioneel: drankkaarten van €10 of €20</li>
                                    </ul>
                                </div>
                            </div>
                        </div>

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

                    <div id="bestellen" className="scroll-mt-24">
                        <div className="flex rounded-xl bg-gray-100 p-1">
                            <button
                                type="button"
                                onClick={() => setActiveTab('sponsor')}
                                className={`flex-1 rounded-lg py-2.5 text-sm font-semibold transition-all ${activeTab === 'sponsor'
                                        ? 'bg-white text-gray-900 shadow'
                                        : 'text-gray-500 hover:text-gray-700'
                                    }`}
                            >
                                🤝 Sponsorpakket bestellen
                            </button>

                            <button
                                type="button"
                                onClick={() => setActiveTab('tickets')}
                                className={`flex-1 rounded-lg py-2.5 text-sm font-semibold transition-all ${activeTab === 'tickets'
                                        ? 'bg-white text-gray-900 shadow'
                                        : 'text-gray-500 hover:text-gray-700'
                                    }`}
                            >
                                🎟️ Tickets bestellen
                            </button>
                        </div>

                        {activeTab === 'tickets' && (
                            <div className="mt-4 rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100">
                                <h2 className="text-2xl font-bold text-gray-900">Tickets bestellen</h2>
                                <p className="mt-2 text-sm text-gray-500">
                                    Vul je gegevens in en kies je tickets. Je wordt doorgestuurd naar een beveiligde betaalpagina. Feesttickets zijn beschikbaar tot 19 juni.
                                </p>

                                <CountdownBanner deadline={ETEN_PARTY_DEADLINE} label="Eten &amp; Party tickets" />
                                <form onSubmit={handleTicketSubmit} className="mt-6 space-y-5">
                                    <div>
                                        <label htmlFor="t-name" className="block text-sm font-medium text-gray-700">
                                            Naam
                                        </label>
                                        <input
                                            id="t-name"
                                            type="text"
                                            required
                                            value={ticketForm.name}
                                            onChange={(e) => setTicketForm({ ...ticketForm, name: e.target.value })}
                                            className={inputClass}
                                            placeholder="Jouw volledige naam"
                                        />
                                    </div>

                                    <div>
                                        <label htmlFor="t-email" className="block text-sm font-medium text-gray-700">
                                            E-mailadres
                                        </label>
                                        <input
                                            id="t-email"
                                            type="email"
                                            required
                                            value={ticketForm.email}
                                            onChange={(e) => setTicketForm({ ...ticketForm, email: e.target.value })}
                                            className={inputClass}
                                            placeholder="jouw@email.be"
                                        />
                                    </div>

                                    <div className="space-y-2">
                                        <label className="block text-sm font-medium text-gray-700">Tickets</label>

                                        <div className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3">
                                            <div>
                                                <p className="text-sm font-semibold text-gray-900">🎉 Toegangsticket</p>
                                                <p className="text-xs text-gray-500">Toegang tot het feest vanaf 22u30 · €8 p.p.</p>
                                                <p className="text-xs text-gray-400">Diner niet inbegrepen</p>
                                            </div>

                                            <Stepper
                                                value={ticketForm.toegangsticketCount}
                                                onChange={(n) => setTicketForm({ ...ticketForm, toegangsticketCount: n })}
                                            />
                                        </div>

                                        <div className="rounded-lg border border-gray-200">
                                            <div className="flex items-center justify-between px-4 py-3">
                                                <div>
                                                    <p className="text-sm font-semibold text-gray-900">🍽️ Eten &amp; Party</p>
                                                    <p className="text-xs text-gray-500">Diner + feest vanaf 19u30 · €50 per persoon</p>
                                                    <p className="text-xs text-gray-400">Vegetarische optie per ticket mogelijk</p>
                                                </div>

                                                <Stepper
                                                    value={ticketForm.etenPartyCount}
                                                    onChange={(n) =>
                                                        setTicketForm({
                                                            ...ticketForm,
                                                            etenPartyCount: n,
                                                            vegetarischCount: Math.min(ticketForm.vegetarischCount, n),
                                                        })
                                                    }
                                                disabled={etenPartyExpired}
                                                />
                                            </div>

                                            {ticketForm.etenPartyCount > 0 && (
                                                <div className="border-t border-gray-100 bg-green-50 px-4 py-3">
                                                    <div className="flex items-center justify-between">
                                                        <div>
                                                            <p className="text-sm font-medium text-green-800">🥗 Aantal vegetarische diners</p>
                                                            <p className="text-xs text-green-600">Max. {ticketForm.etenPartyCount}</p>
                                                        </div>

                                                        <Stepper
                                                            value={ticketForm.vegetarischCount}
                                                            onChange={(n) => setTicketForm({ ...ticketForm, vegetarischCount: n })}
                                                            max={ticketForm.etenPartyCount}
                                                        />
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {ticketForm.toegangsticketCount + ticketForm.etenPartyCount > 0 && (
                                        <div className="space-y-2">
                                            <label className="block text-sm font-medium text-gray-700">
                                                Drankkaarten <span className="text-gray-400">(optioneel)</span>
                                            </label>

                                            <div className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3">
                                                <p className="text-sm font-semibold text-gray-900">🍹 Drankkaart €10</p>
                                                <Stepper
                                                    value={ticketForm.drankkaart10Count}
                                                    onChange={(n) => setTicketForm({ ...ticketForm, drankkaart10Count: n })}
                                                />
                                            </div>

                                            <div className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3">
                                                <p className="text-sm font-semibold text-gray-900">🍹 Drankkaart €20</p>
                                                <Stepper
                                                    value={ticketForm.drankkaart20Count}
                                                    onChange={(n) => setTicketForm({ ...ticketForm, drankkaart20Count: n })}
                                                />
                                            </div>
                                        </div>
                                    )}

                                    {ticketForm.toegangsticketCount + ticketForm.etenPartyCount > 0 && (
                                        <div className="rounded-lg bg-primary-50 px-4 py-3">
                                            <div className="flex justify-between text-sm font-semibold text-primary-900">
                                                <span>Totaal</span>
                                                <span>
                                                    €
                                                    {ticketForm.toegangsticketCount * 8 +
                                                        ticketForm.etenPartyCount * 50 +
                                                        ticketForm.drankkaart10Count * 10 +
                                                        ticketForm.drankkaart20Count * 20}
                                                </span>
                                            </div>
                                        </div>
                                    )}

                                    <div className="flex items-start gap-3">
                                        <input
                                            id="t-terms"
                                            type="checkbox"
                                            checked={ticketForm.acceptTerms}
                                            onChange={(e) => setTicketForm({ ...ticketForm, acceptTerms: e.target.checked })}
                                            className="mt-0.5 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                                        />
                                        <label htmlFor="t-terms" className="text-sm text-gray-600">
                                            Ik ga akkoord met de{' '}
                                            <button
                                                type="button"
                                                onClick={() => setShowTerms(true)}
                                                className="font-medium text-primary-600 underline hover:text-primary-700"
                                            >
                                                algemene voorwaarden
                                            </button>{' '}
                                            en heb kennis genomen van de{' '}
                                            <Link
                                                to="/privacy"
                                                className="font-medium text-primary-600 underline hover:text-primary-700"
                                            >
                                                privacyverklaring
                                            </Link>
                                            .
                                        </label>
                                    </div>

                                    {ticketError && (
                                        <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{ticketError}</div>
                                    )}

                                    <button
                                        type="submit"
                                        disabled={ticketLoading || ticketExpired || ticketForm.toegangsticketCount + ticketForm.etenPartyCount < 1}
                                        className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-50"
                                    >
                                        {ticketLoading ? <Spinner /> : 'Betalen'}
                                    </button>
                                </form>
                            </div>
                        )}

                        {activeTab === 'sponsor' && (
                            <div className="mt-4 rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100">
                                <h2 className="text-2xl font-bold text-gray-900">Sponsorpakket bestellen</h2>
                                <p className="mt-2 text-sm text-gray-500">
                                    Kies een pakket hieronder en vul je gegevens in. Je wordt doorgestuurd naar een beveiligde betaalpagina.
                                </p>

                                <CountdownBanner deadline={SPONSOR_DEADLINE} label="sponsorpakketten" />
                                <form onSubmit={handleSponsorSubmit} className="mt-6 space-y-5">
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700">Sponsorpakket</label>
                                        <div className="mt-2 grid grid-cols-3 gap-2">
                                            {sponsorPackages.map((pkg) => (
                                                <button
                                                    key={pkg.id}
                                                    type="button"
                                                    onClick={() => setSponsorForm((current) => getResetSponsorSelection(current, pkg.id))}
                                                    className={`rounded-lg border-2 p-3 text-center text-sm font-semibold transition-all ${sponsorForm.package === pkg.id
                                                            ? `${pkg.color.border} ${pkg.color.bg} ring-2 ${pkg.color.ring}`
                                                            : 'border-gray-200 hover:border-gray-300'
                                                        }`}
                                                >
                                                    <span className="block text-xl">{pkg.emoji}</span>
                                                    <span className="mt-1 block">{pkg.label}</span>
                                                    <span className="block text-xs text-gray-500">€{pkg.price}</span>
                                                </button>
                                            ))}
                                        </div>
                                    </div>

                                    {selectedSponsorPackage && selectedSponsorPackage.tickets > 0 && (
                                        <div className="rounded-lg border border-green-200 bg-green-50 px-4 py-3">
                                            <div className="flex items-center justify-between">
                                                <div>
                                                    <p className="text-sm font-medium text-green-800">
                                                        🥗 Vegetarische diners (inbegrepen tickets)
                                                    </p>
                                                    <p className="text-xs text-green-600">
                                                        Hoeveel van de {selectedSponsorPackage.tickets} inbegrepen Eten &amp; Party tickets zijn vegetarisch?
                                                    </p>
                                                </div>

                                                <Stepper
                                                    value={sponsorForm.includedVegetarischCount}
                                                    onChange={(n) => setSponsorForm({ ...sponsorForm, includedVegetarischCount: n })}
                                                    max={selectedSponsorPackage.tickets}
                                                />
                                            </div>
                                        </div>
                                    )}

                                    <div className="grid gap-4 sm:grid-cols-2">
                                        <div>
                                            <label htmlFor="s-company" className="block text-sm font-medium text-gray-700">
                                                Bedrijfsnaam
                                            </label>
                                            <input
                                                id="s-company"
                                                type="text"
                                                required
                                                value={sponsorForm.companyName}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, companyName: e.target.value })}
                                                className={inputClass}
                                                placeholder="Jouw bedrijfsnaam"
                                            />
                                        </div>

                                        <div>
                                            <label htmlFor="s-contact" className="block text-sm font-medium text-gray-700">
                                                Contactpersoon
                                            </label>
                                            <input
                                                id="s-contact"
                                                type="text"
                                                required
                                                value={sponsorForm.contactName}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, contactName: e.target.value })}
                                                className={inputClass}
                                                placeholder="Voor- en achternaam"
                                            />
                                        </div>
                                    </div>

                                    <div>
                                        <label htmlFor="s-enterprise" className="block text-sm font-medium text-gray-700">
                                            Ondernemingsnummer
                                        </label>
                                        <input
                                            id="s-enterprise"
                                            type="text"
                                            required
                                            value={sponsorForm.enterpriseNumber}
                                            onChange={(e) => {
                                                setSponsorForm({ ...sponsorForm, enterpriseNumber: e.target.value });
                                                setEnterpriseNumberError('');
                                            }}
                                            onBlur={() => {
                                                if (
                                                    sponsorForm.enterpriseNumber &&
                                                    !validateEnterpriseNumber(sponsorForm.enterpriseNumber)
                                                ) {
                                                    setEnterpriseNumberError(
                                                        'Ongeldig Belgisch ondernemingsnummer. Verwacht formaat: 0xxx.xxx.xxx'
                                                    );
                                                } else {
                                                    setEnterpriseNumberError('');
                                                }
                                            }}
                                            className={`${inputClass} ${enterpriseNumberError ? 'border-red-400 focus:border-red-500 focus:ring-red-200' : ''
                                                }`}
                                            placeholder="0xxx.xxx.xxx"
                                        />
                                        {enterpriseNumberError && (
                                            <p className="mt-1 text-xs text-red-600">{enterpriseNumberError}</p>
                                        )}
                                    </div>

                                    <div className="grid gap-4 sm:grid-cols-2">
                                        <div>
                                            <label htmlFor="s-email" className="block text-sm font-medium text-gray-700">
                                                E-mailadres
                                            </label>
                                            <input
                                                id="s-email"
                                                type="email"
                                                required
                                                value={sponsorForm.email}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, email: e.target.value })}
                                                className={inputClass}
                                                placeholder="info@bedrijf.be"
                                            />
                                        </div>

                                        <div>
                                            <label htmlFor="s-phone" className="block text-sm font-medium text-gray-700">
                                                Telefoonnummer <span className="text-gray-400">(optioneel)</span>
                                            </label>
                                            <input
                                                id="s-phone"
                                                type="tel"
                                                value={sponsorForm.phone}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, phone: e.target.value })}
                                                className={inputClass}
                                                placeholder="+32 4xx xx xx xx"
                                            />
                                        </div>
                                    </div>

                                    <div className="grid gap-4 sm:grid-cols-3">
                                        <div className="sm:col-span-2">
                                            <label htmlFor="s-street" className="block text-sm font-medium text-gray-700">
                                                Straat
                                            </label>
                                            <input
                                                id="s-street"
                                                type="text"
                                                required
                                                value={sponsorForm.street}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, street: e.target.value })}
                                                className={inputClass}
                                                placeholder="Kerkstraat"
                                            />
                                        </div>

                                        <div>
                                            <label htmlFor="s-housenr" className="block text-sm font-medium text-gray-700">
                                                Nr.
                                            </label>
                                            <input
                                                id="s-housenr"
                                                type="text"
                                                required
                                                value={sponsorForm.houseNumber}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, houseNumber: e.target.value })}
                                                className={inputClass}
                                                placeholder="12"
                                            />
                                        </div>
                                    </div>

                                    <div className="grid gap-4 sm:grid-cols-3">
                                        <div>
                                            <label htmlFor="s-postal" className="block text-sm font-medium text-gray-700">
                                                Postcode
                                            </label>
                                            <input
                                                id="s-postal"
                                                type="text"
                                                required
                                                value={sponsorForm.postalCode}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, postalCode: e.target.value })}
                                                className={inputClass}
                                                placeholder="8740"
                                                maxLength={4}
                                            />
                                        </div>

                                        <div className="sm:col-span-2">
                                            <label htmlFor="s-city" className="block text-sm font-medium text-gray-700">
                                                Gemeente
                                            </label>
                                            <input
                                                id="s-city"
                                                type="text"
                                                required
                                                value={sponsorForm.city}
                                                onChange={(e) => setSponsorForm({ ...sponsorForm, city: e.target.value })}
                                                className={inputClass}
                                                placeholder="Pittem"
                                            />
                                        </div>
                                    </div>

                                    <div>
                                        <label className="block text-sm font-medium text-gray-700">
                                            Bedrijfslogo <span className="text-gray-400">(optioneel, max. 5 MB)</span>
                                        </label>

                                        <div className="mt-1">
                                            <label
                                                htmlFor="s-logo"
                                                className="flex cursor-pointer items-center gap-3 rounded-lg border border-dashed border-gray-300 px-4 py-3 text-sm text-gray-500 transition-colors hover:border-primary-400 hover:text-primary-600"
                                            >
                                                <svg className="h-5 w-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                                                    <path strokeLinecap="round" strokeLinejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5" />
                                                </svg>

                                                <span className="truncate">
                                                    {logoFile ? logoFile.name : 'Klik om een afbeelding te kiezen'}
                                                </span>

                                                {logoFile && (
                                                    <button
                                                        type="button"
                                                        onClick={(e) => {
                                                            e.preventDefault();
                                                            setLogoFile(null);
                                                            setLogoError('');
                                                        }}
                                                        className="ml-auto flex-shrink-0 text-gray-400 hover:text-red-500"
                                                        aria-label="Logo verwijderen"
                                                    >
                                                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                                                        </svg>
                                                    </button>
                                                )}
                                            </label>

                                            <input
                                                id="s-logo"
                                                type="file"
                                                accept="image/*"
                                                className="sr-only"
                                                onChange={(e) => {
                                                    const file = e.target.files?.[0] ?? null;

                                                    if (file && file.size > 5 * 1024 * 1024) {
                                                        setLogoError('Bestand te groot. Maximum 5 MB toegestaan.');
                                                        setLogoFile(null);
                                                    } else {
                                                        setLogoError('');
                                                        setLogoFile(file);
                                                    }

                                                    e.target.value = '';
                                                }}
                                            />

                                            {logoError && <p className="mt-1 text-xs text-red-600">{logoError}</p>}
                                        </div>
                                    </div>

                                    {sponsorForm.package !== 'brons' && (
                                        <div className="space-y-2">
                                            <label className="block text-sm font-medium text-gray-700">
                                                Zal u zelf aanwezig zijn op het evenement?
                                            </label>

                                            <div className="flex gap-2">
                                                <button
                                                    type="button"
                                                    onClick={() =>
                                                        setSponsorForm({
                                                            ...sponsorForm,
                                                            sponsorAttends: true,
                                                            sponsorAttendeesCount: Math.max(1, sponsorForm.sponsorAttendeesCount),
                                                        })
                                                    }
                                                    className={`flex-1 rounded-lg border-2 py-2.5 text-sm font-semibold transition-all ${sponsorForm.sponsorAttends
                                                            ? 'border-primary-500 bg-primary-50 text-primary-800'
                                                            : 'border-gray-200 text-gray-500 hover:border-gray-300'
                                                        }`}
                                                >
                                                    ✅ Ja
                                                </button>

                                                <button
                                                    type="button"
                                                    onClick={() =>
                                                        setSponsorForm({
                                                            ...sponsorForm,
                                                            sponsorAttends: false,
                                                            sponsorAttendeesCount: 0,
                                                        })
                                                    }
                                                    className={`flex-1 rounded-lg border-2 py-2.5 text-sm font-semibold transition-all ${!sponsorForm.sponsorAttends
                                                            ? 'border-primary-500 bg-primary-50 text-primary-800'
                                                            : 'border-gray-200 text-gray-500 hover:border-gray-300'
                                                        }`}
                                                >
                                                    ❌ Nee
                                                </button>
                                            </div>

                                            <div className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3">
                                                <div>
                                                    <p className="text-sm font-semibold text-gray-900">Aantal effectieve aanwezigen</p>
                                                    {sponsorForm.sponsorAttends && (
                                                        <p className="text-xs text-gray-500">Inclusief uzelf</p>
                                                    )}
                                                </div>

                                                <Stepper
                                                    value={sponsorForm.sponsorAttendeesCount}
                                                    onChange={(n) => setSponsorForm({ ...sponsorForm, sponsorAttendeesCount: n })}
                                                    min={sponsorForm.sponsorAttends ? 1 : 0}
                                                />
                                            </div>
                                        </div>
                                    )}

                                    {sponsorForm.package !== 'brons' && (
                                        <div className="space-y-2">
                                            <label className="block text-sm font-medium text-gray-700">
                                                Extra tickets <span className="text-gray-400">(optioneel)</span>
                                            </label>

                                            <div className="rounded-lg border border-gray-200">
                                                <div className="flex items-center justify-between px-4 py-3">
                                                    <div>
                                                        <p className="text-sm font-semibold text-gray-900">🍽️ Eten &amp; Party</p>
                                                        <p className="text-xs text-gray-500">Diner + feest vanaf 19u30 · €50 per persoon</p>
                                                        <p className="text-xs text-gray-400">Vegetarische optie per ticket mogelijk</p>
                                                    </div>

                                                    <Stepper
                                                        value={sponsorForm.extraEtenPartyCount}
                                                        onChange={(n) =>
                                                            setSponsorForm({
                                                                ...sponsorForm,
                                                                extraEtenPartyCount: n,
                                                                extraVegetarischCount: Math.min(sponsorForm.extraVegetarischCount, n),
                                                            })
                                                        }
                                                    />
                                                </div>

                                                {sponsorForm.extraEtenPartyCount > 0 && (
                                                    <div className="border-t border-gray-100 bg-green-50 px-4 py-3">
                                                        <div className="flex items-center justify-between">
                                                            <div>
                                                                <p className="text-sm font-medium text-green-800">🥗 Aantal vegetarische diners</p>
                                                                <p className="text-xs text-green-600">Max. {sponsorForm.extraEtenPartyCount}</p>
                                                            </div>

                                                            <Stepper
                                                                value={sponsorForm.extraVegetarischCount}
                                                                onChange={(n) => setSponsorForm({ ...sponsorForm, extraVegetarischCount: n })}
                                                                max={sponsorForm.extraEtenPartyCount}
                                                            />
                                                        </div>
                                                    </div>
                                                )}
                                            </div>

                                            <div className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3">
                                                <div>
                                                    <p className="text-sm font-semibold text-gray-900">🍹 Drankkaart €20</p>
                                                    <p className="text-xs text-gray-400">(optioneel)</p>
                                                </div>

                                                <Stepper
                                                    value={sponsorForm.extraDrankkaart20Count}
                                                    onChange={(n) => setSponsorForm({ ...sponsorForm, extraDrankkaart20Count: n })}
                                                />
                                            </div>
                                        </div>
                                    )}

                                    <div className="space-y-1 rounded-lg bg-primary-50 px-4 py-3">
                                        <div className="flex justify-between text-xs text-primary-700">
                                            <span>
                                                Pakket {selectedSponsorPackage?.label} ({selectedSponsorPackage?.tickets ?? 0} tickets inbegrepen)
                                            </span>
                                            <span>€{selectedSponsorPackagePrice}</span>
                                        </div>

                                        {sponsorForm.extraEtenPartyCount > 0 && (
                                            <div className="flex justify-between text-xs text-primary-700">
                                                <span>{sponsorForm.extraEtenPartyCount}x Eten &amp; Party</span>
                                                <span>€{sponsorForm.extraEtenPartyCount * 50}</span>
                                            </div>
                                        )}

                                        {sponsorForm.extraDrankkaart20Count > 0 && (
                                            <div className="flex justify-between text-xs text-primary-700">
                                                <span>{sponsorForm.extraDrankkaart20Count}x Drankkaart €20</span>
                                                <span>€{sponsorForm.extraDrankkaart20Count * 20}</span>
                                            </div>
                                        )}

                                        <div className="flex justify-between border-t border-primary-200 pt-1 text-sm font-semibold text-primary-900">
                                            <span>Totaal</span>
                                            <span>€{sponsorTotal}</span>
                                        </div>
                                    </div>

                                    <p className="text-center text-xs text-gray-500">
                                        Heb je nog vragen?{' '}
                                        <a
                                            href={`mailto:${contactEmail}`}
                                            className="text-primary-600 underline hover:text-primary-700"
                                        >
                                            {contactEmail}
                                        </a>
                                    </p>

                                    <div className="flex items-start gap-3">
                                        <input
                                            id="s-terms"
                                            type="checkbox"
                                            checked={sponsorForm.acceptTerms}
                                            onChange={(e) => setSponsorForm({ ...sponsorForm, acceptTerms: e.target.checked })}
                                            className="mt-0.5 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                                        />
                                        <label htmlFor="s-terms" className="text-sm text-gray-600">
                                            Ik ga akkoord met de{' '}
                                            <button
                                                type="button"
                                                onClick={() => setShowTerms(true)}
                                                className="font-medium text-primary-600 underline hover:text-primary-700"
                                            >
                                                algemene voorwaarden
                                            </button>{' '}
                                            en heb kennis genomen van de{' '}
                                            <Link
                                                to="/privacy"
                                                className="font-medium text-primary-600 underline hover:text-primary-700"
                                            >
                                                privacyverklaring
                                            </Link>
                                            .
                                        </label>
                                    </div>

                                    {sponsorError && (
                                        <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{sponsorError}</div>
                                    )}

                                    <button
                                        type="submit"
                                        disabled={sponsorLoading || sponsorExpired}
                                        className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-50"
                                    >
                                        {sponsorLoading ? <Spinner /> : 'Sponsorpakket betalen'}
                                    </button>
                                </form>
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
                            Steun het Bal Parental en maak reclame voor jouw zaak. Kies het pakket dat bij je past.
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
                                    <span
                                        className={`mt-2 inline-block rounded-full px-3 py-1 text-xs font-semibold ${pkg.color.badge}`}
                                    >
                                        {pkg.tickets > 0 ? `${pkg.tickets} tickets + menu` : 'Logo & sociale media'}
                                    </span>
                                </div>

                                <ul className="mt-6 flex-1 space-y-2.5">
                                    {pkg.features.map((feature) => (
                                        <li
                                            key={feature.text}
                                            className={`flex items-start gap-2 text-sm ${feature.included ? 'text-gray-700' : 'text-gray-300'
                                                }`}
                                        >
                                            {feature.included ? (
                                                <svg
                                                    className="mt-0.5 h-4 w-4 flex-shrink-0 text-green-500"
                                                    fill="none"
                                                    viewBox="0 0 24 24"
                                                    stroke="currentColor"
                                                    strokeWidth={2.5}
                                                >
                                                    <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                                </svg>
                                            ) : (
                                                <svg
                                                    className="mt-0.5 h-4 w-4 flex-shrink-0 text-gray-300"
                                                    fill="none"
                                                    viewBox="0 0 24 24"
                                                    stroke="currentColor"
                                                    strokeWidth={2}
                                                >
                                                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                                                </svg>
                                            )}
                                            {feature.text}
                                        </li>
                                    ))}
                                </ul>

                                <button
                                    type="button"
                                    onClick={() => {
                                        setActiveTab('sponsor');
                                        setSponsorForm((current) => getResetSponsorSelection(current, pkg.id));
                                        window.scrollTo({ top: 0, behavior: 'smooth' });
                                    }}
                                    className={`mt-8 w-full rounded-lg py-3 text-sm font-semibold transition-all ${pkg.color.button}`}
                                >
                                    Kies {pkg.label}
                                </button>
                            </div>
                        ))}
                    </div>

                    <p className="mt-8 text-center text-sm text-gray-400">
                        Heb je een vraag of wil je een pakket op maat?{' '}
                        <a
                            href={`mailto:${contactEmail}`}
                            className="text-primary-600 underline hover:text-primary-700"
                        >
                            {contactEmail}
                        </a>
                        .
                    </p>
                </Section>
            </div>
        </>
    );
}
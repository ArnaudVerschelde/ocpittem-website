import { useNavigate } from 'react-router-dom';
import Section from "../components/Section";

export default function PrivacyPage() {
    const navigate = useNavigate();

    return (
        <Section>
            <div className="relative mx-auto max-w-4xl rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100">
                <button
                    onClick={() => navigate(-1)}
                    className="absolute right-4 top-4 flex h-8 w-8 items-center justify-center rounded-full text-gray-400 hover:bg-gray-100 hover:text-gray-600"
                    aria-label="Sluiten"
                >
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
                <h1 className="text-3xl font-bold text-gray-900">Privacyverklaring</h1>

                <div className="mt-6 space-y-6 text-sm leading-relaxed text-gray-600">
                    <section>
                        <h2 className="font-semibold text-gray-900">1. Wie zijn wij?</h2>
                        <p>
                            Oudercomité met Pit, Pittem.
                            Contact: <a href="mailto:oudercomitepittem@ocpittem.be" className="text-primary-600 underline">oudercomitepittem@gmail.com</a>
                        </p>
                    </section>

                    <section>
                        <h2 className="font-semibold text-gray-900">2. Welke gegevens verzamelen wij?</h2>
                        <p>
                            Wij verwerken persoonsgegevens die je zelf aan ons bezorgt, zoals naam,
                            e-mailadres, telefoonnummer, bedrijfsgegevens en gegevens in verband met
                            ticket- of sponsoraanvragen.
                        </p>
                    </section>

                    <section>
                        <h2 className="font-semibold text-gray-900">3. Waarom verwerken wij deze gegevens?</h2>
                        <p>
                            Om contactaanvragen te beantwoorden, tickets te verwerken en te verzenden,
                            sponsoraanvragen op te volgen en te voldoen aan wettelijke verplichtingen.
                        </p>
                    </section>

                    <section>
                        <h2 className="font-semibold text-gray-900">4. Met wie delen wij gegevens?</h2>
                        <p>
                            Alleen met dienstverleners die nodig zijn voor de werking van de website en
                            de afhandeling van bestellingen, zoals betalings- en e-maildiensten.
                        </p>
                    </section>

                    <section>
                        <h2 className="font-semibold text-gray-900">5. Bewaartermijn</h2>
                        <p>
                            Wij bewaren persoonsgegevens niet langer dan nodig voor de hierboven vermelde
                            doeleinden of zolang een wettelijke bewaartermijn dat vereist.
                        </p>
                    </section>

                    <section>
                        <h2 className="font-semibold text-gray-900">6. Jouw rechten</h2>
                        <p>
                            Je hebt recht op inzage, verbetering, verwijdering, beperking en bezwaar.
                            Hiervoor kan je ons contacteren via het bovenstaande e-mailadres.
                        </p>
                    </section>

                    <section>
                        <h2 className="font-semibold text-gray-900">7. Cookies</h2>
                        <p>
                            Deze website gebruikt momenteel geen analytische of marketingcookies.
                            Enkel technisch noodzakelijke functionaliteiten kunnen gebruikt worden.
                        </p>
                    </section>
                </div>
            </div>
        </Section>
    );
}
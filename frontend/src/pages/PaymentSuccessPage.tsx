import { useSearchParams, Link } from "react-router-dom";
import Section from "../components/Section";

export default function PaymentSuccessPage() {
    const [params] = useSearchParams();
    const sessionId = params.get("session_id");

    return (
        <Section>
            <div className="mx-auto max-w-2xl rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100 text-center">
                <div className="text-5xl">✅</div>
                <h1 className="mt-4 text-3xl font-bold text-gray-900">Betaling gelukt!</h1>
                <p className="mt-3 text-gray-600">
                    Bedankt! We verwerken je bestelling. Je ontvangt je tickets per e-mail zodra alles klaar is.
                </p>

                {sessionId && (
                    <p className="mt-3 text-xs text-gray-400">
                        Referentie: <span className="font-mono">{sessionId}</span>
                    </p>
                )}

                <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:justify-center">
                    <Link to="/bal-parental" className="btn-primary">Terug naar Bal Parental</Link>
                    <Link to="/contact" className="btn-secondary">Hulp nodig?</Link>
                </div>
            </div>
        </Section>
    );
}
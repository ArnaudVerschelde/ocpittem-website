import { useSearchParams, Link } from "react-router-dom";
import PaymentResultCard from "../components/PaymentResultCard";

export default function PaymentSuccessPage() {
    const [params] = useSearchParams();
    const sessionId = params.get("session_id");

    return (
        <PaymentResultCard
            icon="✅"
            title="Betaling gelukt!"
            actions={(
                <>
                    <Link to="/bal-parental" className="btn-primary">Terug naar Bal Parental</Link>
                    <Link to="/contact" className="btn-secondary">Hulp nodig?</Link>
                </>
            )}
        >
            <p>Bedankt! We verwerken je bestelling. Je ontvangt je tickets per e-mail zodra alles klaar is.</p>
            {sessionId && (
                <p className="mt-3 text-xs text-gray-400">
                    Referentie: <span className="font-mono">{sessionId}</span>
                </p>
            )}
        </PaymentResultCard>
    );
}
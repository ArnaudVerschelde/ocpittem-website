import { Link } from "react-router-dom";
import Section from "../components/Section";

export default function PaymentCancelPage() {
    return (
        <Section>
            <div className="mx-auto max-w-2xl rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100 text-center">
                <div className="text-5xl">ℹ️</div>
                <h1 className="mt-4 text-3xl font-bold text-gray-900">Betaling geannuleerd</h1>
                <p className="mt-3 text-gray-600">
                    Je betaling werd niet afgerond. Je kan gerust opnieuw proberen.
                </p>
                <div className="mt-8">
                    <Link to="/bal-parental" className="btn-primary">Opnieuw proberen</Link>
                </div>
            </div>
        </Section>
    );
}
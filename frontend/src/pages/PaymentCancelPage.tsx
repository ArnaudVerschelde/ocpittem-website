import { Link } from "react-router-dom";
import PaymentResultCard from "../components/PaymentResultCard";

export default function PaymentCancelPage() {
    return (
        <PaymentResultCard
            icon="ℹ️"
            title="Betaling geannuleerd"
            actions={<Link to="/bal-parental" className="btn-primary">Opnieuw proberen</Link>}
        >
            <p>Je betaling werd niet afgerond. Je kan gerust opnieuw proberen.</p>
        </PaymentResultCard>
    );
}
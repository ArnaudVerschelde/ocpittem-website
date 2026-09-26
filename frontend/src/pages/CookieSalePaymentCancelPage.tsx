import { Link } from 'react-router-dom';
import PaymentResultCard from '../components/PaymentResultCard';

export default function CookieSalePaymentCancelPage() {
  return (
    <PaymentResultCard
      icon="ℹ️"
      title="Betaling geannuleerd"
      actions={<Link to="/koekjesverkoop" className="btn-primary">Opnieuw proberen</Link>}
    >
      <p>Je betaling werd niet afgerond. Je bestelling is niet als betaald geregistreerd.</p>
    </PaymentResultCard>
  );
}

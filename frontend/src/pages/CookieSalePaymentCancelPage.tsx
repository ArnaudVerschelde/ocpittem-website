import { Link } from 'react-router-dom';
import PaymentResultCard from '../components/PaymentResultCard';
import { useCookieSaleOpen } from '../hooks/useCookieSaleOpen';

export default function CookieSalePaymentCancelPage() {
  const cookieSaleOpen = useCookieSaleOpen();

  return (
    <PaymentResultCard
      icon="ℹ️"
      title="Betaling geannuleerd"
      actions={cookieSaleOpen
        ? <Link to="/koekjesverkoop" className="btn-primary">Opnieuw proberen</Link>
        : <Link to="/" className="btn-primary">Naar de homepage</Link>}
    >
      <p>Je betaling werd niet afgerond. Je bestelling is niet als betaald geregistreerd.</p>
      {!cookieSaleOpen && (
        <p className="mt-2">De koekjesverkoop is ondertussen afgelopen; nieuwe bestellingen zijn niet meer mogelijk.</p>
      )}
    </PaymentResultCard>
  );
}

import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import PaymentResultCard from '../components/PaymentResultCard';
import { ApiError, api, CookieSaleOrderStatus } from '../services/api';

type PageState =
  | { kind: 'pending' }
  | { kind: 'paid'; confirmationNumber: string }
  | { kind: 'failed' }
  | { kind: 'error' };

export default function CookieSalePaymentSuccessPage() {
  const [params] = useSearchParams();
  const sessionId = params.get('session_id');
  const [state, setState] = useState<PageState>({ kind: 'pending' });

  useEffect(() => {
    if (!sessionId) {
      setState({ kind: 'error' });
      return;
    }

    let active = true;
    let timeoutId: number | undefined;
    let attempts = 0;

    const applyStatus = (status: CookieSaleOrderStatus) => {
      if (status.paymentStatus === 'Paid' && status.confirmationNumber) {
        setState({ kind: 'paid', confirmationNumber: status.confirmationNumber });
        return true;
      }

      if (status.paymentStatus === 'Failed' || status.paymentStatus === 'Cancelled') {
        setState({ kind: 'failed' });
        return true;
      }

      setState({ kind: 'pending' });
      return false;
    };

    const poll = async () => {
      attempts += 1;
      try {
        const status = await api.getCookieSaleOrderStatus(sessionId);
        if (!active || applyStatus(status)) return;
      } catch (error) {
        if (!active) return;
        if (!(error instanceof ApiError) || error.status !== 404) {
          setState({ kind: 'error' });
          return;
        }
      }

      if (attempts < 10 && active) {
        timeoutId = window.setTimeout(poll, 2000);
      }
    };

    void poll();
    return () => {
      active = false;
      if (timeoutId !== undefined) window.clearTimeout(timeoutId);
    };
  }, [sessionId]);

  const content = state.kind === 'paid'
    ? (
      <>
        <p>Je betaling werd bevestigd. Je ontvangt ook een bevestiging per e-mail.</p>
        <p className="mt-4 rounded-lg bg-primary-50 p-4 text-primary-900">
          Bevestigingsnummer:{' '}
          <strong className="font-mono">{state.confirmationNumber}</strong>
        </p>
      </>
    )
    : state.kind === 'failed'
      ? <p>De betaling kon niet bevestigd worden. Er werd geen betaalde bestelling geregistreerd.</p>
      : state.kind === 'error'
        ? <p>We konden de betalingsstatus niet ophalen. Neem contact met ons op als dit probleem blijft bestaan.</p>
        : <p>Je betaling wordt bevestigd. Dit kan enkele ogenblikken duren.</p>;

  return (
    <PaymentResultCard
      icon={state.kind === 'paid' ? '✅' : state.kind === 'failed' ? '⚠️' : '⏳'}
      title={state.kind === 'paid' ? 'Bestelling bevestigd' : 'Betaling verwerken'}
      actions={(
        <>
          <Link to="/koekjesverkoop" className="btn-primary">Terug naar koekjesverkoop</Link>
          <Link to="/contact" className="btn-secondary">Hulp nodig?</Link>
        </>
      )}
    >
      {content}
    </PaymentResultCard>
  );
}

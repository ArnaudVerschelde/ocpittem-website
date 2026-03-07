import { Link } from 'react-router-dom';
import Section from '../components/Section';

export default function NotFoundPage() {
  return (
    <Section className="flex min-h-[60vh] items-center justify-center">
      <div className="text-center">
        <p className="font-display text-7xl font-extrabold text-primary-500">404</p>
        <h1 className="mt-4 text-2xl font-bold text-gray-900">Pagina niet gevonden</h1>
        <p className="mt-2 text-gray-500">
          De pagina die je zoekt bestaat niet of is verplaatst.
        </p>
        <Link to="/" className="btn-primary mt-8 inline-block">
          Terug naar home
        </Link>
      </div>
    </Section>
  );
}

import { Link } from 'react-router-dom';
import Logo from './Logo';

export default function Footer() {
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-gray-100 bg-gray-50">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-4">
          {/* Brand */}
          <div className="sm:col-span-2 lg:col-span-1">
            <div className="flex items-center gap-3">
              <Logo size={36} />
              <span className="font-display text-base font-bold text-gray-900">
                Oudercomité met PIT!
              </span>
            </div>
            <p className="mt-3 text-sm leading-relaxed text-gray-500">
              Het oudercomité van de basisschool PIT! van Pittem.
              Samen zorgen we voor onvergetelijke momenten voor onze kinderen!
            </p>
          </div>

          {/* Navigatie */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-wider text-gray-900">
              Navigatie
            </h3>
            <ul className="mt-4 space-y-2">
              {[
                { to: '/', label: 'Home' },
                { to: '/wat-doen-we', label: 'Wat doen we?' },
                { to: '/wie-zijn-we', label: 'Wie zijn we?' },
                { to: '/activiteiten', label: 'Activiteiten' },
              ].map((item) => (
                <li key={item.to}>
                  <Link
                    to={item.to}
                    className="text-sm text-gray-500 transition-colors hover:text-primary-600"
                  >
                    {item.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-wider text-gray-900">
              Contact
            </h3>
            <ul className="mt-4 space-y-2 text-sm text-gray-500">
              <li>
                <a
                  href="mailto:oudercomitepittem@gmail.com"
                  className="transition-colors hover:text-primary-600"
                >
                  oudercomitepittem@gmail.com
                </a>
              </li>
              <li>Pittem, West-Vlaanderen</li>
            </ul>
          </div>

          {/* Social */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-wider text-gray-900">
              Volg ons
            </h3>
            <div className="mt-4 flex gap-3">
              <a
                href="https://www.facebook.com/oudercomitemetpit"
                target="_blank"
                rel="noopener noreferrer"
                className="flex h-10 w-10 items-center justify-center rounded-full bg-gray-200 text-gray-600 transition-colors hover:bg-primary-500 hover:text-white"
                aria-label="Facebook"
              >
                <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M22 12c0-5.523-4.477-10-10-10S2 6.477 2 12c0 4.991 3.657 9.128 8.438 9.878v-6.987h-2.54V12h2.54V9.797c0-2.506 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562V12h2.773l-.443 2.89h-2.33v6.988C18.343 21.128 22 16.991 22 12z" />
                </svg>
              </a>
            </div>
          </div>
        </div>

        <div className="mt-10 border-t border-gray-200 pt-6 text-center">
          <p className="text-xs text-gray-400">
            &copy; {year} Oudercomité met PIT! — Pittem. Alle rechten voorbehouden.
          </p>
        </div>
      </div>
    </footer>
  );
}

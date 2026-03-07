import { useState } from 'react';
import { Link, NavLink } from 'react-router-dom';
import Logo from './Logo';

const navItems = [
  { to: '/', label: 'Home' },
  { to: '/wat-doen-we', label: 'Wat doen we?' },
  { to: '/wie-zijn-we', label: 'Wie zijn we?' },
  { to: '/activiteiten', label: 'Activiteiten' },
  { to: '/contact', label: 'Contact' },
];

export default function Navbar() {
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <header className="sticky top-0 z-50 bg-white/95 shadow-sm backdrop-blur">
      <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        {/* Logo */}
        <Link to="/" className="flex items-center gap-3">
          <Logo size={40} />
          <div className="hidden sm:block">
            <span className="font-display text-lg font-bold text-gray-900">
              Oudercomité met Pit
            </span>
            <span className="block text-xs text-gray-500">Pittem</span>
          </div>
        </Link>

        {/* Desktop nav */}
        <ul className="hidden items-center gap-1 md:flex">
          {navItems.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  `rounded-lg px-4 py-2 text-sm font-medium transition-colors ${
                    isActive
                      ? 'bg-primary-50 text-primary-700'
                      : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                  }`
                }
              >
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>

        {/* Mobile hamburger */}
        <button
          type="button"
          className="inline-flex items-center justify-center rounded-lg p-2 text-gray-600 hover:bg-gray-100 md:hidden"
          onClick={() => setMobileOpen(!mobileOpen)}
          aria-label="Menu openen"
        >
          {mobileOpen ? (
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          ) : (
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          )}
        </button>
      </nav>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="border-t border-gray-100 bg-white md:hidden">
          <ul className="space-y-1 px-4 py-3">
            {navItems.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    `block rounded-lg px-4 py-2.5 text-sm font-medium transition-colors ${
                      isActive
                        ? 'bg-primary-50 text-primary-700'
                        : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                    }`
                  }
                  onClick={() => setMobileOpen(false)}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
      )}
    </header>
  );
}

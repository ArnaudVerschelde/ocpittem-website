import { useState, FormEvent } from 'react';
import Hero from '../components/Hero';
import Section from '../components/Section';

interface ContactForm {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export default function ContactPage() {
  const [form, setForm] = useState<ContactForm>({
    name: '',
    email: '',
    subject: '',
    message: '',
  });
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess(false);
    setLoading(true);

    try {
      const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await fetch(`${apiBase}/contact`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(form),
      });

      if (!res.ok) {
        throw new Error('Er ging iets mis.');
      }

      setSuccess(true);
      setForm({ name: '', email: '', subject: '', message: '' });
    } catch {
      setError('Er ging iets mis bij het versturen. Probeer het later opnieuw.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Hero
        title="Contact"
        subtitle="Heb je een vraag, suggestie of wil je graag meehelpen? Laat het ons weten!"
        backgroundClass="bg-gradient-to-br from-secondary-500 via-secondary-600 to-accent-700"
      />

      <Section>
        <div className="grid gap-12 lg:grid-cols-5">
          {/* Contact info */}
          <div className="lg:col-span-2">
            <h2 className="section-title">Neem contact op</h2>
            <p className="section-subtitle">
              Je kan ons bereiken via het formulier of rechtstreeks via e-mail. We proberen zo snel
              mogelijk te antwoorden.
            </p>

            <div className="mt-10 space-y-6">
              <div className="flex gap-4">
                <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl bg-primary-100 text-primary-600">
                  <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M21.75 6.75v10.5a2.25 2.25 0 01-2.25 2.25h-15a2.25 2.25 0 01-2.25-2.25V6.75m19.5 0A2.25 2.25 0 0019.5 4.5h-15a2.25 2.25 0 00-2.25 2.25m19.5 0v.243a2.25 2.25 0 01-1.07 1.916l-7.5 4.615a2.25 2.25 0 01-2.36 0L3.32 8.91a2.25 2.25 0 01-1.07-1.916V6.75" />
                  </svg>
                </div>
                <div>
                  <p className="font-semibold text-gray-900">E-mail</p>
                  <a
                    href="mailto:oudercomitepittem@gmail.com"
                    className="text-sm text-primary-600 hover:text-primary-700"
                  >
                    oudercomitepittem@gmail.com
                  </a>
                </div>
              </div>

              <div className="flex gap-4">
                <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl bg-primary-100 text-primary-600">
                  <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path strokeLinecap="round" strokeLinejoin="round" d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 1115 0z" />
                  </svg>
                </div>
                <div>
                  <p className="font-semibold text-gray-900">Locatie</p>
                  <p className="text-sm text-gray-500">Pittem, West-Vlaanderen</p>
                </div>
              </div>

              <div className="flex gap-4">
                <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl bg-primary-100 text-primary-600">
                  <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M22 12c0-5.523-4.477-10-10-10S2 6.477 2 12c0 4.991 3.657 9.128 8.438 9.878v-6.987h-2.54V12h2.54V9.797c0-2.506 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562V12h2.773l-.443 2.89h-2.33v6.988C18.343 21.128 22 16.991 22 12z" />
                  </svg>
                </div>
                <div>
                  <p className="font-semibold text-gray-900">Facebook</p>
                  <a
                    href="https://www.facebook.com/oudercomitemetpit"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm text-primary-600 hover:text-primary-700"
                  >
                    Oudercomité met PIT!
                  </a>
                </div>
              </div>
            </div>
          </div>

          {/* Form */}
          <div className="lg:col-span-3">
            <div className="rounded-2xl bg-white p-8 shadow-xl ring-1 ring-gray-100">
              <h3 className="text-xl font-bold text-gray-900">Stuur ons een bericht</h3>

              {success && (
                <div className="mt-4 rounded-lg bg-green-50 p-4 text-sm text-green-700">
                  ✅ Bedankt voor je bericht! We nemen zo snel mogelijk contact met je op.
                </div>
              )}

              <form onSubmit={handleSubmit} className="mt-6 space-y-5">
                <div className="grid gap-5 sm:grid-cols-2">
                  <div>
                    <label htmlFor="contact-name" className="block text-sm font-medium text-gray-700">
                      Naam
                    </label>
                    <input
                      id="contact-name"
                      type="text"
                      required
                      value={form.name}
                      onChange={(e) => setForm({ ...form, name: e.target.value })}
                      className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200"
                      placeholder="Jouw naam"
                    />
                  </div>
                  <div>
                    <label htmlFor="contact-email" className="block text-sm font-medium text-gray-700">
                      E-mailadres
                    </label>
                    <input
                      id="contact-email"
                      type="email"
                      required
                      value={form.email}
                      onChange={(e) => setForm({ ...form, email: e.target.value })}
                      className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200"
                      placeholder="jouw@email.be"
                    />
                  </div>
                </div>

                <div>
                  <label htmlFor="contact-subject" className="block text-sm font-medium text-gray-700">
                    Onderwerp
                  </label>
                  <input
                    id="contact-subject"
                    type="text"
                    required
                    value={form.subject}
                    onChange={(e) => setForm({ ...form, subject: e.target.value })}
                    className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200"
                    placeholder="Waar gaat je bericht over?"
                  />
                </div>

                <div>
                  <label htmlFor="contact-message" className="block text-sm font-medium text-gray-700">
                    Bericht
                  </label>
                  <textarea
                    id="contact-message"
                    required
                    rows={5}
                    value={form.message}
                    onChange={(e) => setForm({ ...form, message: e.target.value })}
                    className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2.5 text-gray-900 shadow-sm focus:border-primary-500 focus:ring-2 focus:ring-primary-200"
                    placeholder="Typ hier je bericht..."
                  />
                </div>

                {error && (
                  <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</div>
                )}

                <button
                  type="submit"
                  disabled={loading}
                  className="btn-primary w-full disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {loading ? 'Versturen...' : 'Verstuur bericht'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </Section>
    </>
  );
}

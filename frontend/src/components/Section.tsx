import { ReactNode } from 'react';

interface SectionProps {
  id?: string;
  className?: string;
  children: ReactNode;
}

export default function Section({ id, className = '', children }: SectionProps) {
  return (
    <section id={id} className={`px-4 py-16 sm:px-6 sm:py-20 lg:px-8 ${className}`}>
      <div className="mx-auto max-w-7xl">{children}</div>
    </section>
  );
}

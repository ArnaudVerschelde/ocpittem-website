import { ReactNode } from 'react';
import Section from './Section';

interface PaymentResultCardProps {
  icon: string;
  title: string;
  children: ReactNode;
  actions: ReactNode;
}

export default function PaymentResultCard({
  icon,
  title,
  children,
  actions,
}: PaymentResultCardProps) {
  return (
    <Section>
      <div className="mx-auto max-w-2xl rounded-2xl bg-white p-8 text-center shadow-xl ring-1 ring-gray-100">
        <div className="text-5xl">{icon}</div>
        <h1 className="mt-4 text-3xl font-bold text-gray-900">{title}</h1>
        <div className="mt-3 text-gray-600">{children}</div>
        <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:justify-center">
          {actions}
        </div>
      </div>
    </Section>
  );
}

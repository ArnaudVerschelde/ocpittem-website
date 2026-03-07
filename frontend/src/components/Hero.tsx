interface HeroProps {
  title: string;
  subtitle?: string;
  backgroundClass?: string;
}

export default function Hero({ title, subtitle, backgroundClass = 'bg-gradient-to-br from-primary-400 via-primary-500 to-primary-700' }: HeroProps) {
  return (
    <div className={`relative overflow-hidden ${backgroundClass}`}>
      {/* Decorative circles */}
      <div className="absolute -left-20 -top-20 h-72 w-72 rounded-full bg-white/10" />
      <div className="absolute -bottom-16 -right-16 h-56 w-56 rounded-full bg-white/10" />

      <div className="relative mx-auto max-w-7xl px-4 py-20 sm:px-6 sm:py-28 lg:px-8">
        <h1 className="font-display text-4xl font-extrabold tracking-tight text-white sm:text-5xl lg:text-6xl">
          {title}
        </h1>
        {subtitle && (
          <p className="mt-6 max-w-2xl text-lg leading-relaxed text-white/90">{subtitle}</p>
        )}
      </div>
    </div>
  );
}

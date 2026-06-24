import { useCallback, useEffect, useState } from 'react';

interface ImageCarouselProps {
  images: string[];
  autoAdvanceMs?: number;
}

export default function ImageCarousel({ images, autoAdvanceMs = 4000 }: ImageCarouselProps) {
  const [current, setCurrent] = useState(0);
  const [paused, setPaused] = useState(false);

  const count = images.length;

  const goTo = useCallback(
    (index: number) => {
      if (count === 0) return;
      setCurrent(((index % count) + count) % count);
    },
    [count],
  );

  const next = useCallback(() => goTo(current + 1), [current, goTo]);
  const prev = useCallback(() => goTo(current - 1), [current, goTo]);

  // Houd de huidige index geldig wanneer de fotolijst verandert.
  useEffect(() => {
    if (current > count - 1) setCurrent(0);
  }, [count, current]);

  // Automatisch doorbladeren (pauzeert bij hover/focus).
  useEffect(() => {
    if (count <= 1 || paused || autoAdvanceMs <= 0) return;
    const id = window.setInterval(() => {
      setCurrent((c) => (c + 1) % count);
    }, autoAdvanceMs);
    return () => window.clearInterval(id);
  }, [count, paused, autoAdvanceMs]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLDivElement>) => {
    if (e.key === 'ArrowRight') {
      e.preventDefault();
      next();
    } else if (e.key === 'ArrowLeft') {
      e.preventDefault();
      prev();
    }
  };

  if (count === 0) return null;

  return (
    <div
      className="relative mx-auto w-full max-w-4xl focus:outline-none"
      role="region"
      aria-roledescription="carrousel"
      aria-label="Sfeerbeelden Bal Parental 2026"
      tabIndex={0}
      onKeyDown={handleKeyDown}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocus={() => setPaused(true)}
      onBlur={() => setPaused(false)}
    >
      <div className="relative aspect-[4/3] w-full overflow-hidden rounded-2xl bg-gray-100 shadow-lg ring-1 ring-gray-200 sm:aspect-[16/9]">
        {images.map((src, index) => (
          <img
            key={src}
            src={src}
            alt={`Sfeerbeeld Bal Parental 2026 — foto ${index + 1} van ${count}`}
            loading={index === current ? 'eager' : 'lazy'}
            aria-hidden={index !== current}
            className={`absolute inset-0 h-full w-full object-cover transition-opacity duration-500 ${
              index === current ? 'opacity-100' : 'opacity-0'
            }`}
          />
        ))}

        {count > 1 && (
          <>
            <button
              type="button"
              onClick={prev}
              aria-label="Vorige foto"
              className="absolute left-3 top-1/2 -translate-y-1/2 rounded-full bg-white/80 p-2 text-gray-800 shadow-md transition hover:bg-white focus:outline-none focus:ring-2 focus:ring-primary-400"
            >
              <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <button
              type="button"
              onClick={next}
              aria-label="Volgende foto"
              className="absolute right-3 top-1/2 -translate-y-1/2 rounded-full bg-white/80 p-2 text-gray-800 shadow-md transition hover:bg-white focus:outline-none focus:ring-2 focus:ring-primary-400"
            >
              <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
              </svg>
            </button>
          </>
        )}
      </div>
    </div>
  );
}

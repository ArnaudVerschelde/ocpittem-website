import { useCallback, useEffect, useState } from 'react';
import type { GalleryImage } from '../services/api';

interface ImageGalleryGridProps {
  images: GalleryImage[];
}

export default function ImageGalleryGrid({ images }: ImageGalleryGridProps) {
  const [lightboxIndex, setLightboxIndex] = useState<number | null>(null);

  const count = images.length;

  const close = useCallback(() => setLightboxIndex(null), []);
  const showNext = useCallback(
    () => setLightboxIndex((i) => (i === null ? i : (i + 1) % count)),
    [count],
  );
  const showPrev = useCallback(
    () => setLightboxIndex((i) => (i === null ? i : (i - 1 + count) % count)),
    [count],
  );

  useEffect(() => {
    if (lightboxIndex === null) return;

    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close();
      else if (e.key === 'ArrowRight') showNext();
      else if (e.key === 'ArrowLeft') showPrev();
    };

    document.addEventListener('keydown', handleKey);
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', handleKey);
      document.body.style.overflow = '';
    };
  }, [lightboxIndex, close, showNext, showPrev]);

  if (count === 0) return null;

  return (
    <>
      <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4">
        {images.map((image, index) => (
          <li key={image.name}>
            <button
              type="button"
              onClick={() => setLightboxIndex(index)}
              aria-label={`Bekijk foto ${index + 1} groot`}
              className="group block aspect-square w-full overflow-hidden rounded-xl bg-gray-100 ring-1 ring-gray-200 focus:outline-none focus:ring-2 focus:ring-primary-400"
            >
              <img
                src={image.thumbnailUrl}
                alt={`Sfeerbeeld Bal Parental 2026 — foto ${index + 1} van ${count}`}
                loading="lazy"
                className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
              />
            </button>
          </li>
        ))}
      </ul>

      {lightboxIndex !== null && (
        <div
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/90 p-4"
          role="dialog"
          aria-modal="true"
          aria-label="Foto groot bekijken"
          onClick={close}
        >
          <button
            type="button"
            onClick={close}
            aria-label="Sluiten"
            className="absolute right-4 top-4 rounded-full bg-white/10 p-2 text-white transition hover:bg-white/20 focus:outline-none focus:ring-2 focus:ring-white"
          >
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>

          {count > 1 && (
            <>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  showPrev();
                }}
                aria-label="Vorige foto"
                className="absolute left-4 top-1/2 -translate-y-1/2 rounded-full bg-white/10 p-2 text-white transition hover:bg-white/20 focus:outline-none focus:ring-2 focus:ring-white"
              >
                <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
                </svg>
              </button>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  showNext();
                }}
                aria-label="Volgende foto"
                className="absolute right-4 top-1/2 -translate-y-1/2 rounded-full bg-white/10 p-2 text-white transition hover:bg-white/20 focus:outline-none focus:ring-2 focus:ring-white"
              >
                <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                </svg>
              </button>
            </>
          )}

          <img
            src={images[lightboxIndex].originalUrl}
            alt={`Sfeerbeeld Bal Parental 2026 — foto ${lightboxIndex + 1} van ${count}`}
            onClick={(e) => e.stopPropagation()}
            className="max-h-[85vh] max-w-full rounded-lg object-contain shadow-2xl"
          />
        </div>
      )}
    </>
  );
}

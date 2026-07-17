import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import Section from '../components/Section';
import ImageCarousel from '../components/ImageCarousel';
import ImageGalleryGrid from '../components/ImageGalleryGrid';
import { api } from '../services/api';
import type { GalleryImage } from '../services/api';
import { shuffle } from '../utils/shuffle';
import balPoster from '../assets/BalParental2026Poster.jpeg';

type Status = 'loading' | 'ready' | 'empty' | 'error';

type GalleryImages = {
    fotograaf: GalleryImage[];
    photobooth: GalleryImage[];
    sfeerbeelden: GalleryImage[];
};

export default function SfeerbeeldenBalParental2026Page() {
    const [gallery, setGallery] = useState<GalleryImages>({
        fotograaf: [],
        photobooth: [],
        sfeerbeelden: [],
    });

    const [status, setStatus] = useState<Status>('loading');

    const load = useCallback(async () => {
        setStatus('loading');

        try {
            const res = await api.getBalParental2026Gallery();

            const categorized: GalleryImages = {
                fotograaf: [],
                photobooth: [],
                sfeerbeelden: [],
            };

            for (const image of res.images ?? []) {
                if (
                    image.category === 'fotograaf' ||
                    image.category === 'photobooth' ||
                    image.category === 'sfeerbeelden'
                ) {
                    categorized[image.category].push(image);
                }
            }

            const sortImages = (images: GalleryImage[]) =>
                [...images].sort((a, b) =>
                    a.name.localeCompare(b.name, undefined, {
                        numeric: true,
                        sensitivity: 'base',
                    })
                );

            const orderedGallery: GalleryImages = {
                fotograaf: sortImages(categorized.fotograaf),
                photobooth: sortImages(categorized.photobooth),
                sfeerbeelden: sortImages(categorized.sfeerbeelden),
            };

            setGallery(orderedGallery);

            const totalImages =
                orderedGallery.fotograaf.length +
                orderedGallery.photobooth.length +
                orderedGallery.sfeerbeelden.length;

            setStatus(totalImages > 0 ? 'ready' : 'empty');
        } catch (error) {
            console.error(
                'Fout bij het laden van de fotogalerij:',
                error
            );

            setStatus('error');
        }
    }, []);

    useEffect(() => {
        void load();
    }, [load]);

    const carouselImages = useMemo(() => {
        const source =
            gallery.fotograaf.length > 0
                ? gallery.fotograaf
                : [
                    ...gallery.sfeerbeelden,
                    ...gallery.photobooth,
                ];

        return shuffle([...source])
            .slice(0, 12)
            .map((image) => image.originalUrl);
    }, [gallery]);

    return (
        <>
            <section className="relative overflow-hidden bg-black">
                <div className="absolute inset-0">
                    <img
                        src={balPoster}
                        alt=""
                        aria-hidden="true"
                        className="h-full w-full object-cover object-center opacity-25"
                    />

                    <div className="absolute inset-0 bg-gradient-to-br from-black via-[#120018]/90 to-[#3b0764]/80" />

                    <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(236,72,153,0.25),transparent_30%),radial-gradient(circle_at_top_right,rgba(139,92,246,0.30),transparent_35%),radial-gradient(circle_at_bottom,rgba(168,85,247,0.18),transparent_40%)]" />
                </div>

                <div className="relative mx-auto max-w-7xl px-6 py-20 sm:px-8 sm:py-24 lg:px-12">
                    <div className="max-w-3xl">
                        <span className="inline-flex rounded-full border border-fuchsia-400/40 bg-white/10 px-4 py-1 text-sm font-semibold tracking-wide text-fuchsia-200 backdrop-blur">
                            📸 Een terugblik in beeld
                        </span>

                        <h1 className="mt-6 text-4xl font-extrabold uppercase tracking-tight text-white sm:text-5xl lg:text-6xl">
                            <span className="drop-shadow-[0_0_18px_rgba(255,255,255,0.22)]">
                                Sfeerbeelden
                            </span>{' '}
                            <span className="text-fuchsia-300 drop-shadow-[0_0_22px_rgba(217,70,239,0.45)]">
                                Bal Parental 2026
                            </span>
                        </h1>

                        <p className="mt-5 max-w-2xl text-lg leading-8 text-purple-100/90">
                            Herbeleef de gezelligste avond van het jaar. Een terugblik in beeld op ons{' '}
                            <span className="font-semibold text-white">ouderbal</span>.
                        </p>
                    </div>
                </div>
            </section>

            <Section>
                {status === 'loading' && (
                    <div className="flex flex-col items-center justify-center py-20 text-center">
                        <div
                            className="h-12 w-12 animate-spin rounded-full border-4 border-primary-200 border-t-primary-500"
                            role="status"
                            aria-label="Sfeerbeelden laden"
                        />
                        <p className="mt-4 text-gray-600">Sfeerbeelden laden…</p>
                    </div>
                )}

                {status === 'empty' && (
                    <div className="mx-auto max-w-xl py-16 text-center">
                        <h2 className="section-title">Nog geen sfeerbeelden</h2>
                        <p className="section-subtitle">
                            Er zijn voorlopig nog geen foto's beschikbaar.
                        </p>

                        <div className="mt-8">
                            <Link to="/activiteiten" className="btn-secondary">
                                Terug naar activiteiten
                            </Link>
                        </div>
                    </div>
                )}

                {status === 'error' && (
                    <div className="mx-auto max-w-xl py-16 text-center">
                        <h2 className="section-title">Oeps, er ging iets mis</h2>
                        <p className="section-subtitle">
                            De sfeerbeelden konden niet geladen worden.
                        </p>

                        <div className="mt-8">
                            <button type="button" onClick={load} className="btn-primary">
                                Opnieuw proberen
                            </button>
                        </div>
                    </div>
                )}

                {status === 'ready' && (
                    <div className="space-y-20">
                        {carouselImages.length > 0 && (
                            <ImageCarousel images={carouselImages} />
                        )}

                        {gallery.fotograaf.length > 0 && (
                            <div>
                                <h2 className="section-title text-center">
                                    Foto's van de fotograaf
                                </h2>
                                <p className="section-subtitle mx-auto max-w-2xl text-center">
                                    De officiële sfeerbeelden van Bal Parental 2026.
                                </p>

                                <div className="mt-8">
                                    <ImageGalleryGrid images={gallery.fotograaf} />
                                </div>
                            </div>
                        )}

                        {gallery.photobooth.length > 0 && (
                            <div>
                                <h2 className="section-title text-center">Photobooth</h2>
                                <p className="section-subtitle mx-auto max-w-2xl text-center">
                                    Alle leuke en spontane momenten uit onze photobooth.
                                </p>

                                <div className="mt-8">
                                    <ImageGalleryGrid images={gallery.photobooth} />
                                </div>
                            </div>
                        )}

                        {gallery.sfeerbeelden.length > 0 && (
                            <div>
                                <h2 className="section-title text-center">
                                    Extra sfeerbeelden
                                </h2>
                                <p className="section-subtitle mx-auto max-w-2xl text-center">
                                    Nog meer mooie en spontane beelden van de avond.
                                </p>

                                <div className="mt-8">
                                    <ImageGalleryGrid images={gallery.sfeerbeelden} />
                                </div>
                            </div>
                        )}
                    </div>
                )}
            </Section>
        </>
    );
}
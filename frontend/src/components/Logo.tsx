/**
 * Logo component — toont het echte logo als frontend/public/logo.png bestaat,
 * anders de SVG-fallback.
 *
 * Om het echte logo te gebruiken:
 *   1. Download de afbeelding van Facebook
 *   2. Sla op als  frontend/public/logo.png
 *   3. Verander  usePng  hieronder naar  true
 */

const USE_PNG = true;

interface LogoProps {
  size?: number;
}

export default function Logo({ size = 40 }: LogoProps) {
  if (USE_PNG) {
    return (
      <img
        src="/logo.png"
        alt="Oudercomité met Pit logo"
        width={size}
        height={size}
        className="rounded-full object-cover"
        style={{ width: size, height: size }}
      />
    );
  }

  // SVG fallback (zelfde als favicon)
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 64 64"
      width={size}
      height={size}
      aria-label="Oudercomité met Pit logo"
    >
      <defs>
        <linearGradient id="logo-bg" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" stopColor="#26bcbd" />
          <stop offset="100%" stopColor="#13A2A3" />
        </linearGradient>
      </defs>
      <circle cx="32" cy="32" r="30" fill="url(#logo-bg)" />
      <circle cx="32" cy="32" r="30" fill="none" stroke="#0f9091" strokeWidth="1.5" opacity="0.4" />
      <path
        d="M32 44 C32 44 16 34 16 24 C16 19 20 15 24.5 15 C27.5 15 30 17 32 19.5 C34 17 36.5 15 39.5 15 C44 15 48 19 48 24 C48 34 32 44 32 44Z"
        fill="#fff"
        opacity="0.95"
      />
      <text
        x="32"
        y="33"
        textAnchor="middle"
        fontFamily="Arial Black, sans-serif"
        fontWeight="900"
        fontSize="14"
        fill="#13A2A3"
        letterSpacing="-0.5"
      >
        PIT
      </text>
    </svg>
  );
}

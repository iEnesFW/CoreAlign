import React from 'react';

interface LogoProps {
  className?: string;
  size?: number;
  showText?: boolean;
}

export const Logo: React.FC<LogoProps> = ({ className, size = 32, showText = true }) => {
  return (
    <div className={`flex items-center gap-3 ${className ?? ''}`}>
      <svg
        width={size}
        height={size}
        viewBox="0 0 40 40"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        className="shrink-0"
        aria-hidden="true"
      >
        <rect width="40" height="40" rx="8" fill="url(#corealign-logo-gradient)" />
        <path d="M12 12H20V20H12V12Z" fill="white" fillOpacity="0.9" />
        <path d="M20 20H28V28H20V20Z" fill="white" fillOpacity="0.9" />
        <path d="M12 20H20V28H12V20Z" fill="white" fillOpacity="0.5" />
        <defs>
          <linearGradient
            id="corealign-logo-gradient"
            x1="0"
            y1="0"
            x2="40"
            y2="40"
            gradientUnits="userSpaceOnUse"
          >
            <stop stopColor="var(--color-primary-500)" />
            <stop offset="1" stopColor="var(--color-primary-700)" />
          </linearGradient>
        </defs>
      </svg>
      {showText && (
        <span
          className="font-bold tracking-tight text-slate-900 dark:text-white"
          style={{ fontSize: size * 0.7 }}
        >
          CoreAlign
        </span>
      )}
    </div>
  );
};

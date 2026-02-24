import React from 'react';

interface LogoProps {
    className?: string;
    size?: number;
    showText?: boolean;
}

export const Logo: React.FC<LogoProps> = ({ className, size = 32, showText = true }) => {
    return (
        <div className={`flex items-center gap-3 ${className}`} style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <svg
                width={size}
                height={size}
                viewBox="0 0 40 40"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                style={{ flexShrink: 0 }}
            >
                <rect width="40" height="40" rx="8" fill="url(#gradient)" />
                <path d="M12 12H20V20H12V12Z" fill="white" fillOpacity="0.9" />
                <path d="M20 20H28V28H20V20Z" fill="white" fillOpacity="0.9" />
                <path d="M12 20H20V28H12V20Z" fill="white" fillOpacity="0.5" />
                <defs>
                    <linearGradient id="gradient" x1="0" y1="0" x2="40" y2="40" gradientUnits="userSpaceOnUse">
                        <stop stopColor="#3b82f6" />
                        <stop offset="1" stopColor="#2563eb" />
                    </linearGradient>
                </defs>
            </svg>
            {showText && (
                <span style={{
                    fontSize: size * 0.75,
                    fontWeight: 700,
                    letterSpacing: '-0.03em',
                    color: 'var(--color-text)'
                }}>
                    CoreAlign
                </span>
            )}
        </div>
    );
};

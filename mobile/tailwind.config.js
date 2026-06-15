/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./app/**/*.{js,jsx,ts,tsx}', './src/**/*.{js,jsx,ts,tsx}'],
  presets: [require('nativewind/preset')],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#EFF6FF',
          100: '#DBEAFE',
          500: '#3B82F6',
          600: '#2563EB',
          700: '#1D4ED8',
          900: '#0F172A',
        },
        surface: {
          light: '#FFFFFF',
          dark: '#0F172A',
          muted: '#F1F5F9',
        },
        success: { DEFAULT: '#16A34A', soft: '#DCFCE7' },
        warning: { DEFAULT: '#F59E0B', soft: '#FEF3C7' },
        danger: { DEFAULT: '#DC2626', soft: '#FEE2E2' },
      },
      fontSize: {
        'btn-lg': ['20px', { lineHeight: '28px', fontWeight: '600' }],
        'btn-xl': ['24px', { lineHeight: '32px', fontWeight: '700' }],
      },
      spacing: {
        touch: '44px',
        'touch-lg': '56px',
        'touch-xl': '64px',
      },
    },
  },
  plugins: [],
};

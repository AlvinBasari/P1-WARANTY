/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        ink: {
          DEFAULT: '#11161C',
          muted: '#4A5568',
          subtle: '#718096',
        },
        paper: {
          DEFAULT: '#F5F6F8',
          card: '#FFFFFF',
        },
        signal: {
          DEFAULT: '#0E7C7B',
          hover: '#0A5C5B',
          light: '#E6F2F2',
          subtle: 'rgba(14, 124, 123, 0.12)',
        },
        circuit: {
          DEFAULT: '#2B3A67',
          dark: '#1E294B',
          light: '#3C4E85',
        },
        alert: {
          DEFAULT: '#C9821F',
          subtle: 'rgba(201, 130, 31, 0.12)',
          border: 'rgba(201, 130, 31, 0.3)',
        },
        fault: {
          DEFAULT: '#C24141',
          subtle: 'rgba(194, 65, 65, 0.12)',
          border: 'rgba(194, 65, 65, 0.3)',
        },
        stable: {
          DEFAULT: '#1F8F5F',
          subtle: 'rgba(31, 143, 95, 0.12)',
          border: 'rgba(31, 143, 95, 0.3)',
        },
        mist: {
          DEFAULT: '#D8DCE2',
          dark: '#B0B8C4',
          light: '#EEF0F3',
        }
      },
      fontFamily: {
        sans: ['"IBM Plex Sans"', 'system-ui', 'sans-serif'],
        display: ['"Space Grotesk"', 'system-ui', 'sans-serif'],
        mono: ['"IBM Plex Mono"', 'Consolas', 'monospace'],
      },
      borderRadius: {
        card: '6px',
        btn: '4px',
      },
      boxShadow: {
        card: '0 1px 3px rgba(17, 22, 28, 0.06)',
        elevated: '0 4px 12px rgba(17, 22, 28, 0.08)',
        modal: '0 8px 24px rgba(17, 22, 28, 0.12)',
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      }
    },
  },
  plugins: [],
}

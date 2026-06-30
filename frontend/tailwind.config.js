/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts,scss}'],
  theme: {
    extend: {
      // Color palette from DESIGN.md
      colors: {
        // Primary - Purple/Lavender
        primary: {
          900: '#3F2552',
          800: '#4E2D66',
          700: '#5F3B75',
          600: '#72468A',
          500: '#81569B',
          400: '#9C78B2',
          300: '#C7B0D8',
          200: '#E4D8EE',
          100: '#F2ECF8',
        },
        // Neutral
        surface: '#FFFFFF',
        'surface-soft': '#FBF9FE',
        border: '#E8E1EF',
        'border-strong': '#D6CBDD',
        // Text
        text: {
          DEFAULT: '#2F243A',
          muted: '#534560',
          soft: '#6A5B77',
        },
        // Status/Accent
        success: {
          DEFAULT: '#72B77A',
          soft: '#E8F6EA',
        },
        warning: {
          DEFAULT: '#E8B84E',
          soft: '#FFF5D9',
        },
        danger: {
          DEFAULT: '#D96B6B',
          soft: '#FDECEC',
        },
        info: {
          DEFAULT: '#74B7E8',
          soft: '#EAF6FF',
        },
        // Chart colors
        chart: {
          purple: '#9C78B2',
          blue: '#74B7E8',
          green: '#72B77A',
          yellow: '#E8B84E',
          pink: '#E7A6C8',
          orange: '#F2A86B',
        },
      },
      // Font family
      fontFamily: {
        sans: ['Plus Jakarta Sans', 'Inter', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'sans-serif'],
        display: ['Fraunces', 'Georgia', 'serif'],
      },
      // Spacing scale (4px base)
      spacing: {
        '1': '4px',
        '2': '8px',
        '3': '12px',
        '4': '16px',
        '5': '20px',
        '6': '24px',
        '8': '32px',
        '10': '40px',
        '12': '48px',
      },
      // Border radius
      borderRadius: {
        sm: '8px',
        DEFAULT: '8px',
        md: '12px',
        lg: '16px',
        xl: '20px',
        full: '999px',
      },
      // Box shadow
      boxShadow: {
        sm: '0 4px 12px rgba(63, 37, 82, 0.06)',
        DEFAULT: '0 4px 12px rgba(63, 37, 82, 0.06)',
        md: '0 12px 32px rgba(63, 37, 82, 0.09)',
        lg: '0 24px 60px rgba(63, 37, 82, 0.12)',
      },
      // Layout sizes
      width: {
        sidebar: '248px',
        'sidebar-collapsed': '76px',
      },
      height: {
        topbar: '68px',
      },
      // Font sizes
      fontSize: {
        'display-size': ['36px', { lineHeight: '44px', fontWeight: '700' }],
        'page-title': ['28px', { lineHeight: '36px', fontWeight: '700' }],
        'section-title': ['22px', { lineHeight: '30px', fontWeight: '700' }],
        'card-title': ['16px', { lineHeight: '24px', fontWeight: '700' }],
        body: ['14px', { lineHeight: '22px', fontWeight: '400' }],
        'table-text': ['13px', { lineHeight: '20px' }],
        label: ['12px', { lineHeight: '16px', fontWeight: '600' }],
        'metric': ['26px', { lineHeight: '34px', fontWeight: '800' }],
        'metric-sm': ['18px', { lineHeight: '26px', fontWeight: '700' }],
      },
      // Animation
      transitionDuration: {
        DEFAULT: '200ms',
      },
      transitionTimingFunction: {
        DEFAULT: 'ease',
      },
    },
  },
  plugins: [],
};

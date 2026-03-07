/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#e6f7f7',
          100: '#b3e8e9',
          200: '#80d9da',
          300: '#4dcacb',
          400: '#26bcbd',
          500: '#13A2A3',
          600: '#0f9091',
          700: '#0b7b7c',
          800: '#086667',
          900: '#054546',
        },
        secondary: {
          50: '#e8eaf6',
          100: '#c5cbe9',
          200: '#9fa8da',
          300: '#7985cb',
          400: '#5c6bc0',
          500: '#3f51b5',
          600: '#394aad',
          700: '#3140a4',
          800: '#29379b',
          900: '#1b278d',
        },
        accent: {
          50: '#e0f2f1',
          100: '#b2dfdb',
          200: '#80cbc4',
          300: '#4db6ac',
          400: '#26a69a',
          500: '#009688',
          600: '#00897b',
          700: '#00796b',
          800: '#00695c',
          900: '#004d40',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
        display: ['Poppins', 'Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};

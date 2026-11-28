import { defineConfig, presetWind3 } from 'unocss'

export default defineConfig({
  presets: [presetWind3()],
  shortcuts: {
    'nav-link': 'whitespace-nowrap rounded-app px-3 py-2 text-sm font-medium text-muted hover:bg-brand-soft hover:text-brand',
    'nav-link-active': 'bg-brand-soft text-brand',
  },
  theme: {
    colors: {
      brand: 'var(--rzd-red)',
      'brand-hover': 'var(--rzd-dark-red)',
      'brand-soft': 'var(--rzd-red-soft)',
      secondary: 'var(--rzd-blue)',
      'secondary-soft': 'var(--rzd-blue-soft)',
      graphite: 'var(--rzd-graphite)',
      black: 'var(--rzd-black)',
      surface: 'var(--rzd-surface)',
      canvas: 'var(--rzd-canvas)',
      ink: 'var(--rzd-graphite)',
      muted: 'var(--rzd-muted)',
      line: 'var(--rzd-line)',
      danger: '#b42318',
      success: '#18794e',
      'success-soft': 'var(--semantic-success-soft)',
      warning: '#a15c00',
      'warning-soft': 'var(--semantic-warning-soft)',
      'danger-soft': 'var(--semantic-danger-soft)',
    },
    borderRadius: {
      app: '0.75rem',
    },
  },
})

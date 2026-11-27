import { defineConfig, presetWind3 } from 'unocss'

export default defineConfig({
  presets: [presetWind3()],
  shortcuts: {
    'nav-link': 'whitespace-nowrap rounded-app px-3 py-2 text-sm font-medium text-muted hover:bg-blue-50 hover:text-brand',
    'nav-link-active': 'bg-blue-50 text-brand',
  },
  theme: {
    colors: {
      brand: '#2563eb',
      surface: '#ffffff',
      ink: '#172033',
      muted: '#64748b',
      line: '#dbe2ea',
      danger: '#b42318',
      success: '#18794e',
    },
    borderRadius: {
      app: '0.75rem',
    },
  },
})

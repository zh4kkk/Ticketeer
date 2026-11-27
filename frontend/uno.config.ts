import { defineConfig, presetWind3 } from 'unocss'

export default defineConfig({
  presets: [presetWind3()],
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

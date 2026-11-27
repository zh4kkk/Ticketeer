import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { watch } from 'vue'

import App from './App.vue'
import router from './router'
import { registerUnauthorizedHandler } from './api/http'
import { useAuthStore } from './stores/auth'
import 'virtual:uno.css'
import './assets/main.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

const auth = useAuthStore(pinia)
let redirectInProgress = false

function redirectToLogin(reason: 'expired' | 'unauthorized'): void {
  const currentRoute = router.currentRoute.value
  if (!currentRoute.meta.requiresAuth || currentRoute.name === 'login' || redirectInProgress) return
  redirectInProgress = true
  void router.replace({ name: 'login', query: { reason } }).finally(() => {
    redirectInProgress = false
  })
}

registerUnauthorizedHandler(({ token, generation }) => {
  auth.handleUnauthorized(token, generation)
})
watch(
  () => auth.sessionEndReason,
  (reason) => {
    if (reason) redirectToLogin(reason)
  },
)

app.mount('#app')

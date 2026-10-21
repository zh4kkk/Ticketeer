import { createRouter, createWebHashHistory } from 'vue-router'

const router = createRouter({
  history: createWebHashHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/auth',
      name: 'auth',
      component: () => import('@/pages/AuthPage.vue'),
    },

    {
      path: '/',
      redirect: '/auth',
    },
  ],
})

export default router

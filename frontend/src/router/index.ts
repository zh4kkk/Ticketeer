import { createRouter, createWebHistory, type RouteMeta } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AdminLayout from '@/layouts/AdminLayout.vue'
import CashierLayout from '@/layouts/CashierLayout.vue'
import AdminHomeView from '@/views/AdminHomeView.vue'
import CashierHomeView from '@/views/CashierHomeView.vue'
import LoginView from '@/views/LoginView.vue'
import StatusView from '@/views/StatusView.vue'
import RootView from '@/views/RootView.vue'
import CashierTopicsView from '@/views/cashier/CashierTopicsView.vue'
import CashierTicketsView from '@/views/cashier/CashierTicketsView.vue'
import CashierTicketDetailView from '@/views/cashier/CashierTicketDetailView.vue'
import CashierExamsView from '@/views/cashier/CashierExamsView.vue'
import CashierExamDetailView from '@/views/cashier/CashierExamDetailView.vue'
import CashierQuestionsView from '@/views/cashier/CashierQuestionsView.vue'
import CashierQuestionDetailView from '@/views/cashier/CashierQuestionDetailView.vue'
import CashierRandomTrainingView from '@/views/cashier/CashierRandomTrainingView.vue'
import CashierAttemptView from '@/views/cashier/CashierAttemptView.vue'
import CashierAttemptResultView from '@/views/cashier/CashierAttemptResultView.vue'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    roles?: Array<'Admin' | 'Cashier'>
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'root', component: RootView },
    { path: '/login', name: 'login', component: LoginView },
    {
      path: '/cashier',
      component: CashierLayout,
      meta: { requiresAuth: true, roles: ['Cashier'] } satisfies RouteMeta,
      children: [
        { path: '', name: 'cashier-home', component: CashierHomeView },
        { path: 'topics', name: 'cashier-topics', component: CashierTopicsView },
        { path: 'tickets', name: 'cashier-tickets', component: CashierTicketsView },
        { path: 'tickets/:id', name: 'cashier-ticket-detail', component: CashierTicketDetailView },
        { path: 'exams', name: 'cashier-exams', component: CashierExamsView },
        { path: 'exams/:id', name: 'cashier-exam-detail', component: CashierExamDetailView },
        { path: 'questions', name: 'cashier-questions', component: CashierQuestionsView },
        {
          path: 'questions/:id',
          name: 'cashier-question-detail',
          component: CashierQuestionDetailView,
        },
        {
          path: 'training/random',
          name: 'cashier-random-training',
          component: CashierRandomTrainingView,
        },
        { path: 'attempts/:id', name: 'cashier-attempt', component: CashierAttemptView },
        {
          path: 'attempts/:id/result',
          name: 'cashier-attempt-result',
          component: CashierAttemptResultView,
        },
      ],
    },
    {
      path: '/admin',
      component: AdminLayout,
      meta: { requiresAuth: true, roles: ['Admin'] } satisfies RouteMeta,
      children: [{ path: '', name: 'admin-home', component: AdminHomeView }],
    },
    { path: '/403', name: 'forbidden', component: StatusView, props: { code: 403 } },
    { path: '/:pathMatch(.*)*', name: 'not-found', component: StatusView, props: { code: 404 } },
  ],
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  await auth.initializeSession()
  auth.ensureSessionActive()
  if (to.name === 'root') {
    return auth.isAdmin
      ? { name: 'admin-home' }
      : auth.isCashier
        ? { name: 'cashier-home' }
        : { name: 'login' }
  }
  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return {
      name: 'login',
      query: {
        redirect: to.fullPath,
        ...(auth.sessionEndReason ? { reason: auth.sessionEndReason } : {}),
      },
    }
  }
  if (to.name === 'login' && auth.isAuthenticated) {
    return auth.isAdmin ? { name: 'admin-home' } : { name: 'cashier-home' }
  }
  const roles = to.meta.roles
  if (roles && auth.user && !roles.includes(auth.user.role)) return { name: 'forbidden' }
  return true
})

export default router

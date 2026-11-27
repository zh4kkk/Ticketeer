<script setup lang="ts">
import AppAlert from './AppAlert.vue'
import AppButton from './AppButton.vue'
defineProps<{ message: string; retry?: () => void }>()
</script>
<template>
  <div class="space-y-3"><AppAlert :message="message" /><AppButton v-if="retry" variant="secondary" @click="retry">Повторить</AppButton></div>
</template>

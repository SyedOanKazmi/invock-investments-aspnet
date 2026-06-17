<template>
  <NavBar v-if="auth.isAuthed" />
  <main :class="auth.isAuthed ? 'main' : ''">
    <router-view />
  </main>
</template>

<script setup>
import { useAuth } from './store/auth'
import NavBar from './components/NavBar.vue'
const auth = useAuth()
</script>

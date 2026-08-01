<script setup lang="ts">
// "script setup" = el cuerpo de tu componente funcional de React, pero sin necesidad
// de un return explícito para el JSX (ese va abajo, en <template>)
import { ref } from "vue";
import { useAuthStore } from "../stores/auth";

const authStore = useAuthStore(); // como const auth = useAuthContext() en React

// ref() = useState(). email.value se lee/escribe, como [email, setEmail] pero en un solo objeto
const email = ref("");
const password = ref("");
const error = ref("");
const cargando = ref(false);

async function handleLogin() {
  error.value = "";
  cargando.value = true;
  try {
    await authStore.login(email.value, password.value);
  } catch (e: any) {
    // Leemos el "codigo" que definimos en el backend para el error 401
    error.value = e.response?.data?.detail ?? "Error al iniciar sesión.";
  } finally {
    cargando.value = false;
  }
}
</script>

<template>
  <div class="login-container">
    <form @submit.prevent="handleLogin" class="login-form">
      <h1>MesaSitec</h1>

      <label for="email">Email</label>
      <input
        id="email"
        data-testid="login-email"
        v-model="email"
        type="email"
        required
      />

      <label for="password">Contraseña</label>
      <input
        id="password"
        data-testid="login-password"
        v-model="password"
        type="password"
        required
      />

      <p v-if="error" data-testid="login-error" class="error">{{ error }}</p>

      <button data-testid="login-submit" type="submit" :disabled="cargando">
        {{ cargando ? "Ingresando..." : "Ingresar" }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background: #f4f4f5;
}
.login-form {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  width: 320px;
}
input {
  padding: 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
}
button {
  margin-top: 1rem;
  padding: 0.75rem;
  background: #2563eb;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.error {
  color: #dc2626;
  font-size: 0.875rem;
}
</style>

import { defineStore } from "pinia";
import { ref } from "vue";
import http from "../api/http";
import router from "../router";
import type { Usuario } from "../types/solicitud";

// defineStore = como crear un slice de Zustand, o un Context + Provider en React
// 'auth' es el nombre único de este store
export const useAuthStore = defineStore("auth", () => {
  // ref() = como useState en React. Es el "estado reactivo" de Vue.
  const usuario = ref<Usuario | null>(null);
  const token = ref<string | null>(localStorage.getItem("token"));

  async function login(email: string, password: string) {
    const { data } = await http.post("/auth/login", { email, password });
    token.value = data.accessToken;
    usuario.value = data.usuario;
    localStorage.setItem("token", data.accessToken); // persistimos para recargas de página
    router.push("/solicitudes");
  }

  async function cargarUsuarioActual() {
    // Si ya hay token guardado (ej. al recargar la página), reconstruimos la sesión
    if (!token.value) return;
    const { data } = await http.get("/me");
    usuario.value = data;
  }

  function logout() {
    usuario.value = null;
    token.value = null;
    localStorage.removeItem("token");
    router.push("/login");
  }

  // Lo que el store expone hacia afuera — como el return de un custom hook de React
  return { usuario, token, login, cargarUsuarioActual, logout };
});

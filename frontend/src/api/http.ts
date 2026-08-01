import axios from "axios";
import router from "../router";

// Esto es tu "instancia de axios" de siempre, centralizada en un solo lugar
const http = axios.create({
  baseURL: "http://localhost:5080/api/v1",
});

// Interceptor de request: mete el token en cada petición automáticamente
http.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor de respuesta: si el servidor dice 401, mandamos al login
http.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("token");
      router.push("/login");
    }
    return Promise.reject(error);
  },
);

export default http;

import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/login",
      name: "login",
      component: () => import("../views/LoginView.vue"),
    },
    {
      path: "/solicitudes",
      name: "solicitudes",
      component: () => import("../views/SolicitudesListView.vue"),
      meta: { requiereAuth: true },
    },
    {
      path: "/solicitudes/nueva",
      name: "solicitud-nueva",
      component: () => import("../views/SolicitudFormView.vue"),
      meta: { requiereAuth: true },
    },
    {
      path: "/solicitudes/:id",
      name: "solicitud-detalle",
      component: () => import("../views/SolicitudDetalleView.vue"),
      meta: { requiereAuth: true },
    },

    {
      path: "/solicitudes/:id/editar",
      name: "solicitud-editar",
      component: () => import("../views/SolicitudFormView.vue"),
      meta: { requiereAuth: true },
    },
    { path: "/", redirect: "/solicitudes" },
  ],
});

// El "guard" — se ejecuta antes de entrar a cualquier ruta.
// Es como tu middleware de "ProtectedRoute" en React Router, pero built-in en Vue Router.
router.beforeEach((to) => {
  const token = localStorage.getItem("token");
  if (to.meta.requiereAuth && !token) {
    return "/login";
  }
});

export default router;

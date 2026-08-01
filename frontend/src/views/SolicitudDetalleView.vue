<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import http from "../api/http";
import { useAuthStore } from "../stores/auth";
import type { SolicitudDetalle } from "../types/solicitud";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const id = route.params.id as string;
const cargando = ref(true);
const error = ref("");
const solicitud = ref<SolicitudDetalle | null>(null);

// Estado del modal de acción
const modalAbierto = ref(false);
const accionActual = ref("");
const agentes = ref<{ id: string; nombre: string }[]>([]);
const agenteSeleccionado = ref("");
const motivo = ref("");
const modalError = ref("");

async function cargar() {
  cargando.value = true;
  error.value = "";
  try {
    const { data } = await http.get<SolicitudDetalle>(`/solicitudes/${id}`);
    solicitud.value = data;
  } catch (e) {
    error.value = "No se pudo cargar la solicitud.";
  } finally {
    cargando.value = false;
  }
}

onMounted(() => {
  authStore.cargarUsuarioActual();
  cargar();
});

// computed = como useMemo() — se recalcula solo cuando sus dependencias cambian
const rol = computed(() => authStore.usuario?.rol);
const esPropia = computed(
  () => solicitud.value?.solicitante.id === authStore.usuario?.id,
);

// La regla de visibilidad exacta del enunciado (sección 7.5): qué botones mostrar según estado + rol
const accionesDisponibles = computed(() => {
  if (!solicitud.value) return [];
  const estado = solicitud.value.estado;
  const acciones: string[] = [];

  if (rol.value === "Admin" || rol.value === "Agente") {
    if (estado === "Nueva") acciones.push("asignar");
    if (estado === "Asignada") acciones.push("iniciar", "asignar");
    if (estado === "EnProceso") acciones.push("resolver", "asignar");
    if (estado === "Resuelta") acciones.push("cerrar", "reabrir");
    if (
      rol.value === "Admin" &&
      ["Nueva", "Asignada", "EnProceso"].includes(estado)
    ) {
      acciones.push("cancelar");
    }
  }
  if (rol.value === "Solicitante" && esPropia.value && estado === "Resuelta") {
    acciones.push("cerrar");
  }

  return acciones;
});

async function abrirModal(accion: string) {
  accionActual.value = accion;
  modalError.value = "";
  motivo.value = "";
  agenteSeleccionado.value = "";
  if (accion === "asignar") {
    const { data } = await http.get("/usuarios/agentes");
    agentes.value = data;
  }
  modalAbierto.value = true;
}

async function confirmarAccion() {
  modalError.value = "";
  try {
    const body: any = { accion: accionActual.value };
    if (accionActual.value === "asignar")
      body.agenteId = agenteSeleccionado.value;
    if (accionActual.value === "resolver" || accionActual.value === "cancelar")
      body.motivo = motivo.value;

    await http.post(`/solicitudes/${id}/transiciones`, body);
    modalAbierto.value = false;
    await cargar(); // recargamos para ver el nuevo estado
  } catch (e: any) {
    modalError.value =
      e.response?.data?.detail ?? "Error al ejecutar la acción.";
  }
}
</script>

<template>
  <div class="contenedor">
    <button @click="router.push('/solicitudes')">← Volver</button>

    <p v-if="cargando">Cargando...</p>
    <p v-else-if="error" class="error">{{ error }}</p>

    <div v-else-if="solicitud">
      <h1 data-testid="detalle-codigo">{{ solicitud.codigo }}</h1>
      <h2 data-testid="detalle-titulo">{{ solicitud.titulo }}</h2>
      <p data-testid="detalle-descripcion">{{ solicitud.descripcion }}</p>

      <p>
        Estado:
        <strong data-testid="detalle-estado">{{ solicitud.estado }}</strong>
      </p>
      <p>
        Prioridad:
        <span data-testid="detalle-prioridad">{{ solicitud.prioridad }}</span>
      </p>
      <p>
        Categoría:
        <span data-testid="detalle-categoria">{{
          solicitud.categoria.nombre
        }}</span>
      </p>
      <p>
        Agente:
        <span data-testid="detalle-agente">{{
          solicitud.agente?.nombre ?? "Sin asignar"
        }}</span>
      </p>
      <p>
        Creada:
        <span data-testid="detalle-fecha-creacion">{{
          solicitud.fechaCreacion
        }}</span>
      </p>
      <p>
        Límite SLA:
        <span data-testid="detalle-fecha-limite">{{
          solicitud.fechaLimiteSla
        }}</span>
      </p>
      <p v-if="solicitud.vencida" data-testid="detalle-vencida">VENCIDA</p>
      <p
        v-if="solicitud.motivoResolucion || solicitud.motivoCancelacion"
        data-testid="detalle-motivo"
      >
        {{ solicitud.motivoResolucion || solicitud.motivoCancelacion }}
      </p>

      <div class="acciones">
        <button
          v-if="solicitud.estado === 'Nueva' && esPropia"
          data-testid="btn-editar"
          @click="router.push(`/solicitudes/${id}/editar`)"
        >
          Editar
        </button>

        <button
          v-for="accion in accionesDisponibles"
          :key="accion"
          :data-testid="`btn-accion-${accion}`"
          @click="abrirModal(accion)"
        >
          {{ accion }}
        </button>
      </div>
    </div>

    <!-- Modal de acción -->
    <div v-if="modalAbierto" data-testid="modal-accion" class="modal-overlay">
      <div class="modal">
        <h3>Confirmar: {{ accionActual }}</h3>

        <div v-if="accionActual === 'asignar'">
          <label>Agente</label>
          <select
            data-testid="modal-select-agente"
            v-model="agenteSeleccionado"
          >
            <option value="" disabled>Selecciona...</option>
            <option v-for="a in agentes" :key="a.id" :value="a.id">
              {{ a.nombre }}
            </option>
          </select>
        </div>

        <div v-if="accionActual === 'resolver' || accionActual === 'cancelar'">
          <label>Motivo</label>
          <textarea data-testid="modal-motivo" v-model="motivo"></textarea>
        </div>

        <p v-if="modalError" data-testid="modal-error" class="error">
          {{ modalError }}
        </p>

        <div class="modal-botones">
          <button data-testid="modal-confirmar" @click="confirmarAccion">
            Confirmar
          </button>
          <button data-testid="modal-cancelar" @click="modalAbierto = false">
            Cancelar
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.contenedor {
  padding: 2rem;
  max-width: 700px;
  margin: 0 auto;
}
.acciones {
  display: flex;
  gap: 0.5rem;
  margin-top: 1.5rem;
  flex-wrap: wrap;
}
.acciones button {
  padding: 0.5rem 1rem;
  cursor: pointer;
}
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
}
.modal {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  width: 320px;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.modal-botones {
  display: flex;
  gap: 0.5rem;
  justify-content: flex-end;
  margin-top: 1rem;
}
textarea {
  min-height: 80px;
  padding: 0.5rem;
}
input {
  padding: 0.5rem;
}
.error {
  color: #dc2626;
}
</style>

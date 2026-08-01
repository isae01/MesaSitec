<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import http from "../api/http";
import { useAuthStore } from "../stores/auth";
import type { SolicitudesPaginadas } from "../types/solicitud";

const router = useRouter();
const authStore = useAuthStore();

const cargando = ref(true);
const error = ref("");
const datos = ref<SolicitudesPaginadas | null>(null);
const page = ref(1);

// Filtros
const filtroEstado = ref("");
const filtroPrioridad = ref("");
const filtroBusqueda = ref("");
const filtroVencidas = ref(false);

async function cargar() {
  cargando.value = true;
  error.value = "";
  try {
    const { data } = await http.get<SolicitudesPaginadas>("/solicitudes", {
      params: {
        page: page.value,
        pageSize: 20,
        estado: filtroEstado.value || undefined,
        prioridad: filtroPrioridad.value || undefined,
        q: filtroBusqueda.value || undefined,
        vencidas: filtroVencidas.value || undefined,
      },
    });
    datos.value = data;
  } catch (e) {
    error.value = "No se pudieron cargar las solicitudes.";
  } finally {
    cargando.value = false;
  }
}

onMounted(() => {
  authStore.cargarUsuarioActual();
  cargar();
});

function aplicarFiltros() {
  page.value = 1;
  cargar();
}

function limpiarFiltros() {
  filtroEstado.value = "";
  filtroPrioridad.value = "";
  filtroBusqueda.value = "";
  filtroVencidas.value = false;
  page.value = 1;
  cargar();
}

function irADetalle(id: string) {
  router.push(`/solicitudes/${id}`);
}

function cambiarPagina(nueva: number) {
  page.value = nueva;
  cargar();
}
</script>

<template>
  <div class="contenedor">
    <nav data-testid="app-nav" class="nav">
      <span data-testid="nav-usuario-nombre">{{
        authStore.usuario?.nombre
      }}</span>
      <span data-testid="nav-usuario-rol">{{ authStore.usuario?.rol }}</span>
      <button data-testid="btn-logout" @click="authStore.logout">Salir</button>
    </nav>

    <div class="header">
      <h1>Solicitudes</h1>
      <button
        data-testid="btn-nueva-solicitud"
        @click="router.push('/solicitudes/nueva')"
      >
        + Nueva solicitud
      </button>
    </div>

    <div class="filtros">
      <select
        data-testid="filtro-estado"
        v-model="filtroEstado"
        @change="aplicarFiltros"
      >
        <option value="">Todos los estados</option>
        <option value="Nueva">Nueva</option>
        <option value="Asignada">Asignada</option>
        <option value="EnProceso">EnProceso</option>
        <option value="Resuelta">Resuelta</option>
        <option value="Cerrada">Cerrada</option>
        <option value="Cancelada">Cancelada</option>
      </select>

      <select
        data-testid="filtro-prioridad"
        v-model="filtroPrioridad"
        @change="aplicarFiltros"
      >
        <option value="">Todas las prioridades</option>
        <option value="Baja">Baja</option>
        <option value="Media">Media</option>
        <option value="Alta">Alta</option>
        <option value="Critica">Crítica</option>
      </select>

      <select data-testid="filtro-categoria" disabled>
        <option value="">Todas las categorías</option>
      </select>

      <label>
        <input
          data-testid="filtro-vencidas"
          type="checkbox"
          v-model="filtroVencidas"
          @change="aplicarFiltros"
        />
        Solo vencidas
      </label>

      <input
        data-testid="filtro-busqueda"
        v-model="filtroBusqueda"
        placeholder="Buscar..."
        @keyup.enter="aplicarFiltros"
      />

      <button data-testid="btn-limpiar-filtros" @click="limpiarFiltros">
        Limpiar filtros
      </button>
    </div>
    <!-- Estado: cargando -->
    <p v-if="cargando" data-testid="listado-cargando">Cargando...</p>

    <!-- Estado: error -->
    <p v-else-if="error" class="error">{{ error }}</p>

    <!-- Estado: vacío -->
    <p
      v-else-if="datos && datos.items.length === 0"
      data-testid="listado-vacio"
    >
      No hay solicitudes.
    </p>

    <!-- Estado: con datos -->
    <table v-else-if="datos" data-testid="tabla-solicitudes">
      <thead>
        <tr>
          <th>Código</th>
          <th>Título</th>
          <th>Estado</th>
          <th>Prioridad</th>
          <th>SLA</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="s in datos.items"
          :key="s.id"
          data-testid="fila-solicitud"
          :data-codigo="s.codigo"
          @click="irADetalle(s.id)"
        >
          <td data-testid="celda-codigo">{{ s.codigo }}</td>
          <td>{{ s.titulo }}</td>
          <td data-testid="celda-estado">{{ s.estado }}</td>
          <td data-testid="celda-prioridad">{{ s.prioridad }}</td>
          <td data-testid="celda-sla">
            {{ s.fechaLimiteSla }}
            <span v-if="s.vencida" data-testid="badge-vencida">Vencida</span>
          </td>
        </tr>
      </tbody>
    </table>

    <div v-if="datos" class="paginacion">
      <button
        data-testid="paginacion-anterior"
        :disabled="datos.page <= 1"
        @click="cambiarPagina(datos.page - 1)"
      >
        Anterior
      </button>

      <span data-testid="paginacion-info">
        Página {{ datos.page }} de {{ datos.totalPaginas }} —
        {{ datos.total }} resultados
      </span>

      <button
        data-testid="paginacion-siguiente"
        :disabled="datos.page >= datos.totalPaginas"
        @click="cambiarPagina(datos.page + 1)"
      >
        Siguiente
      </button>
    </div>
  </div>
</template>

<style scoped>
.contenedor {
  padding: 2rem;
  max-width: 1000px;
  margin: 0 auto;
}
.nav {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-bottom: 2rem;
  justify-content: flex-end;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}
table {
  width: 100%;
  border-collapse: collapse;
}
th,
td {
  text-align: left;
  padding: 0.5rem;
  border-bottom: 1px solid #e5e5e5;
}
tbody tr {
  cursor: pointer;
}
tbody tr:hover {
  background: #f9fafb;
}
.paginacion {
  display: flex;
  gap: 1rem;
  align-items: center;
  margin-top: 1rem;
}
.error {
  color: #dc2626;
}
button {
  padding: 0.5rem 1rem;
  cursor: pointer;
}

.filtros {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  align-items: center;
}
.filtros select,
.filtros input {
  padding: 0.4rem;
}
</style>

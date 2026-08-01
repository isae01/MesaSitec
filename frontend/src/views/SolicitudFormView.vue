<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import http from "../api/http";
import type { Prioridad } from "../types/solicitud";

const router = useRouter();
const route = useRoute();

// Si la URL trae :id, estamos editando; si no, estamos creando
const idEdicion = route.params.id as string | undefined;
const esEdicion = computed(() => !!idEdicion);

const categorias = ref<{ id: string; nombre: string }[]>([]);
const titulo = ref("");
const descripcion = ref("");
const categoriaId = ref("");
const prioridad = ref<Prioridad>("Media");

const errorTitulo = ref("");
const errorDescripcion = ref("");
const errorCategoria = ref("");
const errorGeneral = ref("");
const enviando = ref(false);
const cargando = ref(false);

onMounted(async () => {
  const { data } = await http.get("/categorias");
  categorias.value = data;

  if (esEdicion.value) {
    cargando.value = true;
    const { data: s } = await http.get(`/solicitudes/${idEdicion}`);
    titulo.value = s.titulo;
    descripcion.value = s.descripcion;
    categoriaId.value = s.categoria.id;
    prioridad.value = s.prioridad;
    cargando.value = false;
  }
});

function validar(): boolean {
  errorTitulo.value =
    titulo.value.length < 5 || titulo.value.length > 120
      ? "El título debe tener entre 5 y 120 caracteres."
      : "";
  errorDescripcion.value =
    descripcion.value.length < 10 || descripcion.value.length > 4000
      ? "La descripción debe tener entre 10 y 4000 caracteres."
      : "";
  errorCategoria.value = !categoriaId.value ? "Selecciona una categoría." : "";
  return !errorTitulo.value && !errorDescripcion.value && !errorCategoria.value;
}

async function guardar() {
  errorGeneral.value = "";
  if (!validar()) return;

  enviando.value = true;
  try {
    const body = {
      titulo: titulo.value,
      descripcion: descripcion.value,
      categoriaId: categoriaId.value,
      prioridad: prioridad.value,
    };

    if (esEdicion.value) {
      await http.put(`/solicitudes/${idEdicion}`, body);
      router.push(`/solicitudes/${idEdicion}`);
    } else {
      const { data } = await http.post("/solicitudes", body);
      router.push(`/solicitudes/${data.id}`);
    }
  } catch (e: any) {
    errorGeneral.value =
      e.response?.data?.detail ?? "Error al guardar la solicitud.";
  } finally {
    enviando.value = false;
  }
}
</script>

<template>
  <div class="contenedor">
    <button data-testid="form-cancelar" @click="router.back()">
      ← Cancelar
    </button>
    <h1>{{ esEdicion ? "Editar solicitud" : "Nueva solicitud" }}</h1>

    <p v-if="cargando">Cargando...</p>

    <form v-else @submit.prevent="guardar" class="form">
      <label>Título</label>
      <input data-testid="form-titulo" v-model="titulo" />
      <p v-if="errorTitulo" data-testid="error-titulo" class="error">
        {{ errorTitulo }}
      </p>

      <label>Descripción</label>
      <textarea data-testid="form-descripcion" v-model="descripcion"></textarea>
      <p v-if="errorDescripcion" data-testid="error-descripcion" class="error">
        {{ errorDescripcion }}
      </p>

      <label>Categoría</label>
      <select data-testid="form-categoria" v-model="categoriaId">
        <option value="" disabled>Selecciona...</option>
        <option v-for="c in categorias" :key="c.id" :value="c.id">
          {{ c.nombre }}
        </option>
      </select>
      <p v-if="errorCategoria" data-testid="error-categoria" class="error">
        {{ errorCategoria }}
      </p>

      <label>Prioridad</label>
      <select data-testid="form-prioridad" v-model="prioridad">
        <option value="Baja">Baja</option>
        <option value="Media">Media</option>
        <option value="Alta">Alta</option>
        <option value="Critica">Crítica</option>
      </select>

      <p v-if="errorGeneral" class="error">{{ errorGeneral }}</p>

      <button data-testid="form-submit" type="submit" :disabled="enviando">
        {{
          enviando
            ? "Guardando..."
            : esEdicion
              ? "Guardar cambios"
              : "Crear solicitud"
        }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.contenedor {
  padding: 2rem;
  max-width: 500px;
  margin: 0 auto;
}
.form {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-top: 1rem;
}
input,
textarea,
select {
  padding: 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
}
textarea {
  min-height: 100px;
}
button[type="submit"] {
  margin-top: 1rem;
  padding: 0.75rem;
  background: #2563eb;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
.error {
  color: #dc2626;
  font-size: 0.875rem;
}
</style>

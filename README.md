# MesaSitec

Mesa de servicio SaaS multi-tenant para Sitecpro.

## Requisitos previos

- .NET 8 SDK
- Node.js 18+

## Cómo levantar el proyecto

**Backend:**

```bash
cd backend/src/Api
dotnet run
```

Corre en `http://localhost:5080`. La base de datos SQLite se crea y siembra automáticamente al arrancar.

**Frontend:**

```bash
cd frontend
npm install
npm run dev
```

Corre en `http://localhost:5173`.

## Credenciales de prueba

Todas usan la contraseña: `Sitec.2026`

| Email              | Organización      | Rol         |
| ------------------ | ----------------- | ----------- |
| admin@norte.test   | Cooperativa Norte | Admin       |
| agente1@norte.test | Cooperativa Norte | Agente      |
| agente2@norte.test | Cooperativa Norte | Agente      |
| user1@norte.test   | Cooperativa Norte | Solicitante |
| admin@sur.test     | Bufete Sur        | Admin       |
| user1@sur.test     | Bufete Sur        | Solicitante |

## Qué está implementado

- Login JWT, endpoint `/me`
- Listado de solicitudes con paginación, filtros (estado, prioridad, búsqueda, vencidas) y aislamiento por tenant (RN-01)
- Creación y edición de solicitudes con cálculo/recálculo automático de SLA (RN-04) y código correlativo (RN-07)
- Detalle de solicitud con botones de acción según estado/rol (sección 7.5)
- Ejecución de transiciones (asignar, iniciar, resolver, cerrar, reabrir, cancelar) con máquina de estados (RN-02), permisos por rol (RN-03), validación de agente (RN-05) y motivo requerido (RN-06)
- Listado de categorías y de agentes (endpoint adicional no listado en el contrato original, ver DECISIONES.md)
- 9 pruebas unitarias (máquina de estados y cálculo de SLA)

## Qué NO está implementado

- Filtro por categoría en el listado del frontend (el backend sí lo soporta vía query param `categoriaId`, pero el `<select>` en el frontend está deshabilitado por tiempo)
- Docker Compose no incluido
- `tsc --noEmit` y `dotnet test` no se corrieron como parte de un pipeline automatizado; se corrieron manualmente durante el desarrollo

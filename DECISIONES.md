# Decisiones técnicas

## 1. Tres decisiones y sus alternativas

**Serialización de enums como string en JSON.** Por defecto .NET serializa enums como números; configuré `JsonStringEnumConverter` para que coincida con el contrato (que pide `"prioridad": "Alta"`, no `2`). La alternativa era mapear manualmente cada enum en cada DTO.

**Endpoint adicional `GET /usuarios/agentes`, no listado en el contrato de 9 endpoints.** Lo agregué porque el modal de "asignar" necesitaba poblar un selector real de agentes de la organización. La alternativa (que usé primero) era un input de texto libre para pegar el ID manualmente; lo reemplacé por tiempo disponible, ya que un selector real mejora mucho la usabilidad y el enunciado permite agregar funcionalidad no listada si se declara.

**Aislamiento por tenant vía extension methods sobre el token, no vía Global Query Filters de EF Core.** Elegí leer `User.TenantId()` explícitamente en cada consulta en vez de un filtro global, porque era más fácil de verificar visualmente en cada endpoint durante el desarrollo bajo presión de tiempo.

## 2. Qué hice con IA y qué escribí a mano

Usé Claude para generar la mayoría del código base (entidades, controllers, componentes Vue) explicándome cada pieza comparada con Node/Express/Prisma, que es mi stack real — nunca había trabajado con C#/.NET ni Vue antes de esta prueba. Yo revisé cada archivo, lo pegué, y corregí errores de compilación (referencias entre proyectos, versiones de paquetes NuGet, código pegado en el lugar equivocado) con guía paso a paso, entendiendo el porqué de cada error antes de corregirlo.

## 3. Qué haría distinto con una semana más

- Conectaría el filtro de categoría en el frontend
- Agregaría tests de integración de los endpoints (no solo unitarios de dominio)
- Docker Compose para un arranque de un solo comando
- Mejoraría el diseño visual del selector de agente y los mensajes de error del modal

## 4. Dónde me atasqué

Me atasqué repetidamente con la organización de proyectos en .NET: referencias entre `Domain`/`Application`/`Infrastructure`/`Api`, versiones de paquetes NuGet que no coincidían entre proyectos (EF Core 10.x vs 8.x), y una carpeta de tests ubicada dentro de `Domain` que hacía que el proyecto `Domain.csproj` intentara compilar archivos de xUnit sin tener xUnit instalado. Nunca había trabajado con .NET — mi stack es Node/Express/React/Prisma. Lo resolví leyendo los mensajes de error de compilación con calma, comparando cada concepto nuevo con su equivalente en Node que ya conocía, y verificando cada corrección con `dotnet build` antes de seguir avanzando.

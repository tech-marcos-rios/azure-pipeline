# CLAUDE.md — azure-pipeline

Este archivo se carga automáticamente en cada sesión de Claude Code dentro de esta carpeta.

---

## Sobre el proyecto

**Portfolio Ops Dashboard** — tablero interno que muestra salud, estado de disco, estado de BD y contenedores Docker de los proyectos numerados del portfolio deployados en `portfolio-hel1-1` (hoy: MiniStock, Kanban, AI Docs Analyzer). Reemplaza, para arrancar, al plan original de "DocAI Pipeline" (ver sección **Archivado** más abajo — no se descarta retomarlo después, pero no es lo que se está construyendo ahora).

**Decisión tomada el 2026-09-09** (resuelve la idea sin decidir planteada el 2026-09-08 en `02-inventory-api/CLAUDE.md`): el proyecto 06 arranca como este dashboard. Alcance v1, confirmado con Marcos:
- **No incluye `p-aeon`** (server de producción, aislado a propósito del resto — ver `docs/INFRAESTRUCTURA.md`). Se suma después como paso aparte, con su propio agente/token, sin reusar credenciales de `portfolio-hel1-1`.
- **Acceso privado**, no público: Basic Auth a nivel de Caddy (bcrypt vía `caddy hash-password`, nunca la contraseña en texto plano ni en código) — sin login propio en la app. Ver bloque de Caddy en "Deploy previsto" más abajo.
- **Backups**: no implementado — hoy ningún proyecto del portfolio tiene backup automatizado de su BD (ver pendiente en `02-inventory-api/CLAUDE.md`), así que no hay nada que el dashboard pueda leer todavía. Fuera de alcance de la v1.
- **Logs**: sin centralizar (no hay Serilog→sink agregado todavía en ningún proyecto). v1 no los muestra; si hace falta, se suma como conteo de líneas `ERROR` de la última hora vía `docker logs`, no como visor de logs completo.

## Estado

- [x] **Backend** (`api/PortfolioOps.Api/`, .NET 8 Minimal API — no Clean Architecture de 4 capas, es herramienta interna) — implementado y probado localmente (2026-09-09). `GET /api/status` agrega, en paralelo, por cada proyecto de `appsettings.json:MonitoredProjects`:
  - **Health**: `HttpClient` GET al `/health` público de cada proyecto (`HttpHealthCheckService`) — probado en vivo contra los 3 proyectos reales, responden `Healthy`.
  - **Contenedores** (API y DB): estado + uptime vía `Docker.DotNet` contra el socket de Docker (`DockerStatusService.GetContainerStatusAsync`, `InspectContainerAsync`). Sin Docker corriendo localmente, falla en timeout de forma controlada (no tira la app) — pendiente de probar contra el socket real del server.
  - **Base de datos**: tamaño (`pg_database_size`) + conexiones activas (`pg_stat_activity`), conectando por IP directa (no por nombre DNS — ver comentario en `DockerStatusService.GetContainerIpAddressAsync` sobre por qué: el contenedor de ops está en las 3 redes Docker a la vez y todas tienen un servicio llamado `db`, la resolución DNS embebida de Docker sería ambigua). La IP se resuelve inspeccionando el contenedor `-db-1`/`-db` de cada proyecto en su red. Passwords vía `ProjectSecrets:DbPasswords` (env vars, nunca en `appsettings.json` — mismo patrón que el resto del portfolio).
  - **Disco**: `DriveInfo` sobre `/host` (mount read-only del filesystem del host, ver `deploy/docker-compose.yml`) — dentro de un contenedor, `DriveInfo` sin ese mount solo ve el disco del propio contenedor.
  - Sin histórico (solo estado actual) — se suma después si hace falta graficar uptime en el tiempo.
- [x] **Frontend** (`web/`, Next.js 16 + React 19 + TypeScript + Tailwind v4 + TanStack Query, `create-next-app` sin tocar a mano la config base) — implementado y probado en browser real (Playwright, 2026-09-11):
  - Página única (`src/app/page.tsx`, Client Component) hace polling a `/api/status` cada 15s (`refetchInterval`), sin histórico — solo estado actual, igual que el backend.
  - **No le habla directo al backend .NET.** `src/app/api/status/route.ts` es un Route Handler que hace de proxy server-side hacia `API_INTERNAL_URL` (en producción, el hostname interno de Compose `http://api:8080` — ver Deploy). Dos motivos: (1) el contenedor `api` no necesita puerto público en absoluto, solo alcanzable desde `web` por la red Docker interna; (2) el Basic Auth de Caddy en `ops.marcosrios.dev` cubre automáticamente estas llamadas también, sin CORS que configurar.
  - Cards por proyecto (`ProjectCard.tsx`) con badge de salud (`StatusBadge.tsx`, semáforo verde/ámbar/rojo/gris) + 3 filas de detalle (contenedor API, contenedor DB, datos de DB). Barra de uso de disco del server arriba (`DiskUsageBar.tsx`).
  - Los mensajes de error técnicos del backend (`"Container not found"`, `"No password configured"`) se traducen al español en el frontend (`ProjectCard.tsx`, `KNOWN_ERRORS`) — el resto de errores de Docker.DotNet/Npgsql (timeouts, red) quedan sin traducir a propósito, son impredecibles.
  - Verificado con Playwright contra el backend real corriendo en local (`localhost:5040`): sin errores de consola, badges "Healthy" en los 3 proyectos (health HTTP real), filas en rojo esperadas para Docker/DB (no hay Docker corriendo local) — ver screenshot en el historial de la sesión, no versionado.
- [x] **Deploy — código escrito** — `deploy/Dockerfile` (api), `deploy/Dockerfile.web` (Next.js standalone), `deploy/docker-compose.yml` (dos servicios: `api` sin puerto público necesario salvo `127.0.0.1:5040` para debug directo, `web` en `127.0.0.1:5041`), `deploy/.env.example`. **No probado contra el server real todavía.** Antes de confiar en esto:
  - Que los nombres de red Docker (`ministock_default`, `kanban_default`, `ai-docs_default`) sean exactamente esos en el server — son la convención default de Compose (`<project>_default`, derivado del `name:` de cada compose file), no confirmados con `docker network ls` real todavía.
  - Que los nombres de contenedor (`ministock-api-1`, `ministock-db-1`, `kanban-api-1`, `kanban-db-1`, `ai-docs-api`, `ai-docs-db`) coincidan — estos sí están confirmados leyendo cada `docker-compose.yml`/`container_name` del repo.
  - `DbUser` de AI Docs Analyzer (`ai_docs`) tomado de `05-ai-docs-analyzer/api/deploy/.env.example` (documentado, no inventado) — confirmar que el `.env` real del server no lo haya cambiado.
- [x] **CI/CD — código escrito** (2026-09-11):
  - `.github/workflows/ci.yml` — build de `api` (.NET) + lint/build de `web` (Next.js) en cada PR a `master`/`develop`.
  - `.github/workflows/deploy.yml` — en cada push a `master`: build de sanity de ambos, luego SSH a Hetzner (`git reset --hard origin/master` + `docker compose up --build -d` en `/opt/portfolio-ops`). **Decisión de Marcos (2026-09-11):** a diferencia del resto del portfolio (donde `DB_PASSWORD` es la password propia del proyecto, generada una vez, y vive solo en el `.env` del server sin pasar por GitHub), acá `MINISTOCK_DB_PASSWORD`/`KANBAN_DB_PASSWORD`/`AI_DOCS_DB_PASSWORD` son las passwords **ya existentes** de otros 3 proyectos — si alguno la rota, no hay forma de que este proyecto se entere solo. Por eso van como secrets de GitHub (de este repo, no de los otros) y `deploy.yml` las sincroniza a `deploy/.env` en cada deploy (mismo patrón que `ANTHROPIC_API_KEY` en `05-ai-docs-analyzer`, no el patrón de `DB_PASSWORD` normal) — con el mismo guard de "abortar si el secret está vacío" que usa `02-inventory-api` para `CORS_ORIGINS`, para no pisar una password real con un valor vacío.
- [x] **Repo en GitHub y secrets cargados** (2026-09-11) — el repo ya existía (`tech-marcos-rios/azure-pipeline`, creado con el scaffold inicial, rama `master`). Los 6 secrets están cargados y confirmados con `gh secret list`: `HETZNER_HOST` (`2.29.23.254`), `HETZNER_USER` (`deploy`), `HETZNER_SSH_KEY` (contenido de `~/.ssh/p_portfolio_hetzner`, mismos valores que el resto del portfolio) + `MINISTOCK_DB_PASSWORD`/`KANBAN_DB_PASSWORD`/`AI_DOCS_DB_PASSWORD` (leídas por SSH directo de `/opt/ministock/deploy/.env`, `/opt/kanban/deploy/.env` y `/opt/ai-docs-analyzer/api/deploy/.env` en `portfolio-hel1-1`, canalizadas directo a `gh secret set` sin pasar por la terminal de la sesión). De paso confirmé las rutas reales en el server: `/opt/ministock`, `/opt/kanban`, `/opt/ai-docs-analyzer` (esta última con guion, no `/opt/ai-docs` — corregido acá si en algún momento se documentó distinto).
- [ ] Todavía no se hizo el primer push del código de este proyecto al repo (sigue local, sin commitear).
- [ ] Caddy + DNS (`ops.marcosrios.dev` con `basicauth`, apuntando al puerto `:5041` del `web`) — sin hacer.
- [ ] Primer deploy manual por SSH (`/opt/portfolio-ops` no existe todavía en el server) antes de confiar en el CI — mismo criterio que el resto del portfolio.
- [ ] Actualizar `docs/INFRAESTRUCTURA.md` con el proyecto una vez deployado.

## Arquitectura implementada

```
Browser
   │  (único origen: ops.marcosrios.dev, Basic Auth en Caddy)
   ▼
web (Next.js)  ── page.tsx hace polling cada 15s a /api/status (mismo origen)
   │                └─ route.ts (Route Handler) proxea server-side a API_INTERNAL_URL
   ▼
api (.NET)  GET /api/status
        │
        ├─ por cada proyecto en MonitoredProjects (en paralelo):
        │     ├─ HTTP GET  https://api.<proyecto>.marcosrios.dev/health
        │     ├─ Docker    InspectContainer(<api-container>)   → estado, uptime
        │     ├─ Docker    InspectContainer(<db-container>)    → estado, uptime
        │     └─ Docker    IP del <db-container> en su red  →  Postgres directo
        │                  (pg_database_size + pg_stat_activity)
        └─ DriveInfo("/host")  → uso de disco del server
```

## Deploy previsto

| Recurso | Detalle |
|---|---|
| `web` (Next.js) | Docker en `portfolio-hel1-1`, puerto `:5041` — es el único servicio al que Caddy le apunta |
| `api` (.NET) | Docker en el mismo server, puerto `:5040` mapeado solo para debug directo por SSH — `web` le habla por la red interna de Compose (`http://api:8080`), no necesita estar expuesto |
| Acceso | `ops.marcosrios.dev` — privado, Basic Auth en Caddy (no público). Al estar todo detrás de un único origen, el Basic Auth cubre página y llamadas a `/api/status` por igual |
| Docker socket (en `api`) | Montado read-only (`/var/run/docker.sock:ro`) — necesario para inspeccionar contenedores de los otros proyectos |
| Host filesystem (en `api`) | Montado read-only (`/:/host:ro`) — necesario para leer disco real del server, no el del propio contenedor |
| Redes Docker (en `api`) | Se une, además de la suya, a las redes `ministock_default`/`kanban_default`/`ai-docs_default` como `external: true` — no requiere tocar los compose files de esos 3 proyectos |

Bloque de Caddy a agregar (pendiente, no aplicado):
```
ops.marcosrios.dev {
    basicauth {
        marcos <hash-bcrypt-generado-con-caddy-hash-password>
    }
    reverse_proxy localhost:5041
}
```

## Git Flow

El repo ya existe con `master` como rama default (verificado `2026-09-11` — no `main`; corregido acá porque el plan original archivado decía `main`, pero nunca se aplicó git flow real todavía). Sigue el mismo modelo que `04-kanban-saas`/`05-ai-docs-analyzer` (que también usan `master`, no `main`):

- `master` — producción, protegida (PR + CI en verde, sin push directo).
- `develop` — integración. Todavía no existe, crear antes de la primera feature branch.
- `feature/*` / `fix/*` / `chore/*` — ramas de trabajo, se mergean a `develop` vía PR.
- `release/*` / `hotfix/*` — promueven `develop` a `master`.
- Conventional Commits obligatorios (igual que el resto del portafolio numerado).

---

## Archivado — plan original "DocAI Pipeline"

No descartado, solo pospuesto. Si se retoma en el futuro, sigue el plan de abajo (arquitectura Azure Functions + Blob + Service Bus + Cosmos DB para procesar PDFs con IA). Tiempo estimado original: 1 semana.

## Arquitectura prevista

```
[Usuario sube PDF]
        ↓
[Azure Blob Storage: contenedor "uploads"]
        ↓ (BlobTrigger)
[Azure Function: ExtractText]
        ↓ (mensaje a cola)
[Service Bus Queue: "documents-to-process"]
        ↓
[Azure Function: SummarizeWithAI]
        ↓
[Azure Blob Storage: contenedor "results"] + [Cosmos DB: metadata]
        ↓
[Azure Function HTTP: GetResult]
        ↓
[Frontend Next.js muestra el resumen]
```

## Stack

- Azure Functions (.NET 8 isolated worker)
- Azure Blob Storage
- Azure Service Bus
- Azure Cosmos DB (free tier: 1000 RU/s)
- Application Insights (monitoring)
- Bicep o Terraform (Infrastructure as Code)
- Frontend: Next.js mínimo para subir PDF y ver resultado

## Features previstos

1. Frontend con drag-and-drop para subir PDFs.
2. Función serverless que extrae texto del PDF (PdfPig).
3. Cola de Service Bus para desacoplar el procesamiento (resiliente a fallos).
4. Función que llama a Claude/OpenAI para resumir el texto.
5. Resultado guardado en Blob + metadata en Cosmos.
6. Función HTTP para consultar el estado/resultado.
7. Logs centralizados en Application Insights.
8. Deploy con `bicep deploy` (Infrastructure as Code).

## Plan paso a paso

- **Día 1-2:** Infraestructura — Resource Group, `infra/main.bicep` (Storage, Service Bus, Cosmos, Function App, App Insights), `az deployment group create`.
- **Día 3:** Funciones — `ExtractText` (BlobTrigger en `uploads/`), `SummarizeWithAI` (ServiceBusTrigger, llama a Claude API).
- **Día 4:** API HTTP — `GetResult` (GET con jobId).
- **Día 5:** Frontend — formulario de subida (SAS URL directo a Blob), polling al endpoint.
- **Día 6-7:** Polish + docs — diagrama de arquitectura, README de deploy, captura de Application Insights.

## Costos esperados

Con uso ocasional: prácticamente gratis (Functions consumption free tier, Storage <0.50 USD/mes, Service Bus básico y Cosmos DB en free tier, App Insights sin costo a bajo volumen).

## Nota sobre AWS

Si en el futuro conviene portar a AWS, la arquitectura equivalente es S3 → Lambda → SQS → Lambda → S3 + DynamoDB. Prioridad: versión Azure primero (más demandado en mercado hispano corporativo).

---

## Pendientes (DocAI, pospuesto)

- Todo — este plan archivado no tiene código todavía. Si se retoma, seguir el plan paso a paso de arriba.

---

## Documentación

- `README.md` — todavía describe el plan archivado de DocAI Pipeline (arquitectura, features, plan día a día, costos). Pendiente reescribirlo para el Ops Dashboard cuando el frontend y el deploy estén listos.

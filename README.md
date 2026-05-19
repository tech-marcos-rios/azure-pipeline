# 06 — Azure Pipeline: procesamiento serverless de documentos

> GitHub: [tech-marcos-rios/azure-pipeline](https://github.com/tech-marcos-rios/azure-pipeline)

Proyecto de nicho que aprovecha experiencia en cloud. Te diferencia de cualquier junior que solo sabe Vercel. Tiempo estimado: **1 semana**. Estado: 📋 planificado.

## ¿Qué construir?

**"DocAI Pipeline"** — sistema serverless que recibe documentos PDF, los procesa con IA y devuelve un resumen estructurado.

## Arquitectura

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

## Features

1. Frontend con drag-and-drop para subir PDFs.
2. Función serverless que extrae texto del PDF (PdfPig).
3. Cola de Service Bus para desacoplar el procesamiento (resiliente a fallos).
4. Función que llama a Claude/OpenAI para resumir el texto.
5. Resultado guardado en Blob + metadata en Cosmos.
6. Función HTTP para consultar el estado/resultado.
7. Logs centralizados en Application Insights.
8. Deploy con `bicep deploy` (Infrastructure as Code).

## Plan paso a paso

### Día 1-2: Infraestructura
- Crear Resource Group en Azure.
- Definir todo en `infra/main.bicep`: Storage, Service Bus, Cosmos, Function App, App Insights.
- `az deployment group create` para deployar.

### Día 3: Funciones de procesamiento
- `ExtractText`: BlobTrigger en `uploads/`. Extrae texto, encola mensaje.
- `SummarizeWithAI`: ServiceBusTrigger. Llama a Claude API, guarda resultado.

### Día 4: API HTTP
- `GetResult`: HTTP GET con jobId, devuelve estado o resultado.

### Día 5: Frontend
- Formulario para subir PDF (con SAS URL para subir directo a Blob).
- Polling al endpoint para ver cuándo está listo.

### Día 6-7: Polish + docs
- Diagrama de arquitectura.
- README con cómo deployar paso a paso (`az login`, `bicep deploy`, etc.).
- Captura de Application Insights mostrando una ejecución end-to-end.

## Por qué este proyecto

Muy pocos freelance juniors pueden mostrar una arquitectura serverless real con IaC. Esto te abre la puerta a:
- Proyectos de migración a cloud (precios mucho más altos).
- Trabajos de optimización de costos en Azure.
- Mantenimiento de pipelines de procesamiento de datos.

Si te interesa AWS en lugar de Azure, la arquitectura equivalente es S3 → Lambda → SQS → Lambda → S3 + DynamoDB. Conviene tener la versión Azure (más demandado en mercado hispano corporativo) pero podés portearlo a AWS después como bonus.

## Costos esperados

Si lo dejás corriendo con uso ocasional:
- Functions consumption: ~0 USD (free tier 1M invocaciones)
- Storage: <0.50 USD/mes
- Service Bus básico: ~0 USD (free tier)
- Cosmos DB: 0 USD (free tier)
- App Insights: 0 USD para volúmenes bajos

Total: prácticamente gratis para tener el demo online.

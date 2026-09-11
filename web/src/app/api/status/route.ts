import { NextResponse } from "next/server";

// Proxy server-side al backend .NET. El browser nunca le habla directo al
// contenedor "api" — no tiene por qué ser alcanzable desde internet, solo
// desde la red Docker interna de este proyecto (ver deploy/docker-compose.yml).
// De paso, esto deja que el Basic Auth de Caddy (aplicado a todo
// ops.marcosrios.dev) cubra también estas llamadas sin configurar nada aparte.
const API_INTERNAL_URL = process.env.API_INTERNAL_URL ?? "http://localhost:5040";

export async function GET() {
  try {
    const res = await fetch(`${API_INTERNAL_URL}/api/status`, { cache: "no-store" });
    const body = await res.text();
    return new NextResponse(body, {
      status: res.status,
      headers: { "content-type": "application/json" },
    });
  } catch (err) {
    console.error("[api/status proxy] fetch error", err);
    return NextResponse.json({ error: String(err) }, { status: 502 });
  }
}

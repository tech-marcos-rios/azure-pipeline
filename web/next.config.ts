import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Imagen de producción mínima (deploy/Dockerfile) — copia solo lo que
  // el server necesita para correr, sin node_modules completo.
  output: "standalone",
};

export default nextConfig;

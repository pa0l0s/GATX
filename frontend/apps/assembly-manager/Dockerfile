FROM node:22-bookworm-slim AS build
WORKDIR /app
RUN corepack enable
COPY package.json pnpm-workspace.yaml nx.json ./
COPY frontend/apps/assembly-manager/package.json frontend/apps/assembly-manager/package.json
RUN pnpm install
COPY frontend frontend
RUN pnpm build

FROM nginx:1.27-alpine AS final
COPY frontend/apps/assembly-manager/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/frontend/apps/assembly-manager/dist /usr/share/nginx/html
EXPOSE 80

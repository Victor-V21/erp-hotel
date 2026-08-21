# Stage 1: Build the React application
FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/. ./
RUN npm run build -- --outDir dist

# Stage 2: Serve the application with Nginx
FROM nginx:stable-alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

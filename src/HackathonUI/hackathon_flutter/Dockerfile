# Build stage - use official Flutter image
FROM ghcr.io/cirruslabs/flutter:stable AS build-env

# Set working directory
WORKDIR /app

# Copy pubspec files
COPY pubspec.* ./

# Get dependencies
RUN flutter pub get

# Copy app source
COPY . .

# Build web app (bez --web-renderer, usunięte w nowszych wersjach)
RUN flutter build web --release 

# Runtime stage - Nginx
FROM nginx:alpine

# Copy built web app
COPY --from=build-env /app/build/web /usr/share/nginx/html

# Configure nginx for Flutter web
RUN echo 'server { \
    listen 80; \
    server_name localhost; \
    root /usr/share/nginx/html; \
    index index.html; \
    location / { \
        try_files $uri $uri/ /index.html; \
    } \
    # Enable compression \
    gzip on; \
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript; \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]

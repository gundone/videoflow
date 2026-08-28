FROM node:22-alpine
WORKDIR /app

# Install dependencies first (cached layer)
COPY package.json package-lock.json* ./
RUN npm install

# Dev mode with hot reload
EXPOSE 5173
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
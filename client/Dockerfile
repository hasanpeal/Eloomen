FROM node:20-alpine

WORKDIR /app

# Copy package files and install dependencies
COPY package.json package-lock.json* ./
RUN npm ci

# Copy the rest (source code will be mounted as volume in docker-compose)
COPY . .

ENV NODE_ENV=development
ENV NEXT_TELEMETRY_DISABLED=1

# Expose port
EXPOSE 3001

# Run dev server with hot reload
CMD ["npm", "run", "dev"]


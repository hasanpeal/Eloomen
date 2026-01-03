FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# Install dotnet-ef tools for migrations
RUN dotnet tool install --global dotnet-ef --version 9.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy csproj and restore dependencies
COPY server.csproj .
RUN dotnet restore

# Copy everything else (source code will be mounted as volume in docker-compose)
COPY . .

# Expose port
EXPOSE 3000

# Use dotnet watch for hot reload (suppress warnings, only show errors)
ENTRYPOINT ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:3000", "--no-launch-profile", "--verbosity", "minimal"]


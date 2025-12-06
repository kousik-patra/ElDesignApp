# ---------- Build Stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV CI=true
WORKDIR /src

# Install Node.js
#RUN apk add --no-cache nodejs npm
# Install Node.js 20.x (LTS) using Debian package manager
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy source + build JS + publish .NET
COPY . .
WORKDIR /src/NpmJS
RUN npm ci --include=dev --legacy-peer-deps
RUN npm run build:prod

WORKDIR /src
RUN dotnet publish ElDesignApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- Final Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/dist
ENTRYPOINT ["dotnet", "ElDesignApp.dll"]
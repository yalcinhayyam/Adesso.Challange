# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Proje dosyalarını kopyala
COPY ["src/Event.Monitor/Event.Monitor.csproj", "Event.Monitor/"]
COPY ["src/Contract/Contract.csproj", "Contract/"]

# Restore işlemi
RUN dotnet restore "Event.Monitor/Event.Monitor.csproj"

# Kaynak kodları kopyala
COPY ["src/Event.Monitor/", "Event.Monitor/"]
COPY ["src/Contract/", "Contract/"]

# Yayınla
WORKDIR /src/Event.Monitor
RUN dotnet publish -c Release -o /app/publish

# Runtime imajı
FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /app

# (Opsiyonel) Non-root user
RUN adduser --disabled-password --uid 1000 appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Event.Monitor.dll"]

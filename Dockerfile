# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files for layer caching
COPY ["src/WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["src/Services/Services.csproj", "Services/"]
COPY ["src/Repositories/Repositories.csproj", "Repositories/"]

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy source code and build
COPY src/ .
WORKDIR "/src/WebAPI"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Create non-root user
RUN adduser --disabled-password --uid 1000 appuser

WORKDIR /app
COPY --from=build /app/publish .

# Set ownership and switch user
RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 80
ENTRYPOINT ["dotnet", "WebAPI.dll"]
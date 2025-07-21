FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ["src/WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["src/Services/Services.csproj", "Services/"]
COPY ["src/Repositories/Repositories.csproj", "Repositories/"]
COPY ["src/Contract/Contract.csproj", "Contract/"]

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"

# Copy all remaining source code
COPY ["src/WebAPI", "WebAPI/"]
COPY ["src/Services", "Services/"]
COPY ["src/Repositories/", "Repositories/"]
COPY ["src/Contract", "Contract/"]


# Publish the application
WORKDIR "/src/WebAPI"
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN adduser --disabled-password --uid 1000 appuser
COPY --from=build --chown=appuser:appuser /app/publish .
USER appuser
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost/health || exit 1
EXPOSE 80
ENTRYPOINT ["dotnet", "WebAPI.dll"]
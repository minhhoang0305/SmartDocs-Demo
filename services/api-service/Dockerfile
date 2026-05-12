FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy csproj files
COPY ["services/api-service/api-service.csproj", "services/api-service/"]
COPY ["shared/shared.csproj", "shared/"]

# Restore dependencies
RUN dotnet restore "services/api-service/api-service.csproj"

# Copy source code
COPY . .

# Publish app
WORKDIR "/src/services/api-service"

RUN dotnet publish "api-service.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Create non-root user
RUN useradd -m -d /app appuser

# Copy published output
COPY --from=build /app/publish .

# Use non-root user
USER appuser

# Expose HTTP port
EXPOSE 8080

# ASP.NET environment
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "api-service.dll"]
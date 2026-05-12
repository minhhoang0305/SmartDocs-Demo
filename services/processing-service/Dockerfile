FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy csproj files
COPY ["services/processing-service/processing-service.csproj", "services/processing-service/"]
COPY ["shared/shared.csproj", "shared/"]

# Restore packages
RUN dotnet restore "services/processing-service/processing-service.csproj"

# Copy source
COPY . .

# Publish app
WORKDIR "/src/services/processing-service"

RUN dotnet publish "processing-service.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Create non-root user
RUN useradd -m -d /app appuser

# Copy build output
COPY --from=build /app/publish .

# Run as non-root
USER appuser

# Expose gRPC/API port
EXPOSE 7001

ENV ASPNETCORE_URLS=http://+:7001

ENTRYPOINT ["dotnet", "processing-service.dll"]
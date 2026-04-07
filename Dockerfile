# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["UUNATEK.API/UUNATEK.API.csproj", "UUNATEK.API/"]
COPY ["UUNATRK.Application/UUNATRK.Application.csproj", "UUNATRK.Application/"]
COPY ["UUNATEK.Domain/UUNATEK.Domain.csproj", "UUNATEK.Domain/"]

# Restore dependencies
RUN dotnet restore "UUNATEK.API/UUNATEK.API.csproj"

# Copy everything else
COPY . .

# Build the project
WORKDIR "/src/UUNATEK.API"
RUN dotnet build "UUNATEK.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "UUNATEK.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks (optional but useful)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

# Expose port 8080 (default for ASP.NET Core in containers)
EXPOSE 8080

ENTRYPOINT ["dotnet", "UUNATEK.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/Shared/CommerceSphere.Shared.Contracts/CommerceSphere.Shared.Contracts.csproj", "src/Shared/CommerceSphere.Shared.Contracts/"]
COPY ["src/Services/Inventory/CommerceSphere.InventoryService.Domain/CommerceSphere.InventoryService.Domain.csproj", "src/Services/Inventory/CommerceSphere.InventoryService.Domain/"]
COPY ["src/Services/Inventory/CommerceSphere.InventoryService.Application/CommerceSphere.InventoryService.Application.csproj", "src/Services/Inventory/CommerceSphere.InventoryService.Application/"]
COPY ["src/Services/Inventory/CommerceSphere.InventoryService.Infrastructure/CommerceSphere.InventoryService.Infrastructure.csproj", "src/Services/Inventory/CommerceSphere.InventoryService.Infrastructure/"]
COPY ["src/Services/Inventory/CommerceSphere.InventoryService.API/CommerceSphere.InventoryService.API.csproj", "src/Services/Inventory/CommerceSphere.InventoryService.API/"]

RUN dotnet restore "src/Services/Inventory/CommerceSphere.InventoryService.API/CommerceSphere.InventoryService.API.csproj"

COPY . .
RUN dotnet build "src/Services/Inventory/CommerceSphere.InventoryService.API/CommerceSphere.InventoryService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Inventory/CommerceSphere.InventoryService.API/CommerceSphere.InventoryService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.InventoryService.API.dll"]

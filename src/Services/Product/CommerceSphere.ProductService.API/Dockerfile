FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/Shared/CommerceSphere.Shared.Contracts/CommerceSphere.Shared.Contracts.csproj", "src/Shared/CommerceSphere.Shared.Contracts/"]
COPY ["src/Services/Product/CommerceSphere.ProductService.Domain/CommerceSphere.ProductService.Domain.csproj", "src/Services/Product/CommerceSphere.ProductService.Domain/"]
COPY ["src/Services/Product/CommerceSphere.ProductService.Application/CommerceSphere.ProductService.Application.csproj", "src/Services/Product/CommerceSphere.ProductService.Application/"]
COPY ["src/Services/Product/CommerceSphere.ProductService.Infrastructure/CommerceSphere.ProductService.Infrastructure.csproj", "src/Services/Product/CommerceSphere.ProductService.Infrastructure/"]
COPY ["src/Services/Product/CommerceSphere.ProductService.API/CommerceSphere.ProductService.API.csproj", "src/Services/Product/CommerceSphere.ProductService.API/"]

RUN dotnet restore "src/Services/Product/CommerceSphere.ProductService.API/CommerceSphere.ProductService.API.csproj"

COPY . .
RUN dotnet build "src/Services/Product/CommerceSphere.ProductService.API/CommerceSphere.ProductService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Product/CommerceSphere.ProductService.API/CommerceSphere.ProductService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.ProductService.API.dll"]

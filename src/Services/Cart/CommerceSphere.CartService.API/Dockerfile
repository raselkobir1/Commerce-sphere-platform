FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/Shared/CommerceSphere.Shared.Contracts/CommerceSphere.Shared.Contracts.csproj", "src/Shared/CommerceSphere.Shared.Contracts/"]
COPY ["src/Services/Cart/CommerceSphere.CartService.Domain/CommerceSphere.CartService.Domain.csproj", "src/Services/Cart/CommerceSphere.CartService.Domain/"]
COPY ["src/Services/Cart/CommerceSphere.CartService.Application/CommerceSphere.CartService.Application.csproj", "src/Services/Cart/CommerceSphere.CartService.Application/"]
COPY ["src/Services/Cart/CommerceSphere.CartService.Infrastructure/CommerceSphere.CartService.Infrastructure.csproj", "src/Services/Cart/CommerceSphere.CartService.Infrastructure/"]
COPY ["src/Services/Cart/CommerceSphere.CartService.API/CommerceSphere.CartService.API.csproj", "src/Services/Cart/CommerceSphere.CartService.API/"]

RUN dotnet restore "src/Services/Cart/CommerceSphere.CartService.API/CommerceSphere.CartService.API.csproj"

COPY . .
RUN dotnet build "src/Services/Cart/CommerceSphere.CartService.API/CommerceSphere.CartService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Cart/CommerceSphere.CartService.API/CommerceSphere.CartService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.CartService.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/ApiGateway/CommerceSphere.ApiGateway/CommerceSphere.ApiGateway.csproj", "src/ApiGateway/CommerceSphere.ApiGateway/"]

RUN dotnet restore "src/ApiGateway/CommerceSphere.ApiGateway/CommerceSphere.ApiGateway.csproj"

COPY . .
RUN dotnet build "src/ApiGateway/CommerceSphere.ApiGateway/CommerceSphere.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/ApiGateway/CommerceSphere.ApiGateway/CommerceSphere.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.ApiGateway.dll"]

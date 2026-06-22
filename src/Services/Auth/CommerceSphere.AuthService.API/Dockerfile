FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/Shared/CommerceSphere.Shared.Contracts/CommerceSphere.Shared.Contracts.csproj", "src/Shared/CommerceSphere.Shared.Contracts/"]
COPY ["src/Services/Auth/CommerceSphere.AuthService.Domain/CommerceSphere.AuthService.Domain.csproj", "src/Services/Auth/CommerceSphere.AuthService.Domain/"]
COPY ["src/Services/Auth/CommerceSphere.AuthService.Application/CommerceSphere.AuthService.Application.csproj", "src/Services/Auth/CommerceSphere.AuthService.Application/"]
COPY ["src/Services/Auth/CommerceSphere.AuthService.Infrastructure/CommerceSphere.AuthService.Infrastructure.csproj", "src/Services/Auth/CommerceSphere.AuthService.Infrastructure/"]
COPY ["src/Services/Auth/CommerceSphere.AuthService.API/CommerceSphere.AuthService.API.csproj", "src/Services/Auth/CommerceSphere.AuthService.API/"]

RUN dotnet restore "src/Services/Auth/CommerceSphere.AuthService.API/CommerceSphere.AuthService.API.csproj"

COPY . .
RUN dotnet build "src/Services/Auth/CommerceSphere.AuthService.API/CommerceSphere.AuthService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Auth/CommerceSphere.AuthService.API/CommerceSphere.AuthService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.AuthService.API.dll"]

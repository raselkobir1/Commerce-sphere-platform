FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/Shared/CommerceSphere.Shared.Contracts/CommerceSphere.Shared.Contracts.csproj", "src/Shared/CommerceSphere.Shared.Contracts/"]
COPY ["src/Services/Notification/CommerceSphere.NotificationService.Domain/CommerceSphere.NotificationService.Domain.csproj", "src/Services/Notification/CommerceSphere.NotificationService.Domain/"]
COPY ["src/Services/Notification/CommerceSphere.NotificationService.Application/CommerceSphere.NotificationService.Application.csproj", "src/Services/Notification/CommerceSphere.NotificationService.Application/"]
COPY ["src/Services/Notification/CommerceSphere.NotificationService.Infrastructure/CommerceSphere.NotificationService.Infrastructure.csproj", "src/Services/Notification/CommerceSphere.NotificationService.Infrastructure/"]
COPY ["src/Services/Notification/CommerceSphere.NotificationService.API/CommerceSphere.NotificationService.API.csproj", "src/Services/Notification/CommerceSphere.NotificationService.API/"]

RUN dotnet restore "src/Services/Notification/CommerceSphere.NotificationService.API/CommerceSphere.NotificationService.API.csproj"

COPY . .
RUN dotnet build "src/Services/Notification/CommerceSphere.NotificationService.API/CommerceSphere.NotificationService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Notification/CommerceSphere.NotificationService.API/CommerceSphere.NotificationService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.NotificationService.API.dll"]

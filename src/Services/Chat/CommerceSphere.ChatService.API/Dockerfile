FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/CommerceSphere.Shared.Common/CommerceSphere.Shared.Common.csproj", "src/Shared/CommerceSphere.Shared.Common/"]
COPY ["src/Services/Chat/CommerceSphere.ChatService.Domain/CommerceSphere.ChatService.Domain.csproj", "src/Services/Chat/CommerceSphere.ChatService.Domain/"]
COPY ["src/Services/Chat/CommerceSphere.ChatService.Application/CommerceSphere.ChatService.Application.csproj", "src/Services/Chat/CommerceSphere.ChatService.Application/"]
COPY ["src/Services/Chat/CommerceSphere.ChatService.Infrastructure/CommerceSphere.ChatService.Infrastructure.csproj", "src/Services/Chat/CommerceSphere.ChatService.Infrastructure/"]
COPY ["src/Services/Chat/CommerceSphere.ChatService.API/CommerceSphere.ChatService.API.csproj", "src/Services/Chat/CommerceSphere.ChatService.API/"]

RUN dotnet restore "src/Services/Chat/CommerceSphere.ChatService.API/CommerceSphere.ChatService.API.csproj"

COPY . .
RUN dotnet build "src/Services/Chat/CommerceSphere.ChatService.API/CommerceSphere.ChatService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Chat/CommerceSphere.ChatService.API/CommerceSphere.ChatService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CommerceSphere.ChatService.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ProjectME_BE/ProjectME_BE.csproj", "ProjectME_BE/"]
COPY ["ProjectME_BE.Application/ProjectME_BE.Application.csproj", "ProjectME_BE.Application/"]
COPY ["ProjectME_BE.Domain/ProjectME_BE.Domain.csproj", "ProjectME_BE.Domain/"]
COPY ["ProjectME_BE.Infrastructure/ProjectME_BE.Infrastructure.csproj", "ProjectME_BE.Infrastructure/"]
RUN dotnet restore "ProjectME_BE/ProjectME_BE.csproj"

COPY . .
WORKDIR "/src/ProjectME_BE"
RUN dotnet build "ProjectME_BE.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectME_BE.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectME_BE.dll"]


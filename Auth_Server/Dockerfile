FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Auth_Server/Auth_Server.csproj", "Auth_Server/"]
RUN dotnet restore "Auth_Server/Auth_Server.csproj"

COPY . .
WORKDIR "/src/Auth_Server"
RUN dotnet build "Auth_Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth_Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auth_Server.dll"]



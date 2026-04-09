FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# The csproj is in the same directory as the Dockerfile
COPY ["FantasyBazaar.Api.csproj", "."]
RUN dotnet restore "./FantasyBazaar.Api.csproj"

# Copy everything from the current directory
COPY . .
WORKDIR "/src"
RUN dotnet build "./FantasyBazaar.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FantasyBazaar.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FantasyBazaar.Api.dll"]
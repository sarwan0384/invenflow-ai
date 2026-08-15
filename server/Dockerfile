# 1. SDK Image for building the app (.NET 10.0)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files from server directory
COPY ["InvenFlow.Api/InvenFlow.Api.csproj", "InvenFlow.Api/"]
COPY ["InvenFlow.Core/InvenFlow.Core.csproj", "InvenFlow.Core/"]
COPY ["InvenFlow.Infrastructure/InvenFlow.Infrastructure.csproj", "InvenFlow.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "InvenFlow.Api/InvenFlow.Api.csproj"

# Copy everything else and build Release binaries
COPY . .
WORKDIR "/src/InvenFlow.Api"
RUN dotnet build "InvenFlow.Api.csproj" -c Release -o /app/build
RUN dotnet publish "InvenFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Runtime Image for running the app (.NET 10.0)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "InvenFlow.Api.dll"]
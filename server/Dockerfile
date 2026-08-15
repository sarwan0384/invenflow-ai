# Use official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["YourProjectName.csproj", "./"]
RUN dotnet restore "YourProjectName.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "YourProjectName.csproj" -c Release -o /app/build
RUN dotnet publish "YourProjectName.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use runtime image for lightweight production container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YourProjectName.dll"]
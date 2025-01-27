# Etap 1: Build (kompilacja)
FROM mcr.microsoft.com/dotnet/sdk:7.0-jammy AS build
WORKDIR /src

# Skopiuj pliki projektu (csproj, Program.cs etc.)
COPY . . 

# Publikuj w trybie Release do folderu /app
RUN dotnet publish -c Release -o /app

# Etap 2: Runtime (uruchamianie)
FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy
WORKDIR /app

# Skopiuj skompilowane pliki z poprzedniego etapu
COPY --from=build /app .

# Ustaw zmienną środowiskową (np. port)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyProject.dll"]

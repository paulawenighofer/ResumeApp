# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# copy csproj files and restore as distinct layers
COPY API/API.csproj API/
COPY Shared/Shared.csproj Shared/
RUN dotnet restore API/API.csproj

# copy everything else and publish app
COPY . .
WORKDIR /source/API
RUN dotnet publish -c Release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "API.dll"]

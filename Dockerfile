FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy project files first so restore can use Docker layer caching.
COPY API/API.csproj API/
COPY Shared/Shared.csproj Shared/
RUN dotnet restore API/API.csproj

# Copy the full source and publish the API.
COPY . .
WORKDIR /source/API
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app ./
ENTRYPOINT ["dotnet", "API.dll"]

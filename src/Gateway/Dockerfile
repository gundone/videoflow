FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore with project graph (shared Api project)
COPY src/Gateway/Gateway.csproj src/Gateway/
COPY src/Api/VideoFlow.Api.csproj src/Api/
RUN dotnet restore src/Gateway/Gateway.csproj

# Build and publish
COPY . .
WORKDIR /src/src/Gateway
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Gateway.dll"]
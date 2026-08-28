FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore with project graph (shared Api project)
COPY src/AuthService/AuthService.csproj src/AuthService/
COPY src/Api/VideoFlow.Api.csproj src/Api/
RUN dotnet restore src/AuthService/AuthService.csproj

# Build and publish
COPY . .
WORKDIR /src/src/AuthService
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5251
ENV ASPNETCORE_URLS=http://+:5251
ENTRYPOINT ["dotnet", "AuthService.dll"]
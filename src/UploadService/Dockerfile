FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore with project graph (shared Api project)
COPY src/UploadService/UploadService.csproj src/UploadService/
COPY src/Api/VideoFlow.Api.csproj src/Api/
RUN dotnet restore src/UploadService/UploadService.csproj

# Build and publish
COPY . .
WORKDIR /src/src/UploadService
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5009
ENV ASPNETCORE_URLS=http://+:5009
ENTRYPOINT ["dotnet", "UploadService.dll"]
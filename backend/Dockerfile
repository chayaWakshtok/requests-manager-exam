FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RequestsManager.sln ./
COPY src/RequestsManager.Domain/*.csproj src/RequestsManager.Domain/
COPY src/RequestsManager.Application/*.csproj src/RequestsManager.Application/
COPY src/RequestsManager.Infrastructure/*.csproj src/RequestsManager.Infrastructure/
COPY src/RequestsManager.Api/*.csproj src/RequestsManager.Api/
RUN dotnet restore src/RequestsManager.Api/RequestsManager.Api.csproj
COPY src/ src/
RUN dotnet publish src/RequestsManager.Api/RequestsManager.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RequestsManager.Api.dll"]

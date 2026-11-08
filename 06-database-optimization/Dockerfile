FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DatabaseApi/DatabaseApi.csproj src/DatabaseApi/
RUN dotnet restore src/DatabaseApi/DatabaseApi.csproj
COPY src/DatabaseApi/ src/DatabaseApi/
RUN dotnet publish src/DatabaseApi/DatabaseApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --retries=3 CMD curl --fail --silent http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "DatabaseApi.dll"]
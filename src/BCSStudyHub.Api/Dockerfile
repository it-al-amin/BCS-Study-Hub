# Build context: repository root.  docker build -f src/BCSStudyHub.Api/Dockerfile -t bcs-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BCSStudyHub.sln ./
COPY src/ src/
COPY tests/ tests/
RUN dotnet restore BCSStudyHub.sln
RUN dotnet publish src/BCSStudyHub.Api/BCSStudyHub.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    LibraryStorage__RootPath=/var/lib/bcs/library-files
COPY --from=build /app/publish ./
# Library files live on a mounted volume, never inside the image; the app runs as the image's non-root user.
RUN mkdir -p /var/lib/bcs/library-files && chown -R $APP_UID /var/lib/bcs
USER $APP_UID
EXPOSE 8080
# Health: GET /health/live (process) and /health/ready (database) through the web proxy or an orchestrator probe.
ENTRYPOINT ["dotnet", "BCSStudyHub.Api.dll"]

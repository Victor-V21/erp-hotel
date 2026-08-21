# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/src/hotel-erp.Api/hotel-erp.Api.csproj", "backend/src/hotel-erp.Api/"]

RUN dotnet restore "backend/src/hotel-erp.Api/hotel-erp.Api.csproj"

COPY backend/src/. .
WORKDIR /src/backend/src/hotel-erp.Api
RUN dotnet build "hotel-erp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "hotel-erp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client rclone \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "hotel-erp.Api.dll"]

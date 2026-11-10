# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CoffeeNChill.Api.csproj", "./"]
RUN dotnet restore "CoffeeNChill.Api.csproj"

COPY . .
RUN dotnet publish "CoffeeNChill.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoffeeNChill.Api.dll"]
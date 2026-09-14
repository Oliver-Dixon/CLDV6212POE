
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY ["CLDV6212POE.csproj", "./"]
RUN dotnet restore "CLDV6212POE.csproj"

COPY . .
RUN dotnet publish "CLDV6212POE.csproj" \
    -c Release \
    -o /home/site/wwwroot \
    --no-restore


FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

EXPOSE 80

COPY --from=build /home/site/wwwroot /home/site/wwwroot
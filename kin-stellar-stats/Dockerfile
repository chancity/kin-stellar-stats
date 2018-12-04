FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY kin-stellar-stats/Kin.Horizon.Api.Poller.csproj kin-stellar-stats/
RUN dotnet restore kin-stellar-stats/Kin.Horizon.Api.Poller.csproj
COPY . .
WORKDIR /src/kin-stellar-stats
RUN dotnet build Kin.Horizon.Api.Poller.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Kin.Horizon.Api.Poller.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Kin.Horizon.Api.Poller.dll"]

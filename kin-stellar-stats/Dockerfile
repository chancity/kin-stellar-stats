FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY kin-stellar-stats/kin_stellar_stats.csproj kin-stellar-stats/
COPY stellar-dotnet-sdk/stellar-dotnet-sdk.csproj stellar-dotnet-sdk/
RUN dotnet restore kin-stellar-stats/kin_stellar_stats.csproj
COPY . .
WORKDIR /src/kin-stellar-stats
RUN dotnet build kin_stellar_stats.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish kin_stellar_stats.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "kin_stellar_stats.dll"]

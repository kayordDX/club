FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG configuration=Release
WORKDIR /src
COPY Club.Api/Club.Api.csproj Club.Api/
COPY Club.ServiceDefaults/Club.ServiceDefaults.csproj Club.ServiceDefaults/
COPY Directory.Build.props Directory.Packages.props ./
RUN dotnet restore "Club.Api/Club.Api.csproj"
COPY . .
WORKDIR "/src/Club.Api"
RUN dotnet build "Club.Api.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Club.Api.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Club.Api.dll"]

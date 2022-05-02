#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ProgettoTesi.ApiGateway/ProgettoTesi.ApiGateway.csproj", "ProgettoTesi.ApiGateway/"]
RUN dotnet restore "ProgettoTesi.ApiGateway/ProgettoTesi.ApiGateway.csproj"
COPY . .
WORKDIR "/src/ProgettoTesi.ApiGateway"
RUN dotnet build "ProgettoTesi.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProgettoTesi.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProgettoTesi.ApiGateway.dll"]
# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY WebShop.sln ./
COPY src ./src
RUN dotnet restore src/Services/IdentityService/IdentityService.Api.csproj
RUN dotnet publish src/Services/IdentityService/IdentityService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]





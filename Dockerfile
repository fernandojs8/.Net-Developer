FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "WEB/ASPNET Core_Part 2/CasaDoCodigo/CasaDoCodigo.csproj" -c Release -o /app --no-self-contained -m:1
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CasaDoCodigo.dll"]

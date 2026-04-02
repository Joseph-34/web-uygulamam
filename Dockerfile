# Kazan Kazan — ASP.NET Core 10 (MVC + SignalR). Vercel uygun değil; Railway / Render / Azure Container için.
# Build: docker build -t kazandakazan .
# Çalıştır: docker run -p 8080:8080 -e ConnectionStrings__DefaultConnection="..." kazandakazan

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY kazandakazan/kazandakazan/ ./kazandakazan/kazandakazan/
WORKDIR /src/kazandakazan/kazandakazan
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "kazandakazan.dll"]

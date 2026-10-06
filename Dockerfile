FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["PersonalProject.csproj", "./"]

RUN dotnet restore "./PersonalProject.csproj"

COPY . .

RUN dotnet publish "./PersonalProject.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 10000

# Official .NET runtime images provide APP_UID.
# Do not run PhilaLink API as root.
USER $APP_UID

CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet PersonalProject.dll"]

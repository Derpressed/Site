FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY myApp/myApp.csproj myApp/
RUN dotnet restore myApp/myApp.csproj

COPY . .
RUN dotnet publish myApp/myApp.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV DataDirectory=/app/App_Data

EXPOSE 10000
ENTRYPOINT ["dotnet", "myApp.dll"]

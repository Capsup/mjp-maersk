FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MJP.MaerskOfflineTest/MJP.MaerskOfflineTest.csproj", "MJP.MaerskOfflineTest/"]
RUN dotnet restore "MJP.MaerskOfflineTest/MJP.MaerskOfflineTest.csproj"
COPY . .
WORKDIR "/src/MJP.MaerskOfflineTest"
RUN dotnet build "MJP.MaerskOfflineTest.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "MJP.MaerskOfflineTest.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MJP.MaerskOfflineTest.dll"]
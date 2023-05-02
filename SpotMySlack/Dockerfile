FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

ENV Spotify__ClientId=""
ENV Spotify__ClientSecret=""
ENV Slack__Token=""
ENV Slack__UserId=""

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SpotMySlack/SpotMySlack.csproj", "SpotMySlack/"]
RUN dotnet restore "SpotMySlack/SpotMySlack.csproj"
COPY . .
WORKDIR "/src/SpotMySlack"
RUN dotnet build "SpotMySlack.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpotMySlack.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpotMySlack.dll"]

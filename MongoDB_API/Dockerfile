FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /MongoDB_API
COPY ./MongoDB_API.csproj .
RUN dotnet restore MongoDB_API.csproj
COPY . .
RUN dotnet publish MongoDB_API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MongoDB_API.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet restore "HackathonGame.SessionService/HackathonGame.SessionService.csproj"
RUN dotnet publish "HackathonGame.SessionService/HackathonGame.SessionService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENV ASPNETCORE_URLS=http://0.0.0.0:8081
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "HackathonGame.SessionService.dll"]

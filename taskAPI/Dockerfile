FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["taskAPI/taskAPI.csproj", "taskAPI/"]
COPY ["Entities/Entities.csproj", "Entities/"]
RUN dotnet restore "taskAPI/taskAPI.csproj"

COPY . .
WORKDIR /src/taskAPI
RUN dotnet publish "taskAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "taskAPI.dll"]

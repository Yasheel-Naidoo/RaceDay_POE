FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/RaceDay.Mvc/RaceDay.Mvc.csproj", "src/RaceDay.Mvc/"]
RUN dotnet restore "src/RaceDay.Mvc/RaceDay.Mvc.csproj"
COPY . .
WORKDIR "/src/src/RaceDay.Mvc"
RUN dotnet publish "RaceDay.Mvc.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["CoreAPI/CoreAPI.csproj", "CoreAPI/"]
COPY ["Model/Model.csproj", "Model/"]
RUN dotnet restore "CoreAPI/CoreAPI.csproj"
COPY . .
WORKDIR "/src/CoreAPI"
RUN dotnet build "CoreAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreAPI.dll"]
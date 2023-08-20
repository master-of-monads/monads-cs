#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Monads.Web/Monads.Web.csproj", "Monads.Web/"]
RUN dotnet restore "Monads.Web/Monads.Web.csproj"
COPY . .
WORKDIR "/src/Monads.Web"
RUN dotnet build "Monads.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Monads.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Monads.Web.dll"]
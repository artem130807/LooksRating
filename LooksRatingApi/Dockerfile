FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["LooksRatingApi/LooksRatingApi.csproj", "LooksRatingApi/"]
RUN dotnet restore "LooksRatingApi/LooksRatingApi.csproj"
COPY . .
WORKDIR "/src/LooksRatingApi"
RUN dotnet publish "LooksRatingApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LooksRatingApi.dll"]
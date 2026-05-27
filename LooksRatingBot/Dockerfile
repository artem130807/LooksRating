FROM ://microsoft.com AS base
WORKDIR /app

FROM ://microsoft.com AS build
WORKDIR /src
COPY ["LooksRatingBot/LooksRatingBot.csproj", "LooksRatingBot/"]
RUN dotnet restore "LooksRatingBot/LooksRatingBot.csproj"

COPY . .
WORKDIR "/src/LooksRatingBot"
RUN dotnet publish "LooksRatingBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LooksRatingBot.dll"]

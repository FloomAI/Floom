#docker tag floom floomai/floom:v1
#docker push floomai/floom:v1

# Base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

# Install curl
RUN apt-get update \
    && apt-get install -y curl

# Build image 
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Floom.Core.csproj", "."]
RUN dotnet restore "./Floom.Core.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Floom.Core.csproj" -c Release -o /app/build

# Publish image
FROM build AS publish
RUN dotnet publish "Floom.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

RUN ls -la /app/publish

# Final image
FROM base AS final
WORKDIR /app

COPY DLLs/ ./DLLs

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Floom.Core.dll"]

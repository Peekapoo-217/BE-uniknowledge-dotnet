# BE/UniKnowledge/Dockerfile

# Stage 1: Build Image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["UniKnowledge.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish "UniKnowledge.csproj" -c Release -o out

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port 8080 (ASP.NET 8/9 standard)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "UniKnowledge.dll"]

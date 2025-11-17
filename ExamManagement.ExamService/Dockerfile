FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 5145

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ExamManagement.ExamService/ExamManagement.ExamService.csproj", "ExamManagement.ExamService/"]
COPY ["ExamManagement.Common/ExamManagement.Common.csproj", "ExamManagement.Common/"]
COPY ["ExamManagement.Models/ExamManagement.Models.csproj", "ExamManagement.Models/"]
COPY ["ExamManagement.Contracts/ExamManagement.Contracts.csproj", "ExamManagement.Contracts/"]
RUN dotnet restore "ExamManagement.ExamService/ExamManagement.ExamService.csproj"
COPY . .
WORKDIR "/src/ExamManagement.ExamService"
RUN dotnet build "ExamManagement.ExamService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExamManagement.ExamService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExamManagement.ExamService.dll"]

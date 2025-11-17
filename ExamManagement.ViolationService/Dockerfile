FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ExamManagement.ViolationService/ExamManagement.ViolationService.csproj", "ExamManagement.ViolationService/"]
COPY ["ExamManagement.Common/ExamManagement.Common.csproj", "ExamManagement.Common/"]
COPY ["ExamManagement.Models/ExamManagement.Models.csproj", "ExamManagement.Models/"]
COPY ["ExamManagement.Contracts/ExamManagement.Contracts.csproj", "ExamManagement.Contracts/"]
RUN dotnet restore "ExamManagement.ViolationService/ExamManagement.ViolationService.csproj"
COPY . .
WORKDIR "/src/ExamManagement.ViolationService"
RUN dotnet build "ExamManagement.ViolationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExamManagement.ViolationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExamManagement.ViolationService.dll"]

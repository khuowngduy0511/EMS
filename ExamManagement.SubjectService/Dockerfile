FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
EXPOSE 5004

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ExamManagement.SubjectService/ExamManagement.SubjectService.csproj", "ExamManagement.SubjectService/"]
COPY ["ExamManagement.Common/ExamManagement.Common.csproj", "ExamManagement.Common/"]
COPY ["ExamManagement.Models/ExamManagement.Models.csproj", "ExamManagement.Models/"]
COPY ["ExamManagement.Contracts/ExamManagement.Contracts.csproj", "ExamManagement.Contracts/"]
RUN dotnet restore "ExamManagement.SubjectService/ExamManagement.SubjectService.csproj"
COPY . .
WORKDIR "/src/ExamManagement.SubjectService"
RUN dotnet build "ExamManagement.SubjectService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExamManagement.SubjectService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExamManagement.SubjectService.dll"]

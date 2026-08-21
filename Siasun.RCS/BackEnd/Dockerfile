# ========================================================
# 阶段 1：.NET 10 SDK 编译构建环境
# ========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. 拷贝全局构建配置与 NuGet 源配置
COPY ["common.props", "NuGet.Config", "./"]

# 2. 预拷贝 csproj 依赖结构并执行 Restore（建立依赖层缓存）
COPY ["src/SIASUN.RCS.Domain.Shared/SIASUN.RCS.Domain.Shared.csproj", "src/SIASUN.RCS.Domain.Shared/"]
COPY ["src/SIASUN.RCS.Domain/SIASUN.RCS.Domain.csproj", "src/SIASUN.RCS.Domain/"]
COPY ["src/SIASUN.RCS.Application.Contracts/SIASUN.RCS.Application.Contracts.csproj", "src/SIASUN.RCS.Application.Contracts/"]
COPY ["src/SIASUN.RCS.Application/SIASUN.RCS.Application.csproj", "src/SIASUN.RCS.Application/"]
COPY ["src/SIASUN.RCS.EntityFrameworkCore/SIASUN.RCS.EntityFrameworkCore.csproj", "src/SIASUN.RCS.EntityFrameworkCore/"]
COPY ["src/SIASUN.RCS.HttpApi/SIASUN.RCS.HttpApi.csproj", "src/SIASUN.RCS.HttpApi/"]
COPY ["src/SIASUN.RCS.HttpApi.Host/SIASUN.RCS.HttpApi.Host.csproj", "src/SIASUN.RCS.HttpApi.Host/"]

RUN dotnet restore "src/SIASUN.RCS.HttpApi.Host/SIASUN.RCS.HttpApi.Host.csproj"

# 3. 拷贝源码并进行 Release 编译发布
COPY . .
RUN dotnet publish "src/SIASUN.RCS.HttpApi.Host/SIASUN.RCS.HttpApi.Host.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ========================================================
# 阶段 2：.NET 10 生产 Alpine 极简运行时 (~115MB)
# ========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
EXPOSE 9000

ENV ASPNETCORE_URLS=http://+:9000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    TZ=Asia/Shanghai

RUN apk add --no-cache tzdata icu-libs
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SIASUN.RCS.HttpApi.Host.dll"]
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app

# Dependencies
RUN apt-get update && \
    apt-get install -y \
    clang \
    gcc \
    binutils-arm-linux-gnueabihf \
    libc6-dev-armhf-cross \
    gcc-arm-linux-gnueabihf \
    g++-arm-linux-gnueabihf \
    binutils-aarch64-linux-gnu \
    libc6-dev-arm64-cross \
    gcc-aarch64-linux-gnu \
    g++-aarch64-linux-gnu \
    binutils-x86-64-linux-gnu \
    libc6-dev-amd64-cross \
    gcc-x86-64-linux-gnu \
    g++-x86-64-linux-gnu

# Copy sources
FROM base AS build
COPY . .

# Build ARM in parallel
FROM build AS build-arm
RUN dotnet publish Askyl.Dsm.WebHosting.DotnetInstaller/Askyl.Dsm.WebHosting.DotnetInstaller.csproj -r linux-arm -c Release --self-contained -o /app/publish/arm

# Build ARM64 in parallel
FROM build AS build-arm64
RUN dotnet publish Askyl.Dsm.WebHosting.DotnetInstaller/Askyl.Dsm.WebHosting.DotnetInstaller.csproj -r linux-arm64 -c Release --self-contained -o /app/publish/arm64

# Build x64 in parallel
FROM build AS build-x64
RUN dotnet publish Askyl.Dsm.WebHosting.DotnetInstaller/Askyl.Dsm.WebHosting.DotnetInstaller.csproj -r linux-x64 -c Release --self-contained -o /app/publish/amd64

# Merge all builds
FROM scratch AS final
COPY --from=build-arm /app/publish/arm /arm/
COPY --from=build-arm64 /app/publish/arm64 /arm64/
COPY --from=build-x64 /app/publish/amd64 /amd64/

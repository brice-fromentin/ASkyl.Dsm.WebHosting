# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Dependencies
RUN apt-get update && \
    apt-get install -y \
    clang \
    gcc

# Enabling linux-arm cross compilation
RUN apt-get install -y \
    binutils-arm-linux-gnueabihf \
    libc6-dev-armhf-cross \
    gcc-arm-linux-gnueabihf \
    g++-arm-linux-gnueabihf

# Enabling linux-arm64 cross compilation
RUN apt-get install -y \
    binutils-aarch64-linux-gnu \
    libc6-dev-arm64-cross \
    gcc-aarch64-linux-gnu \
    g++-aarch64-linux-gnu

# Enabling linux-x64 cross compilation
RUN apt-get install -y \
    binutils-x86-64-linux-gnu \
    libc6-dev-amd64-cross \
    gcc-x86-64-linux-gnu \
    g++-x86-64-linux-gnu

# Copy sources
COPY . .

# Publish
RUN dotnet publish Askyl.Dsm.WebHosting.DotnetInstaller/Askyl.Dsm.WebHosting.DotnetInstaller.csproj -r linux-arm -c Release --self-contained -o /app/publish/arm
RUN dotnet publish Askyl.Dsm.WebHosting.DotnetInstaller/Askyl.Dsm.WebHosting.DotnetInstaller.csproj -r linux-arm64 -c Release --self-contained -o /app/publish/arm64
RUN dotnet publish Askyl.Dsm.WebHosting.DotnetInstaller/Askyl.Dsm.WebHosting.DotnetInstaller.csproj -r linux-x64 -c Release --self-contained -o /app/publish/amd64

# Get Binaries
FROM scratch
COPY --from=build /app/publish /

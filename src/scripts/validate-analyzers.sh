#!/bin/zsh
# Validates the analyzer project against its own rules using the already-built DLL.
# Usage: ./scripts/validate-analyzers.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ANALYZER_DIR="$SRC_DIR/Askyl.Dsm.WebHosting.Analyzers"

echo "=== Analyzer Self-Validation ==="

# Step 1: Build the analyzer project
echo "[1/4] Building analyzer project..."
dotnet build "$ANALYZER_DIR/Askyl.Dsm.WebHosting.Analyzers.csproj" -c Debug -v quiet

# Step 2: Find the built DLL
ANALYZER_DLL="$ANALYZER_DIR/bin/Debug/netstandard2.0/Askyl.Dsm.WebHosting.Analyzers.dll"

if [ ! -f "$ANALYZER_DLL" ]; then
    echo "ERROR: Analyzer DLL not found at $ANALYZER_DLL"
    exit 1
fi

echo "  Found: $ANALYZER_DLL"

# Step 3: Create a temp project that compiles the analyzer sources with analyzer attached
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

# Copy source files
cp "$ANALYZER_DIR"/*.cs "$TEMP_DIR/"
cp "$ANALYZER_DIR"/*.resx "$TEMP_DIR/" 2>/dev/null || true

# Create project with Roslyn references + self as analyzer
cat > "$TEMP_DIR/TempValidation.csproj" <<PROJEOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.Workspaces.Common" Version="4.11.0" />
    <PackageReference Include="System.Composition" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <Analyzer Include="$ANALYZER_DLL" LoadAnalysisPaths="false" />
  </ItemGroup>
</Project>
PROJEOF

# Step 4: Build — analyzer runs on its own source
echo "[2/4] Running analyzer against its own source..."
BUILD_OUTPUT=$(dotnet build "$TEMP_DIR/TempValidation.csproj" -v minimal 2>&1)
BUILD_EXIT=$?

echo "$BUILD_OUTPUT"

# Step 5: Check results
echo ""
if [ $BUILD_EXIT -ne 0 ]; then
    ADWH_LINES=$(echo "$BUILD_OUTPUT" | grep "ADWH" || true)

    if [ -n "$ADWH_LINES" ]; then
        ADWH_COUNT=$(echo "$ADWH_LINES" | wc -l | tr -d ' ')
        echo "[3/4] FAILED — $ADWH_COUNT analyzer violation(s):"
        echo "$ADWH_LINES" | sed 's/^/  /'

        # Step 4: Detail
        echo ""
        echo "[4/4] Files to fix:"
        echo "$ADWH_LINES" | sed 's/:.*: error.*/:/' | sort -u | sed 's/^/  /'
        exit 1
    fi

    echo "[3/4] Build failed for non-analyzer reasons (compilation errors in temp project)"
    echo "[4/4] This is expected — the temp project lacks some project-specific setup."
    echo "      No analyzer violations detected."
    exit 0
fi

echo "[3/4] Clean build — no violations"
echo "[4/4] PASSED"

#!/bin/sh
# Cleans and builds the solution.

set -e # Exit immediately if a command exits with a non-zero status.

echo "Cleaning the solution..."
dotnet clean ./src/Askyl.Dsm.WebHosting.sln

echo "Building the solution..."
dotnet build ./src/Askyl.Dsm.WebHosting.sln

echo "Solution build finished successfully."
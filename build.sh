#!/bin/bash

# Define the output directory for the build artifacts
CORE_OUTPUT_DIR="./build/Floom.Core"
PLUGINS_OUTPUT_DIR="./build/Floom.Plugins"

rm -rf "./build"

# Create the output directories if they do not exist
mkdir -p "$CORE_OUTPUT_DIR/"
mkdir -p "$CORE_OUTPUT_DIR/DLLs"
mkdir -p "$PLUGINS_OUTPUT_DIR/"

# Restore dependencies
echo "Restoring dependencies for Floom.Plugins..."
dotnet restore "Floom.Plugins/Floom.Plugins.csproj" || exit 1

echo "Restoring dependencies for Floom.Core..."
dotnet restore "Floom.Core/Floom.Core.csproj" || exit 1

# Build and publish Floom.Plugins project
echo "Publishing Floom.Plugins project..."
dotnet publish "Floom.Plugins/Floom.Plugins.csproj" -c Release -o "$PLUGINS_OUTPUT_DIR" /p:SkipPostBuild=true || exit 1

# Copy the built Floom.Plugins DLL to Floom.Core's DLLs directory
cp "$PLUGINS_OUTPUT_DIR"/Floom.Plugins.dll "$CORE_OUTPUT_DIR/DLLs/"

# Build and publish Floom.Core project
echo "Publishing Floom.Core project..."
dotnet publish "Floom.Core/Floom.Core.csproj" -c Release -o "$CORE_OUTPUT_DIR" || exit 1

echo "Build and publish completed. Artifacts are in the build directory."

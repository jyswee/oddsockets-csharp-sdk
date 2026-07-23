# OddSockets .NET SDK - containerised two-client demo.
#
# Builds the SDK straight from this repo (no NuGet publish needed) and runs the
# honest cross-client round-trip against the live OddSockets platform.
#
# Build (from the repo root, so the SDK sources are in context):
#   docker build -f demo/Dockerfile -t oddsockets-csharp-demo .
# Run:
#   docker run --rm -e ODDSOCKETS_API_KEY="ak_..." oddsockets-csharp-demo
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src
# Copy the whole SDK repo (see .dockerignore for what is excluded).
COPY . .

# The demo targets net8.0 only; constrain the referenced SDK to net8.0 too so we
# don't pull the SDK's netstandard2.0 transitive graph just to run the demo.
WORKDIR /src/demo
RUN dotnet restore -p:TargetFrameworks=net8.0
RUN dotnet build -c Release --no-restore -p:TargetFrameworks=net8.0

ENTRYPOINT ["dotnet", "run", "-c", "Release", "--no-build", "-p:TargetFrameworks=net8.0"]

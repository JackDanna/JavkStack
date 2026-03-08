let
  lockFile = builtins.fromJSON (builtins.readFile ../flake.lock);
  nixpkgsLock = lockFile.nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? "x86_64-linux",
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
}:
let
  dotnet-full = {
    sdk = pkgs.dotnetCorePackages.dotnet_10.sdk;
    runtime = pkgs.dotnetCorePackages.aspnetcore_10_0;
  };
in
# Build the .NET application for containerization
pkgs.buildDotnetModule {
  pname = "shared";
  version = "1.0.0";

  inherit src;

  projectFile = "Shared.fsproj";
  nugetDeps = ./deps.json;

  packNupkg = true;

  dotnet-sdk = dotnet-full.sdk;
  dotnet-runtime = dotnet-full.runtime;

  # Build configuration
  buildType = "Release";
  dotnetFlags = [
    "--verbosity"
    "detailed"
  ];

  # Runtime dependencies
  runtimeDeps = with pkgs; [
    openssl
    zlib
    icu
  ];

  meta = with pkgs.lib; {
    description = "";
    platforms = platforms.all;
  };

  passthru = {
    inherit dotnet-full;
  };
}
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
  dotnet-sdk = pkgs.dotnetCorePackages.dotnet_10.sdk;
in
# Build the .NET application for containerization
pkgs.buildDotnetModule {
  pname = "shared";
  version = "1.0.0";

  inherit src;

  projectFile = "Shared.fsproj";
  nugetDeps = ./deps.json;

  packNupkg = true;

  inherit dotnet-sdk;

  # Build configuration
  buildType = "Release";
  dotnetFlags = [
    "--verbosity"
    "detailed"
  ];

  meta = with pkgs.lib; {
    description = "";
    platforms = platforms.all;
  };

  passthru = {
    inherit dotnet-sdk;
  };
}
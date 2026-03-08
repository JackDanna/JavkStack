let
  nixpkgsLock = (builtins.fromJSON (builtins.readFile ../flake.lock)).nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
}:
let
  dotnet-sdk = pkgs.dotnetCorePackages.dotnet_10.sdk;
  nodejs = pkgs.nodejs_22;
  fable = pkgs.fable;
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

  passthru = {
    inherit dotnet-sdk nodejs fable;
  };
}
let
  nixpkgsLock = (builtins.fromJSON (builtins.readFile ../flake.lock)).nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
  version ? builtins.readFile ../version,
}:
let
  dotnet-sdk = pkgs.dotnetCorePackages.dotnet_10.sdk;
  nodejs = pkgs.nodejs_22;
  fable = pkgs.fable;
in
pkgs.buildDotnetModule {
  pname = "shared";

  inherit version src dotnet-sdk;

  projectFile = "Shared.fsproj";
  nugetDeps = ./deps.json;

  packNupkg = true;

  passthru = {
    inherit dotnet-sdk nodejs fable;
  };
}
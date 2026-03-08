let
  lockFile = builtins.fromJSON (builtins.readFile ../flake.lock);
  nixpkgsLock = lockFile.nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
  shared ? import ../Shared/default.nix { inherit nixpkgs system pkgs; },
}:
pkgs.buildDotnetModule {
  pname = "side-effect";
  version = "1.0.0";

  inherit src;

  projectFile = "SideEffect.fsproj";
  nugetDeps = ./deps.json;

  packNupkg = true;

  dotnet-sdk = shared.passthru.dotnet-sdk;

  buildInputs = [
    shared
  ];
}

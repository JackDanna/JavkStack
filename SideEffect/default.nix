let
  nixpkgsLock = (builtins.fromJSON (builtins.readFile ../flake.lock)).nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
  version ? builtins.trim (builtins.readFile ../version),
  shared ? import ../Shared/default.nix { inherit nixpkgs system pkgs version; },
}:
pkgs.buildDotnetModule {
  pname = "side-effect";
  inherit version;

  inherit src;

  projectFile = "SideEffect.fsproj";
  nugetDeps = ./deps.json;

  packNupkg = true;

  dotnet-sdk = shared.passthru.dotnet-sdk;

  buildInputs = [
    shared
  ];
}

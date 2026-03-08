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
  # Target framework moniker (e.g., "net10.0", "net10.0-android", "net10.0-windows10.0.19041.0")
  targetFramework ? "net10.0",
}:
pkgs.buildDotnetModule {
  pname = "side-effect";
  version = "1.0.0";

  inherit src;

  projectFile = "SideEffect.fsproj";
  nugetDeps = ./deps.json;

  packNupkg = true;

  dotnet-sdk = shared.passthru.dotnet-sdk;

  dotnetFlags = [
    "--verbosity"
    "detailed"
    "-p:ContinuousIntegrationBuild=true" # Enable CI build mode to use PackageReference instead of ProjectReference
    "-p:TargetFramework=${targetFramework}"
    "-p:TargetFrameworks=${targetFramework}" # Override TargetFrameworks to prevent pack from building all TFMs
  ];

  buildInputs = [
    shared
  ];
}

let
  nixpkgsLock = (builtins.fromJSON (builtins.readFile ../flake.lock)).nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
  version ? builtins.readFile ../version,
  shared ? import ../Shared/default.nix { inherit nixpkgs system pkgs version; },
  sideEffect ? import ../SideEffect/default.nix {
    inherit
      nixpkgs
      system
      pkgs
      version
      shared
      ;
  },
  webClient ? import ../WebClient/default.nix {
    inherit
      nixpkgs
      system
      pkgs
      version
      shared
      ;
  },
}:
let
  server = pkgs.buildDotnetModule {
    pname = "server";
    inherit version;

    inherit src;

    projectFile = "Server.fsproj";
    nugetDeps = ./deps.json;

    dotnet-sdk = shared.passthru.dotnet-sdk;
    dotnet-runtime = pkgs.dotnetCorePackages.aspnetcore_10_0;

    buildInputs = [
      shared
      sideEffect
    ];

    # The WebClient build process handles frontend compilation automatically
    # Copy any additional static assets if needed
    postInstall = ''
      mkdir -p $out/lib/server/public
      cp -r ${webClient}/dist/* $out/lib/server/public/
    '';

    runtimeDeps = with pkgs; [
      openssl
      zlib
      icu
    ];
  };

  # Create a container image for Azure Container Apps
  container = pkgs.dockerTools.buildLayeredImage {
    name = "container";
    tag = "latest";

    maxLayers = 10;

    contents = with pkgs; [
      server
      cacert
      # Runtime dependencies for .NET
      openssl
      zlib
      icu
      # Create tmp directory structure
      (pkgs.runCommand "create-tmp-dirs" { } ''
        mkdir -p $out/tmp/dpkeys
        chmod 1777 $out/tmp
        chmod 755 $out/tmp/dpkeys
      '')
    ];

    config = {
      Cmd = [ "${server}/bin/Server" ];
      ExposedPorts = {
        "8080/tcp" = { };
      };
      # Environment config with proper DataProtection key storage
      Env = [
        "ASPNETCORE_URLS=http://+:8080"
        "ASPNETCORE_ENVIRONMENT=Production"
        "DOTNET_RUNNING_IN_CONTAINER=true"
        "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false"
        "DataProtection__PersistKeysToFileSystem=/tmp/dpkeys"
        "TMPDIR=/tmp"
      ];
      WorkingDir = "${server}/lib/server";
    };
  };

in
{
  inherit server container;
}

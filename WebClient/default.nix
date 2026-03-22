let
  nixpkgsLock = (builtins.fromJSON (builtins.readFile ../flake.lock)).nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  src ? pkgs.nix-gitignore.gitignoreSource [ ] ./.,
  version ? pkgs.lib.trim (builtins.readFile ../version),
  shared ? import ../Shared/default.nix { inherit nixpkgs system pkgs version; },
  sharedSrc ? pkgs.nix-gitignore.gitignoreSource [ ] ../Shared,
}:
let
  npmDeps = pkgs.fetchNpmDeps {
    name = "web-client-npm-deps";
    src = ./.; # or wherever your package-lock.json is
    hash = "sha256-gNzhzXkWjcG6ijXI+zX/ZxpR3X1dHaqjG26GRfVkVcM="; # Fill in the correct hash after first build attempt
  };
in
pkgs.buildDotnetModule {
  pname = "web-client";
  inherit version;

  inherit src;

  projectFile = "WebClient.fsproj";
  nugetDeps = ./deps.json;

  # Don't use dotnet restore's default behavior - we'll handle it in preBuild
  dontDotnetRestore = true;

  dotnet-sdk = shared.passthru.dotnet-sdk;

  buildInputs = [
    shared
  ];

  # Install additional build dependencies for SAFE Stack
  nativeBuildInputs = with pkgs; [
    shared.passthru.nodejs
    shared.passthru.fable
    python3
  ];

  # Copy npm dependencies before build
  preBuild = ''
    # Provide the Shared source at ../Shared/ so Fable's project cracker can resolve
    # the ProjectReference to ../Shared/Shared.fsproj (Fable evaluates the fsproj without
    # ContinuousIntegrationBuild=true, so it always follows the ProjectReference).
    mkdir -p ../Shared
    cp -r ${sharedSrc}/. ../Shared/
    chmod -R +w ../Shared

    echo "Shared source placed at ../Shared/:"
    ls -la ../Shared/ | head -10

    # Make project files writable for Fable (it modifies them during analysis)
    chmod -R +w .

    # Restore .NET dependencies (follows ProjectReference to ../Shared which is now populated)
    export DOTNET_NOLOGO=1
    dotnet restore WebClient.fsproj -r linux-x64

    # Set up npm cache and install dependencies to client directory
    export npm_config_cache=${npmDeps}
    npm ci --offline --loglevel=verbose

    # Add client build here using node to run vite directly
    export NODE_ENV=production
    fable . -o output --run node node_modules/vite/bin/vite.js build --mode production
  '';

  # Override the build phase to skip dotnet build since Fable handles the compilation
  buildPhase = ''
    runHook preBuild
    echo "Skipping dotnet build - Fable already compiled in preBuild"
    runHook postBuild
  '';

  # Skip the install phase - we don't need dotnet publish for a client-side web app
  dontDotnetInstall = true;
  installPhase = ''
    runHook preInstall

    # Copy the Vite build output to the nix store
    mkdir -p $out/dist
    cp -r dist/* $out/dist/

    runHook postInstall
  '';

  postInstall = ''
    echo "Client build complete. Output in $out/dist"
  '';
}

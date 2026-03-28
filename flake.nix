{
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs =
    {
      self,
      nixpkgs,
      flake-utils,
    }:
    flake-utils.lib.eachDefaultSystem (
      system:
      let
        version = builtins.readFile ./version;
        pkgs = import nixpkgs {
          inherit system;
          config = {
            allowUnfree = true;
            android_sdk.accept_license = true;
          };
        };
        commands = import ./commands.nix { inherit pkgs; };
        
        androidComposition = pkgs.androidenv.composeAndroidPackages {
          cmdLineToolsVersion = "19.0";
          platformVersions = [ "36" ];
          buildToolsVersions = [ "36.1.0" ];
          includeEmulator = true;
          includeSystemImages = true;
          systemImageTypes = [ "google_apis_playstore" ];
          abiVersions = [ "x86_64" ];
        };
      in
      {
        devShells.default = pkgs.mkShell {
          shellHook = ''
            ${commands.set.welcome}
            ${commands.set.commandInstructions} 
            . ${commands.set.changeShellPrompt}
      
            export ANDROID_HOME=${androidComposition.androidsdk}/libexec/android-sdk
            export ANDROID_SDK_ROOT=$ANDROID_HOME
            export LD_LIBRARY_PATH=/run/opengl-driver/lib:${pkgs.libGL}/lib:${pkgs.libGLU}/lib:${pkgs.gtk3}/lib:$LD_LIBRARY_PATH
          '';
          buildInputs = with pkgs; [
            commands
            vscode-fhs
            fantomas
            pulumi-bin
            azure-cli
            crane
            (import ./Shared { inherit pkgs system version; }).passthru.dotnet-sdk
            (import ./Shared { inherit pkgs system version; }).passthru.nodejs
            (import ./Shared { inherit pkgs system version; }).passthru.fable

            jdk17
            androidComposition.androidsdk
            libGL
            libGLU
            gtk3
          ];
        };
      }
    );
}

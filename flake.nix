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
          };
        };
        commands = import ./commands.nix { inherit pkgs; };
      in
      {
        devShells.default = pkgs.mkShell {
          shellHook = ''
            ${commands.set.welcome}
            ${commands.set.commandInstructions} 
            . ${commands.set.changeShellPrompt}
          '';
          buildInputs = with pkgs; [
            commands
            vscode-fhs
            fantomas
            pulumi-bin
            azure-cli
            docker
            (import ./Shared { inherit pkgs system version; }).passthru.dotnet-sdk
            (import ./Shared { inherit pkgs system version; }).passthru.nodejs
            (import ./Shared { inherit pkgs system version; }).passthru.fable
          ];
        };
      }
    );
}

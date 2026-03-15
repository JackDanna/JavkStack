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
            (import ./Shared { inherit pkgs system; }).passthru.dotnet-sdk
            (import ./Shared { inherit pkgs system; }).passthru.nodejs
            (import ./Shared { inherit pkgs system; }).passthru.fable
          ];
        };
      }
    );
}

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
      in
      {
        devShells.default = pkgs.mkShell {
          buildInputs = with pkgs; [
            vscode-fhs
            (import ./Shared { inherit pkgs system; }).passthru.dotnet-sdk
            (import ./Shared { inherit pkgs system; }).passthru.nodejs
            (import ./Shared { inherit pkgs system; }).passthru.fable
          ];
        };
      }
    );
}

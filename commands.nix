let
  nixpkgsLock = (builtins.fromJSON (builtins.readFile ./flake.lock)).nodes.nixpkgs.locked;
in
{
  nixpkgs ? builtins.getFlake "github:${nixpkgsLock.owner}/${nixpkgsLock.repo}/${nixpkgsLock.rev}",
  system ? builtins.currentSystem,
  pkgs ? import nixpkgs { inherit system; },
  prefix ? "c",
}:
let
  commands = pkgs.lib.fix (
    self:
    pkgs.lib.mapAttrs pkgs.writeShellScript {

      repoDir = ''${pkgs.lib.getExe pkgs.git} rev-parse --show-toplevel'';

      cmdInstructions = ''
        echo "press $1-<TAB><TAB> to see all the commands"
      '';

      commandInstructions = ''
        ${self.cmdInstructions} "${prefix}"
      '';

      welcome = ''
        ${pkgs.lib.getExe pkgs.figlet} 'Dev Shell' | ${pkgs.lib.getExe pkgs.lolcat}
        echo 'press ${prefix}-<TAB><TAB> to see all the commands'
      '';

      changeShellPrompt = ''
        export PS1+="${prefix}> "
      '';
    }
  );
in
pkgs.symlinkJoin rec {
  name = prefix;
  passthru.set = commands;
  passthru.bin = pkgs.lib.mapAttrs (
    name: command:
    pkgs.runCommand "${prefix}-${name}" { } ''
      mkdir -p $out/bin
      ln -sf ${command} $out/bin/${if name == "default" then prefix else prefix + "-" + name}
    ''
  ) commands;
  paths = pkgs.lib.attrValues passthru.bin;
}

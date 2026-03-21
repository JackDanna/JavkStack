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

      fetch-all-nuget-deps-jsons = ''
        nix build -f Shared/default.nix fetch-deps
        ./result Shared/deps.json
        nix build -f SideEffect/default.nix fetch-deps
        ./result SideEffect/deps.json
        nix build -f WebClient/default.nix fetch-deps
        ./result WebClient/deps.json
        nix build -f Server/default.nix server.fetch-deps
        ./result Server/deps.json
        rm result
      ''

      exportEnv = ''
        REPO=$(${pkgs.lib.getExe pkgs.git} rev-parse --show-toplevel)
        cd "$REPO/CloudInfrastructure"
        if [ -z "$PULUMI_CONFIG_PASSPHRASE" ]; then
          read -rsp "Pulumi passphrase: " PULUMI_CONFIG_PASSPHRASE
          export PULUMI_CONFIG_PASSPHRASE
          echo ""
        fi
        echo "Exporting Pulumi stack outputs to .env.prod..."
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output --shell --show-secrets \
          > "$REPO/CloudInfrastructure/.env.prod"
        echo "Written to CloudInfrastructure/.env.prod"
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

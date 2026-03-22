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
      '';

      deploy = ''
        set -e
        REPO=$(${pkgs.lib.getExe pkgs.git} rev-parse --show-toplevel)
        VERSION=$(cat "$REPO/version")

        if [ -z "$PULUMI_CONFIG_PASSPHRASE" ]; then
          read -rsp "Pulumi passphrase: " PULUMI_CONFIG_PASSPHRASE
          export PULUMI_CONFIG_PASSPHRASE
          echo ""
        fi

        cd "$REPO/Infrastructure"

        echo "==> Phase 1: provisioning infrastructure..."
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} up --yes --skip-preview

        ACR_SERVER=$(${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output acrLoginServer)
        APP_IMAGE_NAME=$(${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output containerImageTag)

        echo "==> Building container with Nix (version: $VERSION)..."
        cd "$REPO"
        nix build -f Server/default.nix container

        echo "==> Logging in to ACR $ACR_SERVER..."
        ${pkgs.lib.getExe pkgs.azure-cli} acr login --name "$ACR_SERVER"

        echo "==> Loading and pushing image as $ACR_SERVER/$APP_IMAGE_NAME:$VERSION..."
        ${pkgs.lib.getExe pkgs.docker} load < result
        ${pkgs.lib.getExe pkgs.docker} tag "$APP_IMAGE_NAME:latest" "$ACR_SERVER/$APP_IMAGE_NAME:$VERSION"
        ${pkgs.lib.getExe pkgs.docker} push "$ACR_SERVER/$APP_IMAGE_NAME:$VERSION"
        rm -f result

        echo "==> Phase 2: switching Container App to real image (tag: $VERSION)..."
        cd "$REPO/Infrastructure"
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} config set appImageTag "$VERSION"
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} up --yes --skip-preview

        echo "==> Deploy complete!"
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output containerAppUrl
      '';

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

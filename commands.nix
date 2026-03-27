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
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} up --stack prod --yes --skip-preview

        ACR_SERVER=$(${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output --stack prod acrLoginServer)
        APP_IMAGE_NAME=$(${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output --stack prod appImageName)

        echo "==> Building container with Nix (version: $VERSION)..."
        cd "$REPO"
        nix build -f Server/default.nix container

        echo "==> Fetching ACR token..."
        ACR_TOKEN=$(${pkgs.lib.getExe pkgs.azure-cli} acr login --name "$ACR_SERVER" --expose-token --output tsv --query accessToken)

        echo "==> Pushing image as $ACR_SERVER/$APP_IMAGE_NAME:$VERSION (via crane)..."
        ${pkgs.lib.getExe pkgs.crane} auth login "$ACR_SERVER" \
          -u "00000000-0000-0000-0000-000000000000" \
          -p "$ACR_TOKEN"
        TMPTAR=$(mktemp /tmp/container-XXXXXX.tar)
        ${pkgs.lib.getExe pkgs.gzip} -dc "$(readlink -f result)" > "$TMPTAR"
        ${pkgs.lib.getExe pkgs.crane} push "$TMPTAR" "$ACR_SERVER/$APP_IMAGE_NAME:$VERSION"
        rm -f "$TMPTAR" result

        echo "==> Phase 2: switching Container App to real image (tag: $VERSION)..."
        cd "$REPO/Infrastructure"
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} config set --stack prod azure-native:appImageTag "$VERSION"
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} up --stack prod --yes --skip-preview

        echo "==> Deploy complete!"
        ${pkgs.lib.getExe' pkgs.pulumi-bin "pulumi"} stack output --stack prod containerAppUrl
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

      runFrontendDebugProd = ''
        cd $(${self.repoDir})/WebClient/
        ${pkgs.lib.getExe pkgs.fable} watch -o output -s --run npx vite --mode prod
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

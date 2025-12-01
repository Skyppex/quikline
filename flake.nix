{
  description = "Flake for developing Quikline";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = {
    nixpkgs,
    flake-utils,
    ...
  }:
    flake-utils.lib.eachDefaultSystem (system: let
      pkgs = import nixpkgs {inherit system;};

      dotnet = pkgs.buildEnv {
        name = "combined-dotnet-sdks";
        paths = [
          (with pkgs.dotnetCorePackages; combinePackages [sdk_9_0])
        ];
      };
    in {
      devShells.default = pkgs.mkShell {
        packages = with pkgs; [
          dotnet
          csharp-ls
          csharpier
          netcoredbg
          alejandra
          nil
        ];
      };
    });
}

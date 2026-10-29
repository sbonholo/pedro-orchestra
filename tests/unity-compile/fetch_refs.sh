#!/bin/sh
# Downloads Unity engine reference assemblies (NuGet) used to compile-check the scripts without Unity.
set -e; cd "$(dirname "$0")"; mkdir -p pkgs/um
curl -sSL -o pkgs/um.nupkg https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg
cd pkgs/um && unzip -qo ../um.nupkg

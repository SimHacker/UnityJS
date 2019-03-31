#!/bin/sh
echo `pwd`/install.sh
DSTDIR="../../build/Packager/Assets/Libraries/UnityJS/Plugins/Windows"
rm -rf "$DSTDIR"
mkdir -p "$DSTDIR"
echo "TODO: build Windows plugin"
echo "Installed Windows plugin in $DSTDIR"
ls -l "$DSTDIR"
echo "========"

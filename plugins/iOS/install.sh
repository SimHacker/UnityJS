#!/bin/sh
echo `pwd`/install.sh
DSTDIR="../../build/Packager/Assets/Libraries/UnityJS/Plugins/iOS"
rm -rf "$DSTDIR"
mkdir -p "$DSTDIR"
cp ../OSX/Sources/UnityJS.mm "$DSTDIR"
cp -r Editor "$DSTDIR"
echo "Installed iOS plugin in $DSTDIR"
ls -l "$DSTDIR"
echo "========"

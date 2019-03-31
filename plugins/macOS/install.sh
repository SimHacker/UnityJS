#!/bin/sh
echo `pwd`/install.sh
DSTDIR="../../build/Packager/Assets/Libraries/UnityJS/Plugins/macOS"
#rm -rf "$DSTDIR" DerivedData
#mkdir -p "DerivedData"
#xcodebuild -scheme UnityJS -configuration Release -arch x86_64 build CONFIGURATION_BUILD_DIR='DerivedData'
xcodebuild -scheme UnityJS -configuration Release -arch x86_64 build
rm -rf "$DSTDIR"
mkdir -p "$DSTDIR"
echo DerivedData
ls -l DerivedData
cp -r DerivedData/UnityJS.bundle "$DSTDIR"
#rm -rf DerivedData
cp UnityJS.bundle.meta "$DSTDIR"
echo "Installed macOS plugin in $DSTDIR"
ls -l "$DSTDIR"
echo "========"

#!/bin/sh
echo `pwd`/install.sh

# OS specific support.  $var _must_ be set to either true or false.
cygwin=false
case "`uname`" in
CYGWIN*) cygwin=true;;
esac

# For Cygwin, ensure paths are in UNIX format
if $cygwin; then
	[ -n "$ANT_HOME" ] && ANT_HOME=`cygpath --unix "$ANT_HOME"`
fi

if $cygwin; then
	UNITYLIBS=`find -L "/cygdrive/c/Program Files/Unity5" | grep classes.jar | tail -1`
else
    # FIXME: this gets the last listed jar file of four:
	# find -L /Applications/Unity | grep classes.jar | tail -1`
    # /Applications/Unity/PlaybackEngines/AndroidPlayer/Variations/mono/Release/Classes/classes.jar
    # others include: il2cpp/mono, build: Development/Release
    # Should be sure to select the right one somehow.
	UNITYLIBS=`find -L /Applications/Unity/PlaybackEngines/AndroidPlayer/Variations/mono/Release | grep classes.jar | tail -1`
fi

DSTDIR="../../build/Packager/Assets/Libraries/UnityJS/Plugins/Android"
rm -rf "$DSTDIR"
mkdir -p "$DSTDIR"
cp -a unityjs/build/outputs/aar/unityjs-release.aar "$DSTDIR/UnityJS.aar"
cp -a UnityJS.aar.meta "$DSTDIR"
echo "Installed Android plugin in $DSTDIR"
ls -l "$DSTDIR"
echo "========"


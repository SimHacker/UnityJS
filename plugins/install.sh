#!/bin/sh
echo "========"
./install-libs.sh
(cd Android ; ./install.sh)
(cd iOS ; ./install.sh)
(cd macOS ; ./install.sh)
(cd WebGL ; ./install.sh)
(cd Windows ; ./install.sh)
echo "========"

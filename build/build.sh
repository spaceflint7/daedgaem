#! /bin/sh

shopt -s nocasematch

# make sure we are in the project root directory
if [ ! -f Directory.Build.Props ]; then
    echo please run this script from the project root using ./build/build.sh
    exit 1
fi

# get the last-built configuration
read currentConfiguration < .obj/intermediate/Configuration.current
case "$1" in
    clean*)
        shift
        currentConfiguration="(none)"
        ;;
esac

# parse the option for the requested configuration
case "$1" in
    debug*)
        export Configuration=Debug
        ;;
    release*)
        export Configuration=Release
        ;;
    android*)
        export Configuration=Android
        ;;
    *)
        echo usage: $0 "[ clean ] debug | release | android [ install ]"
        exit 1
        ;;
esac

# pass -r to MSBuild if the "clean" option was specified,
# or if the specified configuration is different from the last-built configuration
restoreOption=
if [ "$currentConfiguration" != "$Configuration" ]; then
    rm -rf .obj
    mkdir -p .obj/intermediate
    echo $Configuration > .obj/intermediate/Configuration.current
    restoreOption=-r
fi

# we expect that MSBuild can be found in its default install location
alias msbuild='/c/Program\ Files\ \(x86\)/Microsoft\ Visual\ Studio/2019/Community/MSBuild/Current/Bin/MSBuild.exe -clp:DisableConsoleColor'

# build the (only) project in the project root, which should be just
# a wrapper that includes the main project from the 'build' directory.
# note also that the project file name sets the output file name.
msbuild $restoreOption
if [ "$?" == "0" ]; then

    # install the APK if the "install" option was specified
    case "$2" in
        install*)
            echo "***** INSTALLING AND RUNNING APK"
            "$ANDROID_HOME/platform-tools/adb" install -r .obj/game.apk
            "$ANDROID_HOME/platform-tools/adb" logcat -c
            "$ANDROID_HOME/platform-tools/adb" shell am start -n com.spaceflint.daedgaem/com.spaceflint.Activity
            "$ANDROID_HOME/platform-tools/adb" logcat
            ;;
    esac
fi

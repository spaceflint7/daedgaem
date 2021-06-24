# Daed Gaem

The app emulates an PC XT computer with DOS, to the extent needed to run five specific DOS games from 1984.  It includes touchscreen support tailored for each game.

This app serves as a demo for [Bluebonnet](https://github.com/spaceflint7/bluebonnet).  **Bluebonnet** is a light-weight .NET platform for Android Java which translates compiled .NET assemblies into Java classes.  This app is written in C# but does not have to package the .NET runtime with it.  The size of the built APK is under half a megabyte.

(Please note:  The five DOS games are not distributed with this app, and have to be downloaded on first play.  All rights to these fives games belong to their respective owners.)

## Building

- Download [Bluebonnet binaries](https://github.com/spaceflint7/bluebonnet/releases/download/v0.12/Bluebonnet-0.12.zip) from [release 0.12](https://github.com/spaceflint7/bluebonnet/releases/tag/v0.12).

- Extract and set the ``BLUEBONNET_DIR`` environment variable accordingly.

- In a ``bash`` command prompt, change to the project root for this app.

- Type ``./build/build.sh clean debug`` to build a Windows version which includes a debugger.

## Additional links:

- [Play store link](https://play.google.com/store/apps/details?id=com.spaceflint.daedgaem&hl=en&gl=US)

- [Bluebonnet GitHub repository](https://github.com/spaceflint7/bluebonnet)

- [Bluebonnet home page](https://www.spaceflint.com/bluebonnet)

- [Home page for Daed Gaem](https://www.spaceflint.com/?p=236)

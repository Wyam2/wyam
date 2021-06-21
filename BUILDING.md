# How to build

## Using the build script

Wyam uses [Cake](http://cakebuild.net/) to handle scripted build activities. Right now, Wyam2 is Windows-only (both build and execution). If you just want to build Wyam2 and all the extensions, run

```
build.cmd
``` 

If you want to build and run tests, run

```
build.cmd -target Run-Unit-Tests
```

You can also clean the build by running

```
build.cmd -target Clean
```

or run directly the Powershell script `build.ps1`:
```
.\build.ps1 -Target Run-Unit-Tests
.\build.ps1 -Verbosity diagnostic
```

## From Visual Studio

If you want to open and build Wyam2 from Visual Studio, the main solution is in the root folder as `Wyam.sln`.

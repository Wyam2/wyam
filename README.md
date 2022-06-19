![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/Wyam2/wyam?display_name=tag&sort=semver&style=plastic) [![Join the chat at https://gitter.im/Wyam2/Wyam2](https://badges.gitter.im/Wyam2/Wyam2.svg)](https://gitter.im/Wyam2/Wyam2) 

![Nuget](https://img.shields.io/nuget/dt/Wyam2?label=NuGet%20downloads&style=plastic) ![Chocolatey](https://img.shields.io/chocolatey/dt/Wyam2?label=Chocolatey%20downloads&style=plastic) ![GitHub release (latest by SemVer)](https://img.shields.io/github/downloads/Wyam2/wyam/latest/total?label=GitHub%20downloads%40latest&sort=semver)  
 
[![Wyam2 build](https://github.com/Wyam2/wyam/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/Wyam2/wyam/actions/workflows/build.yml)  [![Wyam2 nightly build](https://github.com/Wyam2/wyam/actions/workflows/nightly.yml/badge.svg?branch=main)](https://github.com/Wyam2/wyam/actions/workflows/nightly.yml) ![GitHub commit activity](https://img.shields.io/github/commit-activity/m/Wyam2/wyam?style=plastic)  

A continuation of the awesome project [Wyam](https://github.com/Wyamio/Wyam) created by [Dave Glick](https://github.com/daveaglick) since it's the only open-source, cross-platform .NET documentation project that I know of, except [docFX](https://github.com/dotnet/docfx). 
For Windows-only there is also [Sandcastle](https://github.com/EWSoftware/SHFB).
---


# Wyam2

Wyam2 is a simple to use, highly modular, and extremely configurable static content generator that can be used to generate web sites, produce documentation, create ebooks, and much more. Since everything is configured by chaining together flexible modules (that you can even write yourself), the only limits to what it can create are your imagination.

The easiest way to get started is to install as a .NET Core global tool and use a [recipe and theme](https://wyam2.github.io/recipes).

1. Download and install Wyam as a global tool:

    `dotnet tool install -g Wyam2.Tool`

2. Scaffold a new blog:

    `wyam2 new --recipe Blog`

3. Edit the scaffolded files.

4. Build the blog with a theme:

    `wyam2 --recipe Blog --theme CleanBlog`

To go deeper, read more about the [underlying concepts](https://wyam2.github.io/docs/concepts) and then read about [configuration files](https://wyam2.github.io/docs/usage/configuration) and the [available command line arguments](https://wyam2.github.io/docs/usage/command-line). Then check out the full list of [modules](https://wyam2.github.io/modules).

For more information see [Wyam2](https://wyam2.github.io).  
For questions and everything else
- please open a [new discussion on GitHub](https://github.com/Wyam2/wyam/discussions) 
- or [chat on Gitter](https://gitter.im/Wyam2/community)
- or [on Matrix](https://matrix.to/#/#Wyam2_community:gitter.im)


## Contributing
For more details about how you can help this project, see [this](CONTRIBUTING.md).

For details on building this project, see [this](BUILDING.md).


## History
- up to v2.2.9 
    - builds are provided by original project, [Wyam](https://github.com/Wyamio/Wyam)
    - dotnet tool is `Wyam.Tool` and `wyam`
- from v3.0.0
    - builds are provided by this fork [Wyam2](https://github.com/Wyam2/Wyam)
    - dotnet tool is called `Wyam2.Tool` and `wyam2`


## Acknowledgements

* Original project [Wyam](https://github.com/Wyamio/Wyam) under MIT license.
* Portions of the IO support were originally inspired from [Cake](http://cakebuild.net) under MIT license.
* The RSS/Atom support was originally ported from [WebFeeds](https://github.com/mckamey/web-feeds.net) under MIT license.
* Themes:
    - [Phantom](https://html5up.net/phantom) under CreativeCommons license.
    - [SolidState](https://html5up.net/solid-state) under CreativeCommons license.
    - [Stellar](https://html5up.net/stellar) under CreativeCommons license.
    - [Trophy](https://github.com/thomasvaeth/trophy-jekyll) under MIT license.
    - [Boostrap](https://getbootstrap.com/) under MIT license.
    - [AdminLTE](https://adminlte.io/) under MIT license.
* Many other fantastic OSS libraries are used directly as NuGet packages, thanks to all the OSS authors out there!

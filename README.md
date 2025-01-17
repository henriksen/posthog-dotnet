# PostHog DotNet Client SDK

This repository contains a set of packages for interacting with the PostHog API in .NET applications. 
This README is for those who wish to contribute to these packages.

For documentation on the specific packages, see the README files in the respective package directories.

## Packages

- [PostHog.AspNetCore](src/PostHog.AspNetCore/README.md)
- [PostHog](src/PostHog/README.md)

> [!WARNING]  
> These packages are currently in a pre-release stage. We're making them available publicly to solicit 
> feedback. While we always strive to maintain a high level of quality, use these packages at your own 
> risk. There *will* be many breaking changes until we reach a stable release.

## Platform

These packages currently target `net9.0`. Our goal is to port the [PortHog](./src/PostHog/README.md) package to `netstandard2.1` at some point once we have a sample that requires it (for example, a Unity sample).

## Building

To build the solution, run the following commands in the root of the repository:

```bash
$ dotnet restore
$ dotnet build
```

## Samples

Sample projects are located in the `samples` directory.

To run the samples, you'll need to set your PostHog project API key. From the repository root you can run:

```bash
bin/user-secrets set PostHog:ProjectApiKey YOUR_API_KEY
```

The main ASP.NET Core sample app can be run with the following command:

```bash
$ bin/start
```

You can also run it from your favorite IDE or editor.

## Testing

To run the tests, run the following command in the root of the repository:

```bash
$ dotnet test
```

## PUBLISHING RELEASES

When it's time to cut a release, increment the version element at the top of [`Directory.Build.props`](Directory.Build.props) according to the [Semantic Versioning](http://semver.org/) guidelines.

```xml
<Project>
    <PropertyGroup>
        <Version>0.0.1</Version>
        ...
    </PropertyGroup>
</Project>
```

Submit a pull request with the version change. Once the PR is merged, create a new tag for the release with the updated version number.

```bash
git tag v0.5.5
git push --tags
```

Now you can go to GitHub to [Draft a new Release](https://github.com/Posthog/posthog-dotnet/releases/new) and click the button to "Auto-generate release notes". Edit the notes accordingly create the Release.

When you create the Release, the [`main.yml`](../.github/.workflow.release.yml) workflow builds and publishes the package to NuGet.

> ![IMPORTANT]
> When creating a release, it's important to create and publish it in one go. If you save a draft of the release and
> then later publish it, the workflow will not run. If you find yourself in that position, you can [manually trigger the workflow run](https://github.com/PostHog/posthog-dotnet/actions/workflows/main.yaml)
> and select the tag to publish.

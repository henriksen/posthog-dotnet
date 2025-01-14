# PostHog DotNet Client SDK

This repository contains a set of packages for interacting with the PostHog API in .NET applications. 
This README is for those who wish to contribute to these packages.

For documentation on the specific packages, see the README files in the respective package directories.

## Packages

- [PostHog.AspNetCore](src/PostHog.AspNetCore/README.md)
- [PostHog](src/PostHog/README.md)

## Building

To build the solution, run the following commands in the root of the repository:

```bash
$ dotnet restore
$ dotnet build
```

## Samples

Sample projects are located in the `samples` directory.

The main ASP.NET Core sample app can be run with the following command:

```bash
$ script/server
```

You can also run it from your favorite IDE or editor.

## Testing

To run the tests, run the following command in the root of the repository:

```bash
$ dotnet test
```
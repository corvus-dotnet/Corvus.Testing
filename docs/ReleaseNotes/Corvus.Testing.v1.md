# Release notes for Corvus.Testing v1.

## v1.5

New features:

### Enable use on MacOS X and Linux

To enable this, we have needed to add `netcoreapp3.1` as a target platform. This enables us to use
the support for killing process trees. The `netstandard2.0` version continues to use the
`System.Management` library to find child processes when shutting down the Functions host, meaning
that it can only work on Windows.
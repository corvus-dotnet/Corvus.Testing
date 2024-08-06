# Release notes for Corvus.Testing v3.

## v3.0

The main changes are:

* tests target .NET 8.0 (except for the sample in-process function because as of 2024/01/16, the support for in-process on .NET 8.0 has not yet been made available)
* we now test both in-process and isolated functions

There are also breaking changes:

* `Corvus.Testing.ReqnRoll.NUnit` no longer references Moq; projects that were relying on this package to supply that dependency will now need to add their own reference if they want to continue using Moq
* if a process is already listening on the port you want the hosted function to use, we now throw an exception instead of ploughing on
* The `CopyToEnvironmentVariables` extension method for `FunctionConfiguration` now takes an enumerable of `KeyValuePair<string, string?>` - the value type is now a nullable string for reasons described in https://github.com/corvus-dotnet/Corvus.Testing/issues/368

The reason for the change in behaviour when the port is in use is that the old behaviour often caused baffling test results. It was very easy to hit this case accidentally if you were debugging test, and then stopped debugging—terminating the debug session typically meant that the code that would have torn down the hosted test function never got a chance to run.

It's possible some people were relying on the old behaviour to enable one style of debugging: it meant you could launch the function in a debugger and then run the tests. With the old behaviour, the tests would then run against the already-started instance of your function. This will no longer work. However it's still possible to debug functions under test. The way to do this is to set a breakpoint somewhere in your test code that will execute after your function has been started but before you've attempted to use it. (E.g., on the first line of the first step of the test you're debugging.) You can then attach the debugger to the functions host. (In Visual Studio, you can use the Debug menu's Attach to Process command, usually bound to the Ctrl+Alt+P keyboard shortcut.) You need to find the functions host in the process list. (It offers a search textbox, so you can just type `func` into that, and it'll narrow it down.) You will now have a multi-process debug session, enabling you to step through code in both the test process and the function under test.

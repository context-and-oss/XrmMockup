# Implementing a new request

[Documentation](README.md)

> This migrated contribution guide does not yet explain all request lifecycle/security hooks. See [R10](documentation-review.md#r10-request-handler-contribution-guide) before treating it as a complete implementation checklist. Local validation commands are in [AGENTS.md](../AGENTS.md#build-and-validation).

## "Execute for the given request has not been implemented yet"
If you received this exception, then it means you are in need of a feature that is not part of XrmMockup - yet! In order for XrmMockup to recognize the new request you have to do 2 things, create a new RequestHandler and add that handler to the list of handlers in `Core.cs`.

### Creating a new RequestHandler
In [src/XrmMockup365/Requests](../src/XrmMockup365/Requests), handlers derive from [RequestHandler](../src/XrmMockup365/Requests/RequestHandler.cs). Create a new one by copying an existing one.

Clear the `Execute` method and any helper methods there might be in the RequestHandler you copied. Change the name of the class to *request name*RequestHandler. Next change the name of the constructor to match the class name and change the string that is passed to the base class to be *request name*.

Now the RequestHandler is setup and you can implement the logic it should execute by filling out the execute method. Get inspiration of how to use the `Core`, `XrmDb`, `MetadataSkeleton` and `Security` by looking at the other RequestHandlers.

### Using the RequestHandler in Core
Open [src/XrmMockup365/Core.cs](../src/XrmMockup365/Core.cs). Find the definition of the function `GetRequestHandlers` and add your new RequestHandler to the list - copy the way the other RequestHandlers are added. For example, the existing registration is:

```csharp
new CalculateRollupFieldRequestHandler(this, db, metadata, security),
```

The current engine targets `net462`, `net48`, and `net10.0`, not separate CRM 2011/2013 builds. Keep handlers compatible with all three targets and preserve `DATAVERSE_SERVICE_CLIENT` guards where SDK APIs differ.

### Ensuring it works and making it public
To make sure you did everything correctly you should create a series of tests that ensures your logic is correct. Once all your tests work you make a pull request, which we will review. Thanks for being awesome and helping maintain XrmMockup!

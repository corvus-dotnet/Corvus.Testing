Feature: NoContainer
    In cases where my tests don't need a DI container
    As a developer
    I don't want a container to be set up unless I asked for it

Scenario: Container not present
    When I call ContainerBindings.GetServiceProvider
    Then it should throw an InvalidOperationException

# These tests are a bit meta, because they test that the library enables us to write ReqnRoll specs in the way we want,
# and they do so through the medium of ReqnRoll specs. In some cases this is straightforward, but it makes certain
# aspects of the test look a little odd.

# This tag is essentially the thing we're testing. Tests using per-scenario containers will put this tag at the top of
# their files.
@perScenarioContainer

# This tag required for these tests to work. It enables before/after bindings that perform work needed to test
# the behaviour of the @perScenarioContainer feature.
@runPerScenarioContainerTests

Feature: PerScenarioContainer
    When I write a test scenario that needs its own DI container
    As a developer
    I want to be able to define container services in a BeforeScenario binding and then use those services later in the test

Background:
    Given I have specified the perScenarioContainer tag

Scenario: Services added during PopulateServiceCollection BeforeScenario phase are in Service Provider available to tests
    Given I use scenario ContainerBindings.GetServiceProvider during a Given step
    When I use scenario ContainerBindings.GetServiceProvider during a When step
    Then if I also use scenario ContainerBindings.GetServiceProvider during a Then step
    Then services added during the PopulateServiceCollection BeforeScenario phase should be available during 'Given' steps
    And services added during the PopulateServiceCollection BeforeScenario phase should be available during 'When' steps
    And services added during the PopulateServiceCollection BeforeScenario phase should be available during 'Then' steps

Scenario: Service Provider is available during ServiceProviderAvailable BeforeScenario phase
    Then during the ServiceProviderAvailable BeforeScenario phase, services added during the PopulateServiceCollection BeforeScenario phase should have been available

# TODO: there's no good end-to-end way to verify here that the container is disposed because the very thing we want to
# test by definition happens after our tests have finished!
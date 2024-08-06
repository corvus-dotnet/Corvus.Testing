# These tests are a bit meta, because they test that the library enables us to write ReqnRoll specs in the way we want,
# and they do so through the medium of ReqnRoll specs. In some cases this is straightforward, but it makes certain
# aspects of the test look a little odd.

# This tag is essentially the thing we're testing. Tests using per-feature containers will put this tag at the top of
# their files.
@perFeatureContainer

# This tag required for these tests to work. It enables before/after bindings that perform work needed to test
# the behaviour of the @perFeatureContainer feature.
@runPerFeatureContainerTests

Feature: PerFeatureContainer
    In cases where my tests need a DI container that persists across all the scenarios in a feature
    As a developer
    I want to be able to define container services in a BeforeFeature binding and then use those services later in the test

Background:
    Given I have specified the perFeatureContainer tag

Scenario: Services added during PopulateServiceCollection BeforeFeature phase are in Service Provider available to tests
    Given I use feature ContainerBindings.GetServiceProvider during a Given step
    When I use feature ContainerBindings.GetServiceProvider during a When step
    Then if I also use feature ContainerBindings.GetServiceProvider during a Then step
    Then services added during the PopulateServiceCollection BeforeFeature phase should be available during 'Given' steps
    And services added during the PopulateServiceCollection BeforeFeature phase should be available during 'When' steps
    And services added during the PopulateServiceCollection BeforeFeature phase should be available during 'Then' steps

Scenario: Service Provider is available during ServiceProviderAvailable BeforeFeature phase
    Then during the ServiceProviderAvailable BeforeFeature phase, services added during the PopulateServiceCollection BeforeFeature phase should have been available

@useServiceProviderBeforeScenarioInPerFeatureContainerTests
Scenario: Service Provider is available during BeforeScenario phase
    Then services added during the PopulateServiceCollection BeforeFeature phase should be available during the earliest BeforeScenario processing
# TODO: there's no good end-to-end way to verify here that the container is disposed because the very thing we want to
# test by definition happens after our tests have finished!
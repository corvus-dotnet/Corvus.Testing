@perFeatureContainer
@useWellKnownTenant

Feature: GetAWellKnownTenant
	In order to avoid creating and destroying too many transient tenants
	As spec writer
	I want to be able to use resources from well-known tenants when running my tests

Scenario: Acquire well known tenants
	When I acquire a well known tenant called "TestTenant"
	Then the tenant called "TestTenant" should not be null

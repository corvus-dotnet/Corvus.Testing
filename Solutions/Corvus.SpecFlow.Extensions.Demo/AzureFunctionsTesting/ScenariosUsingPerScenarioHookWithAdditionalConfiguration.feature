@usingDemoFunctionPerScenarioWithAdditionalConfiguration

Feature: Feature using per-scenario hook with additional configuration
	In order to test my Azure functions
	As a developer
	I want to be able to start an Azure function with specific configuration for each scenario using a hook

Scenario: A Get request including a name in the querystring is successful
	When I send a GET request to 'http://localhost:7075/?name=Jon'
	Then I receive a 200 response code
	And the response body contains the text 'Welcome, Jon'
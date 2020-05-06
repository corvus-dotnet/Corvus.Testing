@usingDemoFunctionPerScenario

Feature: Feature using per-scenario hook
	In order to test my Azure functions
	As a developer
	I want to be able to start an Azure function for each scenario using a hook

Scenario: A Get request including a name in the querystring is successful
	When I send a GET request to 'http://localhost:7075/?name=Jon'
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

Scenario: A Get request without providing a name in the querystring fails.
	When I send a GET request to 'http://localhost:7075/'
	Then I receive a 400 response code

Scenario: A Post request including a name in the querystring is successful
	When I send a POST request to 'http://localhost:7075/?name=Jon'
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

Scenario: A Post request including a name in the request body is successful
	When I send a POST request to 'http://localhost:7075/' with data in the request body
	| PropertyName | Value |
	| name         | Jon   |
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

Scenario: A Post request including names in the querystring and request body uses the name in the querystring
	When I send a POST request to 'http://localhost:7075/?name=Jon' with data in the request body
	| PropertyName | Value    |
	| name         | Jonathan |
	Then I receive a 200 response code
	And the response body contains the text 'Hello, Jon'

Scenario: A Post request without a querystring or request body fails
	When I send a POST request to 'http://localhost:7075/'
	Then I receive a 400 response code
